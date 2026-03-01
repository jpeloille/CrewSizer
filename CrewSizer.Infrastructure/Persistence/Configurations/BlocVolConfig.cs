using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class BlocVolConfig : IEntityTypeConfiguration<BlocVol>
{
    public void Configure(EntityTypeBuilder<BlocVol> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Jour).HasMaxLength(20);
        builder.Property(e => e.Periode).HasMaxLength(50);
        builder.Property(e => e.DebutDP).HasMaxLength(5);
        builder.Property(e => e.FinDP).HasMaxLength(5);
        builder.Property(e => e.DebutFDP).HasMaxLength(5);
        builder.Property(e => e.FinFDP).HasMaxLength(5);

        builder.HasIndex(e => e.Code).IsUnique();

        // Étapes : possédées
        builder.OwnsMany(e => e.Etapes, b =>
        {
            b.ToJson("etapes");
        });

        // Vols : navigation transiente, pas persistée (hydratée par CatalogueResolver)
        builder.Ignore(e => e.Vols);

        // Propriétés calculées ignorées
        builder.Ignore(e => e.Nom);
        builder.Ignore(e => e.JourIndex);
        builder.Ignore(e => e.NbEtapes);
        builder.Ignore(e => e.HdvBloc);
        builder.Ignore(e => e.DureeDPHeures);
        builder.Ignore(e => e.DureeFDPHeures);
        builder.Ignore(e => e.DureeTSHeures);
        builder.Ignore(e => e.DureeTSVHeures);
        builder.Ignore(e => e.JourNom);

        // FK nullable vers BlocType
        builder.HasOne(e => e.BlocType)
            .WithMany()
            .HasForeignKey(e => e.BlocTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK obligatoire vers TypeAvion
        builder.HasOne(e => e.TypeAvion)
            .WithMany()
            .HasForeignKey(e => e.TypeAvionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
