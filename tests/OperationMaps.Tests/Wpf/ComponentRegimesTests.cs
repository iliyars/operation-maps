using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Infrastructure;
using OperationMaps.Wpf;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class ComponentRegimesTests : IDisposable
{
  private readonly SqliteConnection _connection = new("Data Source=:memory:");
  private readonly CatalogDbContext _db;
  private readonly Component _component;
  private readonly FormParameter _resource;
  private readonly FormParameter _voltage;
  private readonly IDbContextFactory<CatalogDbContext> _factory;

  public ComponentRegimesTests()
  {
    _connection.Open();
    _db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
    var form4 = new Form { Number = "4", Title = "Оценка" };
    _resource = new() { Form = form4, RowNumber = 1, Name = "Показатель ресурса" };
    var own = new Form { Number = "67", Title = "Режимы" };
    _voltage = new() { Form = own, RowNumber = 1, Name = "Напряжение" };
    var family = new Family { Name = "К10-84в", ComponentType = new ComponentType { Name = "Конденсатор" } };
    _component = new Component { FullName = "К10-84в 10мкФ", Family = family, OwnForm = own };
    _db.Components.Add(_component);
    _db.FamilyNtdValues.Add(new() { Family = family, FormParameter = _resource, Value = "100000" });
    _db.ComponentNtdValues.Add(new() { Component = _component, FormParameter = _voltage, Value = "10" });
    _db.SaveChanges();
    _factory = TestCatalogDbContextFactory.From(_db);
  }

  private async Task<ProjectStore> StoreAsync()
  {
    var result = await new ComponentMatcher(_factory, new ComponentNameParser()).MatchAllAsync([
      new ImportedComponent { RawName = _component.FullName, DetectedCategory = "Конденсатор", Positions = ["C1", "C2"] }
    ]);
    var store = new ProjectStore(); store.Load("Режимы", Path.GetTempPath(), result); return store;
  }

  [Fact]
  public async Task Form4_bulk_relief_applies_to_unopened_components_preserves_others_and_exports_notes()
  {
    var ordinary = new Component { FullName = "К10-84в 20мкФ", FamilyId = _component.FamilyId, OwnFormId = _voltage.FormId };
    var another = new Component { FullName = "К10-84в 30мкФ", FamilyId = _component.FamilyId, OwnFormId = _voltage.FormId };
    _db.Components.AddRange(ordinary, another);
    await _db.SaveChangesAsync();
    foreach (var component in new[] { _component, another })
      _db.ComponentRegimes.Add(new ComponentRegime
      {
        ComponentId = component.Id, Name = "Облегчённый режим", Condition = "Используется в облегчённом режиме при U ≤ 0,6 Uном",
        Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "150000" }]
      });
    await _db.SaveChangesAsync();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    services.AddSingleton(_factory);
    await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    var store = provider.GetRequiredService<ProjectStore>();
    var result = await new ComponentMatcher(_factory, new ComponentNameParser()).MatchAllAsync(
      new[] { _component, ordinary, another }.Select((c, i) => new ImportedComponent
      { RawName = c.FullName, DetectedCategory = "Конденсатор", Positions = [$"C{i + 1}"] }).ToList());
    store.Load("Применить ко всем", Path.GetTempPath(), result);
    var vm = provider.GetRequiredService<OperationMaps.Wpf.Features.Form4.Form4ViewModel>();
    await vm.OnNavigatedToAsync();
    Assert.False(store.Components[2].NtdLoaded);
    vm.SelectGroupCommand.Execute(vm.Groups[0]);
    await vm.ApplyReliefToAllCommand.ExecuteAsync(null);
    Assert.Equal("150000", Assert.Single(store.Components[0].NtdValues).Value);
    Assert.Equal("100000", Assert.Single(store.Components[1].NtdValues).Value);
    Assert.Equal("150000", Assert.Single(store.Components[2].NtdValues).Value);
    Assert.Null(store.Components[1].ActiveRegime);
    Assert.True(store.Components[0].NtdValues[0].AvailableNotes.Single().IsSelected);
    Assert.True(store.Components[2].NtdValues[0].AvailableNotes.Single().IsSelected);
    Assert.NotNull(vm.SelectedGroup);
    Assert.True(store.IsDirty);
    var exported = vm.BuildWordFormData();
    Assert.Equal(2, exported.Components.Count(c => c.Note.Contains("облегчённом режиме")
      && c.NtdValues.Values.Any(v => v.Contains("150000") && v.Contains('*'))));
    var edits = 0;
    foreach (var component in store.Components) component.Edited += () => edits++;
    await vm.ApplyReliefToAllCommand.ExecuteAsync(null);
    Assert.Equal(0, edits);
    Assert.Equal("100000", await _db.FamilyNtdValues.Select(v => v.Value).SingleAsync());
  }

  [Fact]
  public async Task Bulk_relief_does_not_partially_apply_conflicting_alternatives()
  {
    foreach (var value in new[] { "150000", "180000" })
      _db.ComponentRegimes.Add(new ComponentRegime
      {
        ComponentId = _component.Id, Name = $"Облегчённый {value}", Condition = $"Облегчённый режим {value}",
        Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = value }]
      });
    await _db.SaveChangesAsync();
    var component = (await StoreAsync()).Components[0];
    await component.LoadNtdValuesAsync(_db);
    Assert.Throws<InvalidOperationException>(() => component.ApplyAllReliefNotes());
    Assert.Null(component.ActiveRegime);
    Assert.Equal("100000", Assert.Single(component.NtdValues).Value);
    Assert.All(component.NtdValues[0].AvailableNotes, n => Assert.False(n.IsSelected));
  }

  [Fact]
  public async Task Legacy_ordinary_note_is_restored_as_default_and_does_not_switch_value()
  {
    _db.FamilyNtdValues.RemoveRange(_db.FamilyNtdValues);
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = _component.Id, Name = "Условный режим 1", Condition = "Применяются дополнительные средства защиты",
      Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "180000" }]
    });
    await _db.SaveChangesAsync();
    var component = (await StoreAsync()).Components[0];
    await component.LoadNtdValuesAsync(_db);
    var row = Assert.Single(component.NtdValues);
    Assert.Equal("180000", row.Value);
    var note = Assert.Single(row.AvailableNotes);
    note.IsSelected = false;
    Assert.Equal("180000", row.Value);
    Assert.Null(component.ActiveRegime);
  }

  [Fact]
  public async Task Form4_relief_note_is_selectable_at_parameter_and_restores_base_value()
  {
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = _component.Id, Name = "Сохранённое значение", Condition = "Используется в облегченном режиме при U ≤ 0,6 Uном",
      Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "150000" }]
    });
    await _db.SaveChangesAsync();
    var store = await StoreAsync();
    var component = store.Components[0];
    await component.LoadNtdValuesAsync(_db);
    var row = Assert.Single(component.NtdValues);
    var note = Assert.Single(row.AvailableNotes);
    Assert.Contains("150000", note.Text);
    Assert.False(note.IsSelected);
    Assert.Equal("100000", row.Value);
    note.IsSelected = true;
    Assert.Equal("150000", row.Value);
    Assert.Contains("облегченном режиме", row.RegimeCondition);
    Assert.True(store.IsDirty);
    note.IsSelected = false;
    Assert.Equal("100000", row.Value);
    Assert.Empty(row.RegimeCondition);
    Assert.Null(component.ActiveRegime);
    Assert.Equal("100000", await _db.FamilyNtdValues.Select(v => v.Value).SingleAsync());
  }

  [Fact]
  public async Task Relief_note_remains_available_when_default_is_unknown()
  {
    _db.FamilyNtdValues.RemoveRange(_db.FamilyNtdValues);
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = _component.Id, Name = "Облегчённый режим", Condition = "Используется в облегчённом режиме",
      Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "180000" }]
    });
    await _db.SaveChangesAsync();
    var component = (await StoreAsync()).Components[0];
    await component.LoadNtdValuesAsync(_db);
    var row = Assert.Single(component.NtdValues);
    Assert.Equal("—", row.Value);
    var note = Assert.Single(row.AvailableNotes);
    note.IsSelected = true;
    Assert.Equal("180000", row.Value);
    note.IsSelected = false;
    Assert.Equal("—", row.Value);
  }

  [Fact]
  public async Task Conditions_combine_values_reject_conflicts_and_restore_defaults()
  {
    _db.ComponentRegimes.AddRange(
      new ComponentRegime { ComponentId = _component.Id, Name = "Ресурс", Condition = "Облегчённый режим",
        Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "150000" }] },
      new ComponentRegime { ComponentId = _component.Id, Name = "Напряжение", Condition = "При охлаждении",
        Values = [new ComponentRegimeValue { FormParameterId = _voltage.Id, Value = "15" }] },
      new ComponentRegime { ComponentId = _component.Id, Name = "Другой ресурс", Condition = "Особый режим",
        Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "200000" }] });
    await _db.SaveChangesAsync();
    var store = await StoreAsync();
    var component = store.Components[0];
    await component.LoadNtdValuesAsync(_db);
    var editor = new ComponentRegimesViewModel(_factory);
    await editor.LoadAsync(component);
    var resource = editor.ConditionChoices.Single(c => c.Text == "Облегчённый режим");
    var voltage = editor.ConditionChoices.Single(c => c.Text == "При охлаждении");
    resource.IsSelected = true;
    voltage.IsSelected = true;
    Assert.Equal(2, component.ActiveRegime!.Conditions.Count);
    Assert.Equal("150000", component.NtdValues.Single().Value);
    Assert.Equal("Облегчённый режим", component.NtdValues.Single().RegimeCondition);
    var conflicting = editor.ConditionChoices.Single(c => c.Text == "Особый режим");
    conflicting.IsSelected = true;
    Assert.False(conflicting.IsSelected);
    Assert.Contains("разные значения", editor.Error);
    Assert.Equal("150000", component.NtdValues.Single().Value);
    resource.IsSelected = false;
    Assert.Equal("100000", component.NtdValues.Single().Value);
    Assert.True(voltage.IsSelected);
    await editor.LoadAsync(component);
    Assert.True(editor.ConditionChoices.Single(c => c.Text == "При охлаждении").IsSelected);
    editor.ConditionChoices.Single(c => c.Text == "При охлаждении").IsSelected = false;
    Assert.Null(component.ActiveRegime);
  }

  [Fact]
  public async Task Catalog_notes_can_be_deselected_reselected_and_copied_without_changing_catalog()
  {
    _db.FamilyNotes.Add(new FamilyNote { FamilyId = _component.FamilyId, FormParameterId = _resource.Id,
      Note = new Note { Text = "Примечание ресурса" } });
    _db.ComponentNotes.Add(new ComponentNote { ComponentId = _component.Id, FormParameterId = _voltage.Id,
      Note = new Note { Text = "Примечание напряжения" } });
    await _db.SaveChangesAsync();
    var store = await StoreAsync();
    var component = store.Components[0];
    await component.LoadNtdValuesAsync(_db);
    var row = Assert.Single(component.NtdValues);
    var choice = Assert.Single(row.AvailableNotes);
    choice.IsSelected = false;
    Assert.Empty(row.Notes);
    Assert.True(store.IsDirty);
    choice.IsSelected = true;
    Assert.Equal("Примечание ресурса", Assert.Single(row.Notes).NoteText);
    row.Notes[0].DeleteCommand.Execute(null);
    Assert.False(choice.IsSelected);

    var state = await new OwnFormStateService(store, _factory).GetAsync(_voltage.FormId);
    Assert.NotNull(state);
    var column = Assert.Single(state.Columns);
    var ownChoice = Assert.Single(column.GetAvailableNotes(_voltage.Id));
    ownChoice.IsSelected = false;
    var cloned = column.CloneWithPositions(["C1"]);
    Assert.Empty(cloned.GetNotes(_voltage.Id));
    Assert.False(Assert.Single(cloned.GetAvailableNotes(_voltage.Id)).IsSelected);
    cloned.GetAvailableNotes(_voltage.Id)[0].IsSelected = true;
    Assert.Single(cloned.GetNotes(_voltage.Id));
    Assert.Empty(column.GetNotes(_voltage.Id));
    Assert.Equal(1, await _db.FamilyNotes.CountAsync());
    Assert.Equal(1, await _db.ComponentNotes.CountAsync());
  }

  [Fact]
  public async Task Editor_saves_multi_parameter_regime_and_reset_restores_defaults_without_changing_scheme_or_catalog()
  {
    var store = await StoreAsync();
    var component = store.Components[0];
    await component.LoadNtdValuesAsync(_db);
    component.NtdValues[0].AddNote("Свободное пояснение");
    var states = new OwnFormStateService(store, _factory);
    var state = (await states.GetAsync(_voltage.FormId))!;
    var column = Assert.Single(state.Columns);
    column.SetCellValue(_voltage.Id, "3");
    var editor = new ComponentRegimesViewModel(_factory);
    await editor.LoadAsync(component);
    editor.NewRegimeCommand.Execute(null);
    editor.Name = "Облегчённый"; editor.Condition = "U ≤ 0,6 Uном";
    editor.Values.Single(v => v.ParameterId == _resource.Id).Value = "150000";
    editor.Values.Single(v => v.ParameterId == _voltage.Id).Value = "6";
    await editor.SaveCommand.ExecuteAsync(null);
    Assert.Empty(editor.Error);
    Assert.Equal("150000", component.NtdValues[0].Value);
    Assert.Equal("U ≤ 0,6 Uном", component.NtdValues[0].RegimeCondition);
    Assert.Single(component.NtdValues[0].Notes);
    await states.GetAsync(_voltage.FormId);
    Assert.Equal("6", column.GetNtdValue(_voltage.Id));
    Assert.Equal("3", column.GetCellValue(_voltage.Id));
    var data = OwnFormWordDataBuilder.Build("67", "TEST", state.Parameters, state.Columns);
    Assert.Equal("6 *", Assert.Single(data.Components).NtdValues[1]);
    Assert.Contains("U ≤ 0,6 Uном", data.Components[0].Note);
    Assert.Equal("100000", await _db.FamilyNtdValues.Select(v => v.Value).SingleAsync());
    Assert.Equal("10", await _db.ComponentNtdValues.Select(v => v.Value).SingleAsync());
    editor.ResetCommand.Execute(null);
    Assert.Null(component.ActiveRegime);
    Assert.Equal("100000", component.NtdValues[0].Value);
    Assert.Empty(component.NtdValues[0].RegimeCondition);
    await states.GetAsync(_voltage.FormId);
    Assert.Equal("10", column.GetNtdValue(_voltage.Id));
    Assert.Equal("3", column.GetCellValue(_voltage.Id));
    Assert.Empty(column.RegimeConditions);
    Assert.Single(component.NtdValues[0].Notes);
    Assert.True(store.IsDirty);
  }

  [Fact]
  public async Task Word_notes_do_not_create_regimes_or_overwrite_known_family_values()
  {
    var service = new WordCatalogImportService(_factory, new ComponentEntryService(_factory), new ComponentNameParser(),
      new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources")));
    var created = await service.ImportAsync(new WordImportedComponent
    {
      FullName = "К10-84в 20мкФ", OwnFormId = _voltage.FormId, OwnFormNumber = "67", OwnFormTitle = "Режимы",
      ComponentTypeName = "Конденсатор", Form4NtdValuesByRow = new() { [1] = "150000" },
      Form4NotesByRow = new() { [1] = ["При температуре 25 °C"] }, OwnFormNtdValuesByRow = new() { [1] = "10" }
    });
    Assert.False(await _db.ComponentRegimes.AnyAsync(r => r.ComponentId == created.Id));
    Assert.Equal("100000", await _db.FamilyNtdValues.Select(v => v.Value).SingleAsync());
    Assert.Single(await _db.FamilyNotes.ToListAsync());
  }

  [Fact]
  public async Task Migrations_create_regime_tables()
  {
    using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open();
    await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
    await db.Database.MigrateAsync();
    Assert.Empty(await db.ComponentRegimes.ToListAsync());
    Assert.Empty(await db.ComponentRegimeValues.ToListAsync());
  }

  [Fact]
  public async Task Ordinary_word_value_and_note_are_saved_without_automatic_regime()
  {
    var service = new WordCatalogImportService(_factory, new ComponentEntryService(_factory), new ComponentNameParser(),
      new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources")));
    var created = await service.ImportAsync(new WordImportedComponent
    {
      FullName = "К10-79-10мкФ", OwnFormId = _voltage.FormId, OwnFormNumber = "67", OwnFormTitle = "Режимы",
      ComponentTypeName = "Конденсатор", Form4NtdValuesByRow = new() { [1] = "150000" },
      Form4NotesByRow = new() { [1] = ["При температуре 25 °C"] }
    });
    Assert.Equal("150000", await _db.FamilyNtdValues.Where(v => v.FamilyId == created.FamilyId).Select(v => v.Value).SingleAsync());
    Assert.False(await _db.ComponentRegimes.AnyAsync(r => r.ComponentId == created.Id));
  }

  public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
