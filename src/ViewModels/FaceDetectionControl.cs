using Emgu.CV;
using FaceDetection.FaceDetector;
using FaceDetection.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace FaceDetection.ViewModels
{
    public class FaceDetectionControl
    {
        private IFaceDetector _faceDetector;
        private bool _isDetecting;
        private Stopwatch _fpsStopwatch = new Stopwatch();

        public bool IsFaceDetectionEnabled { get; set; }
        public float FPS { get; private set; } = 0;

        public event FaceDetectedEventHandler FaceDetected
        {
            add => _faceDetector.FaceDetected += value;
            remove => _faceDetector.FaceDetected -= value;
        }

        public async Task InitializeAsync(Type _class)
        {
            var configName = UltraFaceDetectorConfig.GetConfigNameByType(_class);
            var config = AppConfig.Instance.GetConfig(configName);
            _faceDetector = Activator.CreateInstance(_class) as IFaceDetector;
            _faceDetector.LoadConfig(config);
            await _faceDetector.LoadModel();
        }

        public async Task RunFaceDetection(SoftwareBitmap bmp)
        {
            if (!IsFaceDetectionEnabled || _faceDetector == null) return;
            else if (_faceDetector != null && !_faceDetector.IsModelLoaded()) return;

            Mat img = ImageUtils.ConvertSoftwareBitmapToMat(bmp);
            if (img == null) return;

            if (_isDetecting) return;
            _isDetecting = true;

            _fpsStopwatch.Restart();
            await _faceDetector.Detect(img);
            _fpsStopwatch.Stop();
            FPS = 1.0f / (float)_fpsStopwatch.Elapsed.TotalSeconds;
            img.Dispose();

            _isDetecting = false;
        }
    }
}
