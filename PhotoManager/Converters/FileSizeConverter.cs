using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace PhotoManager.Converters {
    public class FileSizeConverter :IValueConverter{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return "?";
            }
            var bytes = (long)value;
            const int scale = 1024;
            var orders = new[] { "GB", "MB", "KB", "Bytes" };
            var max = (long)Math.Pow(scale, orders.Length - 1);
            foreach (string order in orders) {
                if (bytes > max) {
                    return string.Format("{0:##.##} {1}", Decimal.Divide(bytes, max), order);
                }

                max /= scale;
            }
            return "0 Bytes";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
