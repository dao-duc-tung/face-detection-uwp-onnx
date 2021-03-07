using FaceDetection.Utils;
using Newtonsoft.Json;
using System;
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

        public async Task ReadAsync(StorageFile file)
        {
            var jsonString = await FileIO.ReadTextAsync(file);
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            ModelLocalPath = (string)jsonData[nameof(ModelLocalPath)];
            ConfidenceThreshold = (float)jsonData[nameof(ConfidenceThreshold)];
            IoUThreshold = (float)jsonData[nameof(IoUThreshold)];
            LimitMaxFaces = (int)jsonData[nameof(LimitMaxFaces)];
        }
    }
}
