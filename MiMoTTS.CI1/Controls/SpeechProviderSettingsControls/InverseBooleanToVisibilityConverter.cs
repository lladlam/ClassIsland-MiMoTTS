using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MiMoTTS.CI1.Controls.SpeechProviderSettingsControls;

/// <summary>
/// 布尔值反转后再转换为可见性：true → Collapsed，false → Visible。
/// 用于替代 Avalonia 的 Binding.ConverterInverse（WPF 的 Binding 不支持该属性）。
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool flag && flag ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility != Visibility.Visible;
    }
}
