using Emgu.CV;
using FaceDetection.Models;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using FaceDetection.FaceDetector;
using System.Diagnostics;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Devices.Enumeration;
using Windows.Media.MediaProperties;
using System.Threading;
using System.IO;
using FaceDetection.DistanceEstimator;
using System.ComponentModel;

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        public FrameModel FrameModel { get; } = new FrameModel();
        public CameraControl CameraControl { get; } = new CameraControl();
        private int _processingFlag;

        private IFaceDetector _faceDetector;
        private bool _isFaceDetectionEnabled = false;
        // TODO: Create Config to load modelFileName
        private string _modelFileName = "version-RFB-320.onnx";
        
        public event FaceDetectedEventHandler FaceDetected
        {
            add
            {
                lock (_faceDetector)
                {
                    _faceDetector.FaceDetected += value;
                }
            }
            remove
            {
                lock (_faceDetector)
                {
                    _faceDetector.FaceDetected -= value;
                }
            }
        }
        public bool IsFaceDetectionEnabled
        {
            get => _isFaceDetectionEnabled;
            set
            {
                SetProperty(ref _isFaceDetectionEnabled, value);
            }
        }

        private FocalLengthBasedDistanceEstimator _distanceEstimator
            = new FocalLengthBasedDistanceEstimator();

        public MainPageViewModel()
        {
            SubscribeEvents();
        }

        public void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (Interlocked.CompareExchange(ref _processingFlag, 1, 0) == 0)
            {
                using (var frame = sender.TryAcquireLatestFrame())
                using (var bmp = frame?.VideoMediaFrame?.SoftwareBitmap)
                {
                    if (bmp != null)
                    {
                        FrameModel.SoftwareBitmap = bmp;
                    }
                }
                Interlocked.Exchange(ref _processingFlag, 0);
            }
        }

        private async void PerformFaceDetection()
        {
            if (!_isFaceDetectionEnabled || _faceDetector == null) return;
            else if (_faceDetector != null && !_faceDetector.IsModelLoaded()) return;

            SoftwareBitmap bmp = FrameModel.SoftwareBitmap;
            if (bmp == null) return;
            Mat img = ImageUtils.ConvertSoftwareBitmapToMat(bmp);
            if (img == null) return;
            await _faceDetector.Detect(img);
            img.Dispose();
        }

        public FaceBoundingBox ScaleBoundingBox(FaceBoundingBox origBB, System.Drawing.Size originalSize)
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

        public float EstimateDistance(FaceBoundingBox bb)
        {
            return _distanceEstimator.ComputeDistance(bb);
        }

        public async Task LoadModelAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_modelFileName}"));
            _faceDetector = new UltraFaceDetector();
            _faceDetector.LoadModel(file);
        }

        private void SubscribeEvents()
        {
            FrameModel.PropertyChanged += _frameModel_PropertyChanged;
            PropertyChanged += MainPageViewModel_PropertyChanged;
        }

        private void MainPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsFaceDetectionEnabled))
            {
                PerformFaceDetection();
            }
        }

        private void _frameModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FrameModel.SoftwareBitmap))
            {
                PerformFaceDetection();
            }
        }

        public async Task InitCameraAsync()
        {
            await CameraControl.InitCameraAsync();
        }

        public async Task StartPreviewAsync()
        {
            CameraControl.FrameArrived += OnFrameArrived;
            await CameraControl.StartPreviewAsync();
        }

        public async Task StopPreviewAsync()
        {
            await CameraControl.StopPreviewAsync();
            CameraControl.FrameArrived -= OnFrameArrived;
        }
    }
}
