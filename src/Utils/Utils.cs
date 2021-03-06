using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace FaceDetection.Utils
{
    public static class UtilFuncs
    {
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        public unsafe static Mat ConvertSoftwareBitmapToMat(SoftwareBitmap bmp)
        {
            int nChannel = 4;
            Mat mat = null;
            try
            {
                using (BitmapBuffer buffer = bmp.LockBuffer(BitmapBufferAccessMode.Read))
                {
                    using (var reference = buffer.CreateReference())
                    {
                        byte* data;
                        uint capacity;
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out data, out capacity);
                        mat = new Mat(new System.Drawing.Size(bmp.PixelWidth, bmp.PixelHeight), DepthType.Cv8U, nChannel, (IntPtr)data, (int)capacity / bmp.PixelHeight);
                    }
                }
            } catch (ObjectDisposedException e)
            {
                Debug.WriteLine(e.Message);
            }
            return mat;
        }
    }
}
