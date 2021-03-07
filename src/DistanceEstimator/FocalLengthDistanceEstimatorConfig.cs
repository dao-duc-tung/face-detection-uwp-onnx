using FaceDetection.Utils;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.DistanceEstimator
{
    public class FocalLengthDistanceEstimatorConfig : IConfig
    {
        public float FixedFaceHeightCM { get; set; }
        public float FixedFaceHeightPixel { get; set; }
        public float FixedFaceCameraDistanceCM { get; set; }

        public async Task ReadAsync(StorageFile file)
        {
            var jsonString = await FileIO.ReadTextAsync(file);
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            FixedFaceHeightCM = (float)jsonData[nameof(FixedFaceHeightCM)];
            FixedFaceHeightPixel = (float)jsonData[nameof(FixedFaceHeightPixel)];
            FixedFaceCameraDistanceCM = (float)jsonData[nameof(FixedFaceCameraDistanceCM)];
        }
    }
}
