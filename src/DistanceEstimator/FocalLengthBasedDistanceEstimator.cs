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
            this._focalLength = this._fixedFaceCameraDistanceCM * this._fixedFaceHeightPixel / this._fixedFaceHeightCM;
        }

        public float ComputeDistance(CornerBoundingBox bb)
        {
            return this._focalLength * this._fixedFaceHeightCM / bb.Height;
        }
    }
}
