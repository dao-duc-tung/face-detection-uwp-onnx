using Emgu.CV;
using Emgu.CV.Structure;
using FaceDetection.Utils;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public sealed class UltraFaceDetectorInput2
    {
        public List<NamedOnnxValue> input; // shape(1,3,240,320)
    }

    public sealed class UltraFaceDetectorOutput2
    {
        // index 0=confidences with shape(1,4420,2)
        // index 1=boxes with shape(1,4420,4)
        public List<float[]> output;
    }

    /// <summary>
    /// This detector uses OnnxRuntime for inference
    /// FPS ~ 6
    /// </summary>
    public sealed class UltraFaceDetector2 : BaseUltraFaceDetector
    {
        private bool _isLoaded = false;
        private InferenceSession _session;
        private string _modelFileName = "UltraFaceModel2.onnx";

        public override bool IsModelLoaded() => _isLoaded;

        public UltraFaceDetector2(UltraFaceDetectorConfig _config) : base(_config) { }

        public override async Task LoadModel(StorageFile file)
        {
            if (file == null) return;
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                await file.CopyAsync(localFolder, _modelFileName, NameCollisionOption.ReplaceExisting);
                var modelPath = Path.Combine(localFolder.Path, _modelFileName);
                _session = new InferenceSession(modelPath);
                _isLoaded = true;
            } catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public override Task Detect(Mat originalImage)
        {
            if (originalImage == null) return Task.CompletedTask;
            var input = Preprocess(originalImage);
            var output = _Detect(input);
            var faces = Postprocess(output);
            RaiseFaceDetectedEvent(faces, originalImage.Size);
            return Task.CompletedTask;
        }

        private UltraFaceDetectorOutput2 _Detect(UltraFaceDetectorInput2 input)
        {
            List<float[]> modelOutputs = new List<float[]>();
            using (var results = _session.Run(input.input))
            {
                foreach (var r in results)
                {
                    float[] rr = r.AsTensor<float>().ToArray();
                    modelOutputs.Add(rr);
                }

            }
            var output = new UltraFaceDetectorOutput2() { output = modelOutputs };
            return output;
        }

        private new UltraFaceDetectorInput2 Preprocess(Mat originalImage)
        {
            var processedImage = base.Preprocess(originalImage);
            Tensor<float> tensorInput = ConvertImageToTensorData(processedImage);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor<float>("input", tensorInput),
            };
            processedImage.Dispose();
            var input = new UltraFaceDetectorInput2() { input = inputs };
            return input;
        }

        private IReadOnlyList<FaceBoundingBox> Postprocess(UltraFaceDetectorOutput2 output)
        {
            var confidences = output.output[0];
            var boxes = output.output[1];
            var picked = base.Postprocess(confidences, boxes);
            return picked;
        }

        private static Tensor<float> ConvertImageToTensorData(Mat image)
        {
            // perform transpose permute [2,0,1] and expand dims at axis=0
            // image = np.transpose(image, [2, 0, 1])
            // image = np.expand_dims(image, axis = 0)
            // image = image.astype(np.float32)
            int width = image.Width;
            int height = image.Height;
            Tensor<float> imageData = new DenseTensor<float>(new[] { 1, 3, height, width }, false);

            var data = image.ToImage<Rgb, float>();
            for (int z = 0; z < 3; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        switch (z)
                        {
                            case 0: imageData[0, z, y, x] = (float)data[y, x].Red; break;
                            case 1: imageData[0, z, y, x] = (float)data[y, x].Green; break;
                            case 2: imageData[0, z, y, x] = (float)data[y, x].Blue; break;
                        }
                    }

                }
            }
            return imageData;
        }
    }
}
