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

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        private FrameModel _frameModel = new FrameModel();

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

        private void DetectAndDisplayFace()
        {
            if (!this._isFaceDetectionEnabled) return;

            SoftwareBitmap bmp = this._frameModel.SoftwareBitmap;
            Mat img = UtilFuncs.ConvertSoftwareBitmapToMat(bmp);
            // TODO: Perform face detection by using ONNX Model
            // TODO: Display bounding boxes
        }

        public async void CacheImageFromStreamAsync(IRandomAccessStream fileStream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            this._frameModel.SoftwareBitmap = softwareBitmap;
        }
    }
}
