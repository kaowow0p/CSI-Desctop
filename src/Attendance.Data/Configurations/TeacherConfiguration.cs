using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.Ignore(x => x.FullName);
        b.Property(x => x.LastName).HasMaxLength(50);
        b.Property(x => x.FirstName).HasMaxLength(50);
        b.Property(x => x.MiddleName).HasMaxLength(50);
        b.HasIndex(x => x.UserId).IsUnique();

        b.HasOne(x => x.User).WithOne(u => u.Teacher).HasForeignKey<Teacher>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
