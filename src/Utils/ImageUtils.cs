using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;

namespace FaceDetection.Utils
{
    public static class ImageUtils
    {
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        public unsafe static Mat ConvertSoftwareBitmapToMat(SoftwareBitmap bmp, int nChannel = 4)
        {
            Mat mat = null;
            try
            {
                if (bmp.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    bmp.BitmapAlphaMode != BitmapAlphaMode.Ignore)
                {
                    bmp = SoftwareBitmap.Convert(bmp, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                }

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
