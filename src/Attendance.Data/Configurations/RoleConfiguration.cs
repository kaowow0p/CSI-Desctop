using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(50);
        b.HasIndex(x => x.Name).IsUnique();

        b.HasData(
            new Role { Id = UserRole.Admin, Name = "Администратор" },
            new Role { Id = UserRole.Curator, Name = "Куратор" },
            new Role { Id = UserRole.Teacher, Name = "Преподаватель" },
            new Role { Id = UserRole.Headman, Name = "Староста" },
            new Role { Id = UserRole.Student, Name = "Студент" });
    }
}
