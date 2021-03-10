<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/dao-duc-tung/face-detection-uwp-onnx">
    <img src="media/banner.png" alt="Logo" width="300" height="100">
  </a>

  <h3 align="center">Face Detection on UWP using ONNX </h3>

  <p align="center">
    <a href="https://github.com/dao-duc-tung/face-detection-uwp-onnx/issues">Report Bug</a>
    ·
    <a href="https://github.com/dao-duc-tung/face-detection-uwp-onnx/issues">Request Feature</a>
  </p>
</p>


<!-- TABLE OF CONTENTS -->
<details open="open">
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#software-design">Software Design</a></li>
    <li><a href="#convert-tensorflow-model-to-onnx">Convert Tensorflow model to ONNX</a></li>
    <li><a href="#tensorflow-and-opencv-compatibility-in-object-detection">Tensorflow and OpenCV compatibility in object detection</a></li>
    <li><a href="#onnx-inference">ONNX Inference</a></li>
    <li><a href="#distance-estimation">Distance Estimation</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgements">Acknowledgements</a></li>
  </ol>
</details>


<!-- ABOUT THE PROJECT -->
## About The Project

![Screen Shot][product-screenshot]

The purpose of the project is to integrate an ONNX-based Face Detection CNN Model into a Universal Windows Platform application.

### Built With

- Emgu.CV v4.5.1
- Emgu.CV.Bitmap v4.5.1
- Emgu.CV.runtime.windows v4.5.1
- Microsoft.ML.OnnxRuntime v1.6.0
- Microsoft.Toolkit.Uwp v6.1.1
- Sentry v3.0.8
- Microsoft.NETCore.UniversalWindowsPlatform v6.2.10
- Microsoft.Xaml.Behaviors.Uwp.Managed v2.0.1
- Microsoft.NET.Test.Sdk v16.6.1
- xunit v2.4.1
- xunit.analyzers v0.10.0
- xunit.runner.console v2.4.1
- xunit.runner.visualstudio v2.4.1 (**Important**)


<!-- GETTING STARTED -->
## Getting Started

### Prerequisites

