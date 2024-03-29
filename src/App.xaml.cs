﻿using FaceDetection.DistanceEstimator;
using FaceDetection.FaceDetector;
using FaceDetection.Utils;
using FaceDetection.ViewModels;
using Sentry;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FaceDetection
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private static Lazy<MainPageViewModel> _mainPageViewModelLazy = new Lazy<MainPageViewModel>();
        public static MainPageViewModel MainPageViewModel { get => _mainPageViewModelLazy.Value; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Init Sentry Log (Release build only)
            using (SentrySdk.Init("https://5c07bf6d3d024a97b3a81f0d5ce3cb10@o546697.ingest.sentry.io/5668551"))
            {
                Task.Run(LoadAppConfigAsync).Wait();
                InitializeComponent();
                Suspending += OnSuspending;
                UnhandledException += Application_UnhandledException;
            }
        }

        private async Task LoadAppConfigAsync()
        {
            await FileUtils.ReadConfigFile<MainConfig>(ConfigName.Main, ConfigLocalPath.Main);
            await FileUtils.ReadConfigFile<UltraFaceDetectorConfig>(ConfigName.UltraFaceDetector, ConfigLocalPath.UltraFaceDetector);
            await FileUtils.ReadConfigFile<UltraFaceDetectorConfig>(ConfigName.UltraFaceDetector2, ConfigLocalPath.UltraFaceDetector2);
            await FileUtils.ReadConfigFile<UltraFaceDetectorConfig>(ConfigName.UltraFaceDetector3, ConfigLocalPath.UltraFaceDetector3);
            await FileUtils.ReadConfigFile<FocalLengthDistanceEstimatorConfig>(ConfigName.FocalLengthDistanceEstimator, ConfigLocalPath.FocalLengthDistanceEstimator);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void Application_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException(e.Exception);

            // Avoid the application from crashing
            e.Handled = true;
        }
    }
}
