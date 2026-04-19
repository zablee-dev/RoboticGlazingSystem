namespace RoboticGlazingSystem.WinForms.Services.Hardware
{
    /// <summary>
    /// PLC I/O information model
    /// </summary>
    public class ThongTinIO
    {
        public string DiaChi { get; set; }
        public string MoTa { get; set; }
        public string GiaTri { get; set; }
        
        public ThongTinIO(string diaChi, string moTa, string giaTri)
        {
            DiaChi = diaChi;
            MoTa = moTa;
            GiaTri = giaTri;
        }
    }
}
