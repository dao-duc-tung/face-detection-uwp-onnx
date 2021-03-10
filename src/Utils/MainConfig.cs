using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.Utils
{
    public class MainConfig : IConfig
    {
        public bool IsDebug { get; set; }
        public bool IsBoxSizeDisplayed { get; set; }

        public async Task ReadAsync(StorageFile file)
        {
            var jsonString = await FileIO.ReadTextAsync(file);
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            IsDebug = (int)jsonData[nameof(IsDebug)] == 1;
            IsBoxSizeDisplayed = (int)jsonData[nameof(IsBoxSizeDisplayed)] == 1;
        }
    }
}
