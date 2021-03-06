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
using FaceDetection.Utils;
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

        public MainPageViewModel()
        {
            this._frameModel.PropertyChanged += _frameModel_PropertyChanged;
            this.PropertyChanged += MainPageViewModel_PropertyChanged;
        }

        private void MainPageViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.IsFaceDetectionEnabled))
            {
                DetectAndDisplayFace();
            }
        }

        private void _frameModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this._frameModel.SoftwareBitmap))
            {
                DetectAndDisplayFace();
            }
        }

        private async void DetectAndDisplayFace()
        {
            if (!this._isFaceDetectionEnabled || this._faceDetector == null) return;
            else if (this._faceDetector != null && !this._faceDetector.IsModelLoaded()) return;

            SoftwareBitmap bmp = this._frameModel.SoftwareBitmap;
            if (bmp == null) return;
            Mat img = UtilFuncs.ConvertSoftwareBitmapToMat(bmp);
            // TODO: Perform face detection by using ONNX Model
            // TODO: Display bounding boxes
        }

        public async Task LoadModelAsync()
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_modelFileName}"));
                _faceDetector = new UltraFaceDetector();
                _faceDetector.LoadModel(file);
            } catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async void CacheImageFromStreamAsync(IRandomAccessStream fileStream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            this._frameModel.SoftwareBitmap = softwareBitmap;
        }
    }
}
