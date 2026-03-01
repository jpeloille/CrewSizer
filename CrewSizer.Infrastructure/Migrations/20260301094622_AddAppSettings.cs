using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CrewSizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_app_settings", x => x.key);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "key", "description", "value" },
                values: new object[,]
                {
                    { "solver.deterministic", "Mode déterministe du solver CP-SAT (plus lent mais reproductible)", "false" },
                    { "solver.timeout", "Timeout du solver en secondes", "30" },
                    { "solver.workers", "Nombre de workers parallèles (0 = automatique selon CPU)", "0" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");
        }
    }
}
