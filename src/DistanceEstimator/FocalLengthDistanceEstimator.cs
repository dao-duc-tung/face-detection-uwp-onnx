using FaceDetection.Utils;

namespace FaceDetection.DistanceEstimator
{
    public class FocalLengthDistanceEstimator
    {
        private FocalLengthDistanceEstimatorConfig _config;
        private float _focalLength;

        public FocalLengthDistanceEstimator(FocalLengthDistanceEstimatorConfig config)
        {
            _config = config;
            _focalLength = _config.FixedFaceCameraDistanceCM * _config.FixedFaceHeightPixel / _config.FixedFaceHeightCM;
        }

        public float ComputeDistance(CornerBoundingBox bb)
        {
            return _focalLength * _config.FixedFaceHeightCM / bb.Height;
        }
    }
}
