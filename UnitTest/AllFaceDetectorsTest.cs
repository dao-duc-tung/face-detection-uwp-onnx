using FaceDetection.FaceDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    public class AllFaceDetectorsTest
    {
        private TestFixture testFixture;
        private UltraFaceDetectorTest<UltraFaceDetector> ultraFaceDetectorTest;
        private UltraFaceDetectorTest<UltraFaceDetector2> ultraFaceDetector2Test;
        private UltraFaceDetectorTest<UltraFaceDetector3> ultraFaceDetector3Test;

        public AllFaceDetectorsTest()
        {
            testFixture = new TestFixture();
            while (testFixture.AppConfig == null);
            ultraFaceDetectorTest = new UltraFaceDetectorTest<UltraFaceDetector>(testFixture);
            ultraFaceDetector2Test = new UltraFaceDetectorTest<UltraFaceDetector2>(testFixture);
            ultraFaceDetector3Test = new UltraFaceDetectorTest<UltraFaceDetector3>(testFixture);
        }

        [Fact]
        public void UltraFaceDetectorTest_LoadModel_GoodFile_IsModelLoadedReturnsTrue()
        {
            ultraFaceDetectorTest.LoadModel_GoodFile_IsModelLoadedReturnsTrue();
        }

        [Fact]
        public void UltraFaceDetectorTest_Detect_ValidFormatImage_FaceDetectedIsRaised()
        {
            ultraFaceDetectorTest.Detect_ValidFormatImage_FaceDetectedIsRaised();
        }

        [Fact]
        public void UltraFaceDetectorTest_Detect_NullImage_FaceDetectedIsNotRaised()
        {
            ultraFaceDetectorTest.Detect_NullImage_FaceDetectedIsNotRaised();
        }

        [Fact]
        public void UltraFaceDetector2Test_LoadModel_GoodFile_IsModelLoadedReturnsTrue()
        {
            ultraFaceDetector2Test.LoadModel_GoodFile_IsModelLoadedReturnsTrue();
        }

        [Fact]
        public void UltraFaceDetector2Test_Detect_ValidFormatImage_FaceDetectedIsRaised()
        {
            ultraFaceDetector2Test.Detect_ValidFormatImage_FaceDetectedIsRaised();
        }

        [Fact]
        public void UltraFaceDetector2Test_Detect_NullImage_FaceDetectedIsNotRaised()
        {
            ultraFaceDetector2Test.Detect_NullImage_FaceDetectedIsNotRaised();
        }

        [Fact]
        public void UltraFaceDetector3Test_LoadModel_GoodFile_IsModelLoadedReturnsTrue()
        {
            ultraFaceDetector3Test.LoadModel_GoodFile_IsModelLoadedReturnsTrue();
        }

        [Fact]
        public void UltraFaceDetector3Test_Detect_ValidFormatImage_FaceDetectedIsRaised()
        {
            ultraFaceDetector3Test.Detect_ValidFormatImage_FaceDetectedIsRaised();
        }

        [Fact]
        public void UltraFaceDetector3Test_Detect_NullImage_FaceDetectedIsNotRaised()
        {
            ultraFaceDetector3Test.Detect_NullImage_FaceDetectedIsNotRaised();
        }
    }
}
