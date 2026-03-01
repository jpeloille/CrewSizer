using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewSizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlocType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "bloc_type_id",
                table: "blocs_vol",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bloc_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    debut_plage = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    fin_plage = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    fdp_max = table.Column<double>(type: "double precision", nullable: false),
                    haute_saison = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_bloc_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_blocs_vol_bloc_type_id",
                table: "blocs_vol",
                column: "bloc_type_id");

            migrationBuilder.CreateIndex(
                name: "i_x_bloc_types_code",
                table: "bloc_types",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "f_k_blocs_vol_bloc_types_bloc_type_id",
                table: "blocs_vol",
                column: "bloc_type_id",
                principalTable: "bloc_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_blocs_vol_bloc_types_bloc_type_id",
                table: "blocs_vol");

            migrationBuilder.DropTable(
                name: "bloc_types");

            migrationBuilder.DropIndex(
                name: "i_x_blocs_vol_bloc_type_id",
                table: "blocs_vol");

            migrationBuilder.DropColumn(
                name: "bloc_type_id",
                table: "blocs_vol");
        }
    }
}
