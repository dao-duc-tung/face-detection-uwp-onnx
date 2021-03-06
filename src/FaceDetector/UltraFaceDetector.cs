using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public sealed class UltraFaceDetectorInput
    {
        public TensorFloat input; // shape(1,3,240,320)
    }

    public sealed class UltraFaceDetectorOutput
    {
        public TensorFloat scores = TensorFloat.Create(new long[] { 1, 4420, 2 }); // shape(1,4420,2)
        public TensorFloat boxes = TensorFloat.Create(new long[] { 1, 4420, 4 }); // shape(1,4420,4)
    }

    public sealed class UltraFaceDetector : IFaceDetector
    {
        private LearningModel _learningModel;
        private LearningModelSession _session;
        // TODO: Load config of confidence_threshold, IoU_threshold
        private float _confidence_threshold = 0.7f;
        private float _iou_threshold = 0.0f;
        private int LIMIT_MAX_FACES = 200;

        public event FaceDetectedEventHandler FaceDetected;

        public bool IsModelLoaded()
        {
            return this._learningModel != null;
        }

        public async void LoadModel(StorageFile file)
        {
            try
            {
                var learningModel = await LearningModel.LoadFromStorageFileAsync(file);
                this._learningModel = learningModel;
                this._session = new LearningModelSession(learningModel);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async Task Detect(Mat originalImage)
        {
            var input = Preprocess(originalImage);

            var output = new UltraFaceDetectorOutput();
            var binding = new LearningModelBinding(this._session);
            binding.Bind("input", input.input);
            binding.Bind("scores", output.scores);
            binding.Bind("boxes", output.boxes);
            LearningModelEvaluationResult result = await this._session.EvaluateAsync(binding, "0");
            
            var faces = Postprocess(output);
            this.RaiseFaceDetectedEvent(faces, originalImage.Size);
        }

        private void RaiseFaceDetectedEvent(IReadOnlyList<FaceBoundingBox> faces, Size originalSize)
        {
            if (this.FaceDetected == null) return;
            this.FaceDetected(this, faces, originalSize);
        }

        private struct InputImageSettings
        {
            public const int NumberOfChannels = 3;
            public const int ImageHeight = 240;
            public const int ImageWidth = 320;
        }

        private UltraFaceDetectorInput Preprocess(Mat originalImage)
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

            // Convert to TensorFloat
            TensorFloat tensorInput = ConvertImageToTensorFloat(floatImage);
            UltraFaceDetectorInput modelInput = new UltraFaceDetectorInput() { input = tensorInput };

            // Clean
            rgbImage.Dispose();
            resizedImage.Dispose();
            floatImage.Dispose();
            return modelInput;
        }

        private static TensorFloat ConvertImageToTensorFloat(Mat image)
        {
            // perform transpose permute [2,0,1] and expand dims at axis=0
            int width = image.Width;
            int height = image.Height;

            float[] imageData = new float[1 * 3 * height * width];

            var data = image.ToImage<Rgb, float>();
            for (int z = 0; z < 3; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int targetidx = z * height * width + y * width + x;
                        switch (z)
                        {
                            case 0: imageData[targetidx] = (float)data[y, x].Red; break;
                            case 1: imageData[targetidx] = (float)data[y, x].Green; break;
                            case 2: imageData[targetidx] = (float)data[y, x].Blue; break;
                        }
                    }

                }
            }
            TensorFloat modelInput = TensorFloat.CreateFromShapeArrayAndDataArray(new long[] { 1, 3, height, width }, imageData);
            return modelInput;
        }

        private IReadOnlyList<FaceBoundingBox> Postprocess(UltraFaceDetectorOutput output)
        {
            var confidences = output.scores.GetAsVectorView();
            var boxes = output.boxes.GetAsVectorView();
            var boxCandidates = this.FilterConfidences(confidences, boxes);
            var predictions = this.HardNMS(boxCandidates);
            var picked = predictions.GetRange(0, Math.Min(predictions.Count, this.LIMIT_MAX_FACES));
            return picked;
        }

        private List<FaceBoundingBox> FilterConfidences(IReadOnlyList<float> confidences, IReadOnlyList<float> boxes)
        {
            List<FaceBoundingBox> boxCandidates = new List<FaceBoundingBox>();
            for (int i = 0; i < confidences.Count; ++i)
            {
                if (i % 2 != 0)
                {
                    float confidence = confidences[i];
                    if (confidence > this._confidence_threshold)
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

        private List<FaceBoundingBox> HardNMS(List<FaceBoundingBox> boxCandidates)
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
                    if (Get_IOU(predictions[predictions.Count - 1], box) > this._iou_threshold)
                    {
                        boxCandidates.Remove(box);
                    }
                }
            }
            return predictions;
        }

        private static float Get_IOU(FaceBoundingBox bb1, FaceBoundingBox bb2)
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
    }
}
