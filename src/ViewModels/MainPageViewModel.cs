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
        private FrameModel _frameModel = new FrameModel();

        private bool _isInitialized;
        public bool IsPreviewing { get; set; }
        public MediaCapture MediaCapture { get; set; }

        private int _processingFlag;
        private MediaFrameReader _frameReader;

        // TODO: Create Config to load modelFileName
        private string _modelFileName = "version-RFB-320.onnx";

        private IFaceDetector _faceDetector;
        private bool _isFaceDetectionEnabled = false;
        public bool IsFaceDetectionEnabled {
            get => this._isFaceDetectionEnabled;
            set
            {
                SetProperty(ref this._isFaceDetectionEnabled, value);
            }
        }
        public event FaceDetectedEventHandler FaceDetected;

        private FocalLengthBasedDistanceEstimator _distanceEstimator
            = new FocalLengthBasedDistanceEstimator();

        public MainPageViewModel()
        {
            this.SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            this._frameModel.PropertyChanged += _frameModel_PropertyChanged;
            this.PropertyChanged += MainPageViewModel_PropertyChanged;
        }

        private void MainPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.IsFaceDetectionEnabled))
            {
                PerformFaceDetection();
            }
        }

        private void _frameModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this._frameModel.SoftwareBitmap))
            {
                PerformFaceDetection();
            }
        }

        public void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (Interlocked.CompareExchange(ref _processingFlag, 1, 0) == 0)
            {
                try
                {
                    using (var frame = sender.TryAcquireLatestFrame())
                    using (var bmp = frame?.VideoMediaFrame?.SoftwareBitmap)
                    {
                        if (bmp != null)
                        {
                            this._frameModel.SoftwareBitmap = bmp;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref _processingFlag, 0);
                }
            }
        }

        public async Task InitCameraAsync()
        {
            if (!this._isInitialized)
            {
                await InitMediaCaptureAsync();
            }
            if (!this._isInitialized) return;

            await InitFrameReaderAsync();
        }

        public async Task StartPreviewAsync()
        {
            try
            {
                await this.MediaCapture.StartPreviewAsync();
                this.IsPreviewing = true;
            }
            catch (FileLoadException)
            {
                Debug.WriteLine("Another app has exclusive access");
            }
        }

        public async Task StopPreviewAsync()
        {
            this.IsPreviewing = false;
            await this.MediaCapture.StopPreviewAsync();
            await this._frameReader.StopAsync();
        }

        private async Task InitMediaCaptureAsync()
        {
            var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Front);
            if (cameraDevice == null)
            {
                Debug.WriteLine("No camera device found");
                return;
            }

            // Set Memory Preference to CPU to guarantee SoftwareBitmap property is non-null
            // Otherwise use Direct3DSurface property
            // https://docs.microsoft.com/en-us/uwp/api/windows.media.capture.frames.videomediaframe.softwarebitmap?view=winrt-19041
            this.MediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraDevice.Id, MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            try
            {
                await this.MediaCapture.InitializeAsync(settings);
                this._isInitialized = true;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("The app was denied access to the camera");
            }
        }

        private async Task InitFrameReaderAsync()
        {
            var frameSource = this.MediaCapture.FrameSources.Where(
                source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                .First();
            this._frameReader = await this.MediaCapture.CreateFrameReaderAsync(frameSource.Value, MediaEncodingSubtypes.Rgb32);
                
            // Setup handler for frames
            this._frameReader.FrameArrived += OnFrameArrived;
            await this._frameReader.StartAsync();    
        }

        private async void PerformFaceDetection()
        {
            if (!this._isFaceDetectionEnabled || this._faceDetector == null) return;
            else if (this._faceDetector != null && !this._faceDetector.IsModelLoaded()) return;

            SoftwareBitmap bmp = this._frameModel.SoftwareBitmap;
            if (bmp == null) return;
            try
            {
                Mat img = UtilFuncs.ConvertSoftwareBitmapToMat(bmp);
                await this._faceDetector.Detect(img);
                img.Dispose();
            } catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async Task LoadModelAsync()
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_modelFileName}"));
                this._faceDetector = new UltraFaceDetector();
                this._faceDetector.LoadModel(file);
                this._faceDetector.FaceDetected += _faceDetector_FaceDetected;
            } catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void _faceDetector_FaceDetected(object sender, IReadOnlyList<FaceBoundingBox> faceBoundingBoxes, System.Drawing.Size originalSize)
        {
            if (this.FaceDetected == null) return;
            this.FaceDetected(sender, faceBoundingBoxes, originalSize);
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
            return this._distanceEstimator.ComputeDistance(bb);
        }

        public async void CacheImageFromStreamAsync(IRandomAccessStream fileStream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            this._frameModel.SoftwareBitmap = softwareBitmap;
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            return (desiredDevice == null) ? allVideoDevices.FirstOrDefault() : desiredDevice;
        }
    }
}
