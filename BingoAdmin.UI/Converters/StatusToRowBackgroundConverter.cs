using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Converters
{
    public class StatusToRowBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = Status (string)
            // values[1] = AlternationIndex (int)

            if (values.Length < 2 || values[0] == null)
                return Brushes.White;

            string status = values[0].ToString() ?? "Disponivel";
            int index = 0;
            if (values[1] is int i) index = i;

            // Base Colors
            Color baseColor;
            switch (status)
            {
                case "Reservado":
                    baseColor = (Color)ColorConverter.ConvertFromString("#FFE0B2"); // Light Orange
                    break;
                case "Confirmado":
                    baseColor = (Color)ColorConverter.ConvertFromString("#C8E6C9"); // Light Green
                    break;
                case "Disponivel":
                default:
                    baseColor = Colors.White;
                    break;
            }

            // Zebra Effect: Darken slightly if index is 1 (odd row)
            if (index == 1)
            {
                // Darken by blending with a bit of black
                // Or just hardcode darker variants.
                // Let's try a simple darkening approach.
                baseColor = Darken(baseColor, 0.05); // 5% darker
            }

            return new SolidColorBrush(baseColor);
        }

        private Color Darken(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * (1 - factor)),
                (byte)(color.G * (1 - factor)),
                (byte)(color.B * (1 - factor))
            );
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
