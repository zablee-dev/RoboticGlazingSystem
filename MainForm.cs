using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using RoboticGlazingSystem.WinForms.Services;
using RoboticGlazingSystem.WinForms.Services.Hardware;

namespace RoboticGlazingSystem.WinForms
{
    /// <summary>
    /// Main Form - UI giống LastDance
    /// </summary>
    public partial class MainForm : Form
    {
        // ========== SERVICES ==========
        private RobotManager _robot;
        private PlcManager _plc;
        private YoloOnnx _yolo;
        
        // ========== CAMERA ==========
        private VideoCapture _capture;
        private Mat _frame;
        private bool _isCameraRunning = false;
        private int _cameraIndex = 0;
        
        // ========== STATE ==========
        private string _selectedTask = "";
        private string _selectedModule = "";
        
        // ========== PERFORMANCE ==========
        private int _frameCounter = 0;
        private const int YOLO_FRAME_SKIP = 15; // Chỉ chạy YOLO mỗi 15 frame (~2 lần/giây) - giảm tải CPU
        private volatile bool _isProcessingYolo = false; // Tránh queue buildup
        
        // ========== SCAN STATE ==========
        private bool _scanCompleted = false; // TRUE khi đã scan xong, giữ giá trị
        private int _savedGlassType = -1; // Giá trị gnGlassType đã lưu
        private bool _lastGbHome = false; // Theo dõi trạng thái gbHome
        private bool _lastI0_0 = false; // Theo dõi nút Start (I0.0)
        private Bitmap? _capturedScanImage = null; // Ảnh đã chụp khi scan
        
        // ========== CYCLE STATE MACHINE ==========
        private enum CycleState { IDLE, WAITING_HOME, WAITING_SCAN, WAITING_GLUE, DONE }
        private CycleState _cycleState = CycleState.IDLE;
        private bool _glueSent = false; // Đã gửi lệnh GLUE chưa
        private bool _doneGlueSent = false; // Sent signal DoneGlue (M1.2) cho Robot A chưa
        private bool _valveActivated = false; // Van 5/2 đã được bật khi robot đến điểm phun đầu tiên

        public MainForm()
        {
            InitializeComponent();
            
            // Enable double buffering to reduce flicker
            this.DoubleBuffered = true;
            
            _robot = new RobotManager();
            _robot.LogEvent += Log;
            
            _plc = new PlcManager();
            _plc.LogEvent += Log;
            
            // SYNC removed - timerUI handles variable refresh
            
            _yolo = new YoloOnnx();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            Log("System ready");
            timerUI.Start();
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera();
            // SYNC removed
            _robot?.Disconnect();
            _plc?.Disconnect();
            _yolo?.Dispose();
        }
        
