using System;
using System.Collections.Generic;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;
using RapidTask = ABB.Robotics.Controllers.RapidDomain.Task;

namespace RoboticGlazingSystem.WinForms.Services.Hardware
{
    /// <summary>
    /// Robot Manager - Quản lý giao tiếp với Robot ABB (Robot B) qua PC SDK
    /// 
    /// ╔══════════════════════════════════════════════════════════════════════════════╗
    /// ║                    TỔNG QUAN LUỒNG TRUYỀN THÔNG                              ║
    /// ╠══════════════════════════════════════════════════════════════════════════════╣
    /// ║  [GỬI LỆNH] PC → Robot B (qua biến RAPID)                                    ║
    /// ║  ────────────────────────────────────────────────────────────────────────────║
    /// ║  • GuiLenh("HOME")       : Lệnh về vị trí Home                               ║
    /// ║  • GuiLenh("SCAN")       : Lệnh di chuyển tới vị trí scan camera             ║
    /// ║  • GuiLenh("WAIT_GLUE")  : Lệnh chờ tại vị trí phun keo                      ║
    /// ║  • GuiLenh("GLUE_REAR")  : Lệnh phun keo kính sau                            ║
    /// ║  • GuiLenh("GLUE_SIDE")  : Lệnh phun keo kính hông                           ║
    /// ║  Cơ chế: PC ghi vào biến "recvMsg" → Robot B đọc và thực thi                 ║
    /// ╠══════════════════════════════════════════════════════════════════════════════╣
    /// ║  [NHẬN TRẠNG THÁI] Robot B → PC (qua biến RAPID)                             ║
    /// ║  ────────────────────────────────────────────────────────────────────────────║
    /// ║  • gbBusy     : Robot đang thực thi lệnh                                     ║
    /// ║  • gbDone     : Robot đã hoàn thành lệnh                                     ║
    /// ║  • gbError    : Robot gặp lỗi                                                ║
    /// ║  • gbHome     : Robot đang ở vị trí Home                                     ║
    /// ║  • gbGlueDone : Robot B đã phun keo xong (dùng để báo PLC → Robot A)         ║
    /// ║  • gbValveOn  : Robot B đang bật van phun keo (PC đồng bộ với M6.6)          ║
    /// ║  • daScan     : Robot B đã tới vị trí scan (PC bật camera)                   ║
    /// ║  Cơ chế: PC liên tục đọc các biến này để theo dõi trạng thái Robot B         ║
    /// ╚══════════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public class QuanLyRobot
    {
        private Controller controller;
        private bool daKetNoi = false;
        private Mastership mastership = null;  // Persistent mastership
        
        public event Action<string> ThongBaoLog;
        
        /// <summary>
        /// Discover robots on LAN
        /// </summary>
        public async System.Threading.Tasks.Task<List<ThongTinRobot>> DoTimRobot()
        {
            var danhSach = new List<ThongTinRobot>();
            
            try
            {
                ThongBaoLog?.Invoke("Scanning for robots...");
                
                NetworkScanner scanner = new NetworkScanner();
                await System.Threading.Tasks.Task.Run(() => scanner.Scan());
                
                ControllerInfoCollection controllers = scanner.Controllers;
                
                foreach (ControllerInfo ctrl in controllers)
                {
                    var r = new ThongTinRobot();
                    r.Ten = ctrl.SystemName;
                    r.DiaChiIP = ctrl.IPAddress.ToString();
                    r.TrangThai = ctrl.Availability.ToString();
                    danhSach.Add(r);
                }
                
                ThongBaoLog?.Invoke("Found " + danhSach.Count + " robot(s)");
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("Error: " + ex.Message);
            }
            
            return danhSach;
        }
        
        /// <summary>
        /// Connect to selected robot with persistent mastership
        /// </summary>
        public bool KetNoiRobot(string tenHoacIP)
        {
            try
            {
                ThongBaoLog?.Invoke("Connecting to " + tenHoacIP);
                
                NetworkScanner scanner = new NetworkScanner();
                scanner.Scan();
                
                ControllerInfo ctrlInfo = null;
                
                foreach (ControllerInfo ctrl in scanner.Controllers)
                {
                    if (ctrl.SystemName == tenHoacIP || ctrl.IPAddress.ToString() == tenHoacIP)
                    {
                        ctrlInfo = ctrl;
                        break;
                    }
                }
                
                if (ctrlInfo == null)
                {
                    ThongBaoLog?.Invoke("Robot not found");
                    return false;
                }
                
                controller = Controller.Connect(ctrlInfo, ConnectionType.Standalone);
                controller.Logon(UserInfo.DefaultUser);
                
                daKetNoi = true;
                ThongBaoLog?.Invoke("Connected to " + tenHoacIP);
                
                // Request persistent mastership (user must approve on FlexPendant)
                ThongBaoLog?.Invoke("Requesting write access... (approve on FlexPendant if prompted)");
                try
                {
                    mastership = Mastership.Request(controller);
                    ThongBaoLog?.Invoke("Write access granted - held until disconnect");
                }
                catch (Exception mEx)
                {
                    ThongBaoLog?.Invoke("Warning: Could not get mastership: " + mEx.Message);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("Error: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from robot
        /// </summary>
        public void NgatKetNoi()
        {
            try
            {
                if (mastership != null)
                {
                    mastership.Release();
                    mastership.Dispose();
                    mastership = null;
                }
                
                if (controller != null)
                {
                    controller.Logoff();
                    controller.Dispose();
                    controller = null;
                    daKetNoi = false;
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Get list of RAPID tasks
        /// </summary>
        public List<string> LayDanhSachTask()
        {
            var ds = new List<string>();
            if (!daKetNoi || controller == null) return ds;
            
            try
            {
                RapidTask[] tasks = controller.Rapid.GetTasks();
                foreach (var t in tasks)
                {
                    ds.Add(t.Name);
                }
                ThongBaoLog?.Invoke("Found " + ds.Count + " task(s)");
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("Error: " + ex.Message);
            }
            
            return ds;
        }
        
        /// <summary>
        /// Get list of modules in a task
        /// </summary>
        public List<string> LayDanhSachModule(string tenTask)
        {
            var ds = new List<string>();
            if (!daKetNoi || controller == null) return ds;
            
            try
            {
                RapidTask task = controller.Rapid.GetTask(tenTask);
                if (task == null) return ds;
                
                Module[] modules = task.GetModules();
                foreach (var m in modules)
                {
                    ds.Add(m.Name);
                }
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("Error: " + ex.Message);
            }
            
            return ds;
        }
        
        /// <summary>
        /// Get list of RAPID variables (bool and num) in a module with their values
        /// </summary>
        public List<ThongTinBien> LayDanhSachBien(string tenTask, string tenModule)
        {
            var ds = new List<ThongTinBien>();
            if (daKetNoi == false || controller == null) return ds;
            
            try
            {
                RapidTask task = controller.Rapid.GetTask(tenTask);
                if (task == null) return ds;
                
                Module module = task.GetModule(tenModule);
                if (module == null) return ds;
                
                RapidSymbolSearchProperties searchProp = RapidSymbolSearchProperties.CreateDefault();
                searchProp.Types = SymbolTypes.Data;
                searchProp.LocalSymbols = true;
                searchProp.GlobalSymbols = true;
                
                // Search for bool variables
                RapidSymbol[] boolSymbols = module.SearchRapidSymbol(searchProp, "bool", "");
                foreach (RapidSymbol sym in boolSymbols)
                {
                    string giaTri = ReadVariableValue(task, tenModule, sym.Name);
                    ds.Add(new ThongTinBien(sym.Name, "bool", giaTri));
                }
                
                // Search for num variables
                RapidSymbol[] numSymbols = module.SearchRapidSymbol(searchProp, "num", "");
                foreach (RapidSymbol sym in numSymbols)
                {
                    string giaTri = ReadVariableValue(task, tenModule, sym.Name);
                    ds.Add(new ThongTinBien(sym.Name, "num", giaTri));
                }
                
                // Search for string variables
                RapidSymbol[] strSymbols = module.SearchRapidSymbol(searchProp, "string", "");
                foreach (RapidSymbol sym in strSymbols)
                {
                    string giaTri = ReadVariableValue(task, tenModule, sym.Name);
                    ds.Add(new ThongTinBien(sym.Name, "string", giaTri));
                }
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("Error reading variables: " + ex.Message);
            }
            
            return ds;
        }
        
        private string ReadVariableValue(RapidTask task, string moduleName, string varName)
        {
            try
            {
                RapidData rd = task.GetRapidData(moduleName, varName);
                if (rd != null && rd.Value != null)
                {
                    return rd.Value.ToString() ?? "?";
                }
            }
            catch { }
            return "?";
        }
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║                      PHƯƠNG THỨC ĐỌC/GHI BIẾN RAPID                          ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// [NHẬN] Đọc giá trị biến RAPID từ Robot
        /// Dùng để đọc trạng thái: gbBusy, gbDone, gbError, gbHome, gbGlueDone, daScan...
        /// </summary>
        public string DocBien(string tenTask, string tenModule, string tenBien)
        {
            if (!daKetNoi || controller == null) return "";
            if (string.IsNullOrEmpty(tenTask) || string.IsNullOrEmpty(tenModule) || string.IsNullOrEmpty(tenBien)) return "";
            
            try
            {
                RapidTask task = controller.Rapid.GetTask(tenTask);
                if (task == null) return "";
                
                RapidData rd = task.GetRapidData(tenModule, tenBien);
                if (rd == null) return "";
                
                var value = rd.Value;
                if (value == null) return "";
                
                return value.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>
        /// [GỬI] Ghi giá trị vào biến RAPID của Robot
        /// Dùng để gửi lệnh: recvMsg, gnGlassType...
        /// Yêu cầu quyền Mastership để ghi
        /// </summary>
        public bool GhiBien(string tenTask, string tenModule, string tenBien, string giaTri)
        {
            if (!daKetNoi || controller == null) return false;
            
            try
            {
                RapidTask task = controller.Rapid.GetTask(tenTask);
                if (task == null)
                {
                    ThongBaoLog?.Invoke($"Task '{tenTask}' not found");
                    return false;
                }
                
                RapidData rd = task.GetRapidData(tenModule, tenBien);
                if (rd == null)
                {
                    ThongBaoLog?.Invoke($"Variable '{tenBien}' not found in '{tenModule}'");
                    return false;
                }
                
                // Dùng mastership đang có hoặc request mới
                bool usePersistent = (mastership != null);
                Mastership m = null;
                
                try
                {
                    m = usePersistent ? mastership : Mastership.Request(controller);
                    
                    // Với string, dùng cách đặc biệt
                    if (rd.Value is ABB.Robotics.Controllers.RapidDomain.String)
                    {
                        // Đặt giá trị trực tiếp với quotes cho string
                        rd.StringValue = $"\"{giaTri}\"";
                    }
                    else if (rd.Value is Num)
                    {
                        Num numVal = (Num)rd.Value;
                        numVal.FillFromString2(giaTri);
                        rd.Value = numVal;
                    }
                    else if (rd.Value is Bool)
                    {
                        Bool boolVal = (Bool)rd.Value;
                        boolVal.FillFromString2(giaTri);
                        rd.Value = boolVal;
                    }
                    else
                    {
                        rd.StringValue = giaTri;
                    }
                    
                    return true;
                }
                finally
                {
                    if (!usePersistent && m != null)
                    {
                        m.Release();
                        m.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke($"GhiBien Error [{tenBien}]: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Request persistent mastership
        /// </summary>
        public bool LayQuyenGhi()
        {
            if (!daKetNoi || controller == null) return false;
            
            try
            {
                TraQuyenGhi();
                mastership = Mastership.Request(controller);
                ThongBaoLog?.Invoke("Write access granted");
                return true;
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("Cannot get write access: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Release persistent mastership
        /// </summary>
        public void TraQuyenGhi()
        {
            if (mastership != null)
            {
                try
                {
                    mastership.Release();
                    mastership.Dispose();
                    mastership = null;
                    ThongBaoLog?.Invoke("Write access released");
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Check if RAPID program is running
        /// </summary>
        public bool KiemTraRapidChay()
        {
            if (!daKetNoi || controller == null)
                return false;
            
            try
            {
                return controller.Rapid.ExecutionStatus == ABB.Robotics.Controllers.RapidDomain.ExecutionStatus.Running;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Start RAPID program
        /// </summary>
        public bool KhoiDongChuongTrinh(string tenTask)
        {
            if (!daKetNoi || controller == null)
            {
                ThongBaoLog?.Invoke("Not connected to robot");
                return false;
            }
            
            try
            {
                if (controller.OperatingMode == ControllerOperatingMode.Auto)
                {
                    bool usePersistent = (mastership != null);
                    Mastership m = usePersistent ? mastership : Mastership.Request(controller);
                    
                    try
                    {
                        RapidTask rapidTask = controller.Rapid.GetTask(tenTask);
                        if (rapidTask != null)
                        {
                            rapidTask.ResetProgramPointer();
                        }
                        
                        StartResult result = controller.Rapid.Start(true);
                        ThongBaoLog?.Invoke("RAPID started: " + result.ToString());
                        return true;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ThongBaoLog?.Invoke("Mastership held by another: " + ex.Message);
                        return false;
                    }
                    finally
                    {
                        if (!usePersistent && m != null)
                        {
                            m.Release();
                            m.Dispose();
                        }
                    }
                }
                else
                {
                    ThongBaoLog?.Invoke("Automatic mode required! Current: " + controller.OperatingMode.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("StartExec error: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Stop RAPID program
        /// </summary>
        public bool DungChuongTrinh()
        {
            if (!daKetNoi || controller == null)
            {
                ThongBaoLog?.Invoke("Not connected to robot");
                return false;
            }
            
            try
            {
                if (controller.OperatingMode == ControllerOperatingMode.Auto)
                {
                    bool usePersistent = (mastership != null);
                    Mastership m = usePersistent ? mastership : Mastership.Request(controller);
                    
                    try
                    {
                        controller.Rapid.Stop(StopMode.Immediate);
                        ThongBaoLog?.Invoke("RAPID stopped");
                        return true;
                    }
                    finally
                    {
                        if (!usePersistent && m != null)
                        {
                            m.Release();
                            m.Dispose();
                        }
                    }
                }
                else
                {
                    ThongBaoLog?.Invoke("Automatic mode required!");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                ThongBaoLog?.Invoke("Mastership held by another: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("StopExec error: " + ex.Message);
                return false;
            }
        }
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║          [GỬI LỆNH] COMMAND HELPERS - PC gửi lệnh đến Robot B                ║
        // ║          Cơ chế: PC ghi lệnh vào biến "recvMsg" → Robot B đọc và thực thi    ║
        // ║          Robot B chạy vòng lặp WHILE TRUE đọc recvMsg liên tục               ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        private const string DEFAULT_TASK = "T_ROB1";
        private const string DEFAULT_MODULE = "RobotB_GlueModule";  // Module chứa MainProc của Robot B
        
        /// <summary>
        /// [GỬI] Gửi lệnh đến Robot B qua biến recvMsg
        /// Robot B đọc biến này trong vòng lặp và thực thi lệnh tương ứng
        /// Các lệnh: HOME, SCAN, WAIT_GLUE, GLUE_REAR, GLUE_SIDE
        /// </summary>
        public bool GuiLenh(string lenh, string task = null, string module = null)
        {
            task = task ?? DEFAULT_TASK;
            module = module ?? DEFAULT_MODULE;
            
            ThongBaoLog?.Invoke($"Sending command: {lenh} to {task}/{module}");
            
            // Retry up to 3 times
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    // Lấy quyền ghi trước
                    LayQuyenGhi();
                    
                    bool success = GhiBien(task, module, "recvMsg", lenh);
                    if (success)
                    {
                        ThongBaoLog?.Invoke($"Command '{lenh}' sent successfully");
                        return true;
                    }
                    
                    ThongBaoLog?.Invoke($"Attempt {attempt} failed, retrying...");
                    System.Threading.Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    ThongBaoLog?.Invoke($"Attempt {attempt} exception: {ex.Message}");
                    System.Threading.Thread.Sleep(200);
                }
            }
            
            ThongBaoLog?.Invoke($"Failed to send command '{lenh}' after 3 attempts");
            return false;
        }
        
        // [GỬI] Các lệnh shortcut cho Robot B:
        /// <summary>[GỬI] Lệnh Robot B về vị trí Home</summary>
        public bool VeHome() => GuiLenh("HOME");
        /// <summary>[GỬI] Lệnh Robot B đi tới vị trí Camera Scan</summary>
        public bool DiScan() => GuiLenh("SCAN");
        /// <summary>[GỬI] Lệnh Robot B chờ tại vị trí phun keo</summary>
        public bool ChoPhunKeo() => GuiLenh("WAIT_GLUE");
        /// <summary>[GỬI] Lệnh Robot B thực hiện phun keo KÍNH SAU</summary>
        public bool PhunKeoSau() => GuiLenh("GLUE_REAR");
        /// <summary>[GỬI] Lệnh Robot B thực hiện phun keo KÍNH HÔNG</summary>
        public bool PhunKeoHong() => GuiLenh("GLUE_SIDE");
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║      [NHẬN TRẠNG THÁI] STATUS HELPERS - PC đọc trạng thái từ Robot B         ║
        // ║      Robot B cập nhật các biến gb* trong RAPID, PC đọc liên tục              ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// [NHẬN] Đọc trạng thái Robot B từ các biến RAPID
        /// • gbBusy: Robot đang thực thi lệnh (TRUE khi đang di chuyển/phun keo)
        /// • gbDone: Robot đã hoàn thành lệnh (TRUE sau khi xong, reset khi có lệnh mới)
        /// • gbError: Robot gặp lỗi
        /// • gbHome: Robot đang ở vị trí Home
        /// • gbGlueDone: Robot B đã phun keo xong (PC sẽ set M1.2 để báo Robot A)
        /// </summary>
        public (bool Busy, bool Done, bool Error, bool AtHome, bool GlueDone) DocTrangThai()
        {
            if (!daKetNoi || controller == null)
                return (false, false, false, false, false);
            
            // [NHẬN] Đọc các biến trạng thái từ Robot B
            bool busy = DocBien(DEFAULT_TASK, DEFAULT_MODULE, "gbBusy").ToUpper() == "TRUE";
            bool done = DocBien(DEFAULT_TASK, DEFAULT_MODULE, "gbDone").ToUpper() == "TRUE";
            bool error = DocBien(DEFAULT_TASK, DEFAULT_MODULE, "gbError").ToUpper() == "TRUE";
            bool atHome = DocBien(DEFAULT_TASK, DEFAULT_MODULE, "gbHome").ToUpper() == "TRUE";
            bool glueDone = DocBien(DEFAULT_TASK, DEFAULT_MODULE, "gbGlueDone").ToUpper() == "TRUE";
            
            return (busy, done, error, atHome, glueDone);
        }
        
        /// <summary>
        /// Check if connected
        /// </summary>
        public bool DaKetNoi => daKetNoi && controller != null;
    }
}
