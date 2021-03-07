using FaceDetection.Utils;
using Windows.Graphics.Imaging;

namespace FaceDetection.Models
{
    public class FrameModel : SingletonNotifyPropertyChanged<FrameModel>
    {
        private SoftwareBitmap _softwareBitmap;
        public SoftwareBitmap SoftwareBitmap
        {
            get => _softwareBitmap;
            set
            {
                if (value != null)
                {
                    if (value.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                    || value.BitmapAlphaMode != BitmapAlphaMode.Ignore
                    || value.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                    {
                        value = SoftwareBitmap.Convert(value, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                    }
                }
                SetProperty(ref _softwareBitmap, value);
            }
        }
    }
}
