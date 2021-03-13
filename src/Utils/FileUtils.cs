using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.Utils
{
    public static class FileUtils
    {
        public static Uri GetUriByLocalFilePath(string localFilePath)
        {
            return new Uri($"ms-appx:///{localFilePath}");
        }

        public static async Task ReadConfigFile<T>(string configName, string configLocalPath) where T : IConfig
        {
            var uri = GetUriByLocalFilePath(configLocalPath);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            await AppConfig.Instance.RegisterConfig<T>(configName, file);
        }
    }
}
