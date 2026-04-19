using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoboticGlazingSystem.WinForms.Models;

namespace RoboticGlazingSystem.WinForms.Services
{
    /// <summary>
    /// AI Vision processing class using ONNX Runtime.
    /// Run YOLO (v5/v8...) model for object detection.
    /// </summary>
    public class YoloOnnx : IDisposable
    {
        private InferenceSession? _session; // ONNX Session
        
        // List of class names. Defaults to side_glass and rear_glass.
        private string[] _classNames = new[] { "side_glass", "rear_glass" };
        
        // Model input size (must match training configuration, usually 640x640)
        private readonly int _inputW = 640;
        private readonly int _inputH = 640;

        // Threshold Configuration
        public float MinConf { get; set; } = 0.25f;       // Minimum confidence to accept (0.0 - 1.0)
        public float NMSThreshold { get; set; } = 0.45f;  // IoU threshold for removing overlapping boxes
        
        public bool IsModelLoaded => _session != null;

        /// <summary>
        /// Load model from .onnx file
        /// </summary>
        /// <param name="modelPath">Path to .onnx file</param>
        /// <param name="classFilePath">Path to .txt file containing class names (optional)</param>
        public void LoadModel(string modelPath, string? classFilePath = null)
        {
            _session?.Dispose();
            _session = new InferenceSession(modelPath);

            if (!string.IsNullOrWhiteSpace(classFilePath) && File.Exists(classFilePath))
            {
                _classNames = File.ReadAllLines(classFilePath)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToArray();
            }

            Console.WriteLine($"[YoloOnnx] Model loaded: {modelPath}");
        }

