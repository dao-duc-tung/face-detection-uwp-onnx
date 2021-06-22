
using Emgu.CV;
using FaceDetection.FaceDetector;
using FaceDetection.Utils;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Xunit;

namespace UnitTest
{
    public class UltraFaceDetectorTest<T> where T : IFaceDetector
    {
        private AppConfig _appConfig;
        private IFaceDetector _detector;

        private const string _badModelPath = "Assets/BadModel.onnx";
        private const string _validFormatImagePath = "Assets/1.jpg";

        public UltraFaceDetectorTest(TestFixture fixture)
        {
            _detector = Activator.CreateInstance(typeof(T)) as IFaceDetector;
            _appConfig = fixture.AppConfig;
            var configName = UltraFaceDetectorConfig.GetConfigNameByType(_detector.GetType());
            var config = (UltraFaceDetectorConfig)_appConfig.GetConfig(configName);
            _detector.LoadConfig(config);
        }

        private async Task<StorageFile> LoadStorageFile(string path)
        {
            var uri = FileUtils.GetUriByLocalFilePath(path);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            return file;
        }

        public async void LoadModel_GoodFile_IsModelLoadedReturnsTrue()
        {
            await _detector.LoadModel();

            Assert.True(_detector.IsModelLoaded());
        }


        public async void Detect_ValidFormatImage_FaceDetectedIsRaised()
        {
            await _detector.LoadModel();
            var file = await LoadStorageFile(_validFormatImagePath);
            Mat img;
            using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                SoftwareBitmap bmp = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                img = ImageUtils.ConvertSoftwareBitmapToMat(bmp);
            }
            _detector.FaceDetected += _detector_FaceDetectedIsRaisedCorrectly;

            await _detector.Detect(img);

            _detector.FaceDetected -= _detector_FaceDetectedIsRaisedCorrectly;
        }

        private void _detector_FaceDetectedIsRaisedCorrectly(object sender, FaceDetectedEventArgs eventArgs)
        {
            Assert.NotNull(eventArgs);
        }

        public async void Detect_NullImage_FaceDetectedIsNotRaised()
        {
            await _detector.LoadModel();
            _detector.FaceDetected += _detector_FaceDetectedIsNotRaised;

            await _detector.Detect(null);

            _detector.FaceDetected -= _detector_FaceDetectedIsNotRaised;
        }

        private void _detector_FaceDetectedIsNotRaised(object sender, FaceDetectedEventArgs eventArgs)
        {
            throw new Exception("Should not reach here!");
        }
    }
}
