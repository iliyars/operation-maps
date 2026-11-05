using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
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

    _service = new WordCatalogImportService(TestCatalogDbContextFactory.From(_db), new ComponentEntryService(TestCatalogDbContextFactory.From(_db)), new ComponentNameParser(), _mapLoader);

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

  [Fact]
  public async Task Cancelled_scan_reports_progress_and_releases_document_handle()
  {
    var path = CreateBlankDocument();
    using var cancellation = new CancellationTokenSource();
    var stages = new List<string>();
    var progress = new CallbackProgress(value =>
    {
      stages.Add(value.Message);
      if (value.Message.Contains("загрузка каталога")) cancellation.Cancel();
    });
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.ScanAsync(path, progress, cancellation.Token));
    Assert.Contains(stages, message => message.Contains("Открытие документа"));
    using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    Assert.True(exclusive.Length > 0);
  }

  private sealed class CallbackProgress(Action<WordImportProgress> callback) : IProgress<WordImportProgress>
  {
    public void Report(WordImportProgress value) => callback(value);
  }

  [Fact]
  public async Task Scan_preserves_quality_prefixes_as_distinct_names_and_keeps_trailing_marks()
  {
    var bytes = await new WordService(_mapLoader).ExportAsync(new WordFormData
    {
      FormNumber = "67", Components = [
        new WordComponentData { Name = "ОСМ " + OwnFormFullName },
        new WordComponentData { Name = "ОС " + OwnFormFullName },
        new WordComponentData { Name = "2ДС627А/ББ ОСМ" }
      ]
    }, _mapLoader.GetTemplatePath("67"));
    var path = Path.Combine(Path.GetTempPath(), $"prefixes-{Guid.NewGuid()}.docx");
    _tempFiles.Add(path);
    await File.WriteAllBytesAsync(path, bytes);
    var result = await _service.ScanAsync(path);
    Assert.Equal(3, result.Components.Count);
    Assert.Contains(result.Components, c => c.FullName == "2ДС627А/ББ ОСМ");
    var item = result.Components.Single(c => c.FullName == "ОСМ " + OwnFormFullName);
    Assert.Contains(result.Components, c => c.FullName == "ОС " + OwnFormFullName);
    Assert.Empty(item.AdditionalVariants);
    item.ComponentTypeName = CapacitorType;
    var created = await _service.ImportAsync(item);
    Assert.Equal("ОСМ " + OwnFormFullName, created.FullName);
    Assert.StartsWith("ОСМ ", created.Family.Name);
    var xml = new ImportedComponent { RawName = "ОСМ " + OwnFormFullName, DetectedCategory = CapacitorType, Positions = ["C1"] };
    var match = await new ComponentMatcher(TestCatalogDbContextFactory.From(_db), new ComponentNameParser()).MatchAsync(xml);
    Assert.True(match.IsMatched);
    Assert.Equal("ОСМ " + OwnFormFullName, xml.RawName);
  }

  [Theory]
  [InlineData("ОС ")]
  [InlineData("ОСМ ")]
  [InlineData("ОСМ–")]
  public async Task Import_preserves_leading_quality_prefix(string prefix)
  {
    var created = await _service.ImportAsync(new WordImportedComponent
    {
      FullName = prefix + OwnFormFullName, OwnFormId = _form67Id, OwnFormNumber = "67", OwnFormTitle = "Форма 67",
      ComponentTypeName = CapacitorType
    });
    Assert.Equal(prefix + OwnFormFullName, created.FullName);
    Assert.Equal((prefix.StartsWith("ОСМ") ? "ОСМ " : "ОС ") + KnownFamilyDesignation, created.Family.Name);
  }

  [Fact]
  public async Task Repeated_word_slots_with_conflicting_values_require_explicit_resolution()
  {
    var form4 = await new WordService(_mapLoader).ExportAsync(new WordFormData
    {
      FormNumber = "4", Components = [
        new WordComponentData { Name = CapacitorType, ComponentTypeName = KnownFamilyDesignation,
          NtdValues = new Dictionary<int, string> { [3] = "100000" } },
        new WordComponentData { Name = CapacitorType, ComponentTypeName = KnownFamilyDesignation,
          NtdValues = new Dictionary<int, string> { [3] = "150000 *" }, Note = "* Облегчённый режим" }
      ]
    }, _mapLoader.GetTemplatePath("4"));
    var bytes = await new WordService(_mapLoader).ExportAsync(new WordFormData
    {
      FormNumber = "67", Components = [
        new WordComponentData { Name = OwnFormFullName, NtdValues = new Dictionary<int, string> { [1] = "10" } },
        new WordComponentData { Name = OwnFormFullName, NtdValues = new Dictionary<int, string> { [1] = "6 *" }, Note = "* Облегчённый режим" },
        new WordComponentData { Name = OwnFormFullName, NtdValues = new Dictionary<int, string> { [1] = "4 *" }, Note = "* Облегчённый режим при охлаждении" }
      ]
    }, _mapLoader.GetTemplatePath("67"));
    var path = Path.Combine(Path.GetTempPath(), $"regimes-{Guid.NewGuid()}.docx");
    _tempFiles.Add(path); await File.WriteAllBytesAsync(path, WordDocumentMerger.Merge([form4, bytes]));
    var item = Assert.Single((await _service.ScanAsync(path)).Components);
    item.ComponentTypeName = CapacitorType;
    var created = await _service.ImportAsync(item);
    var conditions = await _db.ComponentRegimes.Include(r => r.Values).Where(r => r.ComponentId == created.Id).ToListAsync();
    Assert.Equal(2, conditions.Count);
    Assert.Contains(conditions, c => c.Condition == "Облегчённый режим"
      && c.Values.Any(v => v.Value == "150000") && c.Values.Any(v => v.Value == "6"));
    Assert.Contains(conditions, c => c.Condition == "Облегчённый режим при охлаждении" && c.Values.Single().Value == "4");
    Assert.Equal("10", await _db.ComponentNtdValues.Where(v => v.ComponentId == created.Id && v.FormParameter.RowNumber == 1).Select(v => v.Value).SingleAsync());
  }

  [Fact]
  public async Task Relief_note_does_not_invent_default_or_require_mode_selection()
  {
    var item = new WordImportedComponent
    {
      FullName = OwnFormFullName, OwnFormId = _form67Id, OwnFormNumber = "67", OwnFormTitle = "Форма 67",
      ComponentTypeName = CapacitorType,
      OwnFormNtdValuesByRow = new() { [1] = "50" },
      OwnFormNotesByRow = new() { [1] = ["Используется в облегчённом режиме"] }
    };
    var created = await _service.ImportAsync(item);
    Assert.False(await _db.ComponentNtdValues.AnyAsync(v => v.ComponentId == created.Id));
    Assert.False(await _db.ComponentNotes.AnyAsync(v => v.ComponentId == created.Id));
    var condition = await _db.ComponentRegimes.Include(r => r.Values).SingleAsync(r => r.ComponentId == created.Id);
    Assert.Equal("Используется в облегчённом режиме", condition.Condition);
    Assert.Equal("50", Assert.Single(condition.Values).Value);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Load_factor_parenthesis_is_saved_as_component_base_parameter(bool inScheme)
  {
    var parameters = await _db.FormParameters.Where(p => p.FormId == _form67Id).OrderBy(p => p.RowNumber).ToListAsync();
    var basis = parameters.First();
    basis.CanBeLoadFactorBase = true;
    var result = parameters.Last();
    result.Name = "Коэффициент нагрузки";
    result.IsLoadFactorResult = true;
    await _db.SaveChangesAsync();
    var coefficient = $"0,6 ({basis.RowNumber})";
    var bytes = await new WordService(_mapLoader).ExportAsync(new WordFormData
    {
      FormNumber = "67", Components = [new WordComponentData
      {
        Name = OwnFormFullName,
        NtdValues = new Dictionary<int, string> { [basis.RowNumber] = "50", [result.RowNumber] = inScheme ? "0,8" : coefficient },
        SchemeValues = inScheme ? new Dictionary<int, string> { [result.RowNumber] = coefficient } : new Dictionary<int, string>()
      }]
    }, _mapLoader.GetTemplatePath("67"));
    var path = Path.Combine(Path.GetTempPath(), $"load-factor-{Guid.NewGuid()}.docx");
    _tempFiles.Add(path);
    await File.WriteAllBytesAsync(path, bytes);
    var item = Assert.Single((await _service.ScanAsync(path)).Components);
    Assert.Equal(basis.RowNumber, item.LoadFactorBaseRow);
    Assert.Equal(inScheme ? "0,8" : "0,6", item.OwnFormNtdValuesByRow[result.RowNumber]);
    item.ComponentTypeName = CapacitorType;
    var created = await _service.ImportAsync(item);
    Assert.Equal(basis.Id, await _db.Components.Where(c => c.Id == created.Id).Select(c => c.LoadFactorParameterId).SingleAsync());
    var matches = await new ComponentMatcher(TestCatalogDbContextFactory.From(_db), new ComponentNameParser())
      .MatchAllAsync([new ImportedComponent { RawName = OwnFormFullName, DetectedCategory = CapacitorType, Positions = ["C1"] }]);
    var store = new OperationMaps.Wpf.Stores.ProjectStore();
    store.Load("Базовый параметр", Path.GetTempPath(), matches);
    var state = await new OperationMaps.Wpf.Features.OwnForm.OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form67Id);
    Assert.NotNull(state);
    Assert.Equal(basis.Id, Assert.Single(state.Columns).LoadFactorBaseParameterId);
  }

  [Fact]
  public async Task Missing_load_factor_base_row_is_rejected_before_component_creation()
  {
    var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ImportAsync(new WordImportedComponent
    {
      FullName = OwnFormFullName, OwnFormId = _form67Id, OwnFormNumber = "67", OwnFormTitle = "Форма 67",
      ComponentTypeName = CapacitorType, LoadFactorBaseRow = 999
    }));
    Assert.Contains("999", error.Message);
    Assert.False(await _db.Components.AnyAsync(c => c.FullName == OwnFormFullName));
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Scan_and_import_preserve_form4_and_own_form_notes_as_catalog_links(bool legacyForm4)
  {
    var form4Parameters = await _db.FormParameters.Where(p => p.Form.Number == "4").ToListAsync();
    if (legacyForm4)
    {
      _db.FormParameters.RemoveRange(form4Parameters.Where(p => p.RowNumber > 14));
      form4Parameters.Single(p => p.RowNumber == 1).Name = "Показатель ресурса";
    }
    else form4Parameters.Single(p => p.RowNumber == 3).Name = "Показатель ресурса";
    await _db.SaveChangesAsync();
    var resourceRow = legacyForm4 ? 1 : 3;
    var service = new WordService(_mapLoader);
    var form4 = await service.ExportAsync(new WordFormData
    {
      FormNumber = "4", Components = [new WordComponentData
      {
        Name = CapacitorType, ComponentTypeName = KnownFamilyDesignation,
        NtdValues = new Dictionary<int, string> { [1] = "Да", [3] = "150000 *" },
        Note = "* Допускается уточнение ресурса при испытаниях"
      }]
    }, _mapLoader.GetTemplatePath("4"));
    var own = await service.ExportAsync(new WordFormData
    {
      FormNumber = "67", Components = [new WordComponentData
      {
        Name = OwnFormFullName, NtdValues = new Dictionary<int, string> { [1] = "50 **" },
        Note = "** При температуре 25 °C\nДопускается уточнение"
      }]
    }, _mapLoader.GetTemplatePath("67"));
    var path = Path.Combine(Path.GetTempPath(), $"notes-{Guid.NewGuid()}.docx");
    _tempFiles.Add(path);
    await File.WriteAllBytesAsync(path, WordDocumentMerger.Merge([form4, own]));
    var item = Assert.Single((await _service.ScanAsync(path)).Components);
    Assert.Equal("150000", item.Form4NtdValuesByRow[resourceRow]);
    Assert.Equal("50", item.OwnFormNtdValuesByRow[1]);
    Assert.Equal("Допускается уточнение ресурса при испытаниях", Assert.Single(item.Form4NotesByRow[resourceRow]));
    Assert.Equal("При температуре 25 °C\nДопускается уточнение", Assert.Single(item.OwnFormNotesByRow[1]));
    var created = await _service.ImportAsync(item);
    var savedResource = await _db.FamilyNtdValues
      .Where(v => v.FamilyId == created.FamilyId && v.FormParameter.Form.Number == "4"
        && v.FormParameter.RowNumber == resourceRow)
      .Select(v => v.Value).SingleAsync();
    Assert.Equal("150000", savedResource);
    Assert.False(await _db.ComponentRegimes.AnyAsync());
    var savedOwnValue = await _db.ComponentNtdValues
      .Where(v => v.ComponentId == created.Id && v.FormParameter.RowNumber == 1)
      .Select(v => v.Value).SingleAsync();
    Assert.Equal("50", savedOwnValue);
    var familyNotes = await _db.FamilyNotes.Include(n => n.Note).Where(n => n.FamilyId == created.FamilyId).ToListAsync();
    Assert.Equal("Допускается уточнение ресурса при испытаниях", Assert.Single(familyNotes).Note.Text);
    var ownNotes = await _db.ComponentNotes.Include(n => n.Note).Where(n => n.ComponentId == created.Id).ToListAsync();
    Assert.Equal("При температуре 25 °C\nДопускается уточнение", Assert.Single(ownNotes).Note.Text);
    var matches = await new ComponentMatcher(TestCatalogDbContextFactory.From(_db), new ComponentNameParser())
      .MatchAllAsync([new ImportedComponent { RawName = OwnFormFullName, DetectedCategory = CapacitorType, Positions = ["C1"] }]);
    var store = new OperationMaps.Wpf.Stores.ProjectStore();
    store.Load("Примечания", Path.GetTempPath(), matches);
    await store.Components[0].LoadNtdValuesAsync(_db);
    Assert.Equal("Допускается уточнение ресурса при испытаниях", Assert.Single(store.Components[0].NtdValues.Single(r => r.RowNumber == resourceRow).Notes).NoteText);
    Assert.False(store.IsDirty);
    var preview = new OperationMaps.Wpf.Features.Components.ComponentRegimesViewModel(TestCatalogDbContextFactory.From(_db));
    await preview.LoadAsync(store.Components[0]);
    Assert.True(preview.IsAvailable);
    Assert.Equal("Допускается уточнение ресурса при испытаниях",
      Assert.Single(preview.Values.Single(v => v.FormNumber == "4" && v.RowNumber == resourceRow).Notes));
    var ownNotePreview = Assert.Single(preview.OwnFormNotes);
    Assert.Equal("50", ownNotePreview.EffectiveValue);
    Assert.Equal("При температуре 25 °C\nДопускается уточнение", Assert.Single(ownNotePreview.Notes));
    var state = await new OperationMaps.Wpf.Features.OwnForm.OwnFormStateService(store, TestCatalogDbContextFactory.From(_db))
      .GetAsync(_form67Id);
    var column = Assert.Single(state!.Columns);
    var paramId = state.Parameters.Single(p => p.RowNumber == 1).FormParameterId;
    Assert.Equal("При температуре 25 °C\nДопускается уточнение", Assert.Single(column.GetNotes(paramId)).NoteText);
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
