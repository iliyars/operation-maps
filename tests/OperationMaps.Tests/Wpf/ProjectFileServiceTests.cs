using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Features.OwnForm.Commands;
using OperationMaps.Wpf.Features.Project;
using OperationMaps.Wpf.Stores;
using Xunit;

namespace OperationMaps.Tests.Wpf;

/// <summary>
/// Save → reopen round trip of a .omaps project: everything the user
/// entered (split columns, "в схеме" values, notes, Form 4 notes,
/// operating conditions) must come back after reopening.
/// </summary>
public sealed class ProjectFileServiceTests : IDisposable
{
  private const string ComponentName = "Р1-12-0,125-1 кОм";

  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly string _path = Path.Combine(Path.GetTempPath(), $"project-{Guid.NewGuid()}.omaps");

  private Form _form = null!;
  private int _baseParamId;
  private int _resultParamId;
  private int _plainParamId;

  public ProjectFileServiceTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite(_connection)
        .Options);
    _db.Database.EnsureCreated();

    Seed();
  }

  private void Seed()
  {
    var type = new ComponentType { Name = "Резистор" };
    var family = new Family { Name = "Р1-12", ComponentType = type };

    var form4 = new Form { Number = "4", Title = "Форма 4", Parameters = [new FormParameter { RowNumber = 1, Name = "Ресурс" }] };

    var baseParam = new FormParameter { RowNumber = 1, Name = "Напряжение", CanBeLoadFactorBase = true };
    var resultParam = new FormParameter { RowNumber = 2, Name = "Коэффициент нагрузки", IsLoadFactorResult = true };
    var plainParam = new FormParameter { RowNumber = 3, Name = "Мощность" };
    _form = new Form { Number = "99", Title = "Тестовая форма", Parameters = [baseParam, resultParam, plainParam] };

    _db.AddRange(type, family, form4, _form);
    _db.SaveChanges();

    var component = new Component { FullName = ComponentName, Family = family, OwnFormId = _form.Id };
    _db.Components.Add(component);
    _db.SaveChanges();

    _db.ComponentNtdValues.Add(new ComponentNtdValue { ComponentId = component.Id, FormParameterId = baseParam.Id, Value = "10" });
    _db.SaveChanges();

    _baseParamId = baseParam.Id;
    _resultParamId = resultParam.Id;
    _plainParamId = plainParam.Id;
  }

  private ProjectFileService CreateService(ProjectStore store)
      => new(store, _db, new CatalogNameMatcher(_db), new OwnFormStateService(store, _db));

  [Fact]
  public async Task SavedProjectReopensWithAllUserEdits()
  {
    // ── Fill a project in one "session" ──────────────────────────────────────
    var store = new ProjectStore();
    var imported = new List<ImportedComponent>
    {
      new() { ImportIndex = 0, RawName = ComponentName, DetectedCategory = "Резистор", Positions = ["R1", "R2", "R3"], RawPositions = "R1-R3" },
    };
    store.Load("Плата", Path.GetTempPath(), await new CatalogNameMatcher(_db).MatchAllAsync(imported),
        new ImportResult { Components = imported, Warnings = [], DocumentNumber = "АБВГ.123456.001" });
    store.Conditions.Resource = "10000";

    var state = (await new OwnFormStateService(store, _db).GetAsync(_form.Id))!;
    state.History.Execute(new SplitColumnCommand(state.Columns, state.Columns[0], ["R1"], ["R2", "R3"]));
    state.Columns[0].SetCellValue(_plainParamId, "0,1");
    state.Columns[0].AddNote(_plainParamId, "Длительно");
    state.Columns[1].SetCellValue(_baseParamId, "5");

    var component = store.Components[0];
    await component.LoadNtdValuesAsync(_db);
    component.NtdValues[0].AddNote("Примечание к Форме 4");

    Assert.True(store.IsDirty);

    await CreateService(store).SaveAsync(_path);

    Assert.False(store.IsDirty);
    Assert.Equal(_path, store.ProjectFilePath);

    // ── Reopen in a fresh "session" ──────────────────────────────────────────
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);

    Assert.False(reopened.IsDirty);
    Assert.Equal(_path, reopened.ProjectFilePath);
    Assert.Equal("АБВГ.123456.001", reopened.DocumentNumber);
    Assert.Equal("10000", reopened.Conditions.Resource);

    var restored = reopened.OwnForms[_form.Id];
    Assert.Equal(["R1", "R2, R3"], restored.Columns.Select(c => c.Positions));

    Assert.Equal("0,1", restored.Columns[0].GetCellValue(_plainParamId));
    Assert.Equal("Длительно", Assert.Single(restored.Columns[0].GetNotes(_plainParamId)).NoteText);

    Assert.Equal("5", restored.Columns[1].GetCellValue(_baseParamId));
    Assert.Equal("10", restored.Columns[1].GetNtdValue(_baseParamId));
    Assert.Equal("0,5 (1)", restored.Columns[1].GetCellValue(_resultParamId));

    Assert.Equal("Примечание к Форме 4", Assert.Single(reopened.Components[0].NtdValues[0].Notes).NoteText);
  }

  [Fact]
  public async Task EditAfterOpenMarksProjectDirty()
  {
    var store = new ProjectStore();
    var imported = new List<ImportedComponent>
    {
      new() { ImportIndex = 0, RawName = ComponentName, DetectedCategory = "Резистор", Positions = ["R1"] },
    };
    store.Load("Плата", Path.GetTempPath(), await new CatalogNameMatcher(_db).MatchAllAsync(imported));
    await new OwnFormStateService(store, _db).GetAsync(_form.Id);
    await CreateService(store).SaveAsync(_path);

    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    Assert.False(reopened.IsDirty);

    reopened.OwnForms[_form.Id].Columns[0].SetCellValue(_plainParamId, "1");

    Assert.True(reopened.IsDirty);
  }

  [Fact]
  public async Task OpeningANonProjectFileThrowsInvalidData()
  {
    await File.WriteAllTextAsync(_path, "это не проект");

    await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(new ProjectStore()).OpenAsync(_path));
  }

  public void Dispose()
  {
    if (File.Exists(_path)) File.Delete(_path);
    _db.Dispose();
    _connection.Dispose();
  }

  /// <summary>Matches by exact catalog FullName — enough to exercise re-matching on open.</summary>
  private sealed class CatalogNameMatcher(CatalogDbContext db) : IComponentMatcher
  {
    public async Task<MatchResult> MatchAsync(ImportedComponent imported, CancellationToken ct = default)
    {
      var component = await db.Components
          .Include(c => c.Family)
          .FirstOrDefaultAsync(c => c.FullName == imported.RawName, ct);

      return new MatchResult
      {
        IsMatched = component is not null,
        MatchedFamily = component?.Family,
        MatchedComponent = component,
      };
    }

    public async Task<ProjectMatchResult> MatchAllAsync(IReadOnlyList<ImportedComponent> components, CancellationToken ct = default)
    {
      var entries = new List<ComponentMatchEntry>();
      foreach (var c in components)
        entries.Add(new ComponentMatchEntry { Imported = c, MatchResult = await MatchAsync(c, ct) });

      return new ProjectMatchResult
      {
        Matched = entries.Where(e => e.MatchResult.IsMatched).ToList(),
        Unresolved = entries.Where(e => !e.MatchResult.IsMatched).ToList(),
        Warnings = [],
      };
    }
  }
}
