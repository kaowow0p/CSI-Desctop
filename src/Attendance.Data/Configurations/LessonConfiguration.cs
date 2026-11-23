using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_lessons_pair_number", "pair_number BETWEEN 1 AND 7");
            t.HasCheckConstraint("ck_lessons_hours", "hours > 0");
        });

        b.Property(x => x.Hours).HasDefaultValue((short)2);
        // У группы не может быть двух занятий на одной паре в один день.
        b.HasIndex(x => new { x.GroupId, x.Date, x.PairNumber }).IsUnique();
        b.HasIndex(x => new { x.TeacherId, x.Date });
        b.HasIndex(x => x.SubjectId);

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
