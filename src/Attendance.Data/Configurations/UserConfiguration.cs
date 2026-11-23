using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_users_failed_login_count", "failed_login_count >= 0"));

        b.Property(x => x.Login).HasMaxLength(50);
        b.Property(x => x.PasswordHash).HasMaxLength(100);
        b.Property(x => x.FullName).HasMaxLength(150);
        b.Property(x => x.FailedLoginCount).HasDefaultValue(0);
        b.Property(x => x.IsLocked).HasDefaultValue(false);
        // Sentinel = true: EF не отправляет значение по умолчанию, а явное false сохраняет.
        b.Property(x => x.MustChangePassword).HasDefaultValue(true).HasSentinel(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        b.HasIndex(x => x.Login).IsUnique();

        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
