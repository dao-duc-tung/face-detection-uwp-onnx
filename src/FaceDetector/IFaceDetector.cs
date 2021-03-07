using Emgu.CV;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public delegate void FaceDetectedEventHandler(object sender, IReadOnlyList<FaceBoundingBox> faceBoundingBoxes, Size originalSize);

    public interface IFaceDetector
    {
        event FaceDetectedEventHandler FaceDetected;
        Task LoadModel(StorageFile file);
        bool IsModelLoaded();
        Task Detect(Mat input);
    }
}
