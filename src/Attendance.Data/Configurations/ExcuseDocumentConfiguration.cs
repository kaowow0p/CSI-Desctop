using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class ExcuseDocumentConfiguration : IEntityTypeConfiguration<ExcuseDocument>
{
    public void Configure(EntityTypeBuilder<ExcuseDocument> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_excuse_documents_period", "valid_to >= valid_from");
            t.HasCheckConstraint("ck_excuse_documents_type", "type IN ('MedicalCertificate', 'Order', 'Statement')");
            t.HasCheckConstraint("ck_excuse_documents_status", "status IN ('Pending', 'Approved', 'Rejected')");
        });

        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Number).HasMaxLength(50);
        b.Property(x => x.ReviewComment).HasMaxLength(500);
        b.Property(x => x.RegisteredAt).HasDefaultValueSql("now()");
        b.HasIndex(x => new { x.StudentId, x.ValidFrom, x.ValidTo });
        b.HasIndex(x => x.Status);

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RegisteredBy).WithMany().HasForeignKey(x => x.RegisteredByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
