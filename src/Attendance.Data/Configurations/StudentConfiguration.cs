using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.Ignore(x => x.FullName);
        b.Property(x => x.LastName).HasMaxLength(50);
        b.Property(x => x.FirstName).HasMaxLength(50);
        b.Property(x => x.MiddleName).HasMaxLength(50);
        b.Property(x => x.RecordBookNumber).HasMaxLength(20);
        b.HasIndex(x => x.RecordBookNumber).IsUnique();
        b.HasIndex(x => x.UserId).IsUnique();

        b.HasOne(x => x.Group).WithMany(g => g.Students).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithOne(u => u.Student).HasForeignKey<Student>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
