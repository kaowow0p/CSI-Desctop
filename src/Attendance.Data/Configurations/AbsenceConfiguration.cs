using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class AbsenceConfiguration : IEntityTypeConfiguration<Absence>
{
    public void Configure(EntityTypeBuilder<Absence> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_absences_status", "status IN ('Unexcused', 'Excused')"));

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MarkedAt).HasDefaultValueSql("now()");
        b.HasIndex(x => new { x.StudentId, x.LessonId }).IsUnique();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Lesson).WithMany(l => l.Absences).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.MarkedBy).WithMany().HasForeignKey(x => x.MarkedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExcuseDocument).WithMany().HasForeignKey(x => x.ExcuseDocumentId).OnDelete(DeleteBehavior.SetNull);
    }
}
