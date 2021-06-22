using FaceDetection.DistanceEstimator;
using FaceDetection.FaceDetector;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class TestFixture : IDisposable
    {
        public AppConfig AppConfig;

        public TestFixture()
        {
            Task.Run(LoadAppConfig).Wait();
        }

        private async void LoadAppConfig()
        {
            await FileUtils.ReadConfigFile<UltraFaceDetectorConfig>(ConfigName.UltraFaceDetector, ConfigLocalPath.UltraFaceDetector);
            await FileUtils.ReadConfigFile<UltraFaceDetectorConfig>(ConfigName.UltraFaceDetector2, ConfigLocalPath.UltraFaceDetector2);
            await FileUtils.ReadConfigFile<UltraFaceDetectorConfig>(ConfigName.UltraFaceDetector3, ConfigLocalPath.UltraFaceDetector3);
            await FileUtils.ReadConfigFile<FocalLengthDistanceEstimatorConfig>(ConfigName.FocalLengthDistanceEstimator, ConfigLocalPath.FocalLengthDistanceEstimator);
            AppConfig = AppConfig.Instance;
        }

        public void Dispose()
        {
        }
    }
}
