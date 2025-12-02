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
            if (value is string mascara)
            {
                // Padrões Visuais para os modos "Na Louca"
                if (mascara == "RANDOM:5")
                {
                    // Desenha o número 5
                    return GetBrushesFromMask("1111110000111110000111111");
                }
                if (mascara == "RANDOM:7")
                {
                    // Desenha o número 7
                    return GetBrushesFromMask("1111100001000100010001000");
                }
                if (mascara == "RANDOM:10")
                {
                    // Desenha o número 10 (1 na col 0, 0 nas cols 2-4)
                    return GetBrushesFromMask("1011110101101011010110111");
                }

                if (mascara.Length == 25)
                {
                    return GetBrushesFromMask(mascara);
                }
            }
            return null;
        }

        private List<Brush> GetBrushesFromMask(string mascara)
        {
            var brushes = new List<Brush>();
            foreach (char c in mascara)
            {
                if (c == '1')
                    brushes.Add(Brushes.Black);
                else
                    brushes.Add(Brushes.Transparent);
            }
            return brushes;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
