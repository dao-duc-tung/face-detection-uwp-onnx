using FaceDetection.Utils;
using FaceDetection.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceDetection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainPageViewModel _viewModel;
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = App.MainPageViewModel;
            this._viewModel.FaceDetected += _viewModel_FaceDetected;

            this.DataContext = this._viewModel;
            await this._viewModel.LoadModelAsync();
        }

        private async void PhotoButton_Click(object sender, RoutedEventArgs e)
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
                    if (this._viewModel.IsPreviewing)
                    {
                        await StopPreviewingAsync();
                    }
                    this._viewModel.CacheImageFromStreamAsync(fileStream);
                    BitmapImage bmp = new BitmapImage();
                    bmp.DecodePixelHeight = (int)ImageControl.Height;
                    bmp.DecodePixelWidth = (int)ImageControl.Width;
                    await bmp.SetSourceAsync(fileStream);
                    this.ImageControl.Source = bmp;
                }
            }
        }

        private async void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this._viewModel.IsPreviewing)
            {
                await StartPreviewingAsync();
            } else
            {
                await StopPreviewingAsync();
            }
            UpdateCaptureControls();
        }

        private async Task StartPreviewingAsync()
        {
            this.ImageControl.Source = null;
            await this._viewModel.InitCameraAsync();
            PreviewControl.Source = this._viewModel.MediaCapture;
            await this._viewModel.StartPreviewAsync();
        }

        private async Task StopPreviewingAsync()
        {
            await this._viewModel.StopPreviewAsync();
            FacesCanvas.Children.Clear();
            PreviewControl.Source = null;
        }

        private void FaceDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            FacesCanvas.Children.Clear();
            this._viewModel.IsFaceDetectionEnabled = !this._viewModel.IsFaceDetectionEnabled;
            UpdateCaptureControls();
        }

        private async void _viewModel_FaceDetected(object sender, IReadOnlyList<FaceBoundingBox> faces, System.Drawing.Size originalSize)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => HighlightDetectedFaces(faces, originalSize));
        }

        private void HighlightDetectedFaces(IReadOnlyList<FaceBoundingBox> faces, System.Drawing.Size originalSize)
        {
            FacesCanvas.Children.Clear();
            for (int i = 0; i < faces.Count; ++i)
            {
                Rectangle faceBB = ConvertPreviewToUiRectangle(faces[i], originalSize);
                faceBB.StrokeThickness = 2;
                faceBB.Stroke = new SolidColorBrush(Colors.LimeGreen);
                FacesCanvas.Children.Add(faceBB);
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

        private void UpdateCaptureControls()
        {
            FaceDetectionDisabledIcon.Visibility = this._viewModel.IsFaceDetectionEnabled ? Visibility.Visible : Visibility.Collapsed;
            FaceDetectionEnabledIcon.Visibility = !this._viewModel.IsFaceDetectionEnabled ? Visibility.Visible : Visibility.Collapsed;
            FacesCanvas.Visibility = this._viewModel.IsFaceDetectionEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
