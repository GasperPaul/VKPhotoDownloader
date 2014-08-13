using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace VKPhotoDownloader
{
    public class BinaryImageConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value is byte[])
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(value as byte[]);
                bmp.EndInit();
                return bmp;
            }
            return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