- Windows 10 (Version 1809 or higher)
- Windows 10 SDK (Build 16299 or higher)
- [Visual Studio Community 2019](https://visualstudio.microsoft.com/downloads/)

### Installation

1. Clone the repository and open file *src/FaceDetection.sln* in Visual Studio
2. Install required NuGet Packages
  - In **Solution Explorer** window, right-click on solution, select **Restore NuGet Packages**
3. Change project configuration
  - In **Solution Explorer** window, right-click on solution, select **Configuration Manager**
  - On **Active solution platform**, select **x86**
4. (Optional) Run test
  - In **Solution Explorer** window, right click on **UnitTest** project, select **Run Tests**
5. Run main application
  - Build and run **FaceDetection** project

<!-- USAGE EXAMPLES -->
## Usage

### Detect face on the selected photo

![Detect on photo][detect-on-photo]

- Click **Image Button** (first button on the right), select any image
- Click **Detect Button** (last button on the right) to enable/disable face detection function

### Detect face on live camera

![Detect on live camera][detect-on-live-camera]

- Click **Camera Button** (middle button on the right) to enable/disable camera streaming
- Click **Detect Button** to enable/disable face detection function

### Estimate distance from the face to camera

![Estimate distance][estimate-distance]

- This function is automatically enabled when the face detection function is enabled

### Logging

- The application uses full-stack monitoring Sentry for error reporting
- In Release build, the application will report an error to my Sentry Report dashboard


<!-- THINGS TO NOTE -->
## Software Design

### Use case diagram

![Use case Diagram][use-case-diagram]

User can perform 3 actions which are opening an image, starting camera stream, and detecting faces on the image/camera frame. These 3 actions are independent. Each action depends on some other actions. The Use Case diagram above briefly describes all the actions.

### Data flow

![Data flow][data-flow]

This Data Flow diagram shows the data flow in the application. Whenever the user loads an image or turns on the camera, the image or camera frame will be converted into a uniform format which is *SoftwareBitmap*. The *FrameModel* will store this data for further processes.

Whenever *FrameModel* updates new data, it will notify *MainViewModel* to preview the new data (image) on the screen on the *preview layer*

When the user enables the Face Detection function, the Face Detector will get the current data in the *FrameModel* as the input to perform face detection on this data. Face Detector's outputs are the bounding boxes of all the detected faces. These bounding boxes will be displayed on the *Canvas layer* on top of *preview layer*.

Face Detector's outputs are the input of the Distance Estimator as well. The distance from each face to the camera will then be display on the *Canvas layer*.

By making the module's input/output like this, each module will know clearly about their responsibility and the coupling can be reduced in the class design phase.

### Add new face detector

The new face detector must implement **IFaceDetector** interface as below. After the detection is finished, the FaceDetected event should be triggered.

```C#
public class FaceDetectedEventArgs : EventArgs
{
    public IReadOnlyList<FaceBoundingBox> BoundingBoxes;
    public Size OriginalSize;
}
public delegate void FaceDetectedEventHandler(object sender, FaceDetectedEventArgs eventArgs);

public interface IFaceDetector
{
    event FaceDetectedEventHandler FaceDetected;
    Task LoadModel(StorageFile file);
    bool IsModelLoaded();
    Task Detect(Mat input);
}
```

### Add configuration

Usually, the face detector will come along with some configurations such as the model file path, confidence threshold, or intersection-over-union threshold, etc.

The configuration class where those model configurations are stored should implement **IConfig** as below

```C#
public interface IConfig
{
    Task ReadAsync(StorageFile file);
}
```

The new face detector class's constructor can receive the corresponding configuration class for later usage.

All the configuration class which inherits the **IConfig** interface should be registered in **AppConfig** instance. The **AppConfig** class is defined as a **Singleton** class so any class can retrieve the configurations of face detectors, main application, etc from anywhere without loading the configurations again.

## Convert Tensorflow model to ONNX

### From Keras HDF5 format to ONNX

The most convenient way that I figured out is to convert the HDF5 format model to **SavedModel** format first. Then we use **tf2onnx** to convert **SavedModel** format model to ONNX. This way is also recommended by [**tf2onnx team**](https://github.com/onnx/Tensorflow-onnx). You can find the utility functions inside *ModelConverters/hdf5_to_savedmodel.py* for your usage.

### From Frozen model (graphdef) to ONNX

It's also recommended by **tf2onnx team** to convert this frozen format model to **SavedModel** format first and use **tf2onnx** to convert **SavedModel** format model to ONNX. You can find the utility functions inside *ModelConverters/frozen_to_savedmodel_.py* for your usage.

### ONNX Opset option in tf2onnx

Opset stands for operator set. For example, convolution is an operator. When people design a new model, they might create a new operator. By default **tf2onnx** uses the opset 9 to generate the graph. So sometimes it doesn't contain the required opset. We just need to try with a bigger opset.


## Tensorflow and OpenCV compatibility in object detection

Deep neural networks in Tensorflow are represented as graphs where every node is a transformation of its inputs (like *Convolution* or *MaxPooling*).

OpenCV needs an extra configuration file to import object detection models from Tensorflow. It's based on a text version of the same serialized graph in protocol buffers format (protobuf).

Follow [this link](https://github.com/opencv/opencv/wiki/TensorFlow-Object-Detection-API) to generate that extra file from Tensorflow Object detection models.

## ONNX Inference

In this application, I use **Windows ML** and **ONNX Runtime** for inference on [**Ultra-lightweight face detection model**](https://github.com/Linzaer/Ultra-Light-Fast-Generic-Face-Detector-1MB). The inference implementation using **Windows ML** is in *UltraFaceDetector.cs* and the implementation of **ONNX Runtime** is in *UltraFaceDetector2.cs*.

**Windows ML** is a high-performance API for deploying hardware-accelerate ML inferences on Windows devices.

![Windows ML][windows-ml]

For the NuGet package, **Windows ML** is built into *Microsoft.ai.machinelearning.dll*. It does not contain an embedded **ONNX runtime**, instead the **ONNX runtime** is built into the file: *onnxruntime.dll*. Follow [this link](https://docs.microsoft.com/en-us/windows/ai/windows-ml/) for more details.


## Distance Estimation

![Pinhole camera][pinhole-camera]

The pinhole camera generates a uniform relationship between the object and the image. Using this relationship, we form 3 equations as below:

<img src="https://latex.codecogs.com/gif.latex?\frac{f}{d}=\frac{r}{R}" title="(1)" /> <br/>

<img src="https://latex.codecogs.com/gif.latex?f=d\times\frac{r}{R}" title="(2)" /> <br/>

<img src="https://latex.codecogs.com/gif.latex?d=f\times\frac{R}{r}" title="(3)" /> <br/>

where *f* (pixels) is Focal Lenght, *d* (cm) is the distance between the camera and the face, *R* (cm) is the face Height, *r* (pixels) is the face height on the screen.

Firstly, I adjust my face in front of the camera with a fixed distance of *d*. Then I use the application to detect my face at that *d* distance and record the height of the detected bounding box which is *r*. I also need to measure my face height which is *R*. Finally, I calculate the focal length *f* by using the second equation above.

In the application, I use the third equation to estimate the distance between the face and the camera. In the third equation, *f* and *R* are fixed. *r* is given by the Face Detector which is the height of the bounding box.

This approach has its limitations. When people look down or look up, their face height changes. This will affect the estimated distance. We also need to compute the focal length of camera on the new device.

Instead, we could implement facial landmark detection to measure the distance between eyes. Then we can use some linear relationship between the eyes distance and the face height/width to estimate the distance between the face and the camera.


<!-- CONTRIBUTING -->
## Contributing

Contributions make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/FeatureName`)
3. Commit your Changes (`git commit -m 'Add some FeatureName'`)
4. Push to the Branch (`git push origin feature/FeatureName`)
5. Open a Pull Request


<!-- LICENSE -->
## License

Distributed under the MIT License. See [LICENSE](LICENSE) for more information.


<!-- CONTACT -->
## Contact

Tung Dao - [LinkedIn](https://www.linkedin.com/in/tungdao17/)

Project Link: [https://github.com/dao-duc-tung/face-detection-uwp-onnx](https://github.com/dao-duc-tung/face-detection-uwp-onnx)


<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements

- [MVVM architectural pattern](https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/mvvm-performance-tips)
- [Media Control in UWP](https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader)
- [Model Inference using Windows ML](https://docs.microsoft.com/en-us/azure/cognitive-services/custom-vision-service/custom-vision-onnx-windows-ml)
- [Model Inference using OnnxRuntime](https://www.onnxruntime.ai/docs/reference/api/csharp-api.html)
- [Convert Tensorflow model to ONNX](https://github.com/onnx/Tensorflow-onnx)
- [Ultra-lightweight face detection model](https://github.com/Linzaer/Ultra-Light-Fast-Generic-Face-Detector-1MB)
- [Estimate distance using camera focal length](http://emaraic.com/blog/distance-measurement)
- [Sentry service](https://github.com/getsentry/sentry)


<!-- MARKDOWN LINKS & IMAGES -->
[product-screenshot]: media/demo1.gif
[detect-on-photo]: media/detect-on-photo.png
[detect-on-live-camera]: media/detect-on-live-camera.gif
[estimate-distance]: media/estimate-distance.gif
[use-case-diagram]: media/use-case-diagram.png
[data-flow]: media/data-flow.png
[windows-ml]: media/winml-nuget.svg
[pinhole-camera]: media/pinhole-camera.png
