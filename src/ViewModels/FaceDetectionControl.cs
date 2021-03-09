using Emgu.CV;
using FaceDetection.FaceDetector;
using FaceDetection.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace FaceDetection.ViewModels
{
    public class FaceDetectionControl
    {
        private IFaceDetector _faceDetector;
        private bool _isDetecting;
        private Stopwatch _fpsStopwatch = new Stopwatch();

        public bool IsFaceDetectionEnabled { get; set; }
        public float FPS { get; set; } = 0;

        public event FaceDetectedEventHandler FaceDetected
        {
            add
            {
                _faceDetector.FaceDetected += value;
            }
            remove
            {
                _faceDetector.FaceDetected -= value;
            }
        }

        public async Task LoadModelAsync<T>(StorageFile file) where T : IFaceDetector
        {
            var config = (UltraFaceDetectorConfig)AppConfig.Instance.GetConfig(ConfigName.UltraFaceDetector);
            _faceDetector = Activator.CreateInstance(typeof(T), new object[] { config }) as IFaceDetector;
            await _faceDetector.LoadModel(file);
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

        public static FaceBoundingBox ScaleBoundingBox(FaceBoundingBox origBB, System.Drawing.Size originalSize)
        {
            int origWidth = originalSize.Width;
            int origHeight = originalSize.Height;

            var bb = new FaceBoundingBox();
            bb.X0 = origBB.X0 * origWidth;
            bb.X1 = origBB.X1 * origWidth;
            bb.Y0 = origBB.Y0 * origHeight;
            bb.Y1 = origBB.Y1 * origHeight;
            return bb;
        }
    }
}
