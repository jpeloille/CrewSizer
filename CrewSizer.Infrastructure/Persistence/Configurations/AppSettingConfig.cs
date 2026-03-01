using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class AppSettingConfig : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasMaxLength(100);
        builder.Property(e => e.Value).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasData(
            new AppSetting
            {
                Key = "solver.deterministic",
                Value = "false",
                Description = "Mode déterministe du solver CP-SAT (plus lent mais reproductible)"
            },
            new AppSetting
            {
                Key = "solver.timeout",
                Value = "30",
                Description = "Timeout du solver en secondes"
            },
            new AppSetting
            {
                Key = "solver.workers",
                Value = "0",
                Description = "Nombre de workers parallèles (0 = automatique selon CPU)"
            });
    }
}
