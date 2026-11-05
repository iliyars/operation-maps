using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Importing;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Infrastructure.Word;
using Xunit.Abstractions;

namespace OperationMaps.Tests.Services;

public sealed class Driver741MatchingTests(ITestOutputHelper output)
{
  [Theory]
  [InlineData(false, false)]
  [InlineData(true, false)]
  [InlineData(false, true)]
  public async Task Diode_2D510_merges_missing_values_but_rejects_conflicting_numbers(bool missingFirst, bool conflict)
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();
    await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
    await db.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(db);
    var factory = TestCatalogDbContextFactory.From(db);
    var parser = new ComponentNameParser();
    var maps = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    var service = new WordCatalogImportService(factory, new ComponentEntryService(factory), parser, maps);
    var scanned = await service.ScanAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "Word", "driver741.docx"));
    var diode = Assert.Single(scanned.Components, c => c.FullName == "2Д510А/ББ ОС");
    Assert.Equal("1500", diode.OwnFormNtdValuesByRow[4]);
    Assert.Equal("75", diode.OwnFormNtdValuesByRow[6]);
    Assert.Equal("125", diode.OwnFormNtdValuesByRow[9]);
    Assert.Equal(4, diode.LoadFactorBaseRow);
    Assert.True(diode.Form4DataFound);
    Assert.False(string.IsNullOrWhiteSpace(diode.ComponentTypeName));
    var repeatedValues = diode.AdditionalVariants.First(v => v.OwnValues.GetValueOrDefault(6) == "—").OwnValues;
    if (missingFirst)
      (diode.OwnFormNtdValuesByRow[6], repeatedValues[6]) = (repeatedValues[6], diode.OwnFormNtdValuesByRow[6]);
    if (conflict)
    {
      repeatedValues[6] = "80";
      await Assert.ThrowsAsync<InvalidOperationException>(() => service.ImportAsync(diode));
      Assert.False(await db.Components.AnyAsync(c => c.FullName == diode.FullName));
      return;
    }
    await service.ImportAsync(diode);
    var saved = await db.Components.Include(c => c.NtdValues).ThenInclude(v => v.FormParameter)
      .SingleAsync(c => c.FullName == "2Д510А/ББ ОС");
    Assert.Equal("75", Assert.Single(saved.NtdValues, v => v.FormParameter.RowNumber == 6).Value);
    await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "Xml", "ВЕИР.468364.741 ПЭ3.PE.XML"));
    var imported = await new Pe3XmlImporter(parser).ImportAsync(stream);
    var result = await new ComponentMatcher(factory, parser).MatchAllAsync(imported.Components);
    Assert.Contains(result.Matched, e => e.Imported.RawName == "2Д510А/ББ ОС");
  }

  [Fact]
  public async Task Real_word_designations_match_xml_formatting_and_keep_substantive_differences_unresolved()
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();
    await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
    await db.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(db);
    var factory = TestCatalogDbContextFactory.From(db);
    var parser = new ComponentNameParser();
    var maps = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    var service = new WordCatalogImportService(factory, new ComponentEntryService(factory), parser, maps);
    var scanned = await service.ScanAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "Word", "driver741.docx"));
    Assert.NotEmpty(scanned.Components);
    foreach (var item in scanned.Components.Where(c => !WordNoteParser.HasContent(c.OwnFormNoteText)))
      Assert.DoesNotContain(item.Warnings, w => w.StartsWith("Примечания собственной формы прочитаны"));
    Assert.DoesNotContain(scanned.UnsupportedFormNumbers,
      number => number.ToUpperInvariant() is "2А" or "2A");
    var types = new Dictionary<string, ComponentType>();
    // Seed names exactly as read from Word, with real form associations.
    foreach (var item in scanned.Components)
    {
      var typeName = item.ComponentTypeName ?? "Word";
      if (!types.TryGetValue(typeName, out var type)) types[typeName] = type = new ComponentType { Name = typeName };
      var family = new Family { Name = item.FullName, ComponentType = type };
      db.Components.Add(new Component { FullName = item.FullName, Family = family, OwnFormId = item.OwnFormId });
    }
    await db.SaveChangesAsync();
    await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "Xml", "ВЕИР.468364.741 ПЭ3.PE.XML"));
    var imported = await new Pe3XmlImporter(parser).ImportAsync(stream);
    var result = await new ComponentMatcher(factory, parser).MatchAllAsync(imported.Components);
    output.WriteLine($"Word: {scanned.Components.Count}; XML matched: {result.Matched.Count}; unresolved: {result.Unresolved.Count}");
    foreach (var entry in result.Unresolved.DistinctBy(e => e.Imported.RawName)) output.WriteLine(entry.Imported.RawName);
    Assert.Contains(result.Matched, e => e.Imported.RawName == "ОС К53-68\"B\"-10В-10 мкФ+-10%");
    Assert.Contains(result.Matched, e => e.Imported.RawName == "ОС К53-68 \"A\"-50 В-0,1 мкФ+-10 %");
    Assert.Contains(result.Matched, e => e.Imported.RawName == "К10-84в 2012М-25 В-Н20-4700 пФ+-20%-N-А");
    Assert.Contains(result.Matched, e => e.Imported.RawName == "2ДС627А/ББ ОСМ");
    Assert.Contains(result.Matched, e => e.Imported.RawName == "2С215Ж ОС");
    Assert.Contains(result.Matched, e => e.Imported.RawName == "Р2-105-0,75-0,75 Ом+-1% А");
    Assert.Contains(result.Matched, e => e.Imported.RawName == "A3PE600-2PQ208I");
    Assert.Contains(scanned.Components, c => c.FullName == "A3PE600-2PQ208I");
    Assert.All(result.Matched, entry => Assert.Equal(entry.Imported.RawName,
      new OperationMaps.Wpf.Features.Components.ProjectComponentVm(entry).Name));
  }
}
