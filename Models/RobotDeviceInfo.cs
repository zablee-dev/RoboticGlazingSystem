using ABB.Robotics.Controllers;

namespace RoboticGlazingSystem.WinForms.Models
{
    public class RobotDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool IsVirtual { get; set; }
        public string Availability { get; set; } = string.Empty;
        public ControllerInfo? ControllerInfo { get; set; }
    }
}
