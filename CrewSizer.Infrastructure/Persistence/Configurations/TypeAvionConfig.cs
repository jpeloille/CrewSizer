using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class TypeAvionConfig : IEntityTypeConfiguration<TypeAvion>
{
    public void Configure(EntityTypeBuilder<TypeAvion> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Libelle).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
    }
}
