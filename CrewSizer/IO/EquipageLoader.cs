using System.Globalization;
using System.Text.RegularExpressions;
using CrewSizer.Models;

namespace CrewSizer.IO;

/// <summary>Charge les données équipage depuis les exports Excel XML AIMS</summary>
public static class EquipageLoader
{
    /// <summary>Charge les données équipage depuis les 4 fichiers Excel XML (tout paramètre null = ignoré)</summary>
    public static DonneesEquipage Charger(
        string? cheminPnt = null,
        string? cheminPnc = null,
        string? cheminCheckStatus = null,
        string? cheminCheckDesc = null)
    {
        var equipage = new DonneesEquipage();
        var membresDict = new Dictionary<string, MembreEquipage>(StringComparer.OrdinalIgnoreCase);

        if (cheminCheckDesc != null && File.Exists(cheminCheckDesc))
            equipage.Checks = ChargerCheckDescriptions(cheminCheckDesc);

        if (cheminPnt != null && File.Exists(cheminPnt))
            ChargerCrewList(cheminPnt, TypeContrat.PNT, membresDict);

        if (cheminPnc != null && File.Exists(cheminPnc))
            ChargerCrewList(cheminPnc, TypeContrat.PNC, membresDict);

        if (cheminCheckStatus != null && File.Exists(cheminCheckStatus))
            ChargerCheckStatuses(cheminCheckStatus, membresDict);

        equipage.Membres = membresDict.Values.OrderBy(m => m.Nom).ToList();
        equipage.DateExtraction = ExtraireDate(cheminPnt)
                                  ?? ExtraireDate(cheminPnc)
                                  ?? DateTime.Today;
        return equipage;
    }

    /// <summary>Auto-détecte les fichiers dans un dossier et charge</summary>
    public static DonneesEquipage ChargerDepuisDossier(string dossier)
    {
        string? Trouver(string pattern) =>
            Directory.GetFiles(dossier, "*.xml*")
                .FirstOrDefault(f => Regex.IsMatch(Path.GetFileName(f), pattern, RegexOptions.IgnoreCase));

        return Charger(
            cheminPnt: Trouver(@"Pnt.*Crew|CrewList.*Pilot"),
            cheminPnc: Trouver(@"Pnc.*Crew|CrewList.*Cabin"),
            cheminCheckStatus: Trouver(@"CheckStatus|CrewCheck"),
            cheminCheckDesc: Trouver(@"Check.*Desc"));
    }

    // ── Crew List ──

    private static void ChargerCrewList(string chemin, TypeContrat typeContrat,
        Dictionary<string, MembreEquipage> membresDict)
    {
        var rows = ExcelXmlParser.ParseWorksheet(chemin);

        // Row 0 = titre fusionné, Row 1 = en-têtes, Row 2+ = données
        for (int i = 2; i < rows.Count; i++)
        {
            var row = rows[i];
            var code = ExcelXmlParser.CellValue(row, 2);
            if (string.IsNullOrWhiteSpace(code)) continue;

            var roles = ParseMultiValues(ExcelXmlParser.CellValue(row, 9), '+');
            var regles = ParseMultiValues(ExcelXmlParser.CellValue(row, 11), '+');

            // Déduplication : si le membre existe déjà (cas LRS dans PNT+PNC)
            if (membresDict.TryGetValue(code, out var existant))
            {
                foreach (var role in roles)
                    if (!existant.Roles.Contains(role))
                        existant.Roles.Add(role);
                continue;
            }

            var seniorite = ExcelXmlParser.CellValue(row, 4) ?? "";
            membresDict[code] = new MembreEquipage
            {
                Code = code,
                Nom = ExcelXmlParser.CellValue(row, 1) ?? "",
                Actif = ExcelXmlParser.CellBool(row, 0),
                Contrat = typeContrat,
                Grade = DeterminerGrade(seniorite, typeContrat),
                Matricule = ExcelXmlParser.CellValue(row, 6) ?? "",
                DateEntree = ExcelXmlParser.ParseDateTime(ExcelXmlParser.CellValue(row, 7)),
                DateFin = ExcelXmlParser.ParseDateTime(ExcelXmlParser.CellValue(row, 8)),
                Roles = roles,
                Categorie = ExcelXmlParser.CellValue(row, 10) ?? "",
                ReglesApplicables = regles,
                Bases = ParseBases(ExcelXmlParser.CellValue(row, 12)),
                TypeAvion = ExcelXmlParser.CellValue(row, 13) ?? ""
            };
        }
    }

    private static Grade DeterminerGrade(string seniorite, TypeContrat contrat) =>
        seniorite.ToUpperInvariant() switch
        {
            "CDB" => Grade.CDB,
            "OPL" => Grade.OPL,
            "CC" => Grade.CC,
            "PNC" => Grade.PNC,
            _ => contrat == TypeContrat.PNT ? Grade.OPL : Grade.PNC
        };

