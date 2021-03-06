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

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        private FrameModel _frameModel = new FrameModel();
        private IFaceDetector _faceDetector;
        // TODO: Create Config to load modelFileName
        private string _modelFileName;

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
            if (!this._isFaceDetectionEnabled) return;
            if (this._faceDetector == null)
            {
                await LoadModelAsync();
            }

            SoftwareBitmap bmp = this._frameModel.SoftwareBitmap;
            Mat img = UtilFuncs.ConvertSoftwareBitmapToMat(bmp);
            // TODO: Perform face detection by using ONNX Model
            // TODO: Display bounding boxes
        }

        private async Task LoadModelAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_modelFileName}"));
            _faceDetector.LoadModel(file);
        }

        public async void CacheImageFromStreamAsync(IRandomAccessStream fileStream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            this._frameModel.SoftwareBitmap = softwareBitmap;
        }
    }
}
