using FaceDetection.DistanceEstimator;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    public class FocalLengthDistanceEstimatorTest : IClassFixture<TestFixture>
    {
        private AppConfig _appConfig;
        private FocalLengthDistanceEstimator _estimator;

        private CornerBoundingBox validBB = new CornerBoundingBox() { X0 = 0, Y0 = 0, X1 = 0.1f, Y1 = 0.3f };
        private const float expectedDist1 = 75;

        private CornerBoundingBox notValidBB = new CornerBoundingBox() { X0 = -1, Y0 = -2, X1 = -3, Y1 = -4 };

        public FocalLengthDistanceEstimatorTest(TestFixture fixture)
        {
            while (fixture.AppConfig == null);
            _appConfig = fixture.AppConfig;
            var config = (FocalLengthDistanceEstimatorConfig)_appConfig.GetConfig(ConfigName.FocalLengthDistanceEstimator);
            _estimator = new FocalLengthDistanceEstimator(config);
        }

        [Fact]
        public void ComputeDistance_ValidBoundingBoxes_ReturnsCorrectValue()
        {
            var dist = _estimator.ComputeDistance(validBB);
            Assert.Equal(expectedDist1, dist);
        }

        [Fact]
        public void ComputeDistance_NotValidBoundingBox_Returns0()
        {
            var dist = _estimator.ComputeDistance(notValidBB);
            Assert.Equal(0, dist);
        }

        [Fact]
        public void ComputeDistance_NullBoundingBox_Returns0()
        {
            var dist = _estimator.ComputeDistance(null);
            Assert.Equal(0, dist);
        }
    }
}
