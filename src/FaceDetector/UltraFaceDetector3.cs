using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public sealed class UltraFaceDetectorInput3
    {
        public Mat input;
    }

    public sealed class UltraFaceDetectorOutput3
    {
        public VectorOfMat output;
    }

    /// <summary>
    /// This detector uses Emgu.Cv.Dnn for inference
    /// </summary>
    public sealed class UltraFaceDetector3 : BaseUltraFaceDetector
    {
        private bool _isLoaded = false;
        private Net _ultraface;
        private string _modelFileName = "UltraFaceModel3.onnx";
        private string[] _outNames = new string[]
        {
            "scores", "boxes"
        };
        private List<float> _strides = new List<float>
        {
            8.0f, 16.0f, 32.0f, 64.0f
        };
        private List<List<float>> _featuremapSize = new List<List<float>>();
        private List<List<float>> _shrinkageSize = new List<List<float>>();
        private const int _NUM_FEATUREMAP= 4;
        private List<List<float>> _minBoxes = new List<List<float>>()
        {
            new List<float> {10.0f,  16.0f,  24.0f},
            new List<float> {32.0f,  48.0f},
            new List<float> {64.0f,  96.0f},
            new List<float> {128.0f, 192.0f, 256.0f}
        };
        private List<List<float>> _priors = new List<List<float>>();
        private int _numAnchors = 0;
        private const float _CENTER_VARIANCE = 0.1f;
        private const float _SIZE_VARIANCE = 0.2f;

        public override bool IsModelLoaded() => _isLoaded;

        public override async Task LoadModel()
        {
            try
            {
                var modelLocalPath = _config.ModelLocalPath;
                var uri = FileUtils.GetUriByLocalFilePath(modelLocalPath);
                var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                if (file == null) return;

                var localFolder = ApplicationData.Current.LocalFolder;
                await file.CopyAsync(localFolder, _modelFileName, NameCollisionOption.ReplaceExisting);
                var modelPath = Path.Combine(localFolder.Path, _modelFileName);
                _ultraface = DnnInvoke.ReadNetFromONNX(modelPath);
                
                BuildPriors();
                _isLoaded = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void BuildPriors()
        {
            List<float> widthHeightList = new List<float>()
            {
                InputImageSettings.ImageWidth, InputImageSettings.ImageHeight
            };
            foreach (var size in widthHeightList)
            {
                List<float> fmItem = new List<float>();
                foreach (float stride in _strides)
                {
                    fmItem.Add(MathF.Ceiling(size / stride));
                }
                _featuremapSize.Add(fmItem);
            }

            foreach (var _ in widthHeightList)
            {
                _shrinkageSize.Add(_strides);
            }

            // Generate prior anchors
            for (int index = 0; index < _NUM_FEATUREMAP; ++index)
            {
                float scaleWidth = InputImageSettings.ImageWidth / _shrinkageSize[0][index];
                float scaleHeight = InputImageSettings.ImageHeight / _shrinkageSize[1][index];
                for (int j = 0; j < _featuremapSize[1][index]; ++j)
                {
                    for (int i = 0; i < _featuremapSize[0][index]; ++i)
                    {
                        float xCenter = (i + 0.5f) / scaleWidth;
                        float yCenter = (j + 0.5f) / scaleHeight;
                        foreach (float k in _minBoxes[index])
                        {
                            float w = k / InputImageSettings.ImageWidth;
                            float h = k / InputImageSettings.ImageHeight;
                            _priors.Add(new List<float>()
                                {
                                    Clip(xCenter, 1), Clip(yCenter, 1), Clip(w, 1), Clip(h, 1)
                                }
                            );
                        }
                    }
                }
            }
            _numAnchors = _priors.Count;
            // generate prior anchors finished
        }

        public override Task Detect(Mat originalImage)
        {
            if (originalImage == null) return Task.CompletedTask;
            this.originalImage = originalImage;
            var input = Preprocess(originalImage);
            var output = _Detect(input);
            var faces = Postprocess(output);
            RaiseFaceDetectedEvent(faces);
            return Task.CompletedTask;
        }

        private new UltraFaceDetectorInput3 Preprocess(Mat originalImage)
        {
            int nChannel = InputImageSettings.NumberOfChannels;
            Mat rgbImage = new Mat(new Size(originalImage.Width, originalImage.Height), originalImage.Depth, nChannel);
            var conversion = originalImage.NumberOfChannels == 4 ? ColorConversion.Bgra2Rgb : ColorConversion.Bgr2Rgb;
            CvInvoke.CvtColor(originalImage, rgbImage, conversion);

            Mat inputBlob = DnnInvoke.BlobFromImage(
                rgbImage, 1.0 / 128,
                new Size(InputImageSettings.ImageWidth, InputImageSettings.ImageHeight),
                new MCvScalar(127, 127, 127), true
            );
            var input = new UltraFaceDetectorInput3() { input = inputBlob };
            return input;
        }

        private UltraFaceDetectorOutput3 _Detect(UltraFaceDetectorInput3 input)
        {
            VectorOfMat outBlobs = new VectorOfMat(2);
            _ultraface.SetInput(input.input);
            _ultraface.Forward(outBlobs, _outNames);
            var output = new UltraFaceDetectorOutput3() { output = outBlobs };
            return output;
        }

        private IReadOnlyList<FaceBoundingBox> Postprocess(UltraFaceDetectorOutput3 output)
        {
            Mat confidencesMat = output.output[0];
            Mat boxesMat = output.output[1];
            int confidencesLen = 1 * _numAnchors * 2;
            int boxesLen = 1 * _numAnchors * 4;
            float[] confidences = new float[confidencesLen];
            float[] boxes = new float[boxesLen];
            Marshal.Copy(confidencesMat.DataPointer, confidences, 0, confidencesLen);
            Marshal.Copy(boxesMat.DataPointer, boxes, 0, boxesLen);
            var picked = base.Postprocess(confidences, boxes);
            return picked;
        }

        protected override List<FaceBoundingBox> FilterConfidences(IReadOnlyList<float> confidences, IReadOnlyList<float> boxes)
        {
            var origWidth = originalImage.Width;
            var origHeight = originalImage.Height;
            List<FaceBoundingBox> boxCandidates = new List<FaceBoundingBox>();
            for (int i = 0; i < _numAnchors; ++i)
            {
                float score = confidences[2 * i + 1];
                if (score > _config.ConfidenceThreshold)
                {
                    int boxIdx = i * 4;
                    float xCenter = boxes[boxIdx] * _CENTER_VARIANCE * _priors[i][2] + _priors[i][0];
                    float yCenter = boxes[boxIdx + 1] * _CENTER_VARIANCE * _priors[i][3] + _priors[i][1];
                    float w = MathF.Exp(boxes[boxIdx + 2] * _SIZE_VARIANCE) * _priors[i][2];
                    float h = MathF.Exp(boxes[boxIdx + 3] * _SIZE_VARIANCE) * _priors[i][3];
                    FaceBoundingBox bb = new FaceBoundingBox() {
                        X0 = Clip(xCenter - w / 2.0f, 1) * origWidth,
                        Y0 = Clip(yCenter - h / 2.0f, 1) * origHeight,
                        X1 = Clip(xCenter + w / 2.0f, 1) * origWidth,
                        Y1 = Clip(yCenter + h / 2.0f, 1) * origHeight,
                        Confidence = Clip(score, 1),
                    };
                    boxCandidates.Add(bb);
                }
            }
            return boxCandidates;
        }

        private float Clip(float x, float y)
        {
            return (x < 0 ? 0 : (x > y ? y : x));
        }
    }
}
