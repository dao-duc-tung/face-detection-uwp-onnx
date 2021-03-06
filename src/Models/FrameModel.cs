using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace FaceDetection.Models
{
    public class FrameModel : BaseNotifyPropertyChanged
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
