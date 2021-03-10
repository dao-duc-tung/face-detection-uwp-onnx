using Emgu.CV;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public class FaceDetectedEventArgs : EventArgs
    {
        public IReadOnlyList<FaceBoundingBox> BoundingBoxes;
        public Size OriginalSize;
    }
    public delegate void FaceDetectedEventHandler(object sender, FaceDetectedEventArgs eventArgs);

    public interface IFaceDetector
    {
        event FaceDetectedEventHandler FaceDetected;

        Task LoadModel(StorageFile file);
        bool IsModelLoaded();
        Task Detect(Mat input);
    }
}
