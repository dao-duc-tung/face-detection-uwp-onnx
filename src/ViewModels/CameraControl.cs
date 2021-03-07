using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace FaceDetection.ViewModels
{
    public class CameraControl
    {
        private bool _isInitialized;
        private MediaFrameReader _frameReader;

        public bool IsPreviewing { get; set; }
        public MediaCapture MediaCapture { get; set; }

        public event TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> FrameArrived
        {
            add
            {
                lock(_frameReader)
                {
                    _frameReader.FrameArrived += value;
                }
            }
            remove
            {
                lock (_frameReader)
                {
                    _frameReader.FrameArrived -= value;
                }
            }
        }

        public async Task InitCameraAsync()
        {
            if (!_isInitialized)
            {
                await InitMediaCaptureAsync();
            }
        }

        public async Task StartPreviewAsync()
        {
            try
            {
                await _frameReader.StartAsync();
                IsPreviewing = true;
            }
            catch (FileLoadException)
            {
                Debug.WriteLine("Another app has exclusive access");
            }
        }

        public async Task StopPreviewAsync()
        {
            IsPreviewing = false;
            await _frameReader.StopAsync();
        }

        private async Task InitMediaCaptureAsync()
        {
            var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Front);
            if (cameraDevice == null)
            {
                Debug.WriteLine("No camera device found");
                return;
            }

            // Set Memory Preference to CPU to guarantee SoftwareBitmap property is non-null
            // Otherwise use Direct3DSurface property
            // https://docs.microsoft.com/en-us/uwp/api/windows.media.capture.frames.videomediaframe.softwarebitmap?view=winrt-19041
            MediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraDevice.Id,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            };

            try
            {
                await MediaCapture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("The app was denied access to the camera");
                return;
            }

            var frameSource = MediaCapture.FrameSources.Where(
                source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                .First();
            _frameReader = await MediaCapture.CreateFrameReaderAsync(frameSource.Value, MediaEncodingSubtypes.Rgb32);
            _isInitialized = true;
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            return (desiredDevice == null) ? allVideoDevices.FirstOrDefault() : desiredDevice;
        }
    }
}
