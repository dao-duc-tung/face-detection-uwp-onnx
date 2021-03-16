using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Controls;

namespace FaceDetection.ViewModels
{
    public class CameraControl
    {
        private bool _isInitialized;
        private MediaCapture _mediaCapture;
        private MediaFrameReader _frameReader;

        public bool IsPreviewing { get; set; }
        public bool MirroringPreview { get; set; }

        public event TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> FrameArrived
        {
            add => _frameReader.FrameArrived += value;
            remove => _frameReader.FrameArrived -= value;
        }

        public async Task StartPreviewAsync()
        {
            if (!_isInitialized) await InitCameraAsync();
            if (!_isInitialized) return;
            try
            {
                await _frameReader.StartAsync();
                IsPreviewing = true;
            }
            catch (FileLoadException)
            {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCaptureCaptureDeviceExclusiveControlStatusChanged;
                Debug.WriteLine("Another app has exclusive access");
            }
        }

        public async Task StopPreviewAsync()
        {
            IsPreviewing = false;
            await _frameReader.StopAsync();
        }

        private async Task InitCameraAsync()
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
            _mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraDevice.Id,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            };

            try
            {
                await _mediaCapture.InitializeAsync(settings);
                // Only mirror the preview if the camera is on the front panel
                MirroringPreview = (cameraDevice?.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front);
                _isInitialized = true;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("The app was denied access to the camera");
                return;
            }

            var frameSource = _mediaCapture.FrameSources.Where(
                source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                .First();
            _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource.Value, MediaEncodingSubtypes.Rgb32);
        }

        private async void MediaCaptureCaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                ContentDialog accessMsg = new ContentDialog()
                {
                    Title = "No access",
                    Content = "Another app has exclusive access",
                    CloseButtonText = "OK"
                };
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !IsPreviewing)
            {
                await StartPreviewAsync();
            }
        }

        public async Task CleanupCameraAsync()
        {
            if (IsPreviewing) await StopPreviewAsync();
            _frameReader?.Dispose();
            _frameReader = null;
            _mediaCapture?.Dispose();
            _mediaCapture = null;
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            return (desiredDevice == null) ? allVideoDevices.FirstOrDefault() : desiredDevice;
        }
    }
}
