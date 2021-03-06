using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceDetection.DistanceEstimator
{
    public class FocalLengthBasedDistanceEstimator
    {
        // TODO: Read from config
        private float _fixedFaceHeightCM = 20f;
        private float _fixedFaceHeightPixel = 0.75f; // output of FaceDetector
        private float _fixedFaceCameraDistanceCM = 30f;
        private float _focalLength;

        public FocalLengthBasedDistanceEstimator()
        {
            _focalLength = _fixedFaceCameraDistanceCM * _fixedFaceHeightPixel / _fixedFaceHeightCM;
        }

        public float ComputeDistance(CornerBoundingBox bb)
        {
            return _focalLength * _fixedFaceHeightCM / bb.Height;
        }
    }
}
