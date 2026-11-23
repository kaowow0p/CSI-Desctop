namespace Attendance.Services;

/// <summary>Ошибка бизнес-логики, текст которой можно показать пользователю.</summary>
public class ServiceException(string message) : Exception(message);
