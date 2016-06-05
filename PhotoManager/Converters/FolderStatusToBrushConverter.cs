using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using PhotoManager.Models;

namespace PhotoManager.Converters {
    public class FolderStatusToBrushConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            switch ((FolderStatus)value) {
                case FolderStatus.Created:
                    return Brushes.LightGreen;
                case FolderStatus.Deleted:
                    return Brushes.LightPink;
                case FolderStatus.Normal:
                    return Brushes.White;
                default:
                    return Brushes.LightGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
