using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CrewSizer.Domain.Entities;
using CrewSizer.Domain.Enums;

namespace CrewSizer.Infrastructure.Import;

/// <summary>Charge les données équipage depuis les exports Excel XML APM</summary>
public static class EquipageLoader
{
    /// <summary>Charge depuis des streams (upload web)</summary>
    public static DonneesEquipage ChargerDepuisStreams(
        Stream? streamPnt = null,
        Stream? streamPnc = null,
        Stream? streamCheckStatus = null,
        Stream? streamCheckDesc = null)
    {
        var equipage = new DonneesEquipage();
        var membresDict = new Dictionary<string, MembreEquipage>(StringComparer.OrdinalIgnoreCase);

        if (streamCheckDesc != null)
            equipage.Checks = ChargerCheckDescriptions(streamCheckDesc);

        if (streamPnt != null)
            ChargerCrewList(streamPnt, TypeContrat.PNT, membresDict);

        if (streamPnc != null)
            ChargerCrewList(streamPnc, TypeContrat.PNC, membresDict);

        if (streamCheckStatus != null)
            ChargerCheckStatuses(streamCheckStatus, membresDict);

        equipage.Membres = membresDict.Values.OrderBy(m => m.Nom).ToList();
        equipage.DateExtraction = ExtraireDate(streamPnt)
                                  ?? ExtraireDate(streamPnc)
                                  ?? DateTime.Today;
        return equipage;
    }

    /// <summary>Charge depuis des chemins fichiers</summary>
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

    // ── Crew List (Stream) ──

    private static void ChargerCrewList(Stream stream, TypeContrat typeContrat,
        Dictionary<string, MembreEquipage> membresDict)
    {
        var rows = ExcelXmlParser.ParseWorksheet(stream);
        ParseCrewRows(rows, typeContrat, membresDict);
    }

    private static void ChargerCrewList(string chemin, TypeContrat typeContrat,
        Dictionary<string, MembreEquipage> membresDict)
    {
        var rows = ExcelXmlParser.ParseWorksheet(chemin);
        ParseCrewRows(rows, typeContrat, membresDict);
    }

    private static void ParseCrewRows(List<List<ExcelCell>> rows, TypeContrat typeContrat,
        Dictionary<string, MembreEquipage> membresDict)
    {
        for (int i = 2; i < rows.Count; i++)
        {
            var row = rows[i];
            var code = ExcelXmlParser.CellValue(row, 2);
            if (string.IsNullOrWhiteSpace(code)) continue;

            var roles = ParseMultiValues(ExcelXmlParser.CellValue(row, 9), '+');
            var regles = ParseMultiValues(ExcelXmlParser.CellValue(row, 11), '+');

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

    // ── Check Statuses (Stream) ──

    private static void ChargerCheckStatuses(Stream stream,
        Dictionary<string, MembreEquipage> membresDict)
    {
        // On doit lire le stream deux fois (rows + styles), donc on le copie en mémoire
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        ms.Position = 0;
        var rows = ExcelXmlParser.ParseWorksheet(ms);
        ms.Position = 0;
        var styleColors = ExcelXmlParser.ParseStyleColors(ms);

        ParseCheckStatusRows(rows, styleColors, membresDict);
    }

    private static void ChargerCheckStatuses(string chemin,
        Dictionary<string, MembreEquipage> membresDict)
    {
        var rows = ExcelXmlParser.ParseWorksheet(chemin);
        var styleColors = ExcelXmlParser.ParseStyleColors(chemin);
        ParseCheckStatusRows(rows, styleColors, membresDict);
    }

    private static void ParseCheckStatusRows(List<List<ExcelCell>> rows,
        Dictionary<string, string> styleColors,
        Dictionary<string, MembreEquipage> membresDict)
    {
        if (rows.Count < 3) return;

        var headerRow = rows[1];
        var checkCodes = new List<string>();
        for (int col = 2; col < headerRow.Count; col++)
        {
            var headerVal = ExcelXmlParser.CellValue(headerRow, col) ?? "";
            checkCodes.Add(ExtraireCodeCheck(headerVal));
        }

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
        int parenStart = header.LastIndexOf('(');
        int parenEnd = header.LastIndexOf(')');
        if (parenStart >= 0 && parenEnd > parenStart)
            return header[(parenStart + 1)..parenEnd];
        return header;
    }

    // ── Check Descriptions ──

    private static List<DefinitionCheck> ChargerCheckDescriptions(Stream stream)
    {
        var rows = ExcelXmlParser.ParseWorksheet(stream);
        return ParseCheckDescriptionRows(rows);
    }

    private static List<DefinitionCheck> ChargerCheckDescriptions(string chemin)
    {
        var rows = ExcelXmlParser.ParseWorksheet(chemin);
        return ParseCheckDescriptionRows(rows);
    }

    private static List<DefinitionCheck> ParseCheckDescriptionRows(List<List<ExcelCell>> rows)
    {
        var checks = new List<DefinitionCheck>();

        for (int i = 2; i < rows.Count; i++)
        {
            var row = rows[i];
            var code = ExcelXmlParser.CellValue(row, 0);
            if (string.IsNullOrWhiteSpace(code)) continue;

            var groupeRaw = ExcelXmlParser.CellValue(row, 4) ?? "";
            checks.Add(new DefinitionCheck
            {
                Code = code,
                Description = ExcelXmlParser.CellValue(row, 1) ?? "",
                Primaire = ExcelXmlParser.CellBool(row, 2),
                Groupe = groupeRaw.Contains("COCKPIT", StringComparison.OrdinalIgnoreCase)
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

    private static DateTime? ExtraireDate(Stream? stream)
    {
        if (stream == null || !stream.CanRead) return null;
        try
        {
            var pos = stream.Position;
            var rows = ExcelXmlParser.ParseWorksheet(stream);
            if (stream.CanSeek) stream.Position = pos;

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

    private static DateTime? ExtraireDate(string? chemin)
    {
        if (chemin == null || !File.Exists(chemin)) return null;
        try
        {
            using var fs = File.OpenRead(chemin);
            return ExtraireDate(fs);
        }
        catch { return null; }
    }
}
