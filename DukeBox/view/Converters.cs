using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.RegularExpressions;

namespace DukeBox.view;

public class BoolVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}
public class NegBoolVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return (!(bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}

public class NegBoolConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        return value as bool? != true;
    }
}


public class EnumBooleanConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        string? parameterString = parameter as string;
        if (parameterString == null) {
            return DependencyProperty.UnsetValue;
        }

        if (Enum.IsDefined(value.GetType(), value) == false) {
            return DependencyProperty.UnsetValue;
        }

        object paramValue = Enum.Parse(value.GetType(), parameterString);

        if (paramValue.Equals(value)) {
            return true;
        }
        else {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        if (!(bool)value) {
            // true の場合以外は値が不定
            return DependencyProperty.UnsetValue;
        }
        string? parameterString = parameter as string;
        if (parameterString == null) {
            return DependencyProperty.UnsetValue;
        }

        return Enum.Parse(targetType, parameterString);
    }
}

public class NegEnumBooleanConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        string? parameterString = parameter as string;
        if (parameterString == null) {
            return DependencyProperty.UnsetValue;
        }

        if (Enum.IsDefined(value.GetType(), value) == false) {
            return DependencyProperty.UnsetValue;
        }

        object paramValue = Enum.Parse(value.GetType(), parameterString);

        if (paramValue.Equals(value)) {
            return false;
        }
        else {
            return true;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        if ((bool)value) {
            // falseの場合以外は値が不定
            return DependencyProperty.UnsetValue;
        }
        string? parameterString = parameter as string;
        if (parameterString == null) {
            return DependencyProperty.UnsetValue;
        }

        return Enum.Parse(targetType, parameterString);
    }
}

public class EnumVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        string? parameterString = parameter as string;
        if (parameterString == null) {
            return DependencyProperty.UnsetValue;
        }

        if (Enum.IsDefined(value.GetType(), value) == false) {
            return DependencyProperty.UnsetValue;
        }

        object paramValue = Enum.Parse(value.GetType(), parameterString);

        if (paramValue.Equals(value)) {
            return Visibility.Visible;
        }
        else {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        return DependencyProperty.UnsetValue;
    }
}

public class NegEnumVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        string? parameterString = parameter as string;
        if (parameterString == null) {
            return DependencyProperty.UnsetValue;
        }

        if (Enum.IsDefined(value.GetType(), value) == false) {
            return DependencyProperty.UnsetValue;
        }

        object paramValue = Enum.Parse(value.GetType(), parameterString);

        if (paramValue.Equals(value)) {
            return Visibility.Collapsed;
        }
        else {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        return DependencyProperty.UnsetValue;
    }
}

public class DateStringConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is long) {
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)value).DateTime;
        }
        if (value is DateTime dateTime) {
            if (!DateTime.MinValue.Equals(value)) {
                return dateTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
            }
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotSupportedException();
    }
}

public class DecimalStringConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return $"{value:#,0}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotSupportedException();
    }
}

public class SizeStringConverter : IValueConverter {
    public static string formatSizeString(long size) {
        double kb = (double)size / 1000;
        if (kb < 1000) {
            return String.Format(" {0} KB", kb.ToString("n3"));
        }

        double mb = kb / 1000;
        if (mb < 1000) {
            return String.Format(" {0} MB", mb.ToString("n3"));
        }

        double gb = mb / 1000;
        return String.Format(" {0} GB", gb.ToString("n3"));
    }

    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is long size) {
            return formatSizeString(size);
        }
        return "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotSupportedException();
    }
}

public class EmptyStringToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        if (string.IsNullOrEmpty(value as string)) {
            return Visibility.Visible;
        }
        else {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotSupportedException();
    }
}


public class IntVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        if ((int)value == System.Convert.ToInt32(parameter)) {
            return Visibility.Visible;
        }
        else {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        return DependencyProperty.UnsetValue;
    }
}

// PrivateCamera 固有のコンバータ
// ファイル名から日付文字列を取得して整形する。
// CreationDate属性を持っているが、時差が補正できないので、ファイル名から取得する。
// ファイル名は (img|mov)-yyyy.MM.dd-HH:mm:ss.(jpg|mp4) の形式であることを前提とする。

public class FileNameToDateStringConverter : IValueConverter {
    private static Regex regex = new Regex(@"(img|mov)-(\d{4}.\d{2}.\d{2}-\d{2}:\d{2}:\d{2}).(jpg|mp4)");
    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is string fileName) {
            var match = regex.Match(fileName);
            if (match.Success) {
                return match.Groups[2].Value.Replace(".", "/").Replace("-", " ");
            }
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotSupportedException();
    }
}

