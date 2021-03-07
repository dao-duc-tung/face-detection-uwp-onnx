namespace FaceDetection.Utils
{
    public class FaceBoundingBox : CornerBoundingBox
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
    }
}
