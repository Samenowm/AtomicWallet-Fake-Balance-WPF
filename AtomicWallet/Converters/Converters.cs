using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AtomicWallet.Converters;

/// <summary>Positive → positive brush, negative → negative brush.</summary>
public sealed class ChangeBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Up = new((Color)ColorConverter.ConvertFromString("#16C784"));
    private static readonly SolidColorBrush Down = new((Color)ColorConverter.ConvertFromString("#F75555"));

    public object Convert(object value, Type t, object p, CultureInfo c)
        => System.Convert.ToDecimal(value, CultureInfo.InvariantCulture) >= 0 ? Up : Down;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Formats a percent with explicit sign, e.g. "+0.47%".</summary>
public sealed class SignedPercentConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var d = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return $"{(d >= 0 ? "+" : string.Empty)}{d.ToString("0.00", c)}%";
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Hex string → SolidColorBrush.</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { /* fall through */ }
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>First character of a ticker, for letter badges.</summary>
public sealed class InitialConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is string s && s.Length > 0 ? s.Substring(0, 1).ToUpperInvariant() : "?";

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Coin logo key (e.g. "btc") → ImageSource from Assets/coins, or null.</summary>
public sealed class LogoKeyToImageConverter : IValueConverter
{
    public object? Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not string key || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        try
        {
            var uri = new Uri($"pack://application:,,,/Assets/coins/{key}.png", UriKind.Absolute);
            var img = new System.Windows.Media.Imaging.BitmapImage();
            img.BeginInit();
            img.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            img.UriSource = uri;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Resource key (string) → Geometry from app resources.</summary>
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    public object? Convert(object value, Type t, object p, CultureInfo c)
        => value is string key && Application.Current.TryFindResource(key) is Geometry g ? g : null;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>
/// MultiBinding [usdValue, Fx.Symbol, Fx.Rate] → formatted fiat string in the
/// current display currency. The Symbol/Rate inputs make it refresh on change.
/// </summary>
public sealed class FiatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values is not { Length: >= 1 } || values[0] is null)
        {
            return string.Empty;
        }

        decimal usd;
        try { usd = System.Convert.ToDecimal(values[0], CultureInfo.InvariantCulture); }
        catch { return string.Empty; }

        var fx = AtomicWallet.Services.Fx.Instance;
        var v = usd * fx.Rate;
        var fmt = fx.Code switch { "JPY" => "N0", "BTC" => "0.######", _ => "N2" };
        return $"{fx.Symbol}{v.ToString(fmt, CultureInfo.InvariantCulture)}";
    }

    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>value == parameter → primary text brush, else secondary (for sort headers).</summary>
public sealed class MatchParamToTextBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var key = string.Equals(value?.ToString(), p?.ToString(), StringComparison.Ordinal)
            ? "TextPrimaryBrush" : "TextSecondaryBrush";
        return Application.Current.TryFindResource(key) ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Ascending bool → up/down chevron geometry for the active sort header.</summary>
public sealed class SortArrowConverter : IValueConverter
{
    private static readonly Geometry Up = Geometry.Parse("M7 14l5-5 5 5z");
    private static readonly Geometry Down = Geometry.Parse("M7 10l5 5 5-5z");

    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? Up : Down;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>MultiBinding [current, item] → primary text brush if equal, else secondary.</summary>
public sealed class MatchToTextBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        var equal = values is { Length: 2 }
            && string.Equals(values[0]?.ToString(), values[1]?.ToString(), StringComparison.Ordinal);
        var key = equal ? "TextPrimaryBrush" : "TextSecondaryBrush";
        return Application.Current.TryFindResource(key) ?? Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Visible when value equals the parameter (e.g. tab index), else Collapsed.</summary>
public sealed class EqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => string.Equals(value?.ToString(), p?.ToString(), StringComparison.Ordinal)
            ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Non-empty string → Visible, empty/null → Collapsed.</summary>
public sealed class NotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Bool → Visibility, optional "Invert" parameter.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var flag = value is true;
        if (string.Equals(p as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
