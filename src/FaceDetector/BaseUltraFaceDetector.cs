using Emgu.CV;
using Emgu.CV.CvEnum;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace FaceDetection.FaceDetector
{
    public abstract class BaseUltraFaceDetector : IFaceDetector
    {
        protected UltraFaceDetectorConfig _config;
        public event FaceDetectedEventHandler FaceDetected;

        protected void RaiseFaceDetectedEvent(IReadOnlyList<FaceBoundingBox> faces, Size originalSize)
        {
            var eventArgs = new FaceDetectedEventArgs() { BoundingBoxes = faces, OriginalSize = originalSize };
            FaceDetected?.Invoke(this, eventArgs);
        }

        protected struct InputImageSettings
        {
            public const int NumberOfChannels = 3;
            public const int ImageHeight = 240;
            public const int ImageWidth = 320;
        }

        protected virtual Mat Preprocess(Mat originalImage)
        {
            // Convert BGR to RGB
            int nChannel = InputImageSettings.NumberOfChannels;
            Mat rgbImage = new Mat(new Size(originalImage.Width, originalImage.Height), originalImage.Depth, nChannel);
            var conversion = originalImage.NumberOfChannels == 4 ? ColorConversion.Bgra2Rgb : ColorConversion.Bgr2Rgb;
            CvInvoke.CvtColor(originalImage, rgbImage, conversion);

            // Resize
            int inputWidth = InputImageSettings.ImageWidth;
            int inputHeight = InputImageSettings.ImageHeight;
            System.Drawing.Size inputSize = new Size(inputWidth, inputHeight);
            Mat resizedImage = new Mat(inputSize, rgbImage.Depth, nChannel);
            CvInvoke.Resize(rgbImage, resizedImage, inputSize, 1.0, 1.0);

            // Normalize
            Mat floatImage = new Mat(inputSize, resizedImage.Depth, nChannel);
            resizedImage.ConvertTo(floatImage, DepthType.Cv32F);
            floatImage = (floatImage - 127) / 128;

            // Clean
            rgbImage.Dispose();
            resizedImage.Dispose();
            return floatImage;
        }

        protected virtual IReadOnlyList<FaceBoundingBox> Postprocess(IReadOnlyList<float> confidences, IReadOnlyList<float> boxes)
        {
            var boxCandidates = FilterConfidences(confidences, boxes);
            var predictions = HardNMS(boxCandidates);
            var picked = predictions.GetRange(0, Math.Min(predictions.Count, _config.LimitMaxFaces));
            return picked;
        }

        protected List<FaceBoundingBox> FilterConfidences(IReadOnlyList<float> confidences, IReadOnlyList<float> boxes)
        {
            List<FaceBoundingBox> boxCandidates = new List<FaceBoundingBox>();
            for (int i = 0; i < confidences.Count; ++i)
            {
                if (i % 2 != 0)
                {
                    float confidence = confidences[i];
                    if (confidence > _config.ConfidenceThreshold)
                    {
                        int boxIdx = i / 2 * 4;
                        FaceBoundingBox bb = new FaceBoundingBox()
                        {
                            X0 = boxes[boxIdx],
                            Y0 = boxes[boxIdx + 1],
                            X1 = boxes[boxIdx + 2],
                            Y1 = boxes[boxIdx + 3],
                            Confidence = confidence,
                        };
                        boxCandidates.Add(bb);
                    }
                }
            }
            return boxCandidates;
        }

        protected List<FaceBoundingBox> HardNMS(List<FaceBoundingBox> boxCandidates)
        {
            // Do Non-Maximum Suppression to remove overlapping boxes
            boxCandidates.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            List<FaceBoundingBox> predictions = new List<FaceBoundingBox>();
            while (boxCandidates.Count != 0)
            {
                FaceBoundingBox topbox = boxCandidates[0];
                // Remove top box
                boxCandidates.RemoveAt(0);
                predictions.Add(topbox);
                // Copy remaining boxes to a new list
                List<FaceBoundingBox> remaining_boxes = new List<FaceBoundingBox>(boxCandidates);
                foreach (FaceBoundingBox box in remaining_boxes)
                {
                    // Check IOU between top box and each remaining box
                    if (GetIoU(predictions[predictions.Count - 1], box) > _config.IoUThreshold)
                    {
                        boxCandidates.Remove(box);
                    }
                }
            }
            return predictions;
        }

        protected static float GetIoU(FaceBoundingBox bb1, FaceBoundingBox bb2)
        {
            // Calculate Intersection over Union ratio of 2 boxes.
            float x_left = Math.Max(bb1.X0, bb2.X0);
            float y_top = Math.Max(bb1.Y0, bb2.Y0);
            float x_right = Math.Min(bb1.X1, bb2.X1);
            float y_bottom = Math.Min(bb1.Y1, bb2.Y1);

            if ((x_right < x_left) || (y_bottom < y_top))
            {
                return 0.0F;
            }

            float intersection_area = (x_right - x_left) * (y_bottom - y_top);

            float bb1_area = (bb1.X1 - bb1.X0) * (bb1.Y1 - bb1.Y0);
            float bb2_area = (bb2.X1 - bb2.X0) * (bb2.Y1 - bb2.Y0);

            float iou = intersection_area / (bb1_area + bb2_area - intersection_area + 1e-5f);

            if (iou < 0 || iou > 1)
            {
                Debug.WriteLine(iou);
                throw new ArgumentOutOfRangeException("iou not [0,1]");
            }
            return iou;
        }

        public void LoadConfig(IConfig config)
        {
            _config = (UltraFaceDetectorConfig)config;
        }

        public virtual Task LoadModel()
        {
            throw new NotImplementedException();
        }

        public virtual bool IsModelLoaded()
        {
            throw new NotImplementedException();
        }

        public virtual Task Detect(Mat input)
        {
            throw new NotImplementedException();
        }
    }
}