        // ========== LOGGING ==========
        private void Log(string message)
        {
            // Kiểm tra form và control còn tồn tại không
            if (this.IsDisposed || !this.IsHandleCreated)
                return;
            
            if (rtbLog == null || rtbLog.IsDisposed)
                return;
            
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => Log(message)));
                }
                catch (ObjectDisposedException)
                {
                    // Form đã đóng, bỏ qua
                }
                return;
            }
            
            try
            {
                string ts = DateTime.Now.ToString("HH:mm:ss");
                rtbLog.AppendText($"[{ts}] {message}\n");
                rtbLog.ScrollToCaret();
            }
            catch (ObjectDisposedException)
            {
                // Control đã bị dispose, bỏ qua
            }
        }
        
        // =====================================================
        // ROBOT PANEL
        // =====================================================
        
        private async void btnScanRobots_Click(object sender, EventArgs e)
        {
            btnScanRobots.Enabled = false;
            lvRobots.Items.Clear();
            
            var robots = await _robot.DiscoverRobots();
            
            foreach (var r in robots)
            {
                var item = new ListViewItem(new[] { r.Name, r.IpAddress, r.Status });
                lvRobots.Items.Add(item);
            }
            
            btnScanRobots.Enabled = true;
        }
        
        private void btnConnectRobot_Click(object sender, EventArgs e)
        {
            if (lvRobots.SelectedItems.Count == 0)
            {
                Log("Please select a robot first!");
                return;
            }
            
            string name = lvRobots.SelectedItems[0].Text;
            bool ok = _robot.Connect(name);
            
            if (ok)
            {
                lvRobots.SelectedItems[0].BackColor = Color.LightGreen;
                LoadTasks();
            }
            else
            {
                lvRobots.SelectedItems[0].BackColor = Color.LightCoral;
            }
        }
        
        private void LoadTasks()
        {
            cboTask.Items.Clear();
            var tasks = _robot.GetTasks();
            foreach (var t in tasks)
            {
                cboTask.Items.Add(t);
            }
            if (cboTask.Items.Count > 0)
                cboTask.SelectedIndex = 0;
        }
        
        private void cboTask_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboTask.SelectedItem == null) return;
            _selectedTask = cboTask.SelectedItem.ToString();
            
            cboModule.Items.Clear();
            var modules = _robot.GetModules(_selectedTask);
            foreach (var m in modules)
            {
                cboModule.Items.Add(m);
            }
            if (cboModule.Items.Count > 0)
                cboModule.SelectedIndex = 0;
        }
        
        private void cboModule_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboModule.SelectedItem == null) return;
            _selectedModule = cboModule.SelectedItem.ToString();
            RefreshVariables();
        }
        
        private void RefreshVariables()
        {
            if (string.IsNullOrEmpty(_selectedTask) || string.IsNullOrEmpty(_selectedModule))
                return;
                
            lvVariables.Items.Clear();
            var vars = _robot.GetVariables(_selectedTask, _selectedModule);
            
            foreach (var v in vars)
            {
                var item = new ListViewItem(new[] { v.Name, v.Type, v.Value });
                
                // Color code TRUE/FALSE
                if (v.Value.ToUpper() == "TRUE")
                    item.BackColor = Color.LightGreen;
                else if (v.Value.ToUpper() == "FALSE")
                    item.BackColor = Color.White;
                    
                lvVariables.Items.Add(item);
            }
        }
        
        private void btnSetTrue_Click(object sender, EventArgs e)
        {
            if (lvVariables.SelectedItems.Count == 0) return;
            string varName = lvVariables.SelectedItems[0].Text;
            string varType = lvVariables.SelectedItems[0].SubItems[1].Text;
            
            if (varType.ToLower() == "bool")
            {
                _robot.WriteVariable(_selectedTask, _selectedModule, varName, "TRUE");
                RefreshVariables();
            }
        }
        
        private void btnSetFalse_Click(object sender, EventArgs e)
        {
            if (lvVariables.SelectedItems.Count == 0) return;
            string varName = lvVariables.SelectedItems[0].Text;
            string varType = lvVariables.SelectedItems[0].SubItems[1].Text;
            
            if (varType.ToLower() == "bool")
            {
                _robot.WriteVariable(_selectedTask, _selectedModule, varName, "FALSE");
                RefreshVariables();
            }
        }
        
        private void btnResetZero_Click(object sender, EventArgs e)
        {
            if (lvVariables.SelectedItems.Count == 0) return;
            string varName = lvVariables.SelectedItems[0].Text;
            string varType = lvVariables.SelectedItems[0].SubItems[1].Text;
            
            if (varType.ToLower() == "num")
            {
                _robot.WriteVariable(_selectedTask, _selectedModule, varName, "0");
                RefreshVariables();
            }
            else if (varType.ToLower() == "bool")
            {
                _robot.WriteVariable(_selectedTask, _selectedModule, varName, "FALSE");
                RefreshVariables();
            }
        }
        
        // =====================================================
        // PLC PANEL
        // =====================================================
        
        private void btnConnectPLC_Click(object sender, EventArgs e)
        {
            string ip = txtPlcIP.Text.Trim();
            bool ok = _plc.Connect(ip);
            
            if (ok)
            {
                btnConnectPLC.BackColor = Color.LightGreen;
                RefreshIO();
            }
            else
            {
                btnConnectPLC.BackColor = Color.LightCoral;
            }
        }
        
        private void RefreshIO()
        {
            lvIO.Items.Clear();
            var ios = _plc.GetIoList();
            
            foreach (var io in ios)
            {
                var item = new ListViewItem(new[] { io.Address, io.Description, io.Value });
                
                if (io.Value == "1")
                    item.BackColor = Color.LightGreen;
                    
                lvIO.Items.Add(item);
            }
        }
        
        private void btnSet1_Click(object sender, EventArgs e)
        {
            if (lvIO.SelectedItems.Count == 0) return;
            string addr = lvIO.SelectedItems[0].Text;
            
            // Parse address and set
            if (addr.Contains("DBX"))
            {
                // Format: DB50.DBX0.0
                var parts = addr.Split('.');
                int db = int.Parse(parts[0].Replace("DB", ""));
                int byteOff = int.Parse(parts[1].Replace("DBX", ""));
                int bit = int.Parse(parts[2]);
                _plc.WriteBitDB(db, byteOff, bit, true);
            }
            else
            {
                _plc.WriteBit(addr, true);
            }
            
            RefreshIO();
        }
        
        private void btnSet0_Click(object sender, EventArgs e)
        {
            if (lvIO.SelectedItems.Count == 0) return;
            string addr = lvIO.SelectedItems[0].Text;
            
            if (addr.Contains("DBX"))
            {
                var parts = addr.Split('.');
                int db = int.Parse(parts[0].Replace("DB", ""));
                int byteOff = int.Parse(parts[1].Replace("DBX", ""));
                int bit = int.Parse(parts[2]);
                _plc.WriteBitDB(db, byteOff, bit, false);
            }
            else
            {
                _plc.WriteBit(addr, false);
            }
            
            RefreshIO();
        }
        
        // =====================================================
        // CONTROL BUTTONS
        // =====================================================
        
        private void btnStartRapid_Click(object sender, EventArgs e)
        {
            if (!_robot.IsConnected)
            {
                Log("Robot not connected!");
                return;
            }
            
            string task = _selectedTask;
            if (string.IsNullOrEmpty(task) && cboTask.Items.Count > 0)
                task = cboTask.Items[0].ToString();
                
            _robot.StartProgram(task);
        }
        
        private void btnStopRapid_Click(object sender, EventArgs e)
        {
            if (!_robot.IsConnected)
            {
                Log("Robot not connected!");
                return;
            }
            
            _robot.StopProgram();
        }
        
        private void btnStartSync_Click(object sender, EventArgs e)
        {
            // Fast sync - tăng tốc độ refresh (không nên dùng thường xuyên)
            timerUI.Interval = 300; // Faster but not too fast
            btnStartSync.BackColor = Color.DarkBlue;
            btnStopSync.BackColor = Color.DarkRed;
            Log("Fast sync enabled (300ms)");
        }
        
        private void btnStopSync_Click(object sender, EventArgs e)
        {
            timerUI.Interval = 500; // Normal refresh - cân bằng tốc độ/hiệu năng
            btnStartSync.BackColor = Color.Blue;
            btnStopSync.BackColor = Color.DarkRed;
            Log("Normal sync (500ms)");
        }
        
        private bool _isAutoRunning = false;
        
        private async void btnAutoRun_Click(object sender, EventArgs e)
        {
            if (_isAutoRunning)
            {
                // STOP AUTO
                _isAutoRunning = false;
                btnAutoRun.Text = "▶ AUTO RUN";
                btnAutoRun.BackColor = Color.Orange;
                
                timerUI.Interval = 500;
                
                // Reset PLC outputs when stopping
                if (_plc.IsConnected)
                {
                    _plc.SetBackGlass(false);
                    _plc.SetSideGlass(false);
                    _plc.SetDoneGlue(false);
                    Log("Reset PLC outputs: Q0.4, Q0.5, Q0.6 = FALSE");
                }
                
                Log("=== AUTO STOPPED ===");
                return;
            }
            
            // START AUTO
            _isAutoRunning = true;
            btnAutoRun.Text = "⏹ STOP AUTO";
            btnAutoRun.BackColor = Color.Red;
            
            Log("=== AUTO RUN STARTING ===");
            Log("");
            
            try
            {
                // ========== STEP 1: Connect PLC ==========
                Log("【1/5】 Checking PLC connection...");
                if (!_plc.IsConnected)
                {
                    Log("       Connecting to PLC...");
                    if (_plc.Connect(txtPlcIP.Text))
                    {
                        Log("       ✅ PLC Connected!");
                        btnConnectPLC.Text = "Disconnect";
                        btnConnectPLC.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        Log("       ❌ PLC Connection FAILED!");
                        ResetAutoRun();
                        return;
                    }
                }
                else
                {
                    Log("       ✅ PLC already connected!");
                }
                
                await Task.Delay(300);
                
                // ========== STEP 2: Connect Robot B ==========
                Log("【2/5】 Checking Operation Robot connection...");
                if (!_robot.IsConnected)
                {
                    Log("       Connecting to Operation Robot...");
                    
                    // Auto scan if no robots
                    if (lvRobots.Items.Count == 0)
                    {
                        Log("       Scanning robots...");
                        var robots = await _robot.DiscoverRobots();
                        foreach (var r in robots)
                        {
                            var item = new ListViewItem(new[] { r.Name, r.IpAddress, "Ready" });
                            lvRobots.Items.Add(item);
                        }
                    }
                    
                    if (lvRobots.Items.Count == 0)
                    {
                        Log("       ❌ No robots found!");
                        ResetAutoRun();
                        return;
                    }
                    
                    if (lvRobots.SelectedItems.Count == 0)
                        lvRobots.Items[0].Selected = true;
                    
                    string robotName = lvRobots.SelectedItems[0].Text;
                    if (_robot.Connect(robotName))
                    {
                        Log($"       ✅ Operation Robot ({robotName}) Connected!");
                        btnConnectRobot.Text = "Disconnect";
                        btnConnectRobot.BackColor = Color.LightGreen;
                        
                        // Load tasks/modules
                        LoadTasksAndModules();
                    }
                    else
                    {
                        Log("       ❌ Operation Robot Connection FAILED!");
                        ResetAutoRun();
                        return;
                    }
                }
                else
                {
                    Log("       ✅ Operation Robot already connected!");
                }
                
                await Task.Delay(300);
                
                // ========== STEP 3: Check/Start RAPID ==========
                Log("【3/5】 Checking RAPID on Operation Robot...");
                
                // Check if RAPID is running
                bool rapidRunning = _robot.IsRapidRunning();
                if (!rapidRunning)
                {
                    Log("       RAPID not running, starting...");
                    
                    string task = _selectedTask;
                    if (string.IsNullOrEmpty(task) && cboTask.Items.Count > 0)
                        task = cboTask.Items[0]?.ToString() ?? "T_ROB1";
                    
                    if (_robot.StartProgram(task))
                    {
                        Log($"       ✅ RAPID started (Task: {task})");
                        btnStartRapid.BackColor = Color.DarkGreen;
                        await Task.Delay(1000); // Wait for RAPID to start
                    }
                    else
                    {
                        Log("       ⚠️ Cannot start RAPID!");
                        Log("       → Did you check if Robot is in AUTO mode?");
                        ResetAutoRun();
                        return;
                    }
                }
                else
                {
                    Log("       ✅ RAPID is running!");
                    btnStartRapid.BackColor = Color.DarkGreen;
                }
                
                await Task.Delay(300);
                
                // ========== STEP 4: Send HOME command ==========
                Log("【4/5】 Sending HOME command to Operation Robot...");
                
                if (_robot.SendCommand("HOME", _selectedTask, _selectedModule))
                {
                    Log("       ✅ HOME command sent!");
                    
                    // Wait for robot to reach home
                    Log("       Waiting for Operation Robot to go Home...");
                    int waitCount = 0;
                    while (waitCount < 30) // Max 15 seconds
                    {
                        await Task.Delay(500);
                        string homeVal = _robot.ReadVariable(_selectedTask, _selectedModule, "gbHome");
                        if (homeVal?.ToUpper() == "TRUE")
                        {
                            Log("       ✅ Operation Robot is at Home!");
                            break;
                        }
                        waitCount++;
                        if (waitCount % 4 == 0)
                            Log($"       ...đang chờ ({waitCount/2}s)");
                    }
                    
                    if (waitCount >= 30)
                    {
                        Log("       ⚠️ Operation Robot did not reach Home (timeout)");
                    }
                }
                else
                {
                    Log("       ⚠️ Cannot send HOME command!");
                }
                
                await Task.Delay(300);
                
                // ========== STEP 5: Ready ==========
                Log("【5/5】 Starting Fast Sync...");
                timerUI.Interval = 200;
                btnStartSync.BackColor = Color.DarkBlue;
                
                // Reset cycle state
                _cycleState = CycleState.IDLE;
                _scanCompleted = false;
                _savedGlassType = -1;
                _glueSent = false;
                _doneGlueSent = false;
                
                Log("");
                Log("╔══════════════════════════════════════╗");
                Log("║     ✅ SYSTEM READY!            ║");
                Log("║                                      ║");
                Log("║  Operation Robot: Waiting for command              ║");
                Log("║  PLC: Connected                   ║");
                Log("║                                      ║");
                Log("║  👉 PRESS START BUTTON (I0.0) TO BEGIN ║");
                Log("╚══════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                Log($"❌ Auto Run Error: {ex.Message}");
                ResetAutoRun();
            }
        }
        
        private void ResetAutoRun()
        {
            _isAutoRunning = false;
            btnAutoRun.Text = "▶ AUTO RUN";
            btnAutoRun.BackColor = Color.Orange;
            timerUI.Interval = 500;
            _doneGlueSent = false;
        }
        
        private void LoadTasksAndModules()
        {
            try
            {
                cboTask.Items.Clear();
                var tasks = _robot.GetTasks();
                foreach (var t in tasks)
                    cboTask.Items.Add(t);
                
                if (cboTask.Items.Count > 0)
                {
                    cboTask.SelectedIndex = 0;
                    _selectedTask = cboTask.Items[0]?.ToString();
                }
            }
            catch { }
        }
        
        // =====================================================
        // CAMERA
        // =====================================================
        
        private void lblCameraStatus_Click(object sender, EventArgs e)
        {
            if (_isCameraRunning)
            {
                StopCamera();
                lblCameraStatus.Text = "Stopped";
                lblCameraStatus.ForeColor = Color.Red;
            }
            else
            {
                StartCamera();
                lblCameraStatus.Text = "Running";
                lblCameraStatus.ForeColor = Color.Green;
            }
        }
        
        private void btnScanCamera_Click(object sender, EventArgs e)
        {
            ScanCameras();
        }
        
        private void ScanCameras()
        {
            Log("Scanning cameras...");
            cboCamera.Items.Clear();
            
            // Try up to 5 camera indices
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using (var cap = new VideoCapture(i))
                    {
                        Thread.Sleep(200); // Wait for init
                        if (cap != null && cap.IsOpened())
                        {
                            cboCamera.Items.Add($"Camera {i}");
                            Log($"  Found: Camera {i}");
                        }
                    }
                }
                catch { }
            }
            
            if (cboCamera.Items.Count > 0)
            {
                cboCamera.SelectedIndex = 0;
                Log($"Found {cboCamera.Items.Count} camera(s).");
            }
            else
            {
                Log("No cameras found!");
                lblCameraStatus.Text = "No Camera";
                lblCameraStatus.ForeColor = Color.Red;
            }
        }
        
        private void cboCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCamera.SelectedItem == null) return;
            
            string selected = cboCamera.SelectedItem.ToString() ?? "";
            // Extract camera index from "Camera X"
            if (int.TryParse(selected.Replace("Camera ", ""), out int camIdx))
            {
                StopCamera();
                _cameraIndex = camIdx;
                Log($"Switching to Camera {camIdx}...");
                StartCameraWithIndex(camIdx);
            }
        }
        
        private void StartCameraWithIndex(int index)
        {
            try
            {
                _capture = new VideoCapture(index);
                Thread.Sleep(500);
                
                if (_capture != null && _capture.IsOpened())
                {
                    _frame?.Dispose();
                    _frame = new Mat();
                    _isCameraRunning = true;
                    Task.Run(CameraLoop);
                    Log($"Camera {index} started!");
                    lblCameraStatus.Text = $"Cam {index}";
                    lblCameraStatus.ForeColor = Color.Green;
                }
                else
                {
                    Log($"Failed to open Camera {index}");
                    lblCameraStatus.Text = "Error";
                    lblCameraStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                Log($"Camera error: {ex.Message}");
            }
        }
        
        private void btnLoadModel_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "ONNX Model|*.onnx";
                ofd.Title = "Chọn file model YOLO (.onnx)";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _yolo.LoadModel(ofd.FileName);
                        Log($"Loaded model: {Path.GetFileName(ofd.FileName)}");
                        btnLoadModel.BackColor = Color.LimeGreen;
                    }
                    catch (Exception ex)
                    {
                        Log($"Error loading model: {ex.Message}");
                        btnLoadModel.BackColor = Color.LightCoral;
                    }
                }
            }
        }
        
        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                ofd.Title = "Chọn ảnh để detect";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Stop camera if running
                        if (_isCameraRunning)
                        {
                            StopCamera();
                            lblCameraStatus.Text = "Image";
                            lblCameraStatus.ForeColor = Color.Orange;
                        }
                        
                        // Load and display image
                        using (var img = Cv2.ImRead(ofd.FileName))
                        {
                            if (img.Empty())
                            {
                                Log("Cannot load image!");
                                return;
                            }
                            
                            _frame?.Dispose();
                            _frame = img.Clone();
                            
                            using (var display = img.Clone())
                            {
                                // Run YOLO detection if model loaded
                                if (_yolo != null && _yolo.IsModelLoaded)
                                {
                                    var result = _yolo.Infer(img);
                                    foreach (var det in result.Detections)
                                    {
                                        // Vẽ bounding box
                                        Cv2.Rectangle(display,
                                            new OpenCvSharp.Point(det.X, det.Y),
                                            new OpenCvSharp.Point(det.X + det.Width, det.Y + det.Height),
                                            Scalar.LimeGreen, 3);
                                        
                                        // Text hiển thị: ClassName + Confidence %
                                        string label = $"{det.ClassName}: {det.Confidence * 100:F1}%";
                                        
                                        // Tính kích thước text
                                        int baseline;
                                        var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 1.0, 2, out baseline);
                                        
                                        // Vẽ nền đen cho text (dễ đọc hơn)
                                        int textX = (int)det.X;
                                        int textY = (int)det.Y - 10;
                                        if (textY < textSize.Height) textY = (int)det.Y + textSize.Height + 10;
                                        
                                        Cv2.Rectangle(display,
                                            new OpenCvSharp.Point(textX, textY - textSize.Height - 5),
                                            new OpenCvSharp.Point(textX + textSize.Width + 10, textY + 5),
                                            Scalar.Black, -1); // -1 = filled
                                        
                                        // Vẽ text trắng/xanh lên nền đen
                                        Cv2.PutText(display, label,
                                            new OpenCvSharp.Point(textX + 5, textY),
                                            HersheyFonts.HersheySimplex, 1.0, Scalar.LimeGreen, 2);
                                        
                                        // Log chi tiết
                                        Log($"{det.ClassName}: {det.Confidence * 100:F1}% at ({det.X},{det.Y})");
                                    }
                                    Log($"=== Detected {result.Detections.Count} objects ===");
                                    
                                    // ====== GHI gnGlassType LÊN ROBOT (cho test thủ công) ======
                                    if (result.Detections.Count > 0 && _robot != null && _robot.IsConnected)
                                    {
                                        var best = result.Detections.OrderByDescending(d => d.Confidence).First();
                                        int glassType = -1;
                                        
                                        if (best.ClassName.Contains("sau") || best.ClassName.Contains("rear") || best.ClassName.Contains("lon"))
                                            glassType = 0;
                                        else if (best.ClassName.Contains("hong") || best.ClassName.Contains("side") || best.ClassName.Contains("nho"))
                                            glassType = 1;
                                        else
                                            glassType = 0; // Default
                                        
                                        Log($"Attempting to write gnGlassType = {glassType} (manual)...");
                                        _robot.RequestWriteAccess();
                                        bool writeOk = _robot.WriteVariable(_selectedTask, _selectedModule, "gnGlassType", glassType.ToString());
                                        if (writeOk)
                                        {
                                            Log($"SUCCESS: gnGlassType = {glassType} (manual load)");
                                            _savedGlassType = glassType;
                                        }
                                        else
                                        {
                                            Log("FAILED to write gnGlassType (manual)");
                                        }
                                    }
                                }
                                
                                var bmp = BitmapConverter.ToBitmap(display);
                                var old = picCamera.Image;
                                picCamera.Image = bmp;
                                old?.Dispose();
                            }
                        }
                        
                        Log($"Loaded: {Path.GetFileName(ofd.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error loading image: {ex.Message}");
                    }
                }
            }
        }
        
        private void StartCamera()
        {
            try
            {
                // Dùng camera index đã chọn (từ dropdown cboCamera)
                int targetIndex = _cameraIndex;
                Log($"Đang mở camera {targetIndex}...");
                
                _capture = new VideoCapture(targetIndex);
                Thread.Sleep(500); // Wait for camera to initialize
                
                if (_capture != null && _capture.IsOpened())
                {
                    _frame?.Dispose();
                    _frame = new Mat();
                    _isCameraRunning = true;
                    Task.Run(CameraLoop);
                    Log($"Camera {targetIndex} đã bật!");
                    lblCameraStatus.Text = $"Cam {targetIndex}";
                    lblCameraStatus.ForeColor = Color.Green;
                    return;
                }
                
                _capture?.Release();
                _capture?.Dispose();
                _capture = null;
                
                Log($"Không thể mở camera {targetIndex}! Hãy chọn camera khác.");
                lblCameraStatus.Text = "No Camera";
                lblCameraStatus.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                Log($"Camera error: {ex.Message}");
                lblCameraStatus.Text = "Error";
                lblCameraStatus.ForeColor = Color.Red;
            }
        }
        
        private void StopCamera()
        {
            _isCameraRunning = false;
            Thread.Sleep(200);
            
            try
            {
                _capture?.Release();
                _capture?.Dispose();
            }
            catch { }
            _capture = null;
            
            try { _frame?.Dispose(); } catch { }
            _frame = null;
            
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => {
                        if (picCamera.Image != null)
                        {
                            picCamera.Image.Dispose();
                            picCamera.Image = null;
                        }
                    }));
                }
                else
                {
                    if (picCamera.Image != null)
                    {
                        picCamera.Image.Dispose();
                        picCamera.Image = null;
                    }
                }
            }
            catch { }
        }
        
        private async Task CameraLoop()
        {
            int errorCount = 0;
            
            while (_isCameraRunning)
            {
                try
                {
                    if (_capture == null || !_capture.IsOpened()) 
                    { 
                        await Task.Delay(100); 
                        errorCount++;
                        if (errorCount > 30) // 3 seconds
                        {
                            Log("Camera disconnected.");
                            _isCameraRunning = false;
                            break;
                        }
                        continue; 
                    }
                    
                    errorCount = 0;
                    
                    using (var frame = new Mat())
                    {
                        bool readOk = _capture.Read(frame);
                        if (!readOk || frame == null || frame.Empty()) 
                        { 
                            await Task.Delay(50); 
                            continue; 
                        }
                        
                        _frameCounter++;
                        
                        // Chỉ cập nhật _frame khi cần (cho auto-scan)
                        if (_frameCounter % YOLO_FRAME_SKIP == 0)
                        {
                            _frame?.Dispose();
                            _frame = frame.Clone();
                        }
                        
                        // Hiển thị frame (không clone - dùng trực tiếp)
                        Bitmap? bmp = null;
                        
                        // Chỉ chạy YOLO mỗi N frame VÀ không đang xử lý
                        bool shouldRunYolo = _yolo != null && _yolo.IsModelLoaded 
                            && (_frameCounter % YOLO_FRAME_SKIP == 0) 
                            && !_isProcessingYolo;
                        
                        if (shouldRunYolo)
                        {
                            _isProcessingYolo = true;
                            try
                            {
                                var result = _yolo.Infer(frame);
                                foreach (var det in result.Detections)
                                {
                                    Cv2.Rectangle(frame,
                                        new OpenCvSharp.Point(det.X, det.Y),
                                        new OpenCvSharp.Point(det.X + det.Width, det.Y + det.Height),
                                        Scalar.LimeGreen, 3);
                                    
                                    string label = $"{det.ClassName}: {det.Confidence * 100:F1}%";
                                    int baseline;
                                    var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.8, 2, out baseline);
                                    
                                    int textX = (int)det.X;
                                    int textY = (int)det.Y - 5;
                                    if (textY < textSize.Height) textY = (int)det.Y + textSize.Height + 5;
                                    
                                    Cv2.Rectangle(frame,
                                        new OpenCvSharp.Point(textX, textY - textSize.Height - 3),
                                        new OpenCvSharp.Point(textX + textSize.Width + 6, textY + 3),
                                        Scalar.Black, -1);
                                    
                                    Cv2.PutText(frame, label,
                                        new OpenCvSharp.Point(textX + 3, textY),
                                        HersheyFonts.HersheySimplex, 0.8, Scalar.LimeGreen, 2);
                                }
                            }
                            finally
                            {
                                _isProcessingYolo = false;
                            }
                        }
                        
                        bmp = BitmapConverter.ToBitmap(frame);
                        try
                        {
                            picCamera.BeginInvoke(new Action(() =>
                            {
                                if (!_isCameraRunning) { bmp?.Dispose(); return; }
                                var old = picCamera.Image;
                                picCamera.Image = bmp;
                                old?.Dispose();
                            }));
                        }
                        catch { bmp?.Dispose(); }
                    }
                }
                catch { }
                
                await Task.Delay(50); // ~20 FPS - tiết kiệm CPU, vẫn đủ mượt
            }
        }
        
        // =====================================================
        // TIMER - Auto refresh Variables and I/O
        // =====================================================
        
        private bool _lastDaScan = false;
        
        private int _timerTickCounter = 0;
        
        private bool _isTimerBusy = false;  // Prevent overlapping timer events
        
        private void timerUI_Tick(object sender, EventArgs e)
        {
            // Skip if previous tick still running (prevent overlapping)
            if (_isTimerBusy) return;
            _isTimerBusy = true;
            
            try
            {
                _timerTickCounter++;
                
                // Refresh I/O mỗi 4 ticks (giảm tải đáng kể)
                if (_timerTickCounter % 4 == 0 && _plc != null && _plc.IsConnected)
                {
                    try { RefreshIOWithColors(); } catch { }
                }
                
                // Refresh Variables mỗi 6 ticks (giảm tải network calls)
                if (_timerTickCounter % 6 == 0)
                {
                    if (_robot != null && _robot.IsConnected && !string.IsNullOrEmpty(_selectedModule))
                    {
                        try { RefreshVariablesWithColors(); } catch { }
                    }
                }
                
                // Check scan mỗi 3 ticks
                if (_timerTickCounter % 3 == 0 && _robot != null && _robot.IsConnected && !string.IsNullOrEmpty(_selectedModule))
                {
                    try { CheckAndProcessScan(); } catch { }
                }
                
                // Check nút Start I0.0 mỗi 3 ticks
                if (_timerTickCounter % 3 == 0 && _plc != null && _plc.IsConnected && _robot != null && _robot.IsConnected)
                {
                    try { CheckStartButton(); } catch { }
                }

                // Reset counter to prevent overflow
                if (_timerTickCounter > 1000) _timerTickCounter = 0;
            }
            finally
            {
                _isTimerBusy = false;
            }
        }
        
        private void CheckAndProcessScan()
        {
            try
            {
                // Read daScan and gbHome variables
                string daScanValue = _robot.ReadVariable(_selectedTask, _selectedModule, "daScan");
                bool daScan = daScanValue?.ToUpper() == "TRUE";
                
                string gbHomeValue = _robot.ReadVariable(_selectedTask, _selectedModule, "gbHome");
                bool gbHome = gbHomeValue?.ToUpper() == "TRUE";
                
                // ====== RESET KHI ROBOT B VỀ HOME ======
                // Detect rising edge của gbHome (FALSE -> TRUE)
                // CHỈ reset khi:
                // 1. Đã hoàn thành gửi M1.2 (_doneGlueSent = true), HOẶC
                // 2. Chưa bắt đầu GLUE (!_glueSent)
                bool canReset = _doneGlueSent || !_glueSent;
                
                if (gbHome && !_lastGbHome && _scanCompleted && canReset)
                {
                    Log("=== ROBOT B ĐANG Ở HOME - RESET TRẠNG THÁI ===");
                    _scanCompleted = false;
                    _savedGlassType = -1;
                    
                    // Xóa ảnh đã chụp khi hết chu trình
                    if (_capturedScanImage != null)
                    {
                        _capturedScanImage.Dispose();
                        _capturedScanImage = null;
                        this.BeginInvoke(new Action(() => {
                            var old = picCamera.Image;
                            picCamera.Image = null;
                            old?.Dispose();
                            lblCameraStatus.Text = "Ready";
                            lblCameraStatus.ForeColor = Color.Gray;
                        }));
                        Log("Deleted old cycle image.");
                    }
                    
                    Log("Scan state reset. Ready for new cycle.");
                }
                else if (gbHome && !_lastGbHome && _scanCompleted && !canReset)
                {
                    // Robot B về Home nhưng chưa gửi M1.2 - KHÔNG reset, chờ gbGlueDone
                    Log("Robot B về Home, chờ gbGlueDone để gửi M1.2...");
                }
                _lastGbHome = gbHome;
                
                // ====== BẬT CAMERA KHI ĐẾN VỊ TRÍ SCAN ======
                if (daScan && !_lastDaScan && !_scanCompleted)
                {
                    // Robot đã đến vị trí scan - bật camera nếu not running
                    if (!_isCameraRunning)
                    {
                        Log("Robot đến vị trí scan - Đang bật camera...");
                        StartCamera();
                        this.BeginInvoke(new Action(() => {
                            lblCameraStatus.Text = "Scanning...";
                            lblCameraStatus.ForeColor = Color.Yellow;
                        }));
                        // Chờ camera khởi động
                        Thread.Sleep(500);
                    }
                }
                
                // ====== Xử LÝ SCAN ======
                // Detect daScan = TRUE và camera đang chạy
                if (daScan && !_scanCompleted && _isCameraRunning && _frame != null && !_frame.Empty())
                {
                    Log("=== AUTO SCAN TRIGGERED ===");
                    
                    // Run YOLO detection với RETRY (tối đa 10 lần, mỗi lần cách 500ms)
                    int glassType = -1; // -1 = not detected
                    int maxRetry = 10;
                    
                    if (_yolo != null)
                    {
                        for (int attempt = 1; attempt <= maxRetry; attempt++)
                        {
                            // Chờ frame mới
                            Thread.Sleep(500);
                            
                            // Chụp frame mới
                            if (_frame == null || _frame.Empty()) continue;
                            
                            var result = _yolo.Infer(_frame);
                            if (result.Detections != null && result.Detections.Count > 0)
                            {
                                // Find best detection
                                var best = result.Detections.OrderByDescending(d => d.Confidence).First();
                                Log($"[Attempt {attempt}] Detected: {best.ClassName} ({best.Confidence * 100:F1}%)");
                                
                                // Map class name to type
                                // kinh_sau = Rear = 0, kinh_hong = Side = 1
                                if (best.ClassName.Contains("sau") || best.ClassName.Contains("rear") || best.ClassName.Contains("lon"))
                                {
                                    glassType = 0; // Rear/Large
                                    Log("-> gnGlassType = 0 (Rear Glass/Large)");
                                }
                                else if (best.ClassName.Contains("hong") || best.ClassName.Contains("side") || best.ClassName.Contains("nho"))
                                {
                                    glassType = 1; // Side/Small
                                    Log("-> gnGlassType = 1 (Side Glass/Small)");
                                }
                                else
                                {
                                    // Default to Rear if detected but unknown class
                                    glassType = 0;
                                    Log($"-> gnGlassType = 0 (Unknown class, default Rear)");
                                }
                                break; // Thoát vòng lặp khi detect được
                            }
                            else
                            {
                                Log($"[Attempt {attempt}/{maxRetry}] No object detected, retrying...");
                            }
                        }
                        
                        if (glassType == -1)
                        {
                            Log($"*** FAILED: Không detect được sau {maxRetry} lần thử! ***");
                        }
                    }
                    else
                    {
                        Log("YOLO model not loaded!");
                        glassType = -1;
                    }
                    
                    // Write gnGlassType back to Robot B
                    Log($"Attempting to write gnGlassType = {glassType}...");
                    
                    // Try to get write access first
                    _robot.RequestWriteAccess();
                    
                    bool writeOk = _robot.WriteVariable(_selectedTask, _selectedModule, "gnGlassType", glassType.ToString());
                    if (writeOk)
                    {
                        Log($"SUCCESS: gnGlassType = {glassType} written!");
                        
                        // ====== GỬI LỆNH CHO ROBOT A QUA PLC ======
                        Log($"DEBUG: PLC IsConnected = {_plc.IsConnected}, glassType = {glassType}");
                        
                        if (_plc.IsConnected && glassType >= 0)
                        {
                            // Gọi trên UI thread để giống khi nhấn nút bằng tay
                            this.Invoke(new Action(() => {
                                // Reset cả 2 trước
                                _plc.SetBackGlass(false);
                                _plc.SetSideGlass(false);
                            }));
                            Thread.Sleep(100);
                            
                            // Set tín hiệu - GIỮ NGUYÊN cho đến khi chu kỳ hoàn thành
                            // M1.1/M1.0 sẽ được reset khi Robot A done (I0.7 = TRUE)
                            this.Invoke(new Action(() => {
                                if (glassType == 0)
                                {
                                    _plc.SetBackGlass(true);
                                    Log("M1.1 = TRUE (Notify Handling Robot: Fetch REAR GLASS) - Hold until done");
                                }
                                else if (glassType == 1)
                                {
                                    _plc.SetSideGlass(true);
                                    Log("M1.0 = TRUE (Notify Handling Robot: Fetch SIDE GLASS) - Hold until done");
                                }
                            }));
                            
                            // KHÔNG reset ngay, M1.0/M1.1 sẽ được reset khi chu kỳ hoàn thành
                        }
                        else
                        {
                            Log($"*** SKIP PLC: IsConnected={_plc.IsConnected}, glassType={glassType} ***");
                        }
                        
                        // ====== CHỤP ẢNH VỚI BOUNDING BOX VÀ TẮT CAMERA ======
                        _scanCompleted = true;
                        _savedGlassType = glassType;
                        
                        // Tạo ảnh với bounding box để lưu và hiển thị
                        using (var displayFrame = _frame.Clone())
                        {
                            var result2 = _yolo.Infer(displayFrame);
                            float bestConf = 0;
                            foreach (var det in result2.Detections)
                            {
                                if (det.Confidence > bestConf) bestConf = det.Confidence;
                                Cv2.Rectangle(displayFrame,
                                    new OpenCvSharp.Point(det.X, det.Y),
                                    new OpenCvSharp.Point(det.X + det.Width, det.Y + det.Height),
                                    Scalar.LimeGreen, 3);
                                
                                string label = $"{det.ClassName}: {det.Confidence * 100:F1}%";
                                int baseline;
                                var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.8, 2, out baseline);
                                int textX = (int)det.X;
                                int textY = (int)det.Y - 5;
                                if (textY < textSize.Height) textY = (int)det.Y + textSize.Height + 5;
                                
                                Cv2.Rectangle(displayFrame,
                                    new OpenCvSharp.Point(textX, textY - textSize.Height - 3),
                                    new OpenCvSharp.Point(textX + textSize.Width + 6, textY + 3),
                                    Scalar.Black, -1);
                                Cv2.PutText(displayFrame, label,
                                    new OpenCvSharp.Point(textX + 3, textY),
                                    HersheyFonts.HersheySimplex, 0.8, Scalar.LimeGreen, 2);
                            }
                            
                            // Lưu ảnh đã chụp
                            _capturedScanImage?.Dispose();
                            _capturedScanImage = BitmapConverter.ToBitmap(displayFrame);
                            
                            // Log độ chính xác
                            Log($"★★★ CONFIDENCE: {bestConf * 100:F1}% ★★★");
                        }
                        
                        Log("Scan completed. Captured image and turned off camera.");
                        
                        // Tắt camera sau khi scan xong
                        StopCamera();
                        
                        // Hiển thị ảnh đã chụp lên màn hình
                        this.BeginInvoke(new Action(() => {
                            var old = picCamera.Image;
                            picCamera.Image = _capturedScanImage != null ? (Bitmap)_capturedScanImage.Clone() : null;
                            old?.Dispose();
                            lblCameraStatus.Text = "Captured!";
                            lblCameraStatus.ForeColor = Color.Lime;
                        }));
                    }
                    else
                    {
                        // Retry once more
                        Log("First write failed, retrying...");
                        Thread.Sleep(200);
                        writeOk = _robot.WriteVariable(_selectedTask, _selectedModule, "gnGlassType", glassType.ToString());
                        if (writeOk)
                        {
                            Log($"SUCCESS on retry: gnGlassType = {glassType}");
                            
                            // ====== GỬI LỆNH CHO ROBOT A QUA PLC ======
                            if (_plc.IsConnected && glassType >= 0)
                            {
                                _plc.SetBackGlass(false);
                                _plc.SetSideGlass(false);
                                
                                // Set tín hiệu - GIỮ NGUYÊN cho đến khi chu kỳ hoàn thành
                                if (glassType == 0)
                                {
                                    _plc.SetBackGlass(true);
                                    Log("M1.1 = TRUE (Notify Handling Robot: Fetch REAR GLASS) - Hold until done");
                                }
                                else if (glassType == 1)
                                {
                                    _plc.SetSideGlass(true);
                                    Log("M1.0 = TRUE (Notify Handling Robot: Fetch SIDE GLASS) - Hold until done");
                                }
                            }
                            
                            // ====== CHỤP ẢNH VỚI BOUNDING BOX VÀ TẮT CAMERA (retry) ======
                            _scanCompleted = true;
                            _savedGlassType = glassType;
                            
                            // Tạo ảnh với bounding box
                            using (var displayFrame = _frame.Clone())
                            {
                                var result2 = _yolo.Infer(displayFrame);
                                float bestConf = 0;
                                foreach (var det in result2.Detections)
                                {
                                    if (det.Confidence > bestConf) bestConf = det.Confidence;
                                    Cv2.Rectangle(displayFrame,
                                        new OpenCvSharp.Point(det.X, det.Y),
                                        new OpenCvSharp.Point(det.X + det.Width, det.Y + det.Height),
                                        Scalar.LimeGreen, 3);
                                    
                                    string label = $"{det.ClassName}: {det.Confidence * 100:F1}%";
                                    int baseline;
                                    var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.8, 2, out baseline);
                                    int textX = (int)det.X;
                                    int textY = (int)det.Y - 5;
                                    if (textY < textSize.Height) textY = (int)det.Y + textSize.Height + 5;
                                    
                                    Cv2.Rectangle(displayFrame,
                                        new OpenCvSharp.Point(textX, textY - textSize.Height - 3),
                                        new OpenCvSharp.Point(textX + textSize.Width + 6, textY + 3),
                                        Scalar.Black, -1);
                                    Cv2.PutText(displayFrame, label,
                                        new OpenCvSharp.Point(textX + 3, textY),
                                        HersheyFonts.HersheySimplex, 0.8, Scalar.LimeGreen, 2);
                                }
                                
                                _capturedScanImage?.Dispose();
                                _capturedScanImage = BitmapConverter.ToBitmap(displayFrame);
                                Log($"★★★ CONFIDENCE: {bestConf * 100:F1}% ★★★");
                            }
                            
                            Log("Scan completed. Captured image and turned off camera.");
                            
                            StopCamera();
                            this.BeginInvoke(new Action(() => {
                                var old = picCamera.Image;
                                picCamera.Image = _capturedScanImage != null ? (Bitmap)_capturedScanImage.Clone() : null;
                                old?.Dispose();
                                lblCameraStatus.Text = "Captured!";
                                lblCameraStatus.ForeColor = Color.Lime;
                            }));
                        }
                        else
                        {
                            Log("FAILED to write gnGlassType! Check robot is in AUTO mode.");
                        }
                    }
                }
                
                _lastDaScan = daScan;
            }
            catch (Exception ex)
            {
                // Ignore errors silently
            }
        }
        
        /// <summary>
        /// State Machine: gbHome=TRUE + I0.0 -> SCAN -> gnGlassType -> GLUE
        /// Nếu gbHome=FALSE + I0.0 -> HOME
        /// Nếu gbBusy=TRUE -> không làm gì
        /// </summary>
        private void CheckStartButton()
        {
            try
            {
                // Đọc trạng thái I0.0
                bool i0_0 = _plc.ReadBit("I0.0");
                
                // Đọc trạng thái từ Robot B
                string gbHomeVal = _robot.ReadVariable(_selectedTask, _selectedModule, "gbHome");
                bool gbHome = gbHomeVal?.ToUpper() == "TRUE";
                
                string gbDoneVal = _robot.ReadVariable(_selectedTask, _selectedModule, "gbDone");
                bool gbDone = gbDoneVal?.ToUpper() == "TRUE";
                
                string gbBusyVal = _robot.ReadVariable(_selectedTask, _selectedModule, "gbBusy");
                bool gbBusy = gbBusyVal?.ToUpper() == "TRUE";
                
                // ========== KIỂM TRA BUSY ==========
                // Nếu robot đang bận thì không thực hiện gì cả
                if (gbBusy)
                {
                    _lastI0_0 = i0_0;
                    return;
                }
                
                // ========== STATE MACHINE ==========
                
                switch (_cycleState)
                {
                    case CycleState.IDLE:
                        // Nhấn I0.0 khi robot KHÔNG ở Home → gửi HOME
                        if (i0_0 && !_lastI0_0 && !gbHome)
                        {
                            Log("=== NÚT START (I0.0) ĐƯỢC NHẤN ===");
                            Log("Robot chưa ở Home, gửi lệnh HOME...");
                            
                            if (_robot.SendCommand("HOME", _selectedTask, _selectedModule))
                            {
                                Log("HOME command sent!");
                            }
                        }
                        // Nhấn I0.0 khi robot ĐÃ ở Home → bắt đầu SCAN
                        else if (i0_0 && !_lastI0_0 && gbHome)
                        {
                            Log("=== NÚT START (I0.0) ĐƯỢC NHẤN ===");
                            Log("Robot đang ở Home, bắt đầu SCAN...");
                            
                            // Bật camera
                            if (!_isCameraRunning)
                            {
                                Log("Starting camera...");
                                StartCamera();
                            }
                            
                            // Gửi lệnh SCAN (dùng task/module đã chọn từ UI)
                            if (_robot.SendCommand("SCAN", _selectedTask, _selectedModule))
                            {
                                Log("SCAN command sent!");
                                _cycleState = CycleState.WAITING_SCAN;
                                _scanCompleted = false;
                                _savedGlassType = -1;
                                _glueSent = false;
                            }
                        }
                        break;
                        
                    case CycleState.WAITING_SCAN:
                        // Chờ scan xong (YOLO detection đã chạy trong CheckAndProcessScan)
                        if (_scanCompleted && _savedGlassType >= 0)
                        {
                            Log($"Scan xong! gnGlassType = {_savedGlassType}");
                            _cycleState = CycleState.WAITING_GLUE;
                        }
                        break;
                        
                    case CycleState.WAITING_GLUE:
                        // Chờ I0.6 (Robot A Glue Position) trước khi gửi GLUE
                        bool robotAAtGluePos = _plc.ReadOpRobotGluePosition();
                        
                        if (!_glueSent && robotAAtGluePos)
                        {
                            Log("I0.6 = TRUE (Robot A reached wait glue position)");
                            
                            // Gửi lệnh GLUE dựa vào gnGlassType
                            if (_savedGlassType == 0)
                            {
                                Log("gnGlassType = 0 -> Sending GLUE_REAR command...");
                                if (_robot.SendCommand("GLUE_REAR", _selectedTask, _selectedModule))
                                {
                                    Log("GLUE_REAR command sent!");
                                    _glueSent = true;
                                    // Van sẽ được bật khi gbAtGluePos = TRUE (robot đến điểm phun đầu tiên)
                                }
                            }
                            else if (_savedGlassType == 1)
                            {
                                Log("gnGlassType = 1 -> Sending GLUE_SIDE command...");
                                if (_robot.SendCommand("GLUE_SIDE", _selectedTask, _selectedModule))
                                {
                                    Log("GLUE_SIDE command sent!");
                                    _glueSent = true;
                                    // Van sẽ được bật khi gbAtGluePos = TRUE (robot đến điểm phun đầu tiên)
                                }
                            }
                        }
                        else if (!_glueSent && !robotAAtGluePos)
                        {
                            // Chờ I0.6
                            // Log("Chờ I0.6 = TRUE (Robot A Glue Position)...");
                        }
                        // Đọc trạng thái gbGlueDone từ Robot B
                        string gbGlueDoneVal = _robot.ReadVariable(_selectedTask, _selectedModule, "gbGlueDone");
                        bool gbGlueDone = gbGlueDoneVal?.ToUpper() == "TRUE";
                        
                        // DEBUG: Log trạng thái để tìm lỗi M1.2
                        if (_glueSent && !_doneGlueSent)
                        {
                            Log($"[DEBUG] gbGlueDone={gbGlueDone} (raw: '{gbGlueDoneVal}'), _glueSent={_glueSent}, _doneGlueSent={_doneGlueSent}");
                        }
                        
                        // Đọc trạng thái gbValveOn từ Robot B - Robot điều khiển ON/OFF van qua biến này
                        string gbValveOnVal = _robot.ReadVariable(_selectedTask, _selectedModule, "gbValveOn");
                        bool gbValveOn = gbValveOnVal?.ToUpper() == "TRUE";
                        
                        // PC điều khiển van theo trạng thái gbValveOn từ Robot B
                        // Dùng this.Invoke() như cách gửi M1.0/M1.1 thành công
                        if (_glueSent)
                        {
                            if (gbValveOn && !_valveActivated)
                            {
                                // Robot báo BẬT van - gọi trên UI thread
                                this.Invoke(new Action(() => {
                                    _plc.SetGlueValve(true);   // M6.6 = TRUE → Q0.7 → BẬT Van 5/2
                                    Log("★ 5/2 Valve ON (gbValveOn = TRUE) - M6.6 = TRUE");
                                }));
                                _valveActivated = true;
                            }
                            else if (!gbValveOn && _valveActivated)
                            {
                                // Robot báo TẮT van - gọi trên UI thread
                                this.Invoke(new Action(() => {
                                    _plc.SetGlueValve(false);  // M6.6 = FALSE → Q0.7 → TẮT Van 5/2
                                    Log("★ 5/2 Valve OFF (gbValveOn = FALSE) - M6.6 = FALSE");
                                }));
                                _valveActivated = false;
                            }
                        }
                        
                        // Khi Robot B phun keo xong → tắt van và báo Robot A
                        // DB1.DBX0.7 = Done Glue (tắt van)
                        if (_glueSent && gbGlueDone && !_doneGlueSent)
                        {
                            Log($"★★★ PHUN KEO XONG - GỬI M1.2 CHO ROBOT A ★★★");
                            
                            // Gọi trên UI thread như cách gửi M1.0/M1.1 thành công
                            this.Invoke(new Action(() => {
                                // TẮT van 5/2
                                _plc.SetGlueValve(false);  // M6.6 = FALSE → Q0.7 → TẮT Van 5/2
                                Log("✅ 5/2 Valve OFF: M6.6 = FALSE → Q0.7");
                            }));
                            Thread.Sleep(100);
                            
                            // Gửi M1.2 báo Robot A đi lắp kính
                            this.Invoke(new Action(() => {
                                _plc.SetDoneGlue(true);  // M1.2 = TRUE (Q0.6 → DI3)
                                Log("✅ M1.2 = TRUE → Robot A đi lắp kính");
                            }));
                            
                            _doneGlueSent = true;
                        }
                        
                        // Đọc I0.7: Robot A đã hoàn thành lắp kính
                        bool robotADone = _plc.ReadOpRobotDone();
                        
                        // ⚠️ CHỈ reset khi Robot A hoàn thành (I0.7 = TRUE)
                        // KHÔNG reset khi Robot B về Home vì Robot A cần thời gian nhận DI3 và lắp kính
                        if (_glueSent && _doneGlueSent && robotADone)
                        {
                            // Reset TẤT CẢ tín hiệu PLC khi Robot A đã xong
                            if (_plc.IsConnected)
                            {
                                _plc.SetBackGlass(false);       // M1.1 = FALSE (Q0.4)
                                _plc.SetSideGlass(false);       // M1.0 = FALSE (Q0.5)
                                _plc.SetDoneGlue(false);        // M1.2 = FALSE (Q0.6)
                                _plc.SetGlueValve(false);  // M10.1 = FALSE → Q0.7
                                Log("✅ Reset PLC: M1.x, M6.6 = FALSE");
                            }
                            
                            // Reset gbGlueDone trên Robot B cho chu kỳ tiếp theo
                            _robot.WriteVariable(_selectedTask, _selectedModule, "gbGlueDone", "FALSE");
                            Log("Reset Robot B: gbGlueDone = FALSE");
                            
                            Log("=== CHU KỲ HOÀN THÀNH ===");
                            
                            // ★★★ GỬI SCAN CHO ROBOT B ĐỂ BẮT ĐẦU CHU KỲ MỚI ★★★
                            Log("I0.7 = TRUE → Robot A hoàn thành → Gửi SCAN cho Robot B...");
                            if (_robot.SendCommand("SCAN", _selectedTask, _selectedModule))
                            {
                                Log("✅ SCAN command sent! Robot B đi scan tiếp.");
                                _cycleState = CycleState.WAITING_SCAN;
                                _scanCompleted = false;
                                _savedGlassType = -1;
                                _glueSent = false;
                                _doneGlueSent = false;
                                _valveActivated = false;
                            }
                            else
                            {
                                Log("⚠️ Không gửi được SCAN, về IDLE");
                                _cycleState = CycleState.IDLE;
                                _scanCompleted = false;
                                _savedGlassType = -1;
                                _glueSent = false;
                                _doneGlueSent = false;
                                _valveActivated = false;
                            }
                        }
                        break;
                        
                    case CycleState.WAITING_HOME:
                    case CycleState.DONE:
                        // Không sử dụng, reset về IDLE
                        _cycleState = CycleState.IDLE;
                        break;
                }
                
                _lastI0_0 = i0_0;
            }
            catch { }
        }
        
        private void RefreshVariablesWithColors()
        {
            if (string.IsNullOrEmpty(_selectedTask) || string.IsNullOrEmpty(_selectedModule))
                return;
            
            try
            {
                var vars = _robot.GetVariables(_selectedTask, _selectedModule);
                
                for (int i = 0; i < lvVariables.Items.Count && i < vars.Count; i++)
                {
                    var v = vars[i];
                    lvVariables.Items[i].SubItems[2].Text = v.Value;
                    
                    if (v.Value.ToUpper() == "TRUE")
                        lvVariables.Items[i].BackColor = Color.LightGreen;
                    else
                        lvVariables.Items[i].BackColor = Color.White;
                }
            }
            catch { }
        }
        
        private void RefreshIOWithColors()
        {
            try
            {
                var ios = _plc.GetIoList();
                
                for (int i = 0; i < lvIO.Items.Count && i < ios.Count; i++)
                {
                    var io = ios[i];
                    lvIO.Items[i].SubItems[2].Text = io.Value;
                    
                    if (io.Value == "1")
                        lvIO.Items[i].BackColor = Color.LightGreen;
                    else
                        lvIO.Items[i].BackColor = Color.White;
                }
            }
            catch { }
        }
        
    }
}
