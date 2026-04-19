namespace RoboticGlazingSystem.WinForms.Services.Hardware
{
    /// <summary>
    /// RAPID variable information model
    /// </summary>
    public class ThongTinBien
    {
        public string Ten { get; set; }
        public string Kieu { get; set; }
        public string GiaTri { get; set; }
        
        public ThongTinBien(string ten, string kieu, string giaTri)
        {
            Ten = ten;
            Kieu = kieu;
            GiaTri = giaTri;
        }
    }
}