    private static List<string> ParseMultiValues(string? raw, char separator) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => v.Length > 0)
                .ToList();

    private static List<string> ParseBases(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(b => b.Length >= 3 && b.Trim() != "")
            .Select(b => b.Trim())
            .Distinct()
            .ToList();
    }

    // ── Check Statuses ──

    /// <summary>Applique les statuts de qualification aux membres existants. Retourne le nombre de membres mis à jour.</summary>
    public static int AppliquerCheckStatuses(string chemin, List<MembreEquipage> membres)
    {
        var dict = membres.ToDictionary(m => m.Code, StringComparer.OrdinalIgnoreCase);
        // Effacer les qualifications existantes avant réimport
        foreach (var m in membres) m.Qualifications.Clear();
        ChargerCheckStatuses(chemin, dict);
        return membres.Count(m => m.Qualifications.Count > 0);
    }

    private static void ChargerCheckStatuses(string chemin,
        Dictionary<string, MembreEquipage> membresDict)
    {
        var rows = ExcelXmlParser.ParseWorksheet(chemin);
        var styleColors = ExcelXmlParser.ParseStyleColors(chemin);

        if (rows.Count < 3) return;

        // Row 1 = en-têtes des colonnes de checks (format "DESCRIPTION(CODE)")
        var headerRow = rows[1];
        var checkCodes = new List<string>();
        for (int col = 2; col < headerRow.Count; col++)
        {
            var headerVal = ExcelXmlParser.CellValue(headerRow, col) ?? "";
            checkCodes.Add(ExtraireCodeCheck(headerVal));
        }

        // Row 2+ = données
        for (int i = 2; i < rows.Count; i++)
        {
            var row = rows[i];
            var crewCode = ExcelXmlParser.CellValue(row, 0);
            if (string.IsNullOrWhiteSpace(crewCode)) continue;

            if (!membresDict.TryGetValue(crewCode, out var membre)) continue;

            for (int col = 2; col < row.Count && col - 2 < checkCodes.Count; col++)
            {
                var cell = row[col];
                var checkCode = checkCodes[col - 2];
                if (string.IsNullOrEmpty(checkCode)) continue;

                var date = ExcelXmlParser.ParseDateTime(cell.Value);
                var couleur = cell.StyleId != null && styleColors.TryGetValue(cell.StyleId, out var c)
                    ? c : null;
                var statut = ExcelXmlParser.CouleurVersStatut(couleur);

                // Cellule vide sans couleur = non applicable
                if (date == null && statut == StatutCheck.NonApplicable) continue;

                membre.Qualifications.Add(new StatutQualification
                {
                    CodeCheck = checkCode,
                    DateExpiration = date,
                    Statut = statut
                });
            }
        }
    }

    private static string ExtraireCodeCheck(string header)
    {
        // "ENGLISH LANGUAGE PROFICIENCY(ELP)" → "ELP"
        int parenStart = header.LastIndexOf('(');
        int parenEnd = header.LastIndexOf(')');
        if (parenStart >= 0 && parenEnd > parenStart)
            return header[(parenStart + 1)..parenEnd];
        return header;
    }

    // ── Check Descriptions ──

    private static List<DefinitionCheck> ChargerCheckDescriptions(string chemin)
    {
        var rows = ExcelXmlParser.ParseWorksheet(chemin);
        var checks = new List<DefinitionCheck>();

        // Row 0 = sections fusionnées, Row 1 = en-têtes colonnes, Row 2+ = données
        for (int i = 2; i < rows.Count; i++)
        {
            var row = rows[i];
            var code = ExcelXmlParser.CellValue(row, 0);
            if (string.IsNullOrWhiteSpace(code)) continue;

            checks.Add(new DefinitionCheck
            {
                Code = code,
                Description = ExcelXmlParser.CellValue(row, 1) ?? "",
                Primaire = ExcelXmlParser.CellBool(row, 2),
                Groupe = (ExcelXmlParser.CellValue(row, 4) ?? "").Contains("COCKPIT", StringComparison.OrdinalIgnoreCase)
                    ? GroupeCheck.Cockpit : GroupeCheck.Cabine,
                ValiditeNombre = ExcelXmlParser.CellInt(row, 10),
                ValiditeUnite = ExcelXmlParser.CellValue(row, 11) ?? "",
                FinDeMois = ExcelXmlParser.CellBool(row, 13),
                FinDAnnee = ExcelXmlParser.CellBool(row, 14),
                RenouvellementNombre = ExcelXmlParser.CellInt(row, 15),
                RenouvellementUnite = ExcelXmlParser.CellValue(row, 16) ?? "",
                AvertissementNombre = ExcelXmlParser.CellInt(row, 18),
                AvertissementUnite = ExcelXmlParser.CellValue(row, 19) ?? ""
            });
        }

        return checks;
    }

    // ── Utilitaires ──

    private static DateTime? ExtraireDate(string? chemin)
    {
        if (chemin == null || !File.Exists(chemin)) return null;
        try
        {
            var rows = ExcelXmlParser.ParseWorksheet(chemin);
            if (rows.Count == 0 || rows[0].Count == 0) return null;
            var title = rows[0][0].Value ?? "";
            var match = Regex.Match(title, @"FROM:(\d{2}/\d{2}/\d{4})");
            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value,
                    "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }
        catch { /* fichier non parsable */ }
        return null;
    }
}
