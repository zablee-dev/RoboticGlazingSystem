namespace RoboticGlazingSystem.WinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            
            // ========== ROBOT GROUP ==========
            this.grpRobot = new System.Windows.Forms.GroupBox();
            this.lvRobots = new System.Windows.Forms.ListView();
            this.colRobotName = new System.Windows.Forms.ColumnHeader();
            this.colRobotIP = new System.Windows.Forms.ColumnHeader();
            this.colRobotStatus = new System.Windows.Forms.ColumnHeader();
            this.btnScanRobots = new System.Windows.Forms.Button();
            this.btnConnectRobot = new System.Windows.Forms.Button();
            this.lblTask = new System.Windows.Forms.Label();
            this.cboTask = new System.Windows.Forms.ComboBox();
            this.lblModule = new System.Windows.Forms.Label();
            this.cboModule = new System.Windows.Forms.ComboBox();
            this.lblVariables = new System.Windows.Forms.Label();
            this.lvVariables = new System.Windows.Forms.ListView();
            this.colVarName = new System.Windows.Forms.ColumnHeader();
            this.colVarType = new System.Windows.Forms.ColumnHeader();
            this.colVarValue = new System.Windows.Forms.ColumnHeader();
            this.btnSetTrue = new System.Windows.Forms.Button();
            this.btnSetFalse = new System.Windows.Forms.Button();
            this.btnResetZero = new System.Windows.Forms.Button();
            
            // ========== PLC GROUP ==========
            this.grpPLC = new System.Windows.Forms.GroupBox();
            this.lblIP = new System.Windows.Forms.Label();
            this.txtPlcIP = new System.Windows.Forms.TextBox();
            this.btnConnectPLC = new System.Windows.Forms.Button();
            this.lvIO = new System.Windows.Forms.ListView();
            this.colIOAddress = new System.Windows.Forms.ColumnHeader();
            this.colIODesc = new System.Windows.Forms.ColumnHeader();
            this.colIOValue = new System.Windows.Forms.ColumnHeader();
            this.btnSet1 = new System.Windows.Forms.Button();
            this.btnSet0 = new System.Windows.Forms.Button();
            
            // ========== CONTROL BUTTONS ==========
            this.lblChay = new System.Windows.Forms.Label();
            this.lblDung = new System.Windows.Forms.Label();
            this.btnAutoRun = new System.Windows.Forms.Button();
            this.btnStartRapid = new System.Windows.Forms.Button();
            this.btnStartSync = new System.Windows.Forms.Button();
            this.btnStopRapid = new System.Windows.Forms.Button();
            this.btnStopSync = new System.Windows.Forms.Button();
            
            // ========== CAMERA GROUP ==========
            this.grpCamera = new System.Windows.Forms.GroupBox();
            this.picCamera = new System.Windows.Forms.PictureBox();
            this.lblCameraStatus = new System.Windows.Forms.Label();
            this.cboCamera = new System.Windows.Forms.ComboBox();
            this.btnScanCamera = new System.Windows.Forms.Button();
            this.btnLoadModel = new System.Windows.Forms.Button();
            this.btnLoadImage = new System.Windows.Forms.Button();
            
            // ========== LOG ==========
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            
            // ========== TIMER ==========
            this.timerUI = new System.Windows.Forms.Timer(this.components);

            // Suspend
            ((System.ComponentModel.ISupportInitialize)(this.picCamera)).BeginInit();
            this.SuspendLayout();

            // =====================================================
            // ROW 1: ROBOT (left) | CAMERA (center-right) | CONTROLS (far right)
            // =====================================================
            
            // ROBOT GROUP - Left side, narrower
            this.grpRobot.Text = "ROBOT";
            this.grpRobot.Location = new System.Drawing.Point(8, 8);
            this.grpRobot.Size = new System.Drawing.Size(280, 420);
            this.grpRobot.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.grpRobot.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            
            // ListView Robots - compact
            this.lvRobots.View = System.Windows.Forms.View.Details;
            this.lvRobots.FullRowSelect = true;
            this.lvRobots.GridLines = true;
            this.lvRobots.Location = new System.Drawing.Point(8, 20);
            this.lvRobots.Size = new System.Drawing.Size(264, 70);
            this.lvRobots.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.colRobotName.Text = "Name";
            this.colRobotName.Width = 100;
            this.colRobotIP.Text = "IP";
            this.colRobotIP.Width = 85;
            this.colRobotStatus.Text = "Status";
            this.colRobotStatus.Width = 60;
            this.lvRobots.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colRobotName, this.colRobotIP, this.colRobotStatus});
            
            // Buttons
            this.btnScanRobots.Text = "Scan";
            this.btnScanRobots.Location = new System.Drawing.Point(8, 95);
            this.btnScanRobots.Size = new System.Drawing.Size(65, 26);
            this.btnScanRobots.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnScanRobots.Click += new System.EventHandler(this.btnScanRobots_Click);
            
            this.btnConnectRobot.Text = "Connect";
            this.btnConnectRobot.Location = new System.Drawing.Point(78, 95);
            this.btnConnectRobot.Size = new System.Drawing.Size(70, 26);
            this.btnConnectRobot.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnConnectRobot.Click += new System.EventHandler(this.btnConnectRobot_Click);
            
            // Task/Module dropdowns - inline
            this.lblTask.Text = "Task:";
            this.lblTask.Location = new System.Drawing.Point(8, 128);
            this.lblTask.AutoSize = true;
            this.lblTask.Font = new System.Drawing.Font("Segoe UI", 8F);
            
            this.cboTask.Location = new System.Drawing.Point(45, 125);
            this.cboTask.Size = new System.Drawing.Size(100, 22);
            this.cboTask.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTask.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.cboTask.SelectedIndexChanged += new System.EventHandler(this.cboTask_SelectedIndexChanged);
            
            this.lblModule.Text = "Mod:";
            this.lblModule.Location = new System.Drawing.Point(150, 128);
            this.lblModule.AutoSize = true;
            this.lblModule.Font = new System.Drawing.Font("Segoe UI", 8F);
            
            this.cboModule.Location = new System.Drawing.Point(180, 125);
            this.cboModule.Size = new System.Drawing.Size(92, 22);
            this.cboModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboModule.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.cboModule.SelectedIndexChanged += new System.EventHandler(this.cboModule_SelectedIndexChanged);
            
            // Variables
            this.lblVariables.Text = "Variables:";
            this.lblVariables.Location = new System.Drawing.Point(8, 152);
            this.lblVariables.AutoSize = true;
            this.lblVariables.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            
            this.lvVariables.View = System.Windows.Forms.View.Details;
            this.lvVariables.FullRowSelect = true;
            this.lvVariables.GridLines = true;
            this.lvVariables.Location = new System.Drawing.Point(8, 168);
            this.lvVariables.Size = new System.Drawing.Size(264, 210);
            this.lvVariables.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.lvVariables.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.colVarName.Text = "Name";
            this.colVarName.Width = 120;
            this.colVarType.Text = "Type";
            this.colVarType.Width = 50;
            this.colVarValue.Text = "Value";
            this.colVarValue.Width = 70;
            this.lvVariables.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colVarName, this.colVarType, this.colVarValue});
            
            // TRUE/FALSE/Reset buttons
            this.btnSetTrue.Text = "TRUE";
            this.btnSetTrue.Location = new System.Drawing.Point(8, 385);
            this.btnSetTrue.Size = new System.Drawing.Size(80, 28);
            this.btnSetTrue.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnSetTrue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSetTrue.BackColor = System.Drawing.Color.LightGreen;
            this.btnSetTrue.Click += new System.EventHandler(this.btnSetTrue_Click);
            
            this.btnSetFalse.Text = "FALSE";
            this.btnSetFalse.Location = new System.Drawing.Point(95, 385);
            this.btnSetFalse.Size = new System.Drawing.Size(80, 28);
            this.btnSetFalse.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnSetFalse.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSetFalse.BackColor = System.Drawing.Color.LightCoral;
            this.btnSetFalse.Click += new System.EventHandler(this.btnSetFalse_Click);
            
            this.btnResetZero.Text = "Reset=0";
            this.btnResetZero.Location = new System.Drawing.Point(182, 385);
            this.btnResetZero.Size = new System.Drawing.Size(90, 28);
            this.btnResetZero.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnResetZero.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnResetZero.Click += new System.EventHandler(this.btnResetZero_Click);
            
            this.grpRobot.Controls.Add(this.lvRobots);
            this.grpRobot.Controls.Add(this.btnScanRobots);
            this.grpRobot.Controls.Add(this.btnConnectRobot);
            this.grpRobot.Controls.Add(this.lblTask);
            this.grpRobot.Controls.Add(this.cboTask);
            this.grpRobot.Controls.Add(this.lblModule);
            this.grpRobot.Controls.Add(this.cboModule);
            this.grpRobot.Controls.Add(this.lblVariables);
            this.grpRobot.Controls.Add(this.lvVariables);
            this.grpRobot.Controls.Add(this.btnSetTrue);
            this.grpRobot.Controls.Add(this.btnSetFalse);
            this.grpRobot.Controls.Add(this.btnResetZero);

            // =====================================================
            // CAMERA GROUP - Center, LARGE size
            // =====================================================
            this.grpCamera.Text = "CAMERA";
            this.grpCamera.Location = new System.Drawing.Point(295, 8);
            this.grpCamera.Size = new System.Drawing.Size(520, 420);
            this.grpCamera.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this.grpCamera.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            
            // PictureBox - LARGE
            this.picCamera.Location = new System.Drawing.Point(8, 20);
            this.picCamera.Size = new System.Drawing.Size(504, 355);
            this.picCamera.BackColor = System.Drawing.Color.Black;
            this.picCamera.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picCamera.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            
            // Camera status
            this.lblCameraStatus.Text = "Ready";
            this.lblCameraStatus.Location = new System.Drawing.Point(8, 382);
            this.lblCameraStatus.AutoSize = true;
            this.lblCameraStatus.ForeColor = System.Drawing.Color.Blue;
            this.lblCameraStatus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblCameraStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCameraStatus.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.lblCameraStatus.Click += new System.EventHandler(this.lblCameraStatus_Click);
            
            // Camera controls
            this.cboCamera.Location = new System.Drawing.Point(70, 382);
            this.cboCamera.Size = new System.Drawing.Size(100, 25);
            this.cboCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCamera.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboCamera.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.cboCamera.SelectedIndexChanged += new System.EventHandler(this.cboCamera_SelectedIndexChanged);
            
            this.btnScanCamera.Text = "Scan";
            this.btnScanCamera.Location = new System.Drawing.Point(175, 380);
            this.btnScanCamera.Size = new System.Drawing.Size(60, 30);
            this.btnScanCamera.BackColor = System.Drawing.Color.LightBlue;
            this.btnScanCamera.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnScanCamera.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnScanCamera.Click += new System.EventHandler(this.btnScanCamera_Click);
            
            this.btnLoadModel.Text = "Model";
            this.btnLoadModel.Location = new System.Drawing.Point(240, 380);
            this.btnLoadModel.Size = new System.Drawing.Size(60, 30);
            this.btnLoadModel.BackColor = System.Drawing.Color.LightGreen;
            this.btnLoadModel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnLoadModel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnLoadModel.Click += new System.EventHandler(this.btnLoadModel_Click);
            
            this.btnLoadImage.Text = "Ảnh";
            this.btnLoadImage.Location = new System.Drawing.Point(305, 380);
            this.btnLoadImage.Size = new System.Drawing.Size(60, 30);
            this.btnLoadImage.BackColor = System.Drawing.Color.LightYellow;
            this.btnLoadImage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnLoadImage.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnLoadImage.Click += new System.EventHandler(this.btnLoadImage_Click);
            
            this.grpCamera.Controls.Add(this.picCamera);
            this.grpCamera.Controls.Add(this.lblCameraStatus);
            this.grpCamera.Controls.Add(this.cboCamera);
            this.grpCamera.Controls.Add(this.btnScanCamera);
            this.grpCamera.Controls.Add(this.btnLoadModel);
            this.grpCamera.Controls.Add(this.btnLoadImage);

            // =====================================================
            // RIGHT PANEL: Controls + PLC + Log
            // =====================================================
            
            // Labels
            this.lblChay.Text = "CHẠY: Connect → START SYNC → START RAPID";
            this.lblChay.Location = new System.Drawing.Point(825, 10);
            this.lblChay.AutoSize = true;
            this.lblChay.ForeColor = System.Drawing.Color.Green;
            this.lblChay.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblChay.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            
            this.lblDung.Text = "DỪNG: STOP RAPID → STOP SYNC";
            this.lblDung.Location = new System.Drawing.Point(825, 28);
            this.lblDung.AutoSize = true;
            this.lblDung.ForeColor = System.Drawing.Color.Red;
            this.lblDung.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblDung.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            
            // Control buttons - compact grid
            this.btnStartRapid.Text = "START RAPID";
            this.btnStartRapid.Location = new System.Drawing.Point(825, 48);
            this.btnStartRapid.Size = new System.Drawing.Size(110, 40);
            this.btnStartRapid.BackColor = System.Drawing.Color.LimeGreen;
            this.btnStartRapid.ForeColor = System.Drawing.Color.White;
            this.btnStartRapid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStartRapid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartRapid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnStartRapid.Click += new System.EventHandler(this.btnStartRapid_Click);
            
            this.btnStartSync.Text = "START SYNC";
            this.btnStartSync.Location = new System.Drawing.Point(940, 48);
            this.btnStartSync.Size = new System.Drawing.Size(110, 40);
            this.btnStartSync.BackColor = System.Drawing.Color.Blue;
            this.btnStartSync.ForeColor = System.Drawing.Color.White;
            this.btnStartSync.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStartSync.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartSync.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnStartSync.Click += new System.EventHandler(this.btnStartSync_Click);
            
            this.btnStopRapid.Text = "STOP RAPID";
            this.btnStopRapid.Location = new System.Drawing.Point(825, 92);
            this.btnStopRapid.Size = new System.Drawing.Size(110, 40);
            this.btnStopRapid.BackColor = System.Drawing.Color.Red;
            this.btnStopRapid.ForeColor = System.Drawing.Color.White;
            this.btnStopRapid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStopRapid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopRapid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnStopRapid.Click += new System.EventHandler(this.btnStopRapid_Click);
            
            this.btnStopSync.Text = "STOP SYNC";
            this.btnStopSync.Location = new System.Drawing.Point(940, 92);
            this.btnStopSync.Size = new System.Drawing.Size(110, 40);
            this.btnStopSync.BackColor = System.Drawing.Color.DarkRed;
            this.btnStopSync.ForeColor = System.Drawing.Color.White;
            this.btnStopSync.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStopSync.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopSync.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnStopSync.Click += new System.EventHandler(this.btnStopSync_Click);
            
            // AUTO RUN - Big button
            this.btnAutoRun.Text = "▶ AUTO";
            this.btnAutoRun.Location = new System.Drawing.Point(1055, 48);
            this.btnAutoRun.Size = new System.Drawing.Size(100, 84);
            this.btnAutoRun.BackColor = System.Drawing.Color.Orange;
            this.btnAutoRun.ForeColor = System.Drawing.Color.White;
            this.btnAutoRun.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnAutoRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAutoRun.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnAutoRun.Click += new System.EventHandler(this.btnAutoRun_Click);

            // =====================================================
            // PLC GROUP - Right side, compact
            // =====================================================
            this.grpPLC.Text = "PLC";
            this.grpPLC.Location = new System.Drawing.Point(825, 140);
            this.grpPLC.Size = new System.Drawing.Size(330, 200);
            this.grpPLC.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.grpPLC.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            
            this.lblIP.Text = "IP:";
            this.lblIP.Location = new System.Drawing.Point(8, 22);
            this.lblIP.AutoSize = true;
            this.lblIP.Font = new System.Drawing.Font("Segoe UI", 9F);
            
            this.txtPlcIP.Text = "192.168.0.11";
            this.txtPlcIP.Location = new System.Drawing.Point(30, 19);
            this.txtPlcIP.Size = new System.Drawing.Size(120, 25);
            this.txtPlcIP.Font = new System.Drawing.Font("Segoe UI", 9F);
            
            this.btnConnectPLC.Text = "Connect";
            this.btnConnectPLC.Location = new System.Drawing.Point(155, 18);
            this.btnConnectPLC.Size = new System.Drawing.Size(70, 26);
            this.btnConnectPLC.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnConnectPLC.Click += new System.EventHandler(this.btnConnectPLC_Click);
            
            this.lvIO.View = System.Windows.Forms.View.Details;
            this.lvIO.FullRowSelect = true;
            this.lvIO.GridLines = true;
            this.lvIO.Location = new System.Drawing.Point(8, 48);
            this.lvIO.Size = new System.Drawing.Size(260, 145);
            this.lvIO.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.colIOAddress.Text = "Address";
            this.colIOAddress.Width = 90;
            this.colIODesc.Text = "Description";
            this.colIODesc.Width = 130;
            this.colIOValue.Text = "Val";
            this.colIOValue.Width = 35;
            this.lvIO.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colIOAddress, this.colIODesc, this.colIOValue});
            
            this.btnSet1.Text = "1";
            this.btnSet1.Location = new System.Drawing.Point(275, 48);
            this.btnSet1.Size = new System.Drawing.Size(50, 50);
            this.btnSet1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSet1.BackColor = System.Drawing.Color.LimeGreen;
            this.btnSet1.Click += new System.EventHandler(this.btnSet1_Click);
            
            this.btnSet0.Text = "0";
            this.btnSet0.Location = new System.Drawing.Point(275, 103);
            this.btnSet0.Size = new System.Drawing.Size(50, 50);
            this.btnSet0.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSet0.BackColor = System.Drawing.Color.Salmon;
            this.btnSet0.Click += new System.EventHandler(this.btnSet0_Click);
            
            this.grpPLC.Controls.Add(this.lblIP);
            this.grpPLC.Controls.Add(this.txtPlcIP);
            this.grpPLC.Controls.Add(this.btnConnectPLC);
            this.grpPLC.Controls.Add(this.lvIO);
            this.grpPLC.Controls.Add(this.btnSet1);
            this.grpPLC.Controls.Add(this.btnSet0);

            // =====================================================
            // LOG GROUP - Right side bottom
            // =====================================================
            this.grpLog.Text = "LOG";
            this.grpLog.Location = new System.Drawing.Point(825, 345);
            this.grpLog.Size = new System.Drawing.Size(330, 85);
            this.grpLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this.grpLog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            
            this.rtbLog.Location = new System.Drawing.Point(8, 18);
            this.rtbLog.Size = new System.Drawing.Size(314, 118);
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.BackColor = System.Drawing.Color.Black;
            this.rtbLog.ForeColor = System.Drawing.Color.LimeGreen;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.rtbLog.ReadOnly = true;
            
            this.grpLog.Controls.Add(this.rtbLog);

            // =====================================================
            // TIMER
            // =====================================================
            this.timerUI.Interval = 750; // Tăng từ 500ms → 750ms để giảm tải
            this.timerUI.Tick += new System.EventHandler(this.timerUI_Tick);

            // =====================================================
            // FORM
            // =====================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1165, 435);
            this.Controls.Add(this.grpRobot);
            this.Controls.Add(this.grpCamera);
            this.Controls.Add(this.lblChay);
            this.Controls.Add(this.lblDung);
            this.Controls.Add(this.btnAutoRun);
            this.Controls.Add(this.btnStartRapid);
            this.Controls.Add(this.btnStartSync);
            this.Controls.Add(this.btnStopRapid);
            this.Controls.Add(this.btnStopSync);
            this.Controls.Add(this.grpPLC);
            this.Controls.Add(this.grpLog);
            this.MinimumSize = new System.Drawing.Size(1100, 450);
            this.Name = "MainForm";
            this.Text = "ROBOT - PLC - VISION CONTROL";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            
            // Resume
            ((System.ComponentModel.ISupportInitialize)(this.picCamera)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // ROBOT
        private System.Windows.Forms.GroupBox grpRobot;
        private System.Windows.Forms.ListView lvRobots;
        private System.Windows.Forms.ColumnHeader colRobotName;
        private System.Windows.Forms.ColumnHeader colRobotIP;
        private System.Windows.Forms.ColumnHeader colRobotStatus;
        private System.Windows.Forms.Button btnScanRobots;
        private System.Windows.Forms.Button btnConnectRobot;
        private System.Windows.Forms.Label lblTask;
        private System.Windows.Forms.ComboBox cboTask;
        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.ComboBox cboModule;
        private System.Windows.Forms.Label lblVariables;
        private System.Windows.Forms.ListView lvVariables;
        private System.Windows.Forms.ColumnHeader colVarName;
        private System.Windows.Forms.ColumnHeader colVarType;
        private System.Windows.Forms.ColumnHeader colVarValue;
        private System.Windows.Forms.Button btnSetTrue;
        private System.Windows.Forms.Button btnSetFalse;
        private System.Windows.Forms.Button btnResetZero;
        
        // PLC
        private System.Windows.Forms.GroupBox grpPLC;
        private System.Windows.Forms.Label lblIP;
        private System.Windows.Forms.TextBox txtPlcIP;
        private System.Windows.Forms.Button btnConnectPLC;
        private System.Windows.Forms.ListView lvIO;
        private System.Windows.Forms.ColumnHeader colIOAddress;
        private System.Windows.Forms.ColumnHeader colIODesc;
        private System.Windows.Forms.ColumnHeader colIOValue;
        private System.Windows.Forms.Button btnSet1;
        private System.Windows.Forms.Button btnSet0;
        

        // Control
        private System.Windows.Forms.Label lblChay;
        private System.Windows.Forms.Label lblDung;
        private System.Windows.Forms.Button btnAutoRun;
        private System.Windows.Forms.Button btnStartRapid;
        private System.Windows.Forms.Button btnStartSync;
        private System.Windows.Forms.Button btnStopRapid;
        private System.Windows.Forms.Button btnStopSync;
        
        // Camera
        private System.Windows.Forms.GroupBox grpCamera;
        private System.Windows.Forms.PictureBox picCamera;
        private System.Windows.Forms.Label lblCameraStatus;
        private System.Windows.Forms.ComboBox cboCamera;
        private System.Windows.Forms.Button btnScanCamera;
        private System.Windows.Forms.Button btnLoadModel;
        private System.Windows.Forms.Button btnLoadImage;
        
        // Log
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.RichTextBox rtbLog;
        
        // Timer
        private System.Windows.Forms.Timer timerUI;
    }
}
