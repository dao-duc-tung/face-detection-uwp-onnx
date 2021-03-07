using System.Threading.Tasks;
using Windows.Storage;

namespace FaceDetection.Utils
{
    public interface IConfig
    {
        Task ReadAsync(StorageFile file);
    }
}
