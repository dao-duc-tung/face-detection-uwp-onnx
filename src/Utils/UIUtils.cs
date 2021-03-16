using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace FaceDetection.Utils
{
    public static class UIUtils
    {
        public static TextBlock CreateTextBlock(string text, Brush color=null, double left=0, double top=0)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            if (color != null) textBlock.Foreground = color;
            Canvas.SetLeft(textBlock, left);
            Canvas.SetTop(textBlock, top);
            return textBlock;
        }
    }
}
