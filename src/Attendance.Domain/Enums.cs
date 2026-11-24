namespace Attendance.Domain;

public enum UserRole
{
    Admin = 1,
    Curator = 2,
    Teacher = 3,
    Headman = 4,
    Student = 5
}

public enum AbsenceStatus
{
    Unexcused,
    Excused
}

public enum DocumentType
{
    MedicalCertificate,
    Order,
    Statement
}

public enum DocumentStatus
{
    Pending,
    Approved,
    Rejected
}
