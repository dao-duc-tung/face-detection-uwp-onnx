
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
            _appConfig = fixture.AppConfig;
            var config = (UltraFaceDetectorConfig)_appConfig.GetConfig(ConfigName.UltraFaceDetector);
            _detector = Activator.CreateInstance(typeof(T), new object[] { config }) as IFaceDetector;
        }

        private async Task<StorageFile> LoadStorageFile(string path)
        {
            var uri = FileUtils.GetUriByLocalFilePath(path);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            return file;
        }

        public async void LoadModel_GoodFile_IsModelLoadedReturnsTrue()
        {
            var config = (UltraFaceDetectorConfig)_appConfig.GetConfig(ConfigName.UltraFaceDetector);
            var modelLocalPath = config.ModelLocalPath;
            var file = await LoadStorageFile(modelLocalPath);

            await _detector.LoadModel(file);

            Assert.True(_detector.IsModelLoaded());
        }

        public async void LoadModel_BadFile_IsModelLoadedReturnsFalse()
        {
            var file = await LoadStorageFile(_badModelPath);

            await _detector.LoadModel(file);

            Assert.False(_detector.IsModelLoaded());
        }

        public async void LoadModel_NullStorageFile_IsModelLoadedReturnsFalse()
        {
            await _detector.LoadModel(null);

            Assert.False(_detector.IsModelLoaded());
        }

        private async Task LoadOnnxModel()
        {
            var config = (UltraFaceDetectorConfig)_appConfig.GetConfig(ConfigName.UltraFaceDetector);
            var modelLocalPath = config.ModelLocalPath;
            var modelFile = await LoadStorageFile(modelLocalPath);
            await _detector.LoadModel(modelFile);
        }

        public async void Detect_ValidFormatImage_FaceDetectedIsRaised()
        {
            await LoadOnnxModel();
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
            await LoadOnnxModel();
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
