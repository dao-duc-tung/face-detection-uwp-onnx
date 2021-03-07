using FaceDetection.Utils;

namespace FaceDetection.DistanceEstimator
{
    public class FocalLengthDistanceEstimator
    {
        private FocalLengthDistanceEstimatorConfig _config;
        private float _fixedFaceHeightCM;
        private float _focalLength;

        public FocalLengthDistanceEstimator(FocalLengthDistanceEstimatorConfig config)
        {
            _config = config;
            _fixedFaceHeightCM = _config.FixedFaceHeightCM;
            _focalLength = _config.FixedFaceCameraDistanceCM * _config.FixedFaceHeightPixel / _fixedFaceHeightCM;
        }

        public float ComputeDistance(CornerBoundingBox bb)
        {
            return _focalLength * _fixedFaceHeightCM / bb.Height;
        }
    }
}