public class LogLevelToColorConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is LogLevel level) {
            return level switch {
                LogLevel.ERROR or LogLevel.FATAL => new SolidColorBrush(Colors.Red),
                LogLevel.WARN => new SolidColorBrush(Colors.Blue),
                LogLevel.INFO => new SolidColorBrush(Colors.Green),
                LogLevel.DEBUG => new SolidColorBrush(Colors.Black),
                LogLevel.VERBOSE => new SolidColorBrush(Colors.Gray),
                _ => new SolidColorBrush(Colors.Gray),
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}

public class ThousandsSeparatorConverter : IValueConverter {
    public object Convert(object value, Type t, object p, string l) => value is long n ? n.ToString("N0") : value;
    public object ConvertBack(object value, Type t, object p, string l) => throw new NotSupportedException();
}


public class UnixMsToDateStringConverter : IValueConverter {
    public object Convert(object value, Type t, object p, string l) =>
        value is long ms && ms > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime().ToString((p as string) ?? "yyyy/MM/dd HH:mm")
            : "";
    public object ConvertBack(object value, Type t, object p, string l) => throw new NotSupportedException();
}

public class MediaTypeToGlyphConverter : IValueConverter {
    public object Convert(object value, Type t, object p, string l) => (value as string)?.ToLowerInvariant() switch {
        "mp4" or "mov" or "avi" or "mkv" => "\uE714",   // Video
        "jpg" or "jpeg" or "png" or "gif" => "\uEB9F",  // Photo
        "mp3" or "m4a" or "wav" => "\uE8D6",            // Audio
        _ => "\uE8A5",                                  // Document
    };
    public object ConvertBack(object value, Type t, object p, string l) => throw new NotSupportedException();
}

public class RatingToGeometryConverter : IValueConverter {
    private const string Rating5 = "M18.9,18.94L15.94,16C15.76,15.79 15.55,15.5 15.55,15.05A1.3,1.3 0 0,1 16.85,13.75C17.19,13.75 17.53,13.89 17.77,14.15L18.91,15.26L20.03,14.13C20.27,13.89 20.61,13.75 20.95,13.75A1.3,1.3 0 0,1 22.25,15.05C22.25,15.39 22.11,15.73 21.87,15.97L18.9,18.94M17.46,19.62C15.72,21.1 13.47,22 11,22A10,10 0 0,1 1,12A10,10 0 0,1 11,2A10,10 0 0,1 21,12C21,12.09 21,12.17 20.95,12.25C20.21,12.25 19.5,12.55 18.97,13.07L18.9,13.14L18.84,13.09C18.32,12.55 17.6,12.25 16.85,12.25A2.8,2.8 0 0,0 14.05,15.05C14.05,15.78 14.34,16.5 14.87,17.03L17.46,19.62M13,9.5C13,10.3 13.7,11 14.5,11C15.3,11 16,10.3 16,9.5C16,8.7 15.3,8 14.5,8C13.7,8 13,8.7 13,9.5M9,9.5C9,8.7 8.3,8 7.5,8C6.7,8 6,8.7 6,9.5C6,10.3 6.7,11 7.5,11C8.3,11 9,10.3 9,9.5M12.94,15.18L14,14.12L11.88,12L10.82,13.06L11.88,14.12L10.82,15.18L11.88,16.24L10.82,17.3L11.88,18.36L14,16.24L12.94,15.18Z";
    private const string Rating4 = "M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22A10,10 0 0,0 22,12C22,6.47 17.5,2 12,2M8.88,7.82L11,9.94L9.94,11L8.88,9.94L7.82,11L6.76,9.94L8.88,7.82M12,17.5C9.67,17.5 7.69,16.04 6.89,14H17.11C16.31,16.04 14.33,17.5 12,17.5M16.18,11L15.12,9.94L14.06,11L13,9.94L15.12,7.82L17.24,9.94L16.18,11Z";
    private const string Rating3 = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M7,9.5C7,8.7 7.7,8 8.5,8C9.3,8 10,8.7 10,9.5C10,10.3 9.3,11 8.5,11C7.7,11 7,10.3 7,9.5M12,17.23C10.25,17.23 8.71,16.5 7.81,15.42L9.23,14C9.68,14.72 10.75,15.23 12,15.23C13.25,15.23 14.32,14.72 14.77,14L16.19,15.42C15.29,16.5 13.75,17.23 12,17.23M15.5,11C14.7,11 14,10.3 14,9.5C14,8.7 14.7,8 15.5,8C16.3,8 17,8.7 17,9.5C17,10.3 16.3,11 15.5,11Z";
    private const string Rating2 = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M7,9.5V8L10,9.5C10,10.3 9.3,11 8.5,11C7.7,11 7,10.3 7,9.5M14.77,17.23C14.32,16.5 13.25,16 12,16C10.75,16 9.68,16.5 9.23,17.23L7.81,15.81C8.71,14.72 10.25,14 12,14C13.75,14 15.29,14.72 16.19,15.81L14.77,17.23M17,9.5C17,10.3 16.3,11 15.5,11C14.7,11 14,10.3 14,9.5L17,8V9.5Z";
    private const string Rating1 = "M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22A10,10 0 0,0 22,12C22,6.47 17.5,2 12,2M6.76,8.82L7.82,7.76L8.88,8.82L9.94,7.76L11,8.82L9.94,9.88L11,10.94L9.94,12L8.88,10.94L7.82,12L6.76,10.94L7.82,9.88L6.76,8.82M6.89,17.5C7.69,15.46 9.67,14 12,14C14.33,14 16.31,15.46 17.11,17.5H6.89M17.24,10.94L16.18,12L15.12,10.94L14.06,12L13,10.94L14.06,9.88L13,8.82L14.06,7.76L15.12,8.82L16.18,7.76L17.24,8.82L16.18,9.88L17.24,10.94Z";

    public object Convert(object value, Type t, object p, string l) {
        var data = (value is int rating ? rating : 0) switch {
            5 => Rating5,
            4 => Rating4,
            2 => Rating2,
            1 => Rating1,
            _ => Rating3,
        };
        // Geometry は複数の Path で共有できないため毎回生成する
        return Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), data);
    }
    public object ConvertBack(object value, Type t, object p, string l) => throw new NotSupportedException();
}