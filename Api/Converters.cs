using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StarModsManager.Api;

public class NullableBoolToColorConverter : IValueConverter
{
    public IBrush TrueColor { get; set; } = Brushes.LimeGreen;
    public IBrush FalseColor { get; set; } = Brushes.PaleVioletRed;
    public IBrush NullColor { get; set; } = Brushes.CornflowerBlue;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool or null)
        {
            return value switch
            {
                true => TrueColor,
                false => FalseColor,
                null => NullColor,
                _ => FalseColor
            };
        }
        return FalseColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}