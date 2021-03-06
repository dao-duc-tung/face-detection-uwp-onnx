using FaceDetection.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

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
            this._viewModel = App.MainPageViewModel;
            this.DataContext = this._viewModel;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
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

        }

        private void FaceDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            FacesCanvas.Children.Clear();
            if (!this._viewModel.IsFaceDetectionEnabled)
            {
                this._viewModel.IsFaceDetectionEnabled = true;
            } else
            {
                this._viewModel.IsFaceDetectionEnabled = false;
            }
            UpdateCaptureControls();
        }

        private void UpdateCaptureControls()
        {
            FaceDetectionDisabledIcon.Visibility = this._viewModel.IsFaceDetectionEnabled ? Visibility.Visible : Visibility.Collapsed;
            FaceDetectionEnabledIcon.Visibility = !this._viewModel.IsFaceDetectionEnabled ? Visibility.Visible : Visibility.Collapsed;
            FacesCanvas.Visibility = this._viewModel.IsFaceDetectionEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
