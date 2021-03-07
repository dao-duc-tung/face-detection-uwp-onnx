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
                SetProperty(ref _softwareBitmap, value);
            }
        }
    }
}
