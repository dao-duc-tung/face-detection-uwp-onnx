using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceDetection.Utils
{
    public class FaceBoundingBox : CornerBoundingBox
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
    }
}
