using System.Globalization;
using System.IO.Compression;
using System.Xml;
using StarGymApp.Core.Models;

namespace StarGymApp.Core.Services;

public sealed class MemberExcelBackupService
{
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypeNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly DateTime ExcelEpoch = new(1899, 12, 30);
    private static readonly string[] Headers =
    {
        "Badge ID",
        "Member name",
        "Age",
        "Membership plan",
        "Subscription period (months)",
        "Membership price",
        "Status",
        "Term start",
        "Term expiry"
    };

    public void Export(string filePath, IEnumerable<Member> members, IEnumerable<MembershipPlan> plans)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(plans);

        var planLookup = plans.ToDictionary(plan => plan.Id);
        var rows = members
            .OrderBy(member => member.Id)
            .Select(member => CreateRow(member, planLookup))
            .ToList();
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The Excel backup path must include a directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false))
            {
                WriteContentTypes(archive);
                WritePackageRelationships(archive);
                WriteWorkbook(archive);
                WriteWorkbookRelationships(archive);
                WriteStyles(archive);
                WriteWorksheet(archive, rows);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static object?[] CreateRow(Member member, IReadOnlyDictionary<int, MembershipPlan> plans)
    {
        plans.TryGetValue(member.MembershipPlanId, out var plan);
        var durationMonths = member.MembershipDurationMonths is >= 1 and <= 12
            ? member.MembershipDurationMonths
            : plan is null ? 0 : Math.Clamp((plan.DurationDays + 29) / 30, 1, 12);
        return new object?[]
        {
            member.BadgeId,
            member.FullName,
            member.Age,
            plan?.Name ?? string.Empty,
            durationMonths,
            member.MembershipPrice,
            member.SubscriptionStatus,
            member.JoinDate,
            member.GetMembershipExpiry(plan)
        };
    }

    private static void WriteWorksheet(ZipArchive archive, IReadOnlyList<object?[]> rows) =>
        WriteXmlEntry(archive, "xl/worksheets/sheet1.xml", writer =>
        {
            writer.WriteStartElement("worksheet", SpreadsheetNamespace);
            writer.WriteStartElement("sheetViews", SpreadsheetNamespace);
            writer.WriteStartElement("sheetView", SpreadsheetNamespace);
            writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteStartElement("pane", SpreadsheetNamespace);
            writer.WriteAttributeString("ySplit", "1");
            writer.WriteAttributeString("topLeftCell", "A2");
            writer.WriteAttributeString("activePane", "bottomLeft");
            writer.WriteAttributeString("state", "frozen");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("sheetFormatPr", SpreadsheetNamespace);
            writer.WriteAttributeString("defaultRowHeight", "18");
            writer.WriteEndElement();

            writer.WriteStartElement("cols", SpreadsheetNamespace);
            WriteColumn(writer, 1, 1, 17);
            WriteColumn(writer, 2, 2, 28);
            WriteColumn(writer, 3, 3, 10);
            WriteColumn(writer, 4, 4, 23);
            WriteColumn(writer, 5, 5, 28);
            WriteColumn(writer, 6, 6, 18);
            WriteColumn(writer, 7, 7, 18);
            WriteColumn(writer, 8, 9, 16);
            writer.WriteEndElement();

            writer.WriteStartElement("sheetData", SpreadsheetNamespace);
            writer.WriteStartElement("row", SpreadsheetNamespace);
            writer.WriteAttributeString("r", "1");
            for (var column = 0; column < Headers.Length; column++)
            {
                WriteInlineStringCell(writer, $"{ColumnName(column + 1)}1", Headers[column], style: 1);
            }
            writer.WriteEndElement();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var excelRow = rowIndex + 2;
                writer.WriteStartElement("row", SpreadsheetNamespace);
                writer.WriteAttributeString("r", excelRow.ToString(CultureInfo.InvariantCulture));
                for (var column = 0; column < rows[rowIndex].Length; column++)
                {
                    var value = rows[rowIndex][column];
                    var cellReference = $"{ColumnName(column + 1)}{excelRow}";
                    switch (value)
                    {
                        case string text:
                            WriteInlineStringCell(writer, cellReference, text);
                            break;
                        case int integer:
                            WriteNumberCell(writer, cellReference, integer.ToString(CultureInfo.InvariantCulture));
                            break;
                        case decimal amount:
                            WriteNumberCell(writer, cellReference, amount.ToString(CultureInfo.InvariantCulture), style: 3);
                            break;
                        case DateTime date:
                            WriteNumberCell(writer, cellReference, (date.Date - ExcelEpoch).TotalDays.ToString(CultureInfo.InvariantCulture), style: 2);
                            break;
                        case null:
                            WriteInlineStringCell(writer, cellReference, string.Empty);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported Excel backup value type: {value.GetType().Name}.");
                    }
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("autoFilter", SpreadsheetNamespace);
            writer.WriteAttributeString("ref", $"A1:I{rows.Count + 1}");
            writer.WriteEndElement();
            writer.WriteEndElement();
        });

    private static void WriteColumn(XmlWriter writer, int minimum, int maximum, double width)
    {
        writer.WriteStartElement("col", SpreadsheetNamespace);
        writer.WriteAttributeString("min", minimum.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("max", maximum.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("customWidth", "1");
        writer.WriteEndElement();
    }

    private static void WriteInlineStringCell(XmlWriter writer, string reference, string value, int? style = null)
    {
        writer.WriteStartElement("c", SpreadsheetNamespace);
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("t", "inlineStr");
        if (style is not null)
        {
            writer.WriteAttributeString("s", style.Value.ToString(CultureInfo.InvariantCulture));
        }
        writer.WriteStartElement("is", SpreadsheetNamespace);
        writer.WriteStartElement("t", SpreadsheetNamespace);
        writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
        writer.WriteString(value);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteNumberCell(XmlWriter writer, string reference, string value, int? style = null)
    {
        writer.WriteStartElement("c", SpreadsheetNamespace);
        writer.WriteAttributeString("r", reference);
        if (style is not null)
        {
            writer.WriteAttributeString("s", style.Value.ToString(CultureInfo.InvariantCulture));
        }
        writer.WriteElementString("v", SpreadsheetNamespace, value);
        writer.WriteEndElement();
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name;
    }

    private static void WriteContentTypes(ZipArchive archive) =>
        WriteXmlEntry(archive, "[Content_Types].xml", writer =>
        {
            writer.WriteStartElement("Types", ContentTypeNamespace);
            WriteContentTypeDefault(writer, "rels", "application/vnd.openxmlformats-package.relationships+xml");
            WriteContentTypeDefault(writer, "xml", "application/xml");
            WriteContentTypeOverride(writer, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
            WriteContentTypeOverride(writer, "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            WriteContentTypeOverride(writer, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
            writer.WriteEndElement();
        });

    private static void WriteContentTypeDefault(XmlWriter writer, string extension, string contentType)
    {
        writer.WriteStartElement("Default", ContentTypeNamespace);
        writer.WriteAttributeString("Extension", extension);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteContentTypeOverride(XmlWriter writer, string partName, string contentType)
    {
        writer.WriteStartElement("Override", ContentTypeNamespace);
        writer.WriteAttributeString("PartName", partName);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WritePackageRelationships(ZipArchive archive) =>
        WriteXmlEntry(archive, "_rels/.rels", writer =>
        {
            writer.WriteStartElement("Relationships", PackageRelationshipNamespace);
            WriteRelationship(writer, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "xl/workbook.xml");
            writer.WriteEndElement();
        });

    private static void WriteWorkbook(ZipArchive archive) =>
        WriteXmlEntry(archive, "xl/workbook.xml", writer =>
        {
            writer.WriteStartElement("workbook", SpreadsheetNamespace);
            writer.WriteAttributeString("xmlns", "r", null, RelationshipNamespace);
            writer.WriteStartElement("sheets", SpreadsheetNamespace);
            writer.WriteStartElement("sheet", SpreadsheetNamespace);
            writer.WriteAttributeString("name", "Member Backup");
            writer.WriteAttributeString("sheetId", "1");
            writer.WriteAttributeString("r", "id", RelationshipNamespace, "rId1");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        });

    private static void WriteWorkbookRelationships(ZipArchive archive) =>
        WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", writer =>
        {
            writer.WriteStartElement("Relationships", PackageRelationshipNamespace);
            WriteRelationship(writer, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", "worksheets/sheet1.xml");
            WriteRelationship(writer, "rId2", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles", "styles.xml");
            writer.WriteEndElement();
        });

    private static void WriteRelationship(XmlWriter writer, string id, string type, string target)
    {
        writer.WriteStartElement("Relationship", PackageRelationshipNamespace);
        writer.WriteAttributeString("Id", id);
        writer.WriteAttributeString("Type", type);
        writer.WriteAttributeString("Target", target);
        writer.WriteEndElement();
    }

    private static void WriteStyles(ZipArchive archive) =>
        WriteXmlEntry(archive, "xl/styles.xml", writer =>
        {
            writer.WriteStartElement("styleSheet", SpreadsheetNamespace);
            writer.WriteStartElement("numFmts", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("numFmt", SpreadsheetNamespace);
            writer.WriteAttributeString("numFmtId", "164");
            writer.WriteAttributeString("formatCode", "#,##0.00");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("fonts", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("font", SpreadsheetNamespace);
            writer.WriteElementString("sz", SpreadsheetNamespace, "11");
            writer.WriteElementString("name", SpreadsheetNamespace, "Calibri");
            writer.WriteEndElement();
            writer.WriteStartElement("font", SpreadsheetNamespace);
            writer.WriteStartElement("b", SpreadsheetNamespace);
            writer.WriteEndElement();
            writer.WriteStartElement("color", SpreadsheetNamespace);
            writer.WriteAttributeString("rgb", "FFFFFFFF");
            writer.WriteEndElement();
            writer.WriteElementString("sz", SpreadsheetNamespace, "11");
            writer.WriteElementString("name", SpreadsheetNamespace, "Calibri");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("fills", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "3");
            WritePatternFill(writer, "none");
            WritePatternFill(writer, "gray125");
            writer.WriteStartElement("fill", SpreadsheetNamespace);
            writer.WriteStartElement("patternFill", SpreadsheetNamespace);
            writer.WriteAttributeString("patternType", "solid");
            writer.WriteStartElement("fgColor", SpreadsheetNamespace);
            writer.WriteAttributeString("rgb", "FF0B2E68");
            writer.WriteEndElement();
            writer.WriteStartElement("bgColor", SpreadsheetNamespace);
            writer.WriteAttributeString("indexed", "64");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("borders", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("border", SpreadsheetNamespace);
            writer.WriteElementString("left", SpreadsheetNamespace, string.Empty);
            writer.WriteElementString("right", SpreadsheetNamespace, string.Empty);
            writer.WriteElementString("top", SpreadsheetNamespace, string.Empty);
            writer.WriteElementString("bottom", SpreadsheetNamespace, string.Empty);
            writer.WriteElementString("diagonal", SpreadsheetNamespace, string.Empty);
            writer.WriteEndElement();
            writer.WriteStartElement("border", SpreadsheetNamespace);
            writer.WriteElementString("left", SpreadsheetNamespace, string.Empty);
            writer.WriteElementString("right", SpreadsheetNamespace, string.Empty);
            writer.WriteElementString("top", SpreadsheetNamespace, string.Empty);
            writer.WriteStartElement("bottom", SpreadsheetNamespace);
            writer.WriteAttributeString("style", "thin");
            writer.WriteStartElement("color", SpreadsheetNamespace);
            writer.WriteAttributeString("rgb", "FFB9D7FF");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteElementString("diagonal", SpreadsheetNamespace, string.Empty);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("cellStyleXfs", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "1");
            WriteFormat(writer, 0, 0, 0, 0);
            writer.WriteEndElement();

            writer.WriteStartElement("cellXfs", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "4");
            WriteFormat(writer, 0, 0, 0, 0);
            WriteFormat(writer, 0, 1, 2, 1, applyAlignment: true);
            WriteFormat(writer, 14, 0, 0, 0);
            WriteFormat(writer, 164, 0, 0, 0);
            writer.WriteEndElement();

            writer.WriteStartElement("cellStyles", SpreadsheetNamespace);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("cellStyle", SpreadsheetNamespace);
            writer.WriteAttributeString("name", "Normal");
            writer.WriteAttributeString("xfId", "0");
            writer.WriteAttributeString("builtinId", "0");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        });

    private static void WritePatternFill(XmlWriter writer, string pattern)
    {
        writer.WriteStartElement("fill", SpreadsheetNamespace);
        writer.WriteStartElement("patternFill", SpreadsheetNamespace);
        writer.WriteAttributeString("patternType", pattern);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteFormat(XmlWriter writer, int numberFormat, int font, int fill, int border, bool applyAlignment = false)
    {
        writer.WriteStartElement("xf", SpreadsheetNamespace);
        writer.WriteAttributeString("numFmtId", numberFormat.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fontId", font.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fillId", fill.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("borderId", border.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("xfId", "0");
        if (numberFormat != 0) writer.WriteAttributeString("applyNumberFormat", "1");
        if (font != 0) writer.WriteAttributeString("applyFont", "1");
        if (fill != 0) writer.WriteAttributeString("applyFill", "1");
        if (border != 0) writer.WriteAttributeString("applyBorder", "1");
        if (applyAlignment)
        {
            writer.WriteAttributeString("applyAlignment", "1");
            writer.WriteStartElement("alignment", SpreadsheetNamespace);
            writer.WriteAttributeString("vertical", "center");
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static void WriteXmlEntry(ZipArchive archive, string name, Action<XmlWriter> write)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            CloseOutput = false
        });
        write(writer);
    }
}
