using System.Collections.Generic;

namespace RoboticGlazingSystem.WinForms.Models
{
    public class DetectionInfo
    {
        public string ClassName { get; set; }
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class YoloInferenceResult
    {
        public List<DetectionInfo> Detections { get; set; } = new List<DetectionInfo>();
    }
}
