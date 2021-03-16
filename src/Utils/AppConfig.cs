using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.Utils
{
    public class AppConfig : Singleton<AppConfig>
    {
        private Dictionary<string, IConfig> _configs = new Dictionary<string, IConfig>();

        public async Task RegisterConfig<T>(string configName, StorageFile file) where T : IConfig
        {
            var config = Activator.CreateInstance(typeof(T), true) as IConfig;
            await config.ReadAsync(file);
            _configs[configName] = config;
        }

        public IConfig GetConfig(string configName)
        {
            return _configs.GetValueOrDefault(configName, null);
        }
    }
}
