using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Attendance.Desktop;

/// <summary>DatePicker работает с DateTime, а сущности — с DateOnly.</summary>
public sealed class DateOnlyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateOnly date ? date.ToDateTime(TimeOnly.MinValue) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : DependencyProperty.UnsetValue;
}

/// <summary>Отметки времени из БД приходят в UTC — показываем в местном времени.</summary>
public sealed class LocalTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dateTime ? dateTime.ToLocalTime().ToString((string?)parameter ?? "g", culture) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
