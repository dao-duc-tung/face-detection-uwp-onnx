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
        }

        private async void PhotoButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void CameraButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FaceDetectionButton_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
