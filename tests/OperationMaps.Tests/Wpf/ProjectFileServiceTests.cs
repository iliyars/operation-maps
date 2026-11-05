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
using Moq;
using OperationMaps.Application.Services;

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
  private string RecoveryFolder => _path + ".recovery-data";

  private ProjectRecoveryService Recovery(ProjectStore store, IDialogService dialogs)
      => new(store, CreateService(store), new ProjectRecoveryOptions { Folder = RecoveryFolder }, dialogs);

  [Fact]
  public async Task Recovery_tick_without_project_or_existing_folder_does_not_warn()
  {
    var dialogs = new Mock<IDialogService>();
    using var recovery = Recovery(new ProjectStore(), dialogs.Object);
    await recovery.TickAsync();
    Assert.False(Directory.Exists(RecoveryFolder));
    dialogs.Verify(d => d.ShowWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task Recovery_write_failure_warns_once_and_does_not_clear_unsaved_state()
  {
    var store = await LoadedStoreAsync();
    store.MarkDirty();
    var writer = new DelegateWriter((_, _, _) => throw new IOException("Нет места для аварийной копии"));
    var projects = new ProjectFileService(store, TestCatalogDbContextFactory.From(_db), new CatalogNameMatcher(_db), writer);
    var dialogs = new Mock<IDialogService>();
    using var recovery = new ProjectRecoveryService(store, projects,
        new ProjectRecoveryOptions { Folder = RecoveryFolder }, dialogs.Object);
    await recovery.TickAsync();
    await recovery.TickAsync();
    Assert.True(store.IsDirty);
    Assert.Null(store.ProjectFilePath);
    dialogs.Verify(d => d.ShowWarning(It.IsAny<string>(), "Автовосстановление"), Times.Once);
  }

  [Fact]
  public async Task Recovery_round_trip_preserves_original_paths_and_unsaved_edits()
  {
    var store = await LoadedStoreAsync();
    await CreateService(store).SaveAsync(_path);
    var savedBytes = await File.ReadAllBytesAsync(_path);
    store.Conditions.Resource = "Несохранённые условия";
    store.MarkDirty();
    using (var firstSession = Recovery(store, Mock.Of<IDialogService>()))
    {
      await firstSession.CheckpointAsync();
      Assert.True(store.IsDirty);
      Assert.Equal(_path, store.ProjectFilePath);
      Assert.Equal(savedBytes, await File.ReadAllBytesAsync(_path));
      var copy = Assert.Single(Directory.GetFiles(RecoveryFolder, "*.recovery"));
      var copyBytes = await File.ReadAllBytesAsync(copy);
      await firstSession.CheckpointAsync();
      Assert.Equal(copyBytes, await File.ReadAllBytesAsync(copy));
      // Dispose releases the instance lock without marking this as a normal exit.
    }

    var restored = new ProjectStore();
    var dialogs = new Mock<IDialogService>();
    dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
    using var nextSession = Recovery(restored, dialogs.Object);
    Assert.True(await nextSession.OfferRestoreAsync());
    Assert.True(restored.IsDirty);
    Assert.Equal(_path, restored.ProjectFilePath);
    Assert.Equal(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), restored.ProjectFolderPath!.TrimEnd(Path.DirectorySeparatorChar));
    Assert.Equal("Несохранённые условия", restored.Conditions.Resource);
    Assert.Single(Directory.GetFiles(RecoveryFolder, "*.recovery"));
    await nextSession.CompleteAsync();
    Assert.Empty(Directory.GetFiles(RecoveryFolder, "*.recovery"));
  }

  [Fact]
  public async Task Running_instance_is_not_offered_for_recovery()
  {
    var store = await LoadedStoreAsync();
    store.MarkDirty();
    using var first = Recovery(store, Mock.Of<IDialogService>());
    await first.CheckpointAsync();
    var dialogs = new Mock<IDialogService>();
    using var second = Recovery(new ProjectStore(), dialogs.Object);
    Assert.False(await second.OfferRestoreAsync());
    dialogs.Verify(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task Recovery_cancel_keeps_copy_and_discard_removes_it(bool cancel)
  {
    var store = await LoadedStoreAsync();
    store.MarkDirty();
    using (var first = Recovery(store, Mock.Of<IDialogService>())) await first.CheckpointAsync();
    var dialogs = new Mock<IDialogService>();
    dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>())).Returns(cancel ? null : false);
    var current = new ProjectStore();
    using var second = Recovery(current, dialogs.Object);
    Assert.False(await second.OfferRestoreAsync());
    Assert.False(current.HasProject);
    Assert.Equal(cancel ? 1 : 0, Directory.GetFiles(RecoveryFolder, "*.recovery").Length);
  }

  [Fact]
  public async Task Corrupted_recovery_is_kept_and_current_project_is_untouched()
  {
    Directory.CreateDirectory(RecoveryFolder);
    var path = Path.Combine(RecoveryFolder, "broken.recovery");
    await File.WriteAllTextAsync(path, "{}");
    var store = new ProjectStore();
    var dialogs = new Mock<IDialogService>();
    dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
    using var recovery = Recovery(store, dialogs.Object);
    Assert.False(await recovery.OfferRestoreAsync());
    Assert.False(store.HasProject);
    Assert.True(File.Exists(path));
    dialogs.Verify(d => d.ShowError(It.IsAny<string>(), "Восстановление проекта"), Times.Once);
  }

  [Fact]
  public async Task Manual_save_removes_obsolete_copy_on_next_checkpoint()
  {
    var store = await LoadedStoreAsync();
    store.MarkDirty();
    using var recovery = Recovery(store, Mock.Of<IDialogService>());
    await recovery.CheckpointAsync();
    Assert.Single(Directory.GetFiles(RecoveryFolder, "*.recovery"));
    await CreateService(store).SaveAsync(_path);
    await recovery.CheckpointAsync();
    Assert.Empty(Directory.GetFiles(RecoveryFolder, "*.recovery"));
  }

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
      => new(store, TestCatalogDbContextFactory.From(_db), new CatalogNameMatcher(_db), new ProjectFileWriter());

  [Fact]
  public async Task Reopened_project_recalculates_formula_and_load_factor_without_opening_form_screen()
  {
    var sum = new FormParameter { Form = _form, RowNumber = 4, Name = "Сумма", Formula = "row1+row3", CanBeLoadFactorBase = true };
    _db.FormParameters.Add(sum);
    await _db.SaveChangesAsync();
    var component = await _db.Components.SingleAsync();
    _db.ComponentNtdValues.Add(new ComponentNtdValue { ComponentId = component.Id, FormParameterId = sum.Id, Value = "20" });
    await _db.SaveChangesAsync();
    var store = await LoadedStoreAsync();
    var state = (await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
    var column = Assert.Single(state.Columns);
    column.LoadFactorBaseParameterId = sum.Id;
    column.SetCellValue(_baseParamId, "3");
    column.SetCellValue(_plainParamId, "5");
    Assert.Equal("8", column.GetCellValue(sum.Id));
    await CreateService(store).SaveAsync(_path);
    var saved = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(_path))!;
    var values = saved["ownForms"]![0]!["columns"]![0]!["values"]!;
    Assert.Null(values["4"]);
    // Older versions persisted derived values; stale values must not win on restore.
    values["4"] = "999";
    await File.WriteAllTextAsync(_path, saved.ToJsonString());
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    var restored = reopened.OwnForms[_form.Id];
    var data = OwnFormWordDataBuilder.Build(restored.FormNumber, "Плата", restored.Parameters, restored.Columns);
    Assert.Equal("8", data.Components[0].SchemeValues[4]);
    Assert.Equal("0,4 (4)", data.Components[0].SchemeValues[2]);
    Assert.False(reopened.IsDirty);
  }

  private async Task<ProjectStore> LoadedStoreAsync(string name = "Плата")
  {
    var store = new ProjectStore();
    var imported = new List<ImportedComponent>
    {
      new() { ImportIndex = 0, RawName = ComponentName, DetectedCategory = "Резистор", Positions = ["R1"] },
    };
    store.Load(name, Path.GetTempPath(), await new CatalogNameMatcher(_db).MatchAllAsync(imported));
    return store;
  }

  [Fact]
  public async Task Edits_during_save_remain_dirty_and_file_contains_only_original_snapshot()
  {
    var store = await LoadedStoreAsync();
    store.Conditions.Resource = "100";
    store.MarkDirty();
    var entered = new TaskCompletionSource();
    var resume = new TaskCompletionSource();
    var writer = new DelegateWriter(async (path, bytes, ct) =>
    {
      entered.SetResult();
      await resume.Task;
      await new ProjectFileWriter().WriteAsync(path, bytes, ct);
    });
    var service = new ProjectFileService(store, TestCatalogDbContextFactory.From(_db), new CatalogNameMatcher(_db), writer);
    var saving = service.SaveAsync(_path);
    await entered.Task;
    store.Conditions.Resource = "200";
    store.MarkDirty();
    resume.SetResult();
    await saving;

    Assert.True(store.IsDirty);
    var restored = new ProjectStore();
    await CreateService(restored).OpenAsync(_path);
    Assert.Equal("100", restored.Conditions.Resource);
    await CreateService(store).SaveAsync(_path);
    Assert.False(store.IsDirty);
    await CreateService(restored).OpenAsync(_path);
    Assert.Equal("200", restored.Conditions.Resource);
  }

  [Fact]
  public async Task Completed_save_of_previous_project_does_not_change_current_project()
  {
    var store = await LoadedStoreAsync();
    var resume = new TaskCompletionSource();
    var writer = new DelegateWriter(async (path, bytes, ct) =>
    {
      await resume.Task;
      await new ProjectFileWriter().WriteAsync(path, bytes, ct);
    });
    var service = new ProjectFileService(store, TestCatalogDbContextFactory.From(_db), new CatalogNameMatcher(_db), writer);
    var saving = service.SaveAsync(_path);
    store.Load("Другой проект", Path.GetTempPath(), store.MatchResult!);
    store.MarkDirty();
    resume.SetResult();
    await saving;

    Assert.Equal("Другой проект", store.ProjectName);
    Assert.Null(store.ProjectFilePath);
    Assert.True(store.IsDirty);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Failure_or_cancellation_during_restoration_preserves_current_project(bool cancel)
  {
    var source = await LoadedStoreAsync();
    await new OwnFormStateService(source, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id);
    await CreateService(source).SaveAsync(_path);

    var current = await LoadedStoreAsync("Несохранённый проект");
    current.Conditions.Resource = "123";
    current.MarkDirty();
    var component = current.Components.Single();
    var revision = current.Revision;
    var calls = 0;
    var factory = new DelegateFactory(() =>
    {
      if (++calls == 2)
      {
        if (cancel) throw new OperationCanceledException();
        throw new IOException("Ошибка чтения каталога при восстановлении формы");
      }
      return TestCatalogDbContextFactory.From(_db).CreateDbContext();
    });
    var service = new ProjectFileService(current, factory, new CatalogNameMatcher(_db), new ProjectFileWriter());

    if (cancel) await Assert.ThrowsAsync<OperationCanceledException>(() => service.OpenAsync(_path));
    else await Assert.ThrowsAsync<IOException>(() => service.OpenAsync(_path));

    Assert.Equal(2, calls);
    Assert.Equal("Несохранённый проект", current.ProjectName);
    Assert.Same(component, current.Components.Single());
    Assert.Equal("123", current.Conditions.Resource);
    Assert.True(current.IsDirty);
    Assert.Equal(revision, current.Revision);
    Assert.Empty(current.OwnForms);
  }

  [Theory]
  [InlineData("{}")]
  [InlineData("{\"version\":0,\"components\":[]}")]
  [InlineData("{\"version\":999,\"components\":[]}")]
  [InlineData("{\"version\":1,\"components\":null}")]
  [InlineData("{\"version\":1,\"components\":[null]}")]
  [InlineData("{\"version\":1,\"components\":[],\"conditions\":null}")]
  [InlineData("{\"version\":1,\"components\":[],\"ownForms\":[{\"formNumber\":\"99\",\"columns\":[null]}]}")]
  [InlineData("{\"version\":1,\"components\":[{\"rawName\":\"R\",\"positions\":[\"R1\",\"R1\"]}]}")]
  public async Task Invalid_project_structure_is_rejected_without_replacing_current_project(string json)
  {
    await File.WriteAllTextAsync(_path, json);
    var current = await LoadedStoreAsync();
    var component = current.Components.Single();
    await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(current).OpenAsync(_path));
    Assert.Same(component, current.Components.Single());
  }

  [Fact]
  public async Task New_xml_project_resets_conditions_and_old_components_no_longer_mark_it_dirty()
  {
    var store = await LoadedStoreAsync();
    var oldComponent = store.Components.Single();
    await oldComponent.LoadNtdValuesAsync(_db);
    store.Conditions.Resource = "10000";
    store.Load("Новый", Path.GetTempPath(), store.MatchResult!);

    Assert.Null(store.Conditions.Resource);
    oldComponent.NtdValues[0].AddNote("Правка старого проекта");
    Assert.False(store.IsDirty);
  }

  [Fact]
  public async Task Failed_save_preserves_existing_file_and_dirty_state()
  {
    await File.WriteAllTextAsync(_path, "original");
    var store = await LoadedStoreAsync();
    store.MarkDirty();
    var writer = new DelegateWriter((_, _, _) => throw new IOException("Нет места"));
    var service = new ProjectFileService(store, TestCatalogDbContextFactory.From(_db), new CatalogNameMatcher(_db), writer);
    await Assert.ThrowsAsync<IOException>(() => service.SaveAsync(_path));
    Assert.Equal("original", await File.ReadAllTextAsync(_path));
    Assert.True(store.IsDirty);
    Assert.Null(store.ProjectFilePath);
  }

  private sealed class DelegateWriter(Func<string, byte[], CancellationToken, Task> write) : IProjectFileWriter
  {
    public Task WriteAsync(string path, byte[] contents, CancellationToken ct) => write(path, contents, ct);
  }

  private sealed class DelegateFactory(Func<CatalogDbContext> create) : IDbContextFactory<CatalogDbContext>
  {
    public CatalogDbContext CreateDbContext() => create();
  }

  [Fact]
  public async Task Edit_while_opening_prevents_replacement()
  {
    var current = await LoadedStoreAsync();
    await CreateService(current).SaveAsync(_path);
    var component = current.Components.Single();
    var matcher = new Moq.Mock<IComponentMatcher>();
    matcher.Setup(m => m.MatchAllAsync(Moq.It.IsAny<IReadOnlyList<ImportedComponent>>(), Moq.It.IsAny<CancellationToken>()))
        .Returns(async (IReadOnlyList<ImportedComponent> components, CancellationToken ct) =>
        {
          current.Conditions.Resource = "Новая правка";
          current.MarkDirty();
          return await new CatalogNameMatcher(_db).MatchAllAsync(components, ct);
        });
    var service = new ProjectFileService(current, TestCatalogDbContextFactory.From(_db), matcher.Object, new ProjectFileWriter());

    await Assert.ThrowsAsync<IOException>(() => service.OpenAsync(_path));
    Assert.Same(component, current.Components.Single());
    Assert.Equal("Новая правка", current.Conditions.Resource);
    Assert.True(current.IsDirty);
  }

  [Fact]
  public async Task Failed_file_replacement_preserves_target_and_cleans_temporary_file()
  {
    await File.WriteAllTextAsync(_path, "original");
    using (var locked = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.None))
    {
      var error = await Record.ExceptionAsync(
          () => new ProjectFileWriter().WriteAsync(_path, [1, 2, 3], CancellationToken.None));
      Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString());
    }

    Assert.Equal("original", await File.ReadAllTextAsync(_path));
    Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_path)!, Path.GetFileName(_path) + ".*.tmp"));
  }

  [Fact]
  public async Task Empty_project_can_still_be_saved_and_opened()
  {
    var store = new ProjectStore();
    store.Load("Пустой проект", Path.GetTempPath(), new ProjectMatchResult
    {
      Matched = [], Unresolved = [], Warnings = [],
    });
    await CreateService(store).SaveAsync(_path);
    var restored = new ProjectStore();
    await CreateService(restored).OpenAsync(_path);
    Assert.True(restored.HasProject);
    Assert.Empty(restored.Components);
    Assert.False(restored.IsDirty);
  }

  [Fact]
  public async Task Merged_values_and_union_of_notes_survive_save_and_open()
  {
    var store = await LoadedStoreAsync();
    store.Components[0].SetPositions(["R1", "R2"]);
    var state = (await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
    state.History.Execute(new SplitColumnCommand(state.Columns, state.Columns[0], ["R1"], ["R2"]));
    var first = state.Columns[0];
    var second = state.Columns[1];
    first.SetCellValue(_plainParamId, "0,1");
    second.SetCellValue(_baseParamId, "5");
    first.AddNote(_plainParamId, "Общее примечание");
    second.AddNote(_plainParamId, "Общее примечание");
    second.AddNote(_plainParamId, "Второе примечание");
    var plan = new ColumnMergePlan(first, second, calculated: [_resultParamId]);
    Assert.Empty(plan.Conflicts);
    state.History.Execute(new MergeColumnsCommand(state.Columns, first, second, plan));
    var merged = Assert.Single(state.Columns);
    Assert.Equal("0,5 (1)", merged.GetCellValue(_resultParamId));
    await CreateService(store).SaveAsync(_path);
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    var actual = Assert.Single(reopened.OwnForms[_form.Id].Columns);
    Assert.Equal("R1, R2", actual.Positions);
    Assert.Equal("0,1", actual.GetCellValue(_plainParamId));
    Assert.Equal("5", actual.GetCellValue(_baseParamId));
    Assert.Equal("0,5 (1)", actual.GetCellValue(_resultParamId));
    Assert.Equal(["Общее примечание", "Второе примечание"], actual.GetNotes(_plainParamId).Select(n => n.NoteText));
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Explicit_merged_condition_or_base_choice_survives_sync_undo_redo_and_reopening(bool chooseBase)
  {
    var store = await LoadedStoreAsync();
    var component = store.Components[0];
    component.SetPositions(["R1", "R2"]);
    var alternative = new OperationMaps.Application.Models.ComponentRegimeSelection(
      "Облегчённый", "Облегчённый режим", [new("99", 1, "20")]);
    // Make the choice opposite to the inherited project value.
    component.SelectRegime(chooseBase ? alternative : null);
    var service = new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db));
    var state = (await service.GetAsync(_form.Id))!;
    var original = state.Columns[0];
    original.SetCellValue(_baseParamId, "5");
    state.History.Execute(new SplitColumnCommand(state.Columns, original, ["R1"], ["R2"]));
    var first = state.Columns[0];
    var second = state.Columns[1];
    second.SelectRegimeOverride(chooseBase ? null : alternative);
    second.RefreshRegime("99", _form.Parameters);
    second.SetCellValue(_baseParamId, "7");
    first.AddNote(_plainParamId, "Первое пояснение");
    second.AddNote(_baseParamId, "Второе пояснение");
    var plan = new ColumnMergePlan(first, second, calculated: [_resultParamId]);
    foreach (var conflict in plan.Conflicts) conflict.SelectedSide = MergeSide.Second;
    state.History.Execute(new MergeColumnsCommand(state.Columns, first, second, plan));
    var expectedNtd = chooseBase ? "10" : "20";
    var expectedFactor = chooseBase ? "0,7 (1)" : "0,35 (1)";
    var merged = Assert.Single(state.Columns);
    Assert.True(merged.HasRegimeOverride);
    Assert.Equal(expectedNtd, merged.GetNtdValue(_baseParamId));
    Assert.True(state.History.Undo());
    Assert.Same(first, state.Columns[0]);
    Assert.Same(second, state.Columns[1]);
    Assert.True(state.History.Redo());
    await service.GetAsync(_form.Id);
    merged = Assert.Single(state.Columns);
    Assert.Equal(expectedNtd, merged.GetNtdValue(_baseParamId));
    var before = OwnFormWordDataBuilder.Build("99", "Плата", state.Parameters, state.Columns);
    await CreateService(store).SaveAsync(_path);
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    var restored = (await new OwnFormStateService(reopened, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
    var actual = Assert.Single(restored.Columns);
    Assert.True(actual.HasRegimeOverride);
    Assert.Equal(expectedNtd, actual.GetNtdValue(_baseParamId));
    Assert.Equal("7", actual.GetCellValue(_baseParamId));
    Assert.Equal(expectedFactor, actual.LoadFactorValue);
    Assert.Equal(chooseBase, actual.Component.ActiveRegime is null);
    var after = OwnFormWordDataBuilder.Build("99", "Плата", restored.Parameters, restored.Columns);
    Assert.Equal(before.Components[0].Note, after.Components[0].Note);
    Assert.Equal(before.Components[0].NtdValues.OrderBy(p => p.Key), after.Components[0].NtdValues.OrderBy(p => p.Key));
    Assert.Equal(["R1", "R2"], actual.Component.Entry.Imported.Positions);
    Assert.False(reopened.IsDirty);
  }

  [Fact]
  public async Task Explicitly_cleared_load_factor_base_remains_empty_after_reopening()
  {
    var store = await LoadedStoreAsync();
    var state = (await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
    state.Columns[0].SetCellValue(_baseParamId, "5");
    state.Columns[0].LoadFactorBaseParameterId = null;
    await CreateService(store).SaveAsync(_path);
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    var column = Assert.Single(reopened.OwnForms[_form.Id].Columns);
    Assert.Null(column.LoadFactorBaseParameterId);
    Assert.Equal("", column.LoadFactorValue);
  }

  [Fact]
  public async Task Split_optional_values_pins_and_note_markers_survive_reopening_without_aliasing()
  {
    var optional = new FormParameter
    {
      Form = _form, RowNumber = 4, Name = "Дополнительное напряжение", IsOptional = true, OptionalForRowNumber = 1,
    };
    _db.FormParameters.Add(optional);
    await _db.SaveChangesAsync();
    var catalogComponent = await _db.Components.SingleAsync();
    _db.ComponentNtdValues.Add(new ComponentNtdValue { ComponentId = catalogComponent.Id, FormParameterId = optional.Id, Value = "20" });
    _db.ComponentPinValues.Add(new ComponentPinValue { ComponentId = catalogComponent.Id, FormParameterId = optional.Id, Pins = "7, 8" });
    await _db.SaveChangesAsync();
    var store = await LoadedStoreAsync();
    store.Components[0].SetPositions(["R1", "R2", "R3"]);
    var state = (await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
    var original = state.Columns[0];
    original.SetCellValue(_baseParamId, "5");
    original.SetOptionalCellValue(optional.Id, "7");
    original.AddNote(optional.Id, "Дополнительное питание");
    original.AddNote(_baseParamId, "Основное питание");
    state.History.Execute(new SplitColumnCommand(state.Columns, original, ["R1"], ["R2", "R3"]));
    state.Columns[0].SetOptionalCellValue(optional.Id, "8");
    state.Columns[0].AddNote(_plainParamId, "Только R1");
    Assert.Equal("7", state.Columns[1].GetOptionalCellValue(optional.Id));
    Assert.Empty(state.Columns[1].GetNotes(_plainParamId));
    Assert.Equal("7", original.GetOptionalCellValue(optional.Id));
    Assert.Empty(original.GetNotes(_plainParamId));
    var before = OwnFormWordDataBuilder.Build("99", "Плата", state.Parameters, state.Columns);
    await CreateService(store).SaveAsync(_path);
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    var restored = reopened.OwnForms[_form.Id];
    Assert.Equal(["R1", "R2, R3"], restored.Columns.Select(c => c.Positions));
    Assert.Equal(["R1", "R2", "R3"], restored.Columns.SelectMany(c => c.Component.Entry.Imported.Positions));
    var after = OwnFormWordDataBuilder.Build("99", "Плата", restored.Parameters, restored.Columns);
    for (var i = 0; i < 2; i++)
    {
      Assert.Equal(before.Components[i].Note, after.Components[i].Note);
      var expected = before.Components[i].OptionalRowValuesByParameter[1];
      var actual = after.Components[i].OptionalRowValuesByParameter[1];
      Assert.Equal(expected.NtdValue, actual.NtdValue);
      Assert.Equal(expected.SchemeValue, actual.SchemeValue);
      Assert.Equal("7, 8", actual.PinsValue);
    }
    Assert.False(reopened.IsDirty);
  }

  [Fact]
  public async Task Malformed_saved_column_condition_is_rejected_without_replacing_current_project()
  {
    var store = await LoadedStoreAsync();
    await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id);
    await CreateService(store).SaveAsync(_path);
    var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(_path))!;
    var column = json["ownForms"]![0]!["columns"]![0]!;
    column["hasRegimeOverride"] = true;
    column["regime"] = System.Text.Json.Nodes.JsonNode.Parse("{\"name\":\"Режим\",\"condition\":\"Условие\",\"values\":null}");
    await File.WriteAllTextAsync(_path, json.ToJsonString());
    var current = await LoadedStoreAsync("Текущий проект");
    var original = current.Components[0];
    await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(current).OpenAsync(_path));
    Assert.Same(original, current.Components[0]);
    Assert.Equal("Текущий проект", current.ProjectName);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public async Task Previous_project_versions_still_restore_input_values(int version)
  {
    var store = await LoadedStoreAsync();
    var state = (await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
    state.Columns[0].SetCellValue(_baseParamId, "5");
    await CreateService(store).SaveAsync(_path);
    var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(_path))!;
    json["version"] = version;
    var column = json["ownForms"]![0]!["columns"]![0]!.AsObject();
    column.Remove("hasRegimeOverride");
    column.Remove("regime");
    await File.WriteAllTextAsync(_path, json.ToJsonString());
    var reopened = new ProjectStore();
    await CreateService(reopened).OpenAsync(_path);
    Assert.Equal("5", Assert.Single(reopened.OwnForms[_form.Id].Columns).GetCellValue(_baseParamId));
  }

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
    store.HeadName = "Иванов И.И.";
    store.HeadOccupation = "Генеральный директор";

    var state = (await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id))!;
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
    Assert.Equal("Иванов И.И.", reopened.HeadName);
    Assert.Equal("Генеральный директор", reopened.HeadOccupation);

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
  public async Task Form4_notes_on_components_without_catalog_family_survive_save_and_open()
  {
    var store = new ProjectStore();
    var component = new ImportedComponent
    { RawName = "Нет в каталоге", DetectedCategory = "Неизвестный тип", Positions = ["X99"] };
    store.Load("Примечания", Path.GetTempPath(), await new CatalogNameMatcher(_db).MatchAllAsync([component]));
    await store.Components[0].LoadNtdValuesAsync(_db);
    var row = Assert.Single(store.Components[0].NtdValues.Take(1));
    row.StartAddNoteCommand.Execute(null);
    row.PendingNoteText = "Уточняется при испытаниях";
    row.ConfirmAddNoteCommand.Execute(null);
    Assert.True(store.IsDirty);
    await CreateService(store).SaveAsync(_path);
    var restored = new ProjectStore();
    await CreateService(restored).OpenAsync(_path);
    Assert.False(restored.IsDirty);
    var restoredRow = restored.Components[0].NtdValues.Single(r => r.RowNumber == row.RowNumber);
    Assert.Equal("Уточняется при испытаниях", Assert.Single(restoredRow.Notes).NoteText);
    restoredRow.Notes[0].DeleteCommand.Execute(null);
    Assert.Empty(restoredRow.Notes);
    Assert.True(restored.IsDirty);
  }

  [Fact]
  public async Task Deselected_catalog_note_stays_available_after_save_and_open()
  {
    var store = await LoadedStoreAsync();
    var component = store.Components[0];
    var familyId = component.Entry.MatchResult.MatchedFamily!.Id;
    var parameter = await _db.FormParameters.FirstAsync(p => p.Form.Number == "4");
    _db.FamilyNotes.Add(new FamilyNote { FamilyId = familyId, FormParameterId = parameter.Id,
      Note = new Note { Text = "Доступно для выбора" } });
    await _db.SaveChangesAsync();
    // Reload after adding catalog data to avoid the existing lazy-load cache.
    store = await LoadedStoreAsync();
    component = store.Components[0];
    await component.LoadNtdValuesAsync(_db);
    var row = component.NtdValues.Single(r => r.FormParameterId == parameter.Id);
    Assert.Single(row.AvailableNotes).IsSelected = false;
    await CreateService(store).SaveAsync(_path);
    var restored = new ProjectStore();
    await CreateService(restored).OpenAsync(_path);
    var restoredRow = restored.Components[0].NtdValues.Single(r => r.FormParameterId == parameter.Id);
    Assert.Empty(restoredRow.Notes);
    var choice = Assert.Single(restoredRow.AvailableNotes);
    Assert.False(choice.IsSelected);
    Assert.False(restored.IsDirty);
    choice.IsSelected = true;
    Assert.Single(restoredRow.Notes);
    Assert.True(restored.IsDirty);
  }

  [Fact]
  public async Task Multiple_conditions_and_parameter_conditions_survive_save_and_open()
  {
    var store = await LoadedStoreAsync();
    var snapshot = OperationMaps.Application.Models.ComponentRegimeSelection.Combine([
      new("Ресурс", "Облегчённый режим", [new("4", 1, "150000")]),
      new("Напряжение", "При охлаждении", [new("67", 1, "50")])
    ]);
    store.Components[0].SelectRegime(snapshot);
    await CreateService(store).SaveAsync(_path);
    var restored = new ProjectStore();
    await CreateService(restored).OpenAsync(_path);
    var actual = restored.Components[0].ActiveRegime!;
    Assert.Equal(2, actual.Conditions.Count);
    Assert.Equal(snapshot!.Values, actual.Values);
    Assert.Equal("Облегчённый режим", actual.Values.Single(v => v.FormNumber == "4").Condition);
    Assert.False(restored.IsDirty);
  }

  [Fact]
  public async Task Selected_regime_is_a_project_snapshot_and_survives_save_and_open()
  {
    var store = await LoadedStoreAsync();
    var component = store.Components[0];
    component.SelectRegime(new OperationMaps.Application.Models.ComponentRegimeSelection(
      "Облегчённый", "U ≤ 0,6 Uном", [new("4", 1, "150000")]));
    await CreateService(store).SaveAsync(_path);
    var restored = new ProjectStore();
    await CreateService(restored).OpenAsync(_path);
    Assert.Equal("Облегчённый", restored.Components[0].ActiveRegime!.Name);
    Assert.Equal("150000", Assert.Single(restored.Components[0].ActiveRegime!.Values).Value);
    Assert.False(restored.IsDirty);
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
    await new OwnFormStateService(store, TestCatalogDbContextFactory.From(_db)).GetAsync(_form.Id);
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
    if (Directory.Exists(RecoveryFolder))
    {
      foreach (var file in Directory.GetFiles(RecoveryFolder)) File.Delete(file);
      Directory.Delete(RecoveryFolder);
    }
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
