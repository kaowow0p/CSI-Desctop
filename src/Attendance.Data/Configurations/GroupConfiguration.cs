using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_groups_course", "course BETWEEN 1 AND 6"));

        b.Property(x => x.Name).HasMaxLength(20);
        b.Property(x => x.Specialty).HasMaxLength(150);
        b.HasIndex(x => x.Name).IsUnique();

        b.HasOne(x => x.Curator).WithMany().HasForeignKey(x => x.CuratorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
