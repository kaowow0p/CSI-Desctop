using System.Globalization;
using System.Windows.Data;
using Attendance.Domain;

namespace Attendance.Desktop;

public sealed class EnumNameConverter : IValueConverter
{
    public static string Name(object? value) => value switch
    {
        AbsenceStatus.Unexcused => "Неуважительный",
        AbsenceStatus.Excused => "Уважительный",
        DocumentType.MedicalCertificate => "Справка",
        DocumentType.Order => "Приказ",
        DocumentType.Statement => "Заявление",
        DocumentStatus.Pending => "На проверке",
        DocumentStatus.Approved => "Подтверждён",
        DocumentStatus.Rejected => "Отклонён",
        null => "",
        _ => value.ToString() ?? ""
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Name(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
