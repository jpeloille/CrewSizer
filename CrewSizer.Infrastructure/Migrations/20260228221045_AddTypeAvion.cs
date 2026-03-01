using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewSizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeAvion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Créer la table types_avion
            migrationBuilder.CreateTable(
                name: "types_avion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nb_cdb = table.Column<int>(type: "integer", nullable: false),
                    nb_opl = table.Column<int>(type: "integer", nullable: false),
                    nb_cc = table.Column<int>(type: "integer", nullable: false),
                    nb_pnc = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_types_avion", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_types_avion_code",
                table: "types_avion",
                column: "code",
                unique: true);

            // 2. Seed ATR 72-600 par défaut
            migrationBuilder.Sql(@"
                INSERT INTO types_avion (id, code, libelle, nb_cdb, nb_opl, nb_cc, nb_pnc)
                VALUES ('00000000-0000-0000-0000-000000000001', 'ATR72', 'ATR 72-600', 1, 1, 1, 0);
            ");

            // 3. Ajouter la colonne FK nullable d'abord
            migrationBuilder.AddColumn<Guid>(
                name: "type_avion_id",
                table: "blocs_vol",
                type: "uuid",
                nullable: true);

            // 4. Backfill avec la valeur par défaut
            migrationBuilder.Sql(@"
                UPDATE blocs_vol SET type_avion_id = '00000000-0000-0000-0000-000000000001'
                WHERE type_avion_id IS NULL;
            ");

            // 5. Rendre la colonne NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "type_avion_id",
                table: "blocs_vol",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 6. Index + FK
            migrationBuilder.CreateIndex(
                name: "i_x_blocs_vol_type_avion_id",
                table: "blocs_vol",
                column: "type_avion_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_blocs_vol__types_avion_type_avion_id",
                table: "blocs_vol",
                column: "type_avion_id",
                principalTable: "types_avion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_blocs_vol__types_avion_type_avion_id",
                table: "blocs_vol");

            migrationBuilder.DropTable(
                name: "types_avion");

            migrationBuilder.DropIndex(
                name: "i_x_blocs_vol_type_avion_id",
                table: "blocs_vol");

            migrationBuilder.DropColumn(
                name: "type_avion_id",
                table: "blocs_vol");
        }
    }
}
