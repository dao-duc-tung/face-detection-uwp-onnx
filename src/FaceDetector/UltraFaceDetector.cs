using Emgu.CV;
using Emgu.CV.Structure;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    /// <summary>
    /// This detector uses Windows.AI for inference
    /// FPS ~ 12
    /// </summary>
    public sealed class UltraFaceDetector : BaseUltraFaceDetector
    {
        private LearningModel _learningModel;
        private LearningModelSession _session;

        public override bool IsModelLoaded() => _learningModel != null;

        public UltraFaceDetector(UltraFaceDetectorConfig _config) : base(_config) { }

        public override async Task LoadModel(StorageFile file)
        {
            if (file == null) return;
            try
            {
                var learningModel = await LearningModel.LoadFromStorageFileAsync(file);
                _learningModel = learningModel;
                _session = new LearningModelSession(learningModel);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public override async Task Detect(Mat originalImage)
        {
            if (originalImage == null) return;
            var input = Preprocess(originalImage);

            var output = new UltraFaceDetectorOutput();
            var binding = new LearningModelBinding(_session);
            binding.Bind("input", input.input);
            binding.Bind("scores", output.scores);
            binding.Bind("boxes", output.boxes);
            LearningModelEvaluationResult result = await _session.EvaluateAsync(binding, "0");

            var faces = Postprocess(output);
            RaiseFaceDetectedEvent(faces, originalImage.Size);
        }

        private new UltraFaceDetectorInput Preprocess(Mat originalImage)
        {
            var processedImage = base.Preprocess(originalImage);
            TensorFloat tensorInput = ConvertImageToTensorFloat(processedImage);
            UltraFaceDetectorInput modelInput = new UltraFaceDetectorInput() { input = tensorInput };
            processedImage.Dispose();
            return modelInput;
        }

        private IReadOnlyList<FaceBoundingBox> Postprocess(UltraFaceDetectorOutput output)
        {
            var confidences = output.scores.GetAsVectorView();
            var boxes = output.boxes.GetAsVectorView();
            var picked = base.Postprocess(confidences, boxes);
            return picked;
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
    }
}
