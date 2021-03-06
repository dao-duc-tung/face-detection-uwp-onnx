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

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        private FrameModel _frameModel = new FrameModel();

        public async void CacheImageFromStreamAsync(IRandomAccessStream fileStream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            this._frameModel.SoftwareBitmap = softwareBitmap;
        }
    }
}
