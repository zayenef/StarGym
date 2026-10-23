using System.IO.Compression;
using System.Xml.Linq;
using StarGymApp.Core.Models;
using StarGymApp.Core.Services;
using Xunit;

namespace StarGymApp.Core.Tests;

public class MemberExcelBackupServiceTests
{
    [Fact]
    public void Export_CreatesAndOverwritesMemberWorkbook()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"stargym-excel-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "members.xlsx");
        var member = new Member
        {
            Id = 1,
            BadgeId = "10000001",
            FullName = "Marta & Sons",
            DateOfBirth = DateTime.Today.AddYears(-28),
            MembershipPlanId = 3,
            MembershipDurationMonths = 6,
            MembershipPrice = 1234.50m,
            JoinDate = DateTime.Today,
            Status = "Active"
        };
        var plan = new MembershipPlan { Id = 3, Name = "Strength", DurationDays = 180 };
        var service = new MemberExcelBackupService();

        try
        {
            service.Export(filePath, new[] { member }, new[] { plan });
            member.MembershipDurationMonths = 12;
            member.MembershipPrice = 2500;
            service.Export(filePath, new[] { member }, new[] { plan });

            using var archive = ZipFile.OpenRead(filePath);
            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            {
                using var xmlStream = entry.Open();
                XDocument.Load(xmlStream);
            }

            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            Assert.NotNull(workbookEntry);
            using (var workbookStream = workbookEntry!.Open())
            {
                var workbook = XDocument.Load(workbookStream);
                Assert.Equal("Member Backup", workbook.Descendants(spreadsheet + "sheet").Single().Attribute("name")?.Value);
            }

            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
            Assert.NotNull(sheetEntry);
            using var sheetStream = sheetEntry!.Open();
            var sheet = XDocument.Load(sheetStream);
            var cells = sheet.Descendants(spreadsheet + "c")
                .ToDictionary(
                    cell => (string)cell.Attribute("r")!,
                    cell => cell.Element(spreadsheet + "is")?.Element(spreadsheet + "t")?.Value
                        ?? cell.Element(spreadsheet + "v")?.Value);

            Assert.Equal("Badge ID", cells["A1"]);
            Assert.Equal("Member name", cells["B1"]);
            Assert.Equal("10000001", cells["A2"]);
            Assert.Equal("Marta & Sons", cells["B2"]);
            Assert.Equal("28", cells["C2"]);
            Assert.Equal("Strength", cells["D2"]);
            Assert.Equal("12", cells["E2"]);
            Assert.Equal("2500", cells["F2"]);
            Assert.Single(sheet.Descendants(spreadsheet + "row"), row => (string?)row.Attribute("r") != "1");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
