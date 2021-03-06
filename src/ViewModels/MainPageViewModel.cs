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

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        private FrameModel _frameModel = new FrameModel();
        private IFaceDetector _faceDetector;
        // TODO: Create Config to load modelFileName
        private string _modelFileName = "version-RFB-320.onnx";

        private bool _isFaceDetectionEnabled = false;
        public bool IsFaceDetectionEnabled {
            get => this._isFaceDetectionEnabled;
            set
            {
                SetProperty(ref this._isFaceDetectionEnabled, value);
            }
        }
        public event FaceDetectedEventHandler FaceDetected;

        public MainPageViewModel()
        {
            this.SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            this._frameModel.PropertyChanged += _frameModel_PropertyChanged;
            this.PropertyChanged += MainPageViewModel_PropertyChanged;
        }

        private void MainPageViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.IsFaceDetectionEnabled))
            {
                PerformFaceDetection();
            }
        }

        private void _frameModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this._frameModel.SoftwareBitmap))
            {
                PerformFaceDetection();
            }
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
            
            int origWidth = originalSize.Width;
            int origHeight = originalSize.Height;

            var scaledBBs = new List<FaceBoundingBox>();
            for (int i = 0; i < faceBoundingBoxes.Count; ++i)
            {
                FaceBoundingBox bb = faceBoundingBoxes[i];
                bb.X0 *= origWidth;
                bb.X1 *= origWidth;
                bb.Y0 *= origHeight;
                bb.Y1 *= origHeight;
                scaledBBs.Add(bb);
            }
            this.FaceDetected(sender, scaledBBs, originalSize);
        }

        public async void CacheImageFromStreamAsync(IRandomAccessStream fileStream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            this._frameModel.SoftwareBitmap = softwareBitmap;
        }
    }
}
