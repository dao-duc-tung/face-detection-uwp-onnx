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
        public FrameModel FrameModel { get; } = FrameModel.Instance;
        public CameraControl CameraControl { get; } = new CameraControl();
        private int _processingFlag;

        private IFaceDetector _faceDetector;
        private bool _isFaceDetectionEnabled = false;
        public bool IsFaceDetectionEnabled
        {
            get => _isFaceDetectionEnabled;
            set
            {
                SetProperty(ref _isFaceDetectionEnabled, value);
            }
        }
        // TODO: Create Config to load modelFileName
        private string _modelFileName = "version-RFB-320.onnx";

        public ICommand ImageControlLoaded { get; set; }
        public ICommand PreviewControlLoaded { get; set; }
        public ICommand FacesCanvasLoaded { get; set; }

        public ICommand PhotoButtonClicked { get; set; }
        public ICommand CameraButtonClicked { get; set; }
        public ICommand FaceDetectionButtonClicked { get; set; }

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

        private int _previewWidth;
        public int PreviewWidth
        {
            get => _previewWidth;
            set
            {
                SetProperty(ref _previewWidth, value);
            }
        }

        private FocalLengthBasedDistanceEstimator _distanceEstimator
            = new FocalLengthBasedDistanceEstimator();

        public MainPageViewModel()
        {
            SubscribeEvents();

            ImageControlLoaded = new DelegateCommand<Image>(imageControl =>
            {
                _imageControl = imageControl;
            });
            PreviewControlLoaded = new DelegateCommand<CaptureElement>(previewControl =>
            {
                _previewControl = previewControl;
            });
            FacesCanvasLoaded = new DelegateCommand<Canvas>(facesCanvas =>
            {
                _facesCanvas = facesCanvas;
            });

            PhotoButtonClicked = new DelegateCommand(async () =>
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add("*");
                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        await ClearFacesCanvas();
                        if (CameraControl.IsPreviewing)
                        {
                            await StopPreviewAsync();
                        }
                        BitmapImage bmp = new BitmapImage();
                        bmp.DecodePixelHeight = (int)ImageControl.Height;
                        bmp.DecodePixelWidth = (int)ImageControl.Width;
                        await bmp.SetSourceAsync(fileStream);
                        ImageControl.Source = bmp;
                        
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                        FrameModel.SoftwareBitmap = softwareBitmap;
                    }
                }
            });
            CameraButtonClicked = new DelegateCommand(async () =>
            {
                if (!CameraControl.IsPreviewing)
                {
                    ImageControl.Source = null;
                    await InitCameraAsync();
                    PreviewControl.Source = CameraControl.MediaCapture;
                    await StartPreviewAsync();
                }
                else
                {
                    PreviewControl.Source = null;
                    await StopPreviewAsync();
                }
                await ClearFacesCanvas();
            });
            FaceDetectionButtonClicked = new DelegateCommand(async () =>
            {
                IsFaceDetectionEnabled = !IsFaceDetectionEnabled;
                if (IsFaceDetectionEnabled)
                {
                    _faceDetector.FaceDetected += _faceDetector_FaceDetected;
                } else
                {
                    _faceDetector.FaceDetected -= _faceDetector_FaceDetected;
                }
                await ClearFacesCanvas();
            });
        }

        public void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (Interlocked.CompareExchange(ref _processingFlag, 1, 0) == 0)
            {
                using (var frame = sender.TryAcquireLatestFrame())
                using (var bmp = frame?.VideoMediaFrame?.SoftwareBitmap)
                {
                    if (bmp != null)
                    {
                        FrameModel.SoftwareBitmap = bmp;
                    }
                }
                Interlocked.Exchange(ref _processingFlag, 0);
            }
        }

        private async void PerformFaceDetection()
        {
            if (!_isFaceDetectionEnabled || _faceDetector == null) return;
            else if (_faceDetector != null && !_faceDetector.IsModelLoaded()) return;

            SoftwareBitmap bmp = FrameModel.SoftwareBitmap;
            if (bmp == null) return;
            Mat img = ImageUtils.ConvertSoftwareBitmapToMat(bmp);
            if (img == null) return;
            await _faceDetector.Detect(img);
            img.Dispose();
        }

        public FaceBoundingBox ScaleBoundingBox(FaceBoundingBox origBB, System.Drawing.Size originalSize)
        {
            int origWidth = originalSize.Width;
            int origHeight = originalSize.Height;

            var bb = new FaceBoundingBox();
            bb.X0 = origBB.X0 * origWidth;
            bb.X1 = origBB.X1 * origWidth;
            bb.Y0 = origBB.Y0 * origHeight;
            bb.Y1 = origBB.Y1 * origHeight;
            return bb;
        }

        public float EstimateDistance(FaceBoundingBox bb)
        {
            return _distanceEstimator.ComputeDistance(bb);
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
                var bb = ScaleBoundingBox(faces[i], originalSize);
                Rectangle faceBB = ConvertPreviewToUiRectangle(bb, originalSize);
                faceBB.StrokeThickness = 2;
                faceBB.Stroke = new SolidColorBrush(Colors.LimeGreen);
                _facesCanvas.Children.Add(faceBB);

                TextBlock txtBlk = new TextBlock();
                txtBlk.Text = EstimateDistance(faces[i]).ToString("n0") + " cm";
                txtBlk.Foreground = new SolidColorBrush(Colors.LimeGreen);
                Canvas.SetLeft(txtBlk, Canvas.GetLeft(faceBB));
                Canvas.SetTop(txtBlk, Canvas.GetTop(faceBB));
                _facesCanvas.Children.Add(txtBlk);
            }
        }

        private Rectangle ConvertPreviewToUiRectangle(FaceBoundingBox faceBox, System.Drawing.Size actualContentSize)
        {
            var result = new Rectangle();
            double streamWidth = actualContentSize.Width;
            double streamHeight = actualContentSize.Height;

            //If there is no available information about the preview, return an empty rectangle, as re - scaling to the screen coordinates will be impossible
            //  Similarly, if any of the dimensions is zero(which would only happen in an error case) return an empty rectangle
            if (streamWidth == 0 || streamHeight == 0) return result;


            //Get the rectangle that is occupied by the actual video feed
            var previewInUI = GetDisplayRectInControl(actualContentSize, PreviewControl);
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

        private Rect GetDisplayRectInControl(System.Drawing.Size actualContentSize, CaptureElement previewControl)
        {
            var result = new Rect();
            // In case this function is called before everything is initialized correctly, return an empty result
            if (previewControl == null || previewControl.ActualHeight < 1 || previewControl.ActualWidth < 1 ||
                actualContentSize.Height == 0 || actualContentSize.Width == 0)
            {
                return result;
            }

            var streamWidth = actualContentSize.Width;
            var streamHeight = actualContentSize.Height;

            // Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
            result.Width = previewControl.ActualWidth;
            result.Height = previewControl.ActualHeight;

            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((previewControl.ActualWidth / previewControl.ActualHeight > streamWidth / (double)streamHeight))
            {
                var scale = previewControl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;

                result.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = previewControl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;

                result.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }

            return result;
        }

        private async Task ClearFacesCanvas()
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() => _facesCanvas.Children.Clear());
        }

        public async Task LoadModelAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_modelFileName}"));
            _faceDetector = new UltraFaceDetector();
            _faceDetector.LoadModel(file);
        }

        private void SubscribeEvents()
        {
            FrameModel.PropertyChanged += _frameModel_PropertyChanged;
            PropertyChanged += MainPageViewModel_PropertyChanged;
        }

        private void MainPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsFaceDetectionEnabled))
            {
                PerformFaceDetection();
            }
        }

        private void _frameModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FrameModel.SoftwareBitmap))
            {
                PerformFaceDetection();
            }
        }

        public async Task InitCameraAsync()
        {
            await CameraControl.InitCameraAsync();
        }

        public async Task StartPreviewAsync()
        {
            CameraControl.FrameArrived += OnFrameArrived;
            await CameraControl.StartPreviewAsync();
        }

        public async Task StopPreviewAsync()
        {
            await CameraControl.StopPreviewAsync();
            CameraControl.FrameArrived -= OnFrameArrived;
        }
    }
}
