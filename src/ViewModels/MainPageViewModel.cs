using Emgu.CV;
using FaceDetection.Models;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using FaceDetection.FaceDetector;
using System.Diagnostics;
using Windows.Media.Capture.Frames;
using System.Threading;
using FaceDetection.DistanceEstimator;
using System.ComponentModel;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.Foundation;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Storage.Pickers;

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        private FrameModel _frameModel { get; } = FrameModel.Instance;
        private CameraControl _cameraControl { get; } = new CameraControl();
        private FaceDetectionControl _faceDetectionControl { get; } = new FaceDetectionControl();
        // TODO: Create Config to load modelFileName
        private string _modelFileName = "version-RFB-320.onnx";
        private int _processingFlag;

        private Image _imageControl;
        public Image ImageControl
        {
            get => _imageControl;
            set
            {
                SetProperty(ref _imageControl, value);
            }
        }

        private CaptureElement _previewControl;
        public CaptureElement PreviewControl
        {
            get => _previewControl;
            set
            {
                SetProperty(ref _previewControl, value);
            }
        }

        private Canvas _facesCanvas;
        private FocalLengthBasedDistanceEstimator _distanceEstimator
            = new FocalLengthBasedDistanceEstimator();

        public ICommand ImageControlLoaded { get; set; }
        public ICommand PreviewControlLoaded { get; set; }
        public ICommand FacesCanvasLoaded { get; set; }

        public ICommand LoadPhotoCmd { get; set; }
        public ICommand ToggleCameraCmd { get; set; }
        public ICommand ToggleFaceDetectionCmd { get; set; }

        public MainPageViewModel()
        {
            BindCommands();
            SubscribeEvents();
            Task.Run(LoadModelAsync);
        }

        private async Task LoadModelAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_modelFileName}"));
            await _faceDetectionControl.LoadModelAsync(file);
        }

        private void BindCommands()
        {
            ImageControlLoaded = new DelegateCommand<Image>(imageControl => _imageControl = imageControl);
            PreviewControlLoaded = new DelegateCommand<CaptureElement>(previewControl => _previewControl = previewControl);
            FacesCanvasLoaded = new DelegateCommand<Canvas>(facesCanvas => _facesCanvas = facesCanvas);
            LoadPhotoCmd = new DelegateCommand(async () => await LoadPhoto());
            ToggleCameraCmd = new DelegateCommand(async () => await ToggleCamera());
            ToggleFaceDetectionCmd = new DelegateCommand(async () => await ToggleFaceDetection());
        }

        private void SubscribeEvents()
        {
            _frameModel.PropertyChanged += _frameModel_PropertyChanged;
        }

        private void _frameModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_frameModel.SoftwareBitmap)) RunFaceDetection();
        }

        private async Task InitCameraAsync() => await _cameraControl.InitCameraAsync();

        private async Task StartPreviewAsync()
        {
            _cameraControl.FrameArrived += OnFrameArrived;
            await _cameraControl.StartPreviewAsync();
        }

        private async Task StopPreviewAsync()
        {
            await _cameraControl.StopPreviewAsync();
            _cameraControl.FrameArrived -= OnFrameArrived;
        }

        private async Task LoadPhoto()
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;
            using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                if (_cameraControl.IsPreviewing) await StopPreviewAsync();

                BitmapImage bmp = new BitmapImage();
                bmp.DecodePixelHeight = (int)ImageControl.Height;
                bmp.DecodePixelWidth = (int)ImageControl.Width;
                await bmp.SetSourceAsync(fileStream);
                ImageControl.Source = bmp;
                await ClearFacesCanvas();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                _frameModel.SoftwareBitmap = softwareBitmap;
            }
        }

        private async Task ToggleCamera()
        {
            if (!_cameraControl.IsPreviewing)
            {
                ImageControl.Source = null;
                await InitCameraAsync();
                PreviewControl.Source = _cameraControl.MediaCapture;
                await StartPreviewAsync();
            }
            else
            {
                PreviewControl.Source = null;
                await StopPreviewAsync();
            }
            await ClearFacesCanvas();
        }

        private async Task ToggleFaceDetection()
        {
            await ClearFacesCanvas();
            _faceDetectionControl.IsFaceDetectionEnabled = !_faceDetectionControl.IsFaceDetectionEnabled;
            if (_faceDetectionControl.IsFaceDetectionEnabled)
            {
                _faceDetectionControl.FaceDetected += _faceDetector_FaceDetected;
                RunFaceDetection();
            } else
            {
                _faceDetectionControl.FaceDetected -= _faceDetector_FaceDetected;
            }
        }

        private void RunFaceDetection()
        {
            SoftwareBitmap bmp = _frameModel.SoftwareBitmap;
            if (bmp == null) return;
            _faceDetectionControl.RunFaceDetection(bmp);
        }

        private async void _faceDetector_FaceDetected(object sender, IReadOnlyList<FaceBoundingBox> faceBoundingBoxes, System.Drawing.Size originalSize)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() => HighlightDetectedFaces(faceBoundingBoxes, originalSize));
        }

        private void HighlightDetectedFaces(IReadOnlyList<FaceBoundingBox> faces, System.Drawing.Size originalSize)
        {
            _facesCanvas.Children.Clear();
            for (int i = 0; i < faces.Count; ++i)
            {
                var bb = FaceDetectionControl.ScaleBoundingBox(faces[i], originalSize);
                Rectangle faceBB = ConvertPreviewToUiRectangle(bb, originalSize);
                faceBB.StrokeThickness = 2;
                faceBB.Stroke = new SolidColorBrush(Colors.LimeGreen);
                _facesCanvas.Children.Add(faceBB);

                TextBlock txtBlk = new TextBlock();
                txtBlk.Text = _distanceEstimator.ComputeDistance(faces[i]).ToString("n0") + " cm";
                txtBlk.Foreground = new SolidColorBrush(Colors.LimeGreen);
                Canvas.SetLeft(txtBlk, Canvas.GetLeft(faceBB) + 5);
                Canvas.SetTop(txtBlk, Canvas.GetTop(faceBB));
                _facesCanvas.Children.Add(txtBlk);
            }
        }

        public Rectangle ConvertPreviewToUiRectangle(FaceBoundingBox faceBox, System.Drawing.Size actualContentSize)
        {
            var result = new Rectangle();
            double streamWidth = actualContentSize.Width;
            double streamHeight = actualContentSize.Height;

            //If there is no available information about the preview, return an empty rectangle, as re - scaling to the screen coordinates will be impossible
            //  Similarly, if any of the dimensions is zero(which would only happen in an error case) return an empty rectangle
            if (streamWidth == 0 || streamHeight == 0) return result;

            //Get the rectangle that is occupied by the actual video feed
            var previewInUI = GetDisplayRectInControl(actualContentSize);
            var scaleWidth = previewInUI.Width / streamWidth;
            var scaleHeight = previewInUI.Height / streamHeight;

            //Scale the width and height from preview stream coordinates to window coordinates
            result.Width = faceBox.Width * scaleWidth;
            result.Height = faceBox.Height * scaleHeight;

            // Scale the X and Y coordinates from preview stream coordinates to window coordinates
            var x = previewInUI.X + faceBox.X0 * scaleWidth;
            var y = previewInUI.Y + faceBox.Y0 * scaleHeight;
            Canvas.SetLeft(result, x);
            Canvas.SetTop(result, y);
            return result;
        }

        public Rect GetDisplayRectInControl(System.Drawing.Size actualContentSize)
        {
            var result = new Rect();
            // In case this function is called before everything is initialized correctly, return an empty result
            if (PreviewControl == null || PreviewControl.ActualHeight < 1 || PreviewControl.ActualWidth < 1 ||
                actualContentSize.Height == 0 || actualContentSize.Width == 0) return result;

            var streamWidth = actualContentSize.Width;
            var streamHeight = actualContentSize.Height;

            // Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
            result.Width = PreviewControl.ActualWidth;
            result.Height = PreviewControl.ActualHeight;

            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((PreviewControl.ActualWidth / PreviewControl.ActualHeight > streamWidth / (double)streamHeight))
            {
                var scale = PreviewControl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;
                result.X = (PreviewControl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = PreviewControl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;
                result.Y = (PreviewControl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }
            return result;
        }

        private async Task ClearFacesCanvas() => await DispatcherHelper.ExecuteOnUIThreadAsync(() => _facesCanvas.Children.Clear());

        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (Interlocked.CompareExchange(ref _processingFlag, 1, 0) == 0)
            {
                using (var frame = sender.TryAcquireLatestFrame())
                using (var bmp = frame?.VideoMediaFrame?.SoftwareBitmap)
                {
                    if (bmp != null)
                    {
                        _frameModel.SoftwareBitmap = bmp;
                    }
                }
                Interlocked.Exchange(ref _processingFlag, 0);
            }
        }
    }
}
