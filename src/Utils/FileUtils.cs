using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceDetection.Utils
{
    public static class FileUtils
    {
        public static Uri GetUriByLocalFilePath(string localFilePath)
        {
            return new Uri($"ms-appx:///{localFilePath}");
        }
    }
}
