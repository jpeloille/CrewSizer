using System.Globalization;
using System.Xml.Linq;
using CrewSizer.Models;

namespace CrewSizer.IO;

/// <summary>Cellule parsée d'un fichier Excel XML Spreadsheet</summary>
public record ExcelCell(string? Value, string? DataType, string? StyleId);

/// <summary>Parseur générique pour les fichiers Excel XML Spreadsheet (format mso-application)</summary>
public static class ExcelXmlParser
{
    private static readonly XNamespace SS = "urn:schemas-microsoft-com:office:spreadsheet";

    /// <summary>Parse un worksheet et retourne les lignes sous forme de listes de ExcelCell</summary>
    public static List<List<ExcelCell>> ParseWorksheet(string chemin, int worksheetIndex = 0)
    {
        var doc = XDocument.Load(chemin);
        var workbook = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var worksheet = workbook.Elements(SS + "Worksheet").ElementAt(worksheetIndex);
        var table = worksheet.Element(SS + "Table")
                    ?? throw new InvalidOperationException("Pas de Table dans le Worksheet");

        var rows = new List<List<ExcelCell>>();

        foreach (var row in table.Elements(SS + "Row"))
        {
            var cells = new List<ExcelCell>();

            foreach (var cell in row.Elements(SS + "Cell"))
            {
                var mergeAcross = cell.Attribute(SS + "MergeAcross");
                int mergeCount = mergeAcross != null
                    ? int.Parse(mergeAcross.Value, CultureInfo.InvariantCulture)
                    : 0;

                var styleId = cell.Attribute(SS + "StyleID")?.Value;
                var data = cell.Element(SS + "Data");

                string? value = data?.Value;
                string? dataType = data?.Attribute(SS + "Type")?.Value;

                cells.Add(new ExcelCell(value, dataType, styleId));

                for (int m = 0; m < mergeCount; m++)
                    cells.Add(new ExcelCell(null, null, styleId));
            }

            rows.Add(cells);
        }

        return rows;
    }

    /// <summary>Extrait le mapping StyleID → couleur intérieure depuis le bloc Styles</summary>
    public static Dictionary<string, string> ParseStyleColors(string chemin)
    {
        var doc = XDocument.Load(chemin);
        var workbook = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var styles = workbook.Element(SS + "Styles");
        if (styles == null) return new Dictionary<string, string>();

        var dict = new Dictionary<string, string>();
        foreach (var style in styles.Elements(SS + "Style"))
        {
            var id = style.Attribute(SS + "ID")?.Value;
            var interior = style.Element(SS + "Interior");
            var color = interior?.Attribute(SS + "Color")?.Value;
            if (id != null && color != null)
                dict[id] = color;
        }
        return dict;
    }

    /// <summary>Parse une valeur DateTime ISO depuis le format Excel XML</summary>
    public static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dt))
            return dt;
        return null;
    }

    /// <summary>Détermine le StatutCheck à partir de la couleur du style</summary>
    public static StatutCheck CouleurVersStatut(string? couleur) => couleur?.ToUpperInvariant() switch
    {
        "#00FF00" => StatutCheck.Valide,
        "#FF9900" => StatutCheck.ExpirationProche,
        "#FFFF00" => StatutCheck.Avertissement,
        "#FF99CC" => StatutCheck.Expire,
        _ => StatutCheck.NonApplicable
    };

    public static string? CellValue(List<ExcelCell> row, int index) =>
        index < row.Count ? row[index].Value : null;

    public static bool CellBool(List<ExcelCell> row, int index) =>
        string.Equals(CellValue(row, index), "True", StringComparison.OrdinalIgnoreCase);

    public static int CellInt(List<ExcelCell> row, int index)
    {
        var val = CellValue(row, index);
        if (string.IsNullOrEmpty(val)) return 0;
        return int.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
