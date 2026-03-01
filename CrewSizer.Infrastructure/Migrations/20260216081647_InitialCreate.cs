using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewSizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "affectations_equipage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    activite = table.Column<int>(type: "integer", nullable: false),
                    bloc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bloc_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    commentaire = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    heures_vol = table.Column<double>(type: "double precision", nullable: false),
                    temps_service = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_affectations_equipage", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blocs_vol",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    jour = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    periode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    debut_dp = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    fin_dp = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    debut_fdp = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    fin_fdp = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    etapes = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_blocs_vol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "competences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    groupe = table.Column<int>(type: "integer", nullable: false),
                    checks_requis = table.Column<List<string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_competences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "definitions_check",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    primaire = table.Column<bool>(type: "boolean", nullable: false),
                    groupe = table.Column<int>(type: "integer", nullable: false),
                    validite_nombre = table.Column<int>(type: "integer", nullable: false),
                    validite_unite = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fin_de_mois = table.Column<bool>(type: "boolean", nullable: false),
                    fin_d_annee = table.Column<bool>(type: "boolean", nullable: false),
                    renouvellement_nombre = table.Column<int>(type: "integer", nullable: false),
                    renouvellement_unite = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    avertissement_nombre = table.Column<int>(type: "integer", nullable: false),
                    avertissement_unite = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_definitions_check", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disponibilites_membre",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motif = table.Column<int>(type: "integer", nullable: false),
                    date_debut = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    commentaire = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_disponibilites_membre", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historiques_hdv",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date_releve = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cumul28j = table.Column<double>(type: "double precision", nullable: false),
                    cumul90j = table.Column<double>(type: "double precision", nullable: false),
                    cumul12m = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_historiques_hdv", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "membres_equipage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false),
                    contrat = table.Column<int>(type: "integer", nullable: false),
                    grade = table.Column<int>(type: "integer", nullable: false),
                    matricule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date_entree = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    roles = table.Column<List<string>>(type: "jsonb", nullable: false),
                    categorie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    regles_applicables = table.Column<List<string>>(type: "jsonb", nullable: false),
                    bases = table.Column<List<string>>(type: "jsonb", nullable: false),
                    type_avion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    qualifications = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_membres_equipage", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_modification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cree_par = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modifie_par = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    periode_mois = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    periode_annee = table.Column<int>(type: "integer", nullable: false),
                    periode_nb_jours = table.Column<int>(type: "integer", nullable: false),
                    effectif_cdb = table.Column<int>(type: "integer", nullable: false),
                    effectif_opl = table.Column<int>(type: "integer", nullable: false),
                    effectif_cc = table.Column<int>(type: "integer", nullable: false),
                    effectif_pnc = table.Column<int>(type: "integer", nullable: false),
                    ftl_tsv_max_journalier = table.Column<double>(type: "double precision", nullable: false),
                    ftl_tsv_moyen_retenu = table.Column<double>(type: "double precision", nullable: false),
                    ftl_repos_minimum = table.Column<double>(type: "double precision", nullable: false),
                    cumul_h28_max = table.Column<double>(type: "double precision", nullable: false),
                    cumul_h90_max = table.Column<double>(type: "double precision", nullable: false),
                    cumul_h12_max = table.Column<double>(type: "double precision", nullable: false),
                    cumul_pnt_28_entrant = table.Column<double>(type: "double precision", nullable: false),
                    cumul_pnt_90_entrant = table.Column<double>(type: "double precision", nullable: false),
                    cumul_pnt_12_entrant = table.Column<double>(type: "double precision", nullable: false),
                    cumul_pnc_28_entrant = table.Column<double>(type: "double precision", nullable: false),
                    cumul_pnc_90_entrant = table.Column<double>(type: "double precision", nullable: false),
                    cumul_pnc_12_entrant = table.Column<double>(type: "double precision", nullable: false),
                    jours_off_reglementaire = table.Column<int>(type: "integer", nullable: false),
                    jours_off_accord_entreprise = table.Column<int>(type: "integer", nullable: false),
                    ts_max_7j = table.Column<double>(type: "double precision", nullable: false),
                    ts_max_14j = table.Column<double>(type: "double precision", nullable: false),
                    ts_max_28j = table.Column<double>(type: "double precision", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    abattements_pnc = table.Column<string>(type: "jsonb", nullable: true),
                    abattements_pnt = table.Column<string>(type: "jsonb", nullable: true),
                    calendrier = table.Column<string>(type: "jsonb", nullable: true),
                    fonctions_sol_pnc = table.Column<string>(type: "jsonb", nullable: true),
                    fonctions_sol_pnt = table.Column<string>(type: "jsonb", nullable: true),
                    table_tsv_max = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "semaines_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    saison = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    placements = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_semaines_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vols",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    depart = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    arrivee = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    heure_depart = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    heure_arrivee = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    mh = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_vols", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_calcul = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    calcule_par = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    taux_engagement_global = table.Column<double>(type: "double precision", nullable: false),
                    statut_global = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    categorie_contraignante = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_blocs = table.Column<int>(type: "integer", nullable: false),
                    total_hdv = table.Column<double>(type: "double precision", nullable: false),
                    rotations = table.Column<int>(type: "integer", nullable: false),
                    resultat_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "f_k_snapshots__scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_affectations_equipage_membre_id_date",
                table: "affectations_equipage",
                columns: new[] { "membre_id", "date" });

            migrationBuilder.CreateIndex(
                name: "i_x_blocs_vol_code",
                table: "blocs_vol",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_competences_code",
                table: "competences",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_definitions_check_code",
                table: "definitions_check",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_disponibilites_membre_membre_id",
                table: "disponibilites_membre",
                column: "membre_id");

            migrationBuilder.CreateIndex(
                name: "i_x_historiques_hdv_membre_id_date_releve",
                table: "historiques_hdv",
                columns: new[] { "membre_id", "date_releve" });

            migrationBuilder.CreateIndex(
                name: "i_x_membres_equipage_code",
                table: "membres_equipage",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_semaines_types_reference",
                table: "semaines_types",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_snapshots_date_calcul",
                table: "snapshots",
                column: "date_calcul");

            migrationBuilder.CreateIndex(
                name: "i_x_snapshots_scenario_id",
                table: "snapshots",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "i_x_vols_numero",
                table: "vols",
                column: "numero");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "affectations_equipage");

            migrationBuilder.DropTable(
                name: "blocs_vol");

            migrationBuilder.DropTable(
                name: "competences");

            migrationBuilder.DropTable(
                name: "definitions_check");

            migrationBuilder.DropTable(
                name: "disponibilites_membre");

            migrationBuilder.DropTable(
                name: "historiques_hdv");

            migrationBuilder.DropTable(
                name: "membres_equipage");

            migrationBuilder.DropTable(
                name: "semaines_types");

            migrationBuilder.DropTable(
                name: "snapshots");

            migrationBuilder.DropTable(
                name: "vols");

            migrationBuilder.DropTable(
                name: "scenarios");
        }
    }
}
