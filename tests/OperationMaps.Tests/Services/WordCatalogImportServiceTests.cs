using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Word;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Services;

/// <summary>
/// Exercises <see cref="WordCatalogImportService"/> against a synthetic
/// combined document — Form4 + Form67 exported separately and merged into
/// one file via <see cref="WordReportBuilder"/>, exactly like the app's own
/// final report or a real work-order document bundles many forms together.
/// Validates the part that can't be unit-tested at the single-form level:
/// matching an own-form component's full designation back to its Form4
/// slot by prefix, and mapping RowNumber-keyed values to FormParameterId
/// for a real DB-backed Form.
/// </summary>
public sealed class WordCatalogImportServiceTests : IDisposable
{
  private const string CapacitorType = "Конденсатор";
  private const string KnownFamilyDesignation = "К10-84в"; // Form4's "Обозначение" for slot 0
  private const string OwnFormFullName = "К10-84в 3216М-50 В-Н90-0,15 мкФ-N-A"; // starts with the above
  private const string UnmatchedFullName = "НЕИЗВЕСТНЫЙ-ТИП-999-0,1 мкФ";
  private const string AlreadyExistingFullName = "К10-84в 3216М-25 В-Н90-1,0 мкФ-N-A";
  private const string NewTypeName = "Дроссель"; // Form4's "Наименование ЭРИ" — not seeded as a ComponentType
  private const string NewTypeFamilyDesignation = "НОВЫЙ-ТИП-Х";
  private const string NewTypeFullName = "НОВЫЙ-ТИП-Х-100пФ";
  private const string DiodeMatrixType = "Диодная матрица";
  private const string DiodeMatrixDesignationCyrillicA = "2Д906А2/ББ"; // Form4 — Cyrillic А (U+0410)
  private const string DiodeMatrixFullNameLatinA = "2Д906A2/ББ"; // own-form — Latin A (U+0041), real-world typo
  private const string DiodeMatrixDesignationCyrillicB = "2ДС627/ББ"; // Form4 — Cyrillic Б (U+0411) x2
  private const string DiodeMatrixFullNameLatinB = "2ДС627/BB"; // own-form — Latin B (U+0042) x2, real-world typo
  private const string ResistorType = "Резистор";
  private const string OsPrefixedDesignation = "ОС К52-18"; // Form4 — "особая приёмка" quality-grade prefix
  private const string OsPrefixOmittedFullName = "К52-18-63В-470мкФ+-20%"; // own-form omits the "ОС " prefix

  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly WordFormMapLoader _mapLoader;
  private readonly WordCatalogImportService _service;
  private readonly List<string> _tempFiles = [];

  private int _capacitorTypeId;
  private int _form67Id;

  public WordCatalogImportServiceTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    var options = new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite(_connection)
        .Options;

    _db = new CatalogDbContext(options);
    _db.Database.EnsureCreated();

