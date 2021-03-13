using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace FaceDetection.Converters
{
    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool? data = value as bool?;
            if (data != null)
            {
                return data == true ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
