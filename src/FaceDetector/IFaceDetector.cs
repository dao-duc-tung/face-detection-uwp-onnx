using Emgu.CV;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public interface IFaceDetector
    {
        void LoadModel(StorageFile file);
        IReadOnlyList<FaceBoundingBox> Detect(Mat input);
    }
}
