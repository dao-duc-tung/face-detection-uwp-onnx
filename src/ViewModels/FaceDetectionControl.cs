﻿using Emgu.CV;
using FaceDetection.FaceDetector;
using FaceDetection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace FaceDetection.ViewModels
{
    public class FaceDetectionControl
    {
        private IFaceDetector _faceDetector;
        private int _detectingFlag;
        public bool IsFaceDetectionEnabled { get; set; } = false;

        public event FaceDetectedEventHandler FaceDetected
        {
            add
            {
                _faceDetector.FaceDetected += value;
            }
            remove
            {
                _faceDetector.FaceDetected -= value;
            }
        }

        public async Task LoadModelAsync(StorageFile file)
        {
            _faceDetector = new UltraFaceDetector();
            await _faceDetector.LoadModel(file);
        }

        public async void RunFaceDetection(SoftwareBitmap bmp)
        {
            if (!IsFaceDetectionEnabled || _faceDetector == null) return;
            else if (_faceDetector != null && !_faceDetector.IsModelLoaded()) return;

            Mat img = ImageUtils.ConvertSoftwareBitmapToMat(bmp);
            if (img == null) return;

            if (Interlocked.CompareExchange(ref _detectingFlag, 1, 0) == 0)
            {
                await _faceDetector.Detect(img);
                img.Dispose();
            }
            Interlocked.Exchange(ref _detectingFlag, 0);
        }

        public static FaceBoundingBox ScaleBoundingBox(FaceBoundingBox origBB, System.Drawing.Size originalSize)
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
    }
}