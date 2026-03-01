using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class BlocTypeConfig : IEntityTypeConfiguration<BlocType>
{
    public void Configure(EntityTypeBuilder<BlocType> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Libelle).HasMaxLength(100).IsRequired();
        builder.Property(e => e.DebutPlage).HasMaxLength(5);
        builder.Property(e => e.FinPlage).HasMaxLength(5);

        builder.HasIndex(e => e.Code).IsUnique();
    }
}
