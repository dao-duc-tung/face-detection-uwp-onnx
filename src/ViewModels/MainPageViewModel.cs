using FaceDetection.DistanceEstimator;
using FaceDetection.FaceDetector;
using FaceDetection.Models;
using FaceDetection.Utils;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace FaceDetection.ViewModels
{
    public class MainPageViewModel : BaseNotifyPropertyChanged
    {
        private FrameModel _frameModel { get; } = FrameModel.Instance;
        private CameraControl _cameraControl { get; } = new CameraControl();
        private FaceDetectionControl _faceDetectionControl { get; } = new FaceDetectionControl();
        private int _processingFlag;
        private SolidColorBrush _canvasObjectColor = new SolidColorBrush(Colors.LimeGreen);

        private Image _imageControl;
        public Image ImageControl
        {
            get => _imageControl;
            set
            {
                SetProperty(ref _imageControl, value);
            }
        }

        private Canvas _facesCanvas;
        private FocalLengthDistanceEstimator _distanceEstimator;

        public ICommand ImageControlLoaded { get; set; }
        public ICommand FacesCanvasLoaded { get; set; }
        public ICommand LoadPhotoCmd { get; set; }
        public ICommand ToggleCameraCmd { get; set; }
        public ICommand ToggleFaceDetectionCmd { get; set; }

        public MainPageViewModel()
        {
            BindCommands();
            SubscribeEvents();
            Task.Run(LoadModelAsync).Wait();
            InitDistanceEstimator();
        }

        private async Task LoadModelAsync()
        {
            var config = (UltraFaceDetectorConfig)AppConfig.Instance.GetConfig(ConfigName.UltraFaceDetector);
            var modelLocalPath = config.ModelLocalPath;
            var uri = FileUtils.GetUriByLocalFilePath(modelLocalPath);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            await _faceDetectionControl.LoadModelAsync(file);
        }

        private void InitDistanceEstimator()
        {
            var config = (FocalLengthDistanceEstimatorConfig)AppConfig.Instance.GetConfig(ConfigName.FocalLengthDistanceEstimator);
            _distanceEstimator = new FocalLengthDistanceEstimator(config);
        }

        private void BindCommands()
        {
            ImageControlLoaded = new DelegateCommand<Image>(imageControl => _imageControl = imageControl);
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
            if (e.PropertyName == nameof(_frameModel.SoftwareBitmap))
            {
                RunFaceDetection();
                Task.Run(UpdateImageSource);
            }
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
                if (_cameraControl.IsPreviewing) await TurnOffCameraPreview();
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                _frameModel.SoftwareBitmap = softwareBitmap;
            }
        }

        private async Task UpdateImageSource()
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(async () => {
                if (ImageControl.Source == null) ImageControl.Source = new SoftwareBitmapSource();
                await ((SoftwareBitmapSource)ImageControl.Source).SetBitmapAsync(_frameModel.SoftwareBitmap);
            });
        }

        private async Task ToggleCamera()
        {
            if (!_cameraControl.IsPreviewing) await TurnOnCameraPreview();
            else await TurnOffCameraPreview();
            await ClearFacesCanvas();
        }

        private async Task TurnOnCameraPreview()
        {
            await InitCameraAsync();
            await StartPreviewAsync();
        }

        private async Task TurnOffCameraPreview()
        {
            await StopPreviewAsync();
            await DispatcherHelper.ExecuteOnUIThreadAsync(() => { ImageControl.Source = null; });
        }

        private async Task ToggleFaceDetection()
        {
            _faceDetectionControl.IsFaceDetectionEnabled = !_faceDetectionControl.IsFaceDetectionEnabled;
            if (_faceDetectionControl.IsFaceDetectionEnabled)
            {
                _faceDetectionControl.FaceDetected += _faceDetector_FaceDetected;
                RunFaceDetection();
            } else
            {
                _faceDetectionControl.FaceDetected -= _faceDetector_FaceDetected;
            }
            await ClearFacesCanvas();
        }

        private void RunFaceDetection()
        {
            var bmp = _frameModel.SoftwareBitmap;
            if (bmp == null) return;
            _faceDetectionControl.RunFaceDetection(bmp);
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

        private async void _faceDetector_FaceDetected(object sender, IReadOnlyList<FaceBoundingBox> faceBoundingBoxes, System.Drawing.Size originalSize)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() => HighlightDetectedFaces(faceBoundingBoxes, originalSize));
        }

        private void HighlightDetectedFaces(IReadOnlyList<FaceBoundingBox> faces, System.Drawing.Size originalSize)
        {
            _facesCanvas.Children.Clear();
            var IsBoxSizeDisplayed = ((MainConfig)AppConfig.Instance.GetConfig(ConfigName.Main)).IsBoxSizeDisplayed;
            for (int i = 0; i < faces.Count; ++i)
            {
                var bb = FaceDetectionControl.ScaleBoundingBox(faces[i], originalSize);
                Rectangle faceBB = ConvertPreviewToUiRectangle(bb, originalSize);
                faceBB.StrokeThickness = 2;
                faceBB.Stroke = _canvasObjectColor;
                _facesCanvas.Children.Add(faceBB);

                TextBlock distance = new TextBlock();
                distance.Text = _distanceEstimator.ComputeDistance(faces[i]).ToString("n0") + " cm";
                distance.Foreground = _canvasObjectColor;
                Canvas.SetLeft(distance, Canvas.GetLeft(faceBB) + 5);
                Canvas.SetTop(distance, Canvas.GetTop(faceBB));
                _facesCanvas.Children.Add(distance);

                if (_faceDetectionControl.IsFaceDetectionEnabled)
                {
                    TextBlock faceDetectionFPS = new TextBlock();
                    faceDetectionFPS.Text = $"Face Detection FPS: {_faceDetectionControl.FPS.ToString("n2")}";
                    faceDetectionFPS.Foreground = _canvasObjectColor;
                    Canvas.SetLeft(faceDetectionFPS, 20);
                    Canvas.SetTop(faceDetectionFPS, 20);
                    _facesCanvas.Children.Add(faceDetectionFPS);
                }

                if (IsBoxSizeDisplayed)
                {
                    TextBlock origSize = new TextBlock();
                    origSize.Text = $"Orig Size: {faces[i].Width.ToString("n2")} x {faces[i].Height.ToString("n2")}";
                    origSize.Foreground = _canvasObjectColor;
                    Canvas.SetLeft(origSize, Canvas.GetLeft(faceBB));
                    Canvas.SetTop(origSize, Canvas.GetTop(faceBB) - 40);
                    _facesCanvas.Children.Add(origSize);

                    TextBlock scaledSize = new TextBlock();
                    scaledSize.Text = $"Scaled Size: {faceBB.Width.ToString("n2")} x {faceBB.Height.ToString("n2")}";
                    scaledSize.Foreground = _canvasObjectColor;
                    Canvas.SetLeft(scaledSize, Canvas.GetLeft(faceBB));
                    Canvas.SetTop(scaledSize, Canvas.GetTop(faceBB) - 20);
                    _facesCanvas.Children.Add(scaledSize);

                }
            }
        }

        private Rectangle ConvertPreviewToUiRectangle(FaceBoundingBox faceBox, System.Drawing.Size streamSize)
        {
            var result = new Rectangle();
            double streamWidth = streamSize.Width;
            double streamHeight = streamSize.Height;

            // If there is no available information about the preview, return an empty rectangle, as re - scaling to the screen coordinates will be impossible
            // Similarly, if any of the dimensions is zero(which would only happen in an error case) return an empty rectangle
            if (streamWidth == 0 || streamHeight == 0) return result;

            // Get the rectangle that is occupied by the actual video feed
            var currWindowFrame = Window.Current.Bounds;
            var windowSize = new Size(currWindowFrame.Width, currWindowFrame.Height);
            var previewInUI = GetDisplayRectInControl(streamSize, windowSize);
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

        private Rect GetDisplayRectInControl(System.Drawing.Size streamSize, Size windowSize)
        {
            var result = new Rect();
            // In case this function is called before everything is initialized correctly, return an empty result
            if (windowSize == null || windowSize.Height < 1 || windowSize.Width < 1 ||
                streamSize.Height == 0 || streamSize.Width == 0) return result;

            var streamWidth = streamSize.Width;
            var streamHeight = streamSize.Height;

            // Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
            var actualWidth = windowSize.Width;
            var actualHeight = windowSize.Height;
            result.Width = actualWidth;
            result.Height = actualHeight;
            
            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((actualWidth / actualHeight > streamWidth / (double)streamHeight))
            {
                var scale = actualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;
                result.X = (actualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = actualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;
                result.Y = (actualHeight - scaledHeight) / 2.0;
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
