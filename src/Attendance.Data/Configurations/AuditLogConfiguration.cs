using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_log", t => t.HasCheckConstraint("ck_audit_log_action", "action IN ('INSERT', 'UPDATE', 'DELETE')"));

        b.Property(x => x.Action).HasMaxLength(10);
        b.Property(x => x.TableName).HasMaxLength(63);
        b.Property(x => x.ChangedAt).HasDefaultValueSql("now()");
        b.Property(x => x.OldValues).HasColumnType("jsonb");
        b.Property(x => x.NewValues).HasColumnType("jsonb");
        b.HasIndex(x => x.ChangedAt);
        b.HasIndex(x => new { x.TableName, x.RecordId });

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