        /// <summary>
        /// Perform detection on a single image
        /// </summary>
        /// <param name="frameBgr">Input image (OpenCV Mat BGR format)</param>
        /// <returns>Detection results (List of DetectionInfo)</returns>
        public YoloInferenceResult Infer(Mat frameBgr)
        {
            var result = new YoloInferenceResult();

            if (_session == null)
            {
                Console.WriteLine("[ERROR] Model not loaded!");
                return result;
            }

            try
            {
                int origW = frameBgr.Width;
                int origH = frameBgr.Height;

                // 1. Preprocessing: Letterbox Resize
                // Maintain aspect ratio, add gray padding to fill 640x640
                float rW = (float)_inputW / origW;
                float rH = (float)_inputH / origH;
                float r = Math.Min(rW, rH);

                int newW = (int)(origW * r);
                int newH = (int)(origH * r);
                int padX = (_inputW - newW) / 2;
                int padY = (_inputH - newH) / 2;

                using Mat resized = new();
                Cv2.Resize(frameBgr, resized, new OpenCvSharp.Size(newW, newH));

                using Mat letterbox = new Mat(
                    new OpenCvSharp.Size(_inputW, _inputH),
                    MatType.CV_8UC3,
                    new Scalar(114, 114, 114)); // Gray color (padding value)

                resized.CopyTo(letterbox[new Rect(padX, padY, newW, newH)]);

                // 2. Normalize: Convert BGR to RGB and divide by 255.0
                // USE OPENCV INSTEAD OF PIXEL-BY-PIXEL LOOP (10x FASTER)
                using Mat rgb = new();
                Cv2.CvtColor(letterbox, rgb, ColorConversionCodes.BGR2RGB);
                
                using Mat floatMat = new();
                rgb.ConvertTo(floatMat, MatType.CV_32FC3, 1.0 / 255.0);
                
                // Split channels and copy to tensor
                var input = new DenseTensor<float>(new[] { 1, 3, _inputH, _inputW });
                
                Mat[] channels = Cv2.Split(floatMat);
                try
                {
                    for (int c = 0; c < 3; c++)
                    {
                        channels[c].GetArray(out float[] channelData);
                        for (int i = 0; i < channelData.Length; i++)
                        {
                            int y = i / _inputW;
                            int x = i % _inputW;
                            input[0, c, y, x] = channelData[i];
                        }
                    }
                }
                finally
                {
                    foreach (var ch in channels) ch?.Dispose();
                }

                // 3. Run Inference
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("images", input)
                };

                using var results = _session.Run(inputs);
                var output = results.First().AsTensor<float>(); // Format output YOLO
                var dims = output.Dimensions.ToArray();

                // 4. Post-processing
                // YOLOv8 output shape is typically [1, 4 + num_classes, 8400]
                int numClasses = dims[1] - 4;
                int numBoxes = dims[2];
                var data = output.ToArray();

                var dets = new List<DetectionInfo>();

                for (int i = 0; i < numBoxes; i++)
                {
                    // Get coordinates and size (center_x, center_y, width, height)
                    float cx = data[0 * numBoxes + i];
                    float cy = data[1 * numBoxes + i];
                    float w = data[2 * numBoxes + i];
                    float h = data[3 * numBoxes + i];

                    // Find class with highest score
                    float[] classScores = new float[numClasses];
                    for (int c = 0; c < numClasses; c++)
                    {
                        classScores[c] = data[(4 + c) * numBoxes + i];
                    }

                    int bestClass = 0;
                    float maxScore = classScores[0];
                    for (int c = 1; c < numClasses; c++)
                    {
                        if (classScores[c] > maxScore)
                        {
                            maxScore = classScores[c];
                            bestClass = c;
                        }
                    }

                    // Filter by MinConf threshold
                    if (maxScore < MinConf) continue;

                    // Convert from relative to original absolute coordinates (x1, y1, w, h tren anh goc)
                    float x1 = cx - w / 2;
                    float y1 = cy - h / 2;

                    // Reverse Letterbox transformation
                    x1 = (x1 - padX) / r;
                    y1 = (y1 - padY) / r;
                    w = w / r;
                    h = h / r;

                    // Clip bounding boxes to stay within image boundaries
                    x1 = Math.Clamp(x1, 0, origW);
                    y1 = Math.Clamp(y1, 0, origH);
                    w = Math.Min(w, origW - x1);
                    h = Math.Min(h, origH - y1);

                    if (w < 5 || h < 5) continue; // Ignore if too small

                    string className = bestClass < _classNames.Length
                        ? _classNames[bestClass]
                        : $"class_{bestClass}";

                    dets.Add(new DetectionInfo
                    {
                        X = x1,
                        Y = y1,
                        Width = w,
                        Height = h,
                        ClassName = className,
                        Confidence = maxScore
                    });
                }

                // 5. NMS (Non-Maximum Suppression) - Remove overlapping boxes
                var filtered = ApplyNms(dets, NMSThreshold);

                foreach (var det in filtered)
                {
                    result.Detections.Add(det);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] YoloOnnx.Infer: {ex.Message}");
            }

            return result;
        }

        // Method to apply NMS
        private static List<DetectionInfo> ApplyNms(List<DetectionInfo> dets, float iouThresh)
        {
            var results = new List<DetectionInfo>();
            
            // Group by Class
            var grouped = dets.GroupBy(d => d.ClassName);

            foreach (var group in grouped)
            {
                // Sort by confidence descending
                var sorted = group.OrderByDescending(d => d.Confidence).ToList();

                while (sorted.Count > 0)
                {
                    var best = sorted[0];
                    results.Add(best);
                    sorted.RemoveAt(0);

                    // Remove boxes with IoU > threshold against the best box
                    sorted = sorted.Where(d => IoU(best, d) < iouThresh).ToList();
                }
            }

            return results;
        }

        // Method to compute IoU (Intersection over Union) between 2 boxes
        private static float IoU(DetectionInfo a, DetectionInfo b)
        {
            float x1 = Math.Max(a.X, b.X);
            float y1 = Math.Max(a.Y, b.Y);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            float w = Math.Max(0, x2 - x1);
            float h = Math.Max(0, y2 - y1);
            float inter = w * h;

            float areaA = a.Width * a.Height;
            float areaB = b.Width * b.Height;
            float union = areaA + areaB - inter;

            return inter / (union + 1e-6f);
        }

        public void Dispose() => _session?.Dispose();
    }
}
