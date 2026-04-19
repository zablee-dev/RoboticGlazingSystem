using System;
using System.Collections.Generic;
using System.Windows.Forms;
using S7.Net;

namespace RoboticGlazingSystem.WinForms.Services.Hardware
{
    /// <summary>
    /// PLC Manager - Quản lý giao tiếp với PLC S7-1200 qua thư viện S7.Net
    /// 
    /// ╔══════════════════════════════════════════════════════════════════════════════╗
    /// ║                    TỔNG QUAN LUỒNG TRUYỀN THÔNG                              ║
    /// ╠══════════════════════════════════════════════════════════════════════════════╣
    /// ║  [GỬI LỆNH] PC → PLC → Robot A                                               ║
    /// ║  ────────────────────────────────────────────────────────────────────────────║
    /// ║  • SetBackGlass()  : M1.1 → Q0.4 → DI1 (Lệnh lấy kính sau)                   ║
    /// ║  • SetSideGlass()  : M1.0 → Q0.5 → DI2 (Lệnh lấy kính hông)                  ║
    /// ║  • SetDoneGlue()   : M1.2 → Q0.6 → DI3 (Báo Robot A: Robot B phun keo xong)  ║
    /// ║  • SetGlueValve()  : M6.6 → Q0.7       (Điều khiển van 5/2 phun keo)         ║
    /// ╠══════════════════════════════════════════════════════════════════════════════╣
    /// ║  [NHẬN TRẠNG THÁI] Robot A → PLC → PC                                        ║
    /// ║  ────────────────────────────────────────────────────────────────────────────║
    /// ║  • ReadRobotAGluePosition() : I0.6 ← DO2 (Robot A đã tới vị trí trét keo)    ║
    /// ║  • ReadRobotADone()         : I0.7 ← DO3 (Robot A đã hoàn thành lắp kính)    ║
    /// ╠══════════════════════════════════════════════════════════════════════════════╣
    /// ║  [ĐỌC CẢM BIẾN] Loadcell → PLC → PC                                          ║
    /// ║  ────────────────────────────────────────────────────────────────────────────║
    /// ║  • DocGlueWeight() : MD20 (Đọc trọng lượng keo từ loadcell, đơn vị kg)       ║
    /// ╚══════════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public class QuanLyPLC
    {
        private Plc plc;
        private bool daKetNoi = false;
        private string lastIP = "";
        
        // Auto-reconnect
        private System.Windows.Forms.Timer reconnectTimer;
        private int reconnectAttempts = 0;
        private int[] reconnectDelays = { 1000, 2000, 5000, 10000 };
        
        public event Action<string> ThongBaoLog;
        
        public QuanLyPLC()
        {
            reconnectTimer = new System.Windows.Forms.Timer();
            reconnectTimer.Tick += ReconnectTimer_Tick;
        }
        
        /// <summary>
        /// Connect to PLC
        /// </summary>
        public bool KetNoiPLC(string diaChiIP)
        {
            try
            {
                plc = new Plc(CpuType.S71200, diaChiIP, 0, 1);
                plc.Open();
                
                if (plc.IsConnected == true)
                {
                    daKetNoi = true;
                    ThongBaoLog?.Invoke("Connected to PLC at " + diaChiIP);
                    
                    // Start monitoring for auto-reconnect
                    lastIP = diaChiIP;
                    reconnectAttempts = 0;
                    reconnectTimer.Interval = 1000;
                    reconnectTimer.Start();
                    
                    return true;
                }
                
                ThongBaoLog?.Invoke("Cannot connect to PLC");
                return false;
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke("PLC Error: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from PLC
        /// </summary>
        public void NgatKetNoi()
        {
            try
            {
                reconnectTimer.Stop();
                if (plc != null && plc.IsConnected == true)
                {
                    plc.Close();
                    daKetNoi = false;
                }
            }
            catch { }
        }
        
        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (plc != null && plc.IsConnected)
                {
                    reconnectAttempts = 0;
                    return;
                }
                
                if (reconnectAttempts == 0)
                {
                    ThongBaoLog?.Invoke("[AutoReconnect] Connection lost, attempting reconnect...");
                }
                
                try
                {
                    if (plc != null)
                    {
                        plc.Close();
                    }
                    
                    plc = new Plc(CpuType.S71200, lastIP, 0, 1);
                    plc.Open();
                    
                    if (plc.IsConnected)
                    {
                        daKetNoi = true;
                        reconnectAttempts = 0;
                        reconnectTimer.Interval = 1000;
                        ThongBaoLog?.Invoke("[AutoReconnect] Reconnected successfully!");
                        return;
                    }
                }
                catch { }
                
                reconnectAttempts++;
                int delayIndex = Math.Min(reconnectAttempts - 1, reconnectDelays.Length - 1);
                reconnectTimer.Interval = reconnectDelays[delayIndex];
                
                ThongBaoLog?.Invoke($"[AutoReconnect] Attempt {reconnectAttempts} failed, retry in {reconnectTimer.Interval / 1000}s");
            }
            catch { }
        }
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║                      PHƯƠNG THỨC ĐỌC/GHI CƠ BẢN                              ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// [NHẬN] Đọc 1 bit từ PLC (I/O hoặc Memory bit)
        /// Dùng để đọc trạng thái input (I0.x) hoặc memory (M1.x)
        /// </summary>
        public bool DocBit(string diaChiIO)
        {
            if (daKetNoi == false || plc == null) return false;
            
            try
            {
                // Convert IEC to German notation: Q->A
                string diaChiIODuc = diaChiIO.Replace("Q", "A").Replace("q", "a");
                
                object giaTriDocDuoc = plc.Read(diaChiIODuc);

                if (giaTriDocDuoc is bool)
                {
                    return (bool)giaTriDocDuoc;
                }

                if (giaTriDocDuoc is int)
                {
                    return (int)giaTriDocDuoc != 0;
                }

                if (giaTriDocDuoc is byte)
                {
                    return (byte)giaTriDocDuoc != 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// [GỬI] Ghi 1 bit vào PLC (Output hoặc Memory bit)
        /// Dùng để điều khiển output (Q0.x) hoặc memory (M1.x)
        /// </summary>
        public bool GhiBit(string diaChiIO, bool giaTri)
        {
            if (daKetNoi == false || plc == null)
            {
                ThongBaoLog?.Invoke($"[GhiBit] FAILED: Not connected. Address={diaChiIO}");
                return false;
            }
            
            try
            {
                // Convert IEC to German notation for Q/A only
                string diaChiIODuc = diaChiIO.Replace("Q", "A").Replace("q", "a");
                
                ThongBaoLog?.Invoke($"[GhiBit] Writing {diaChiIODuc} = {giaTri}");
                
                // Use direct write for all addresses (like M1.2)
                plc.Write(diaChiIODuc, giaTri);
                
                ThongBaoLog?.Invoke($"[GhiBit] SUCCESS: {diaChiIODuc} = {giaTri}");
                return true;
            }
            catch (Exception ex)
            {
                ThongBaoLog?.Invoke($"[GhiBit] ERROR: {diaChiIO} - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Read byte from DB
        /// </summary>
        public byte DocByte(int dbNumber, int startByte)
        {
            if (daKetNoi == false || plc == null) return 0;
            
            try
            {
                var result = plc.Read(DataType.DataBlock, dbNumber, startByte, VarType.Byte, 1);
                if (result is byte[] bytes && bytes.Length > 0)
                    return bytes[0];
                if (result is byte b)
                    return b;
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Read bit from DB
        /// </summary>
        public bool DocBitDB(int dbNumber, int startByte, int bitIndex)
        {
            byte b = DocByte(dbNumber, startByte);
            return (b & (1 << bitIndex)) != 0;
        }
        
        /// <summary>
        /// Write bit to DB
        /// </summary>
        public bool GhiBitDB(int dbNumber, int startByte, int bitIndex, bool giaTri)
        {
            if (daKetNoi == false || plc == null) return false;
            
            try
            {
                string address = $"DB{dbNumber}.DBX{startByte}.{bitIndex}";
                plc.Write(address, giaTri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get list of I/O addresses for display
        /// </summary>
        public List<ThongTinIO> LayDanhSachIO()
        {
            var danhsach = new List<ThongTinIO>();
            
            // === INPUTS (từ Robot A / Field) ===
            danhsach.Add(new ThongTinIO("I0.0", "Start Button", "0"));
            danhsach.Add(new ThongTinIO("I0.6", "← RobotA tại vị trí keo (DO2)", "0"));
            danhsach.Add(new ThongTinIO("I0.7", "← RobotA Done (DO3)", "0"));
            
            // === OUTPUTS (PC → PLC → Robot A) via Memory bits ===
            danhsach.Add(new ThongTinIO("M1.1", "→ Kính Sau (Q0.4 → DI1)", "0"));
            danhsach.Add(new ThongTinIO("M1.0", "→ Kính Hông (Q0.5 → DI2)", "0"));
            danhsach.Add(new ThongTinIO("M1.2", "→ Done Glue (Q0.6 → DI3)", "0"));
            danhsach.Add(new ThongTinIO("M6.6", "→ Van 5/2 Phun Keo (Q0.7)", "0"));
            // Van 3/2 Vacuum: Robot điều khiển trực tiếp, không qua PLC
            
            // Van 3/2 Vacuum: Robot điều khiển trực tiếp, không qua PLC
            // Van 5/2 Phun Keo: M6.6 → Q0.7 (không còn dùng DB1.DBX0.6)
            
            if (daKetNoi)
            {
                for (int i = 0; i < danhsach.Count; i++)
                {
                    try
                    {
                        bool v = DocBit(danhsach[i].DiaChi);
                        danhsach[i].GiaTri = v ? "1" : "0";
                    }
                    catch { }
                }
            }
            
            return danhsach;
        }
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║      [GỬI LỆNH] OUTPUT HELPERS - PC gửi lệnh đến PLC rồi tới Robot A         ║
        // ║      Luồng: PC → PLC (Memory Bit) → PLC (Output) → Robot A (Digital Input)   ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// [GỬI] Gửi lệnh lấy KÍNH SAU cho Robot A
        /// Luồng: PC → M1.1 → Q0.4 → Robot A DI1
        /// Robot A sẽ đọc DI1=TRUE và thực hiện quy trình hút-lắp kính sau
        /// </summary>
        public bool SetBackGlass(bool value) => GhiBit("M1.1", value);
        
        /// <summary>
        /// [GỬI] Gửi lệnh lấy KÍNH HÔNG cho Robot A
        /// Luồng: PC → M1.0 → Q0.5 → Robot A DI2
        /// Robot A sẽ đọc DI2=TRUE và thực hiện quy trình hút-lắp kính hông
        /// </summary>
        public bool SetSideGlass(bool value) => GhiBit("M1.0", value);
        
        /// <summary>
        /// [GỬI] Báo Robot A rằng Robot B đã PHUN KEO XONG
        /// Luồng: PC → M1.2 → Q0.6 → Robot A DI3
        /// Khi Robot A đọc DI3=TRUE, nó sẽ tiến hành lắp kính vào ô tô
        /// </summary>
        public bool SetDoneGlue(bool value) => GhiBit("M1.2", value);
        
        /// <summary>
        /// [GỬI] Kích hoạt VAN 5/2 - súng phun keo
        /// Luồng: PC → M6.6 → Q0.7 → Van điện từ 5/2
        /// PC theo dõi biến gbValveOn từ Robot B để đồng bộ bật/tắt van
        /// Van 3/2 Vacuum: Robot điều khiển trực tiếp qua I/O riêng (không qua PC)
        /// </summary>
        public bool SetGlueValve(bool value) => GhiBit("M6.6", value);
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║   [NHẬN TRẠNG THÁI] INPUT HELPERS - PC đọc trạng thái từ Robot A qua PLC     ║
        // ║   Luồng: Robot A (Digital Output) → PLC (Input) → PC                         ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// [NHẬN] Đọc trạng thái: Robot A đã tới VỊ TRÍ TRÉT KEO
        /// Luồng: Robot A DO2 → I0.6 ← PC
        /// Khi TRUE: Robot A đã giữ kính và chờ Robot B tới phun keo
        /// PC sử dụng tín hiệu này để gửi lệnh GLUE_REAR hoặc GLUE_SIDE cho Robot B
        /// </summary>
        public bool ReadRobotAGluePosition() => DocBit("I0.6");
        
        /// <summary>
        /// [NHẬN] Đọc trạng thái: Robot A đã HOÀN THÀNH lắp kính
        /// Luồng: Robot A DO3 → I0.7 ← PC
        /// Khi TRUE: Robot A đã lắp xong kính và về vị trí Home
        /// PC sử dụng tín hiệu này để reset chu kỳ và sẵn sàng cho lệnh tiếp theo
        /// </summary>
        public bool ReadRobotADone() => DocBit("I0.7");
        
        // ╔══════════════════════════════════════════════════════════════════════════════╗
        // ║        [NHẬN DỮ LIỆU] SENSOR HELPERS - PC đọc dữ liệu từ cảm biến            ║
        // ╚══════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// Read REAL (float) from MD address - dùng cho Loadcell
        /// MD20 = Glue_Weight (kg)
        /// </summary>
        public float DocReal(int mdAddress)
        {
            if (daKetNoi == false || plc == null) return 0f;
            
            try
            {
                object result = plc.Read($"MD{mdAddress}");
                
                if (result is float f)
                    return f;
                if (result is double d)
                    return (float)d;
                if (result is uint u)
                    return BitConverter.ToSingle(BitConverter.GetBytes(u), 0);
                    
                return 0f;
            }
            catch
            {
                return 0f;
            }
        }
        
        /// <summary>
        /// Đọc lượng keo từ Loadcell (MD20) - đơn vị: kg
        /// </summary>
        public float DocGlueWeight() => DocReal(20);
        
        // Van 5/2 phun keo giờ dùng M6.6 → Q0.7 (SetGlueValve), không còn dùng DB1.DBX0.6
        
        /// <summary>Check if connected</summary>
        public bool DaKetNoi => daKetNoi && plc != null && plc.IsConnected;
    }
}
