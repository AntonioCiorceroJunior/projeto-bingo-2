using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BingoAdmin.UI.Views
{
    public class MascaraToBrushCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string mascara && mascara.Length == 25)
            {
                var brushes = new List<Brush>();
                foreach (char c in mascara)
                {
                    if (c == '1')
                        brushes.Add(Brushes.Black);
                    else
                        brushes.Add(Brushes.Transparent); // Or White/LightGray
                }
                return brushes;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
