namespace FaceDetection.Utils
{
    public class StrUtils
    {
        public static string RdFloat(float number, int nDigit = 2)
        {
            if (nDigit < 0) return number.ToString();
            return number.ToString($"n{nDigit}");
        }

        public static string RdDouble(double number, int nDigit = 2)
        {
            if (nDigit < 0) return number.ToString();
            return number.ToString($"n{nDigit}");
        }
    }
}
