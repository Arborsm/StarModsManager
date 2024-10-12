using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using StarModsManager.ViewModels.Pages;

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

public class ModLangIsMatchComparer : IComparer
{
    private static int Compare(ModLang? x, ModLang? y)
    {
        return x?.IsMatch.GetValueOrDefault() == y?.IsMatch.GetValueOrDefault() ? 0 : 1;
    }

    public int Compare(object? x, object? y)
    {
        return Compare(x as ModLang, y as ModLang);
    }
}