    _mapLoader = new WordFormMapLoader(
        Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));

    _service = new WordCatalogImportService(_db, new ComponentEntryService(_db), new ComponentNameParser(), _mapLoader);

    Seed();
  }

  private void Seed()
  {
    var capacitorType = new ComponentType { Name = CapacitorType };
    _db.ComponentTypes.Add(capacitorType);
    _db.ComponentTypes.Add(new ComponentType { Name = DiodeMatrixType });
    _db.ComponentTypes.Add(new ComponentType { Name = ResistorType });

    // Form 4 — real map.json has parameter rows 1..16, but only the DB
    // side needs to exist for FormParameterId lookups to succeed; seed all
    // 16 so MapRowValuesToParamIdsAsync finds every row the reader produces.
    var form4 = new Form { Number = "4", Title = "Форма 4" };
    for (int row = 1; row <= 16; row++)
      form4.Parameters.Add(new FormParameter { RowNumber = row, Name = $"Параметр {row}", Form = form4 });
    _db.Forms.Add(form4);

    // Form 67 — seed the rows its real map.json actually defines.
    var form67Map = _mapLoader.Load("67");
    var form67 = new Form { Number = "67", Title = "Форма 67" };
    var form67Rows = form67Map.ComponentSlots[0].ParameterCells.Keys.Select(int.Parse).Distinct();
    foreach (var row in form67Rows)
      form67.Parameters.Add(new FormParameter { RowNumber = row, Name = $"П{row}", Form = form67 });
    _db.Forms.Add(form67);

    // A component that already exists — the scan should flag it, not
    // propose creating a duplicate.
    var existingFamily = new Family { Name = KnownFamilyDesignation, ComponentType = capacitorType };
    _db.Families.Add(existingFamily);
    _db.Components.Add(new Component { FullName = AlreadyExistingFullName, Family = existingFamily });

    _db.SaveChanges();

    _capacitorTypeId = capacitorType.Id;
    _form67Id = form67.Id;
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
    foreach (var f in _tempFiles)
      if (File.Exists(f)) File.Delete(f);
  }

  // ── Building the synthetic combined document ────────────────────────────

  private async Task<string> BuildCombinedDocumentAsync()
  {
    var wordService = new WordService(_mapLoader);

    var form4Data = new WordFormData
    {
      FormNumber = "4",
      Components =
      [
        new WordComponentData
        {
          Name = CapacitorType,               // "Наименование ЭРИ" row
          ComponentTypeName = KnownFamilyDesignation, // "Обозначение" row
          Quantity = "20",
          NtdValues = Enumerable.Range(1, 16).ToDictionary(r => r, r => $"f4-{r}"),
        },
        new WordComponentData
        {
          Name = NewTypeName,
          ComponentTypeName = NewTypeFamilyDesignation,
          Quantity = "5",
          NtdValues = Enumerable.Range(1, 16).ToDictionary(r => r, r => $"f4b-{r}"),
        },
        new WordComponentData
        {
          Name = DiodeMatrixType,
          ComponentTypeName = DiodeMatrixDesignationCyrillicA,
          Quantity = "26",
          NtdValues = Enumerable.Range(1, 16).ToDictionary(r => r, r => $"f4c-{r}"),
        },
        new WordComponentData
        {
          Name = DiodeMatrixType,
          ComponentTypeName = DiodeMatrixDesignationCyrillicB,
          Quantity = "4",
          NtdValues = Enumerable.Range(1, 16).ToDictionary(r => r, r => $"f4d-{r}"),
        },
        new WordComponentData
        {
          Name = ResistorType,
          ComponentTypeName = OsPrefixedDesignation,
          Quantity = "2",
          NtdValues = Enumerable.Range(1, 16).ToDictionary(r => r, r => $"f4e-{r}"),
        },
      ],
    };

    var map67 = _mapLoader.Load("67");
    var slot0Rows = map67.ComponentSlots[0].ParameterCells.Keys.Select(int.Parse).ToList();

    var form67Data = new WordFormData
    {
      FormNumber = "67",
      Components =
      [
        new WordComponentData
        {
          Name = OwnFormFullName,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd-{r}"),
        },
        new WordComponentData
        {
          Name = UnmatchedFullName,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd2-{r}"),
        },
        new WordComponentData
        {
          Name = AlreadyExistingFullName,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd3-{r}"),
        },
        new WordComponentData
        {
          Name = NewTypeFullName,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd4-{r}"),
        },
        new WordComponentData
        {
          Name = DiodeMatrixFullNameLatinA,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd5-{r}"),
        },
        new WordComponentData
        {
          Name = DiodeMatrixFullNameLatinB,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd6-{r}"),
        },
        new WordComponentData
        {
          Name = OsPrefixOmittedFullName,
          NtdValues = slot0Rows.ToDictionary(r => r, r => $"ntd7-{r}"),
        },
      ],
    };

    var reportBuilder = new WordReportBuilder(wordService);
    var coverPath = CreateBlankDocument();

    var combinedBytes = await reportBuilder.BuildAsync(
        coverPath,
        [
          (form4Data, _mapLoader.GetTemplatePath("4")),
          (form67Data, _mapLoader.GetTemplatePath("67")),
        ]);

    var outPath = Path.Combine(Path.GetTempPath(), $"combined-{Guid.NewGuid()}.docx");
    await File.WriteAllBytesAsync(outPath, combinedBytes);
    _tempFiles.Add(outPath);
    return outPath;
  }

  private string CreateBlankDocument()
  {
    var path = Path.Combine(Path.GetTempPath(), $"blank-{Guid.NewGuid()}.docx");
    _tempFiles.Add(path);

    using var doc = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
    var mainPart = doc.AddMainDocumentPart();
    mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("")))));
    mainPart.Document.Save();
    return path;
  }

  // ── Tests ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task ScanAsync_matches_own_form_component_to_its_form4_slot_by_prefix()
  {
    var path = await BuildCombinedDocumentAsync();

    var result = await _service.ScanAsync(path);

    Assert.Empty(result.UnsupportedFormNumbers);
    Assert.Equal(7, result.Components.Count);

    var matched = result.Components.Single(c => c.FullName == OwnFormFullName);
    Assert.True(matched.Form4DataFound);
    Assert.Equal(CapacitorType, matched.DetectedTypeName);
    Assert.Equal(_capacitorTypeId, matched.ComponentTypeId);
    Assert.False(matched.AlreadyInCatalog);
    Assert.Empty(matched.Warnings);
    Assert.NotEmpty(matched.Form4NtdValuesByRow);
    Assert.NotEmpty(matched.OwnFormNtdValuesByRow);
  }

  [Fact]
  public async Task ScanAsync_flags_component_with_no_form4_match_and_leaves_type_unresolved()
  {
    var path = await BuildCombinedDocumentAsync();

    var result = await _service.ScanAsync(path);

    var unmatched = result.Components.Single(c => c.FullName == UnmatchedFullName);
    Assert.False(unmatched.Form4DataFound);
    Assert.Null(unmatched.ComponentTypeId);
    Assert.Null(unmatched.DetectedTypeName);
    Assert.Contains(unmatched.Warnings, w => w.Contains("Форме 4"));
  }

  [Fact]
  public async Task ScanAsync_flags_already_cataloged_component()
  {
    var path = await BuildCombinedDocumentAsync();

    var result = await _service.ScanAsync(path);

    var existing = result.Components.Single(c => c.FullName == AlreadyExistingFullName);
    Assert.True(existing.AlreadyInCatalog);
  }

  [Fact]
  public async Task ImportAsync_creates_component_with_form4_and_own_form_values_mapped_by_parameter_id()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);
    var item = result.Components.Single(c => c.FullName == OwnFormFullName);

    var component = await _service.ImportAsync(item);

    Assert.Equal(OwnFormFullName, component.FullName);
    Assert.Equal(KnownFamilyDesignation, component.Family.Name);
    Assert.Equal(_form67Id, component.OwnFormId);

    var familyValues = await _db.FamilyNtdValues.Where(v => v.FamilyId == component.FamilyId).ToListAsync();
    Assert.NotEmpty(familyValues); // Form4 values were persisted onto the (new) family

    var ownValues = await _db.ComponentNtdValues.Where(v => v.ComponentId == component.Id).ToListAsync();
    Assert.NotEmpty(ownValues); // Form67 "по НТД" values were persisted onto the component
  }

  [Fact]
  public async Task ResolveComponentTypeAsync_lets_the_review_ui_fix_an_unmatched_component()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);
    var item = result.Components.Single(c => c.FullName == UnmatchedFullName);

    await _service.ResolveComponentTypeAsync(item, CapacitorType);

    Assert.Equal(_capacitorTypeId, item.ComponentTypeId);
    Assert.False(string.IsNullOrWhiteSpace(item.FamilyName));

    // Now importable, even without Form4 data (Form4Values stays empty).
    var component = await _service.ImportAsync(item);
    Assert.Equal(UnmatchedFullName, component.FullName);
  }

  [Fact]
  public async Task ScanAsync_flags_a_form4_type_that_does_not_exist_in_the_catalog_yet()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);

    var item = result.Components.Single(c => c.FullName == NewTypeFullName);

    Assert.True(item.Form4DataFound);
    Assert.Equal(NewTypeName, item.DetectedTypeName);
    Assert.Equal(NewTypeName, item.ComponentTypeName);
    Assert.Null(item.ComponentTypeId); // not in the catalog yet
    Assert.True(item.IsNewComponentType);
    Assert.Contains(item.Warnings, w => w.Contains(NewTypeName));
  }

  [Fact]
  public async Task ImportAsync_creates_a_new_component_type_read_straight_from_word()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);
    var item = result.Components.Single(c => c.FullName == NewTypeFullName);

    var component = await _service.ImportAsync(item);

    Assert.Equal(NewTypeFullName, component.FullName);
    var newType = await _db.ComponentTypes.SingleAsync(t => t.Name == NewTypeName);
    Assert.Equal(newType.Id, component.Family.ComponentTypeId);
    Assert.StartsWith(component.Family.Name, NewTypeFamilyDesignation);
  }

  /// <summary>
  /// Real bug found by Ilya on his actual document: Form4 wrote the
  /// designation with a Cyrillic "А" ("2Д906А2/ББ") while the own-form page
  /// had a Latin "A" typed in the same spot ("2Д906A2/ББ") — a one-character,
  /// invisible-to-the-eye difference between the two pages that made the
  /// exact-text prefix match miss a component whose type (Диодная матрица)
  /// was right there in the document.
  /// </summary>
  [Fact]
  public async Task ScanAsync_matches_form4_designation_despite_a_latin_cyrillic_homoglyph_typo()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);

    var item = result.Components.Single(c => c.FullName == DiodeMatrixFullNameLatinA);

    Assert.True(item.Form4DataFound);
    Assert.Equal(DiodeMatrixType, item.DetectedTypeName);
    Assert.NotNull(item.ComponentTypeId);
    Assert.False(item.IsNewComponentType);
    Assert.Empty(item.Warnings);
  }

  /// <summary>
  /// A second real mismatch found on a different one of Ilya's documents:
  /// Form4 wrote "2ДС627/ББ" (Cyrillic "Б") while the own-form page had
  /// "2ДС627/BB" (Latin "B") — not a look-alike substitution (Б and B don't
  /// resemble each other visually) but a sound-alike one, since "Б" is
  /// commonly transliterated as "B".
  /// </summary>
  [Fact]
  public async Task ScanAsync_matches_form4_designation_despite_a_cyrillic_b_to_latin_b_phonetic_typo()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);

    var item = result.Components.Single(c => c.FullName == DiodeMatrixFullNameLatinB);

    Assert.True(item.Form4DataFound);
    Assert.Equal(DiodeMatrixType, item.DetectedTypeName);
    Assert.NotNull(item.ComponentTypeId);
    Assert.Empty(item.Warnings);
  }

  /// <summary>
  /// A third real mismatch: Form4 wrote "ОС К52-18" (a leading "особая
  /// приёмка" quality-grade marker) while the own-form's "Наименование
  /// изделия" just said "К52-18-63В-470мкФ+-20%", omitting the "ОС " —
  /// the base designation still has to prefix-match after stripping it.
  /// </summary>
  [Fact]
  public async Task ScanAsync_matches_form4_designation_despite_an_omitted_os_prefix()
  {
    var path = await BuildCombinedDocumentAsync();
    var result = await _service.ScanAsync(path);

    var item = result.Components.Single(c => c.FullName == OsPrefixOmittedFullName);

    Assert.True(item.Form4DataFound);
    Assert.Equal(ResistorType, item.DetectedTypeName);
    Assert.Equal(ResistorType, item.ComponentTypeName);
    Assert.Empty(item.Warnings);
  }
}
