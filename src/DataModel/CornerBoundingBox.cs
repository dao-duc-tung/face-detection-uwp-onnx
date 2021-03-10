namespace FaceDetection.Utils
{
    public class CornerBoundingBox
    {
        //corner-form bounding boxes(x0, y0, x1, y1)
        public float X0, Y0, X1, Y1;
        
        public float Width
        {
            get
            {
                return X1 - X0;
            }
            set
            {
                X1 = X0 + value;
            }
        }
        
        public float Height
        {
            get
            {
                return Y1 - Y0;
            }
            set
            {
                Y1 = Y0 + value;
            }
        }

        public bool IsValid()
        {
            if (Width < 0 || Height < 0) return false;
            return true;
        }
    }
}
