using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewSizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PeriodeDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Ajouter les nouvelles colonnes
            migrationBuilder.AddColumn<DateOnly>(
                name: "periode_date_debut",
                table: "scenarios",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "periode_date_fin",
                table: "scenarios",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // 2. Migrer les données existantes (mois français → dates)
            migrationBuilder.Sql("""
                UPDATE scenarios SET
                    periode_date_debut = make_date(
                        periode_annee,
                        CASE lower(periode_mois)
                            WHEN 'janvier' THEN 1 WHEN 'février' THEN 2 WHEN 'fevrier' THEN 2
                            WHEN 'mars' THEN 3 WHEN 'avril' THEN 4 WHEN 'mai' THEN 5
                            WHEN 'juin' THEN 6 WHEN 'juillet' THEN 7
                            WHEN 'août' THEN 8 WHEN 'aout' THEN 8
                            WHEN 'septembre' THEN 9 WHEN 'octobre' THEN 10
                            WHEN 'novembre' THEN 11 WHEN 'décembre' THEN 12 WHEN 'decembre' THEN 12
                            ELSE 1
                        END,
                        1
                    ),
                    periode_date_fin = (make_date(
                        periode_annee,
                        CASE lower(periode_mois)
                            WHEN 'janvier' THEN 1 WHEN 'février' THEN 2 WHEN 'fevrier' THEN 2
                            WHEN 'mars' THEN 3 WHEN 'avril' THEN 4 WHEN 'mai' THEN 5
                            WHEN 'juin' THEN 6 WHEN 'juillet' THEN 7
                            WHEN 'août' THEN 8 WHEN 'aout' THEN 8
                            WHEN 'septembre' THEN 9 WHEN 'octobre' THEN 10
                            WHEN 'novembre' THEN 11 WHEN 'décembre' THEN 12 WHEN 'decembre' THEN 12
                            ELSE 1
                        END,
                        1
                    ) + interval '1 month' - interval '1 day')::date
                WHERE periode_annee > 0 AND periode_mois IS NOT NULL AND periode_mois <> '';
                """);

            // 3. Supprimer les anciennes colonnes
            migrationBuilder.DropColumn(
                name: "periode_annee",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "periode_mois",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "periode_nb_jours",
                table: "scenarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "periode_annee",
                table: "scenarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "periode_mois",
                table: "scenarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "periode_nb_jours",
                table: "scenarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Migrer les données en sens inverse
            migrationBuilder.Sql("""
                UPDATE scenarios SET
                    periode_annee = EXTRACT(YEAR FROM periode_date_debut)::int,
                    periode_nb_jours = (periode_date_fin - periode_date_debut + 1),
                    periode_mois = CASE EXTRACT(MONTH FROM periode_date_debut)::int
                        WHEN 1 THEN 'Janvier' WHEN 2 THEN 'Février' WHEN 3 THEN 'Mars'
                        WHEN 4 THEN 'Avril' WHEN 5 THEN 'Mai' WHEN 6 THEN 'Juin'
                        WHEN 7 THEN 'Juillet' WHEN 8 THEN 'Août' WHEN 9 THEN 'Septembre'
                        WHEN 10 THEN 'Octobre' WHEN 11 THEN 'Novembre' WHEN 12 THEN 'Décembre'
                        ELSE 'Janvier'
                    END
                WHERE periode_date_debut IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "periode_date_debut",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "periode_date_fin",
                table: "scenarios");
        }
    }
}
