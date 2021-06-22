using FaceDetection.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.FaceDetector
{
    public class UltraFaceDetectorConfig : IConfig
    {
        public string ModelLocalPath { get; set; }
        public float ConfidenceThreshold { get; set; }
        public float IoUThreshold { get; set; }
        public int LimitMaxFaces { get; set; }

        private static Dictionary<Type, string> _configNameDict = new Dictionary<Type, string>()
        {
            { typeof(UltraFaceDetector), ConfigName.UltraFaceDetector },
            { typeof(UltraFaceDetector2), ConfigName.UltraFaceDetector2 },
            { typeof(UltraFaceDetector3), ConfigName.UltraFaceDetector3 },
        };

        public async Task ReadAsync(StorageFile file)
        {
            var jsonString = await FileIO.ReadTextAsync(file);
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            ModelLocalPath = (string)jsonData[nameof(ModelLocalPath)];
            ConfidenceThreshold = (float)jsonData[nameof(ConfidenceThreshold)];
            IoUThreshold = (float)jsonData[nameof(IoUThreshold)];
            LimitMaxFaces = (int)jsonData[nameof(LimitMaxFaces)];
        }

        public static string GetConfigNameByType(Type detectorType)
        {
            if (!_configNameDict.ContainsKey(detectorType))
            {
                throw new KeyNotFoundException(detectorType + " not found!");
            }
            return _configNameDict[detectorType];
        }
    }
}
