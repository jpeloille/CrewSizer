using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class SemaineTypeConfig : IEntityTypeConfiguration<SemaineType>
{
    public void Configure(EntityTypeBuilder<SemaineType> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Reference).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Saison).HasMaxLength(50);

        builder.HasIndex(e => e.Reference).IsUnique();

        // Placements : possédés
        builder.OwnsMany(e => e.Placements, b =>
        {
            b.ToJson("placements");
        });

        // Blocs : navigation transiente, pas persistée
        builder.Ignore(e => e.Blocs);
    }
}
