using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StarModsManager.Lib;

public class NullableBoolToColorConverter : IValueConverter
{
    // Match
    public IBrush TrueColor { get; set; } = Brushes.Yellow;

    // NotMatch
    public IBrush FalseColor { get; set; } = Brushes.LimeGreen;

    // Null
    public IBrush NullColor { get; set; } = Brushes.PaleVioletRed;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool or null)
            return value switch
            {
                true => TrueColor,
                false => FalseColor,
                null => NullColor,
                _ => FalseColor
            };
        return FalseColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ClipRectConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is Rect bounds &&
            values[1] is double buttonWidth)
        {
            return new RectangleGeometry(new Rect(
                0, 
                0,
                bounds.Width - buttonWidth, 
                bounds.Height
            ));
        }
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ModViewModelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool disable)
        {
            return disable ? Brushes.Red : Brushes.Yellow;
        }

        return Brushes.Yellow;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class HeightMultiplierConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double height || parameter is not string multiplierString) return value;
        if (double.TryParse(multiplierString, out var multiplier)) return height * multiplier;
        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}