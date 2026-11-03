using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.ExportCheck;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Stores;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Infrastructure.Navigation;

namespace OperationMaps.Tests.Wpf;

public sealed class ProjectExportCheckTests
{
  [Fact]
  public async Task Form4_exposes_empty_cells_for_notes_and_exports_their_markers()
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
    db.Database.EnsureCreated();
    db.Forms.Add(new Form { Number = "4", Title = "Форма 4", Parameters = [
      new FormParameter { RowNumber = 1, Name = "Первый параметр" },
      new FormParameter { RowNumber = 2, Name = "Второй параметр" }
    ] });
    db.SaveChanges();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(TestCatalogDbContextFactory.From(db));
    await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    var store = provider.GetRequiredService<ProjectStore>();
    store.Load("Примечания", Path.GetTempPath(), new ProjectMatchResult
    {
      Matched = [], Warnings = [], Unresolved = [new ComponentMatchEntry
      {
        Imported = new ImportedComponent { RawName = "Неизвестный компонент", DetectedCategory = "Резистор", Positions = ["R1"] },
        MatchResult = new MatchResult { IsMatched = false }
      }]
    });
    var vm = provider.GetRequiredService<OperationMaps.Wpf.Features.Form4.Form4ViewModel>();
    await vm.OnNavigatedToAsync();
    var group = Assert.Single(vm.Groups);
    Assert.False(group.HasCatalogData);
    Assert.True(group.HasNtdValues);
    Assert.Equal(2, group.NtdValues.Count);
    group.NtdValues[0].AddNote("Первое примечание");
    group.NtdValues[1].AddNote("Второе примечание");
    var exported = Assert.Single(vm.BuildWordFormData().Components);
    Assert.Equal("— *", exported.NtdValues[1]);
    Assert.Equal("— **", exported.NtdValues[2]);
    Assert.Equal("* Первое примечание\n** Второе примечание", exported.Note);
    group.NtdValues[0].Notes[0].DeleteCommand.Execute(null);
    Assert.Equal("*", group.NtdValues[1].Notes[0].Marker);
  }

  [Fact]
  public async Task Boards_are_planned_as_form3_before_form4_without_catalog_requirements()
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
    db.Database.EnsureCreated();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(TestCatalogDbContextFactory.From(db));
    services.AddSingleton(Mock.Of<IDialogService>());
    services.AddSingleton(Mock.Of<INavigationService>());
    await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    var board = new ImportedComponent
    {
      RawName = "модуля питания ВЕИР.687282.840", DetectedCategory = "Плата", Positions = ["A1"]
    };
    var result = new ProjectMatchResult
    {
      Matched = [], Warnings = [],
      Unresolved = [new ComponentMatchEntry { Imported = board, MatchResult = new MatchResult { IsMatched = false } }]
    };
    var store = provider.GetRequiredService<ProjectStore>();
    store.Load("Тест формы 3", Path.GetTempPath(), result);
    var shell = provider.GetRequiredService<ShellViewModel>();
    shell.OnProjectLoaded("Тест формы 3", result);
    var plan = await shell.PlanContentsAsync();
    Assert.Equal(3, plan.FirstSheetOf("3"));
    Assert.Equal(4, plan.FirstSheetOf("4"));
    var issues = await provider.GetRequiredService<ProjectExportCheckService>().CheckAsync();
    Assert.DoesNotContain(issues, i => i.Target == ExportIssueTarget.Components);
    Assert.DoesNotContain(issues, i => i.Message.StartsWith("Форма 3:"));
    var form4 = provider.GetRequiredService<OperationMaps.Wpf.Features.Form4.Form4ViewModel>();
    await form4.OnNavigatedToAsync();
    Assert.Empty(form4.BuildWordFormData().Components);
  }

  [Fact]
  public async Task Declining_incomplete_export_shows_check_screen_and_writes_no_document()
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
    db.Database.EnsureCreated();
    var dialogs = new Mock<IDialogService>();
    dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>())).Returns((bool?)null);
    var navigation = new Mock<INavigationService>();
    navigation.Setup(n => n.NavigateAsync<ExportCheckViewModel>(It.IsAny<object>(), true, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(TestCatalogDbContextFactory.From(db));
    services.AddSingleton(dialogs.Object);
    services.AddSingleton(navigation.Object);
    await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    var store = provider.GetRequiredService<ProjectStore>();
    var result = new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] };
    store.Load("Плата", Path.Combine(Path.GetTempPath(), "no-export-" + Guid.NewGuid().ToString("N")), result);
    var shell = provider.GetRequiredService<ShellViewModel>();
    shell.OnProjectLoaded("Плата", result);

    await shell.ExportReportCommand.ExecuteAsync(null);

    Assert.False(Directory.Exists(store.FormsFolder));
    Assert.False(shell.IsExportingReport);
    navigation.Verify(n => n.NavigateAsync<ExportCheckViewModel>(It.IsAny<object>(), true, It.IsAny<CancellationToken>()), Times.Once);
    dialogs.Verify(d => d.AskYesNoCancel(It.IsAny<string>(), "Проверка перед экспортом"), Times.Once);
    dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

    var screen = provider.GetRequiredService<ExportCheckViewModel>();
    await screen.OpenIssueCommand.ExecuteAsync(new ExportIssue("Пропуск в форме", ExportIssueTarget.OwnForm, 67));
    navigation.Verify(n => n.NavigateAsync<OwnFormViewModel>(67, true, It.IsAny<CancellationToken>()), Times.Once);
  }

  private static (OwnFormState State, FormColumnVm Column) Form(params FormParameter[] parameters)
  {
    var state = new OwnFormState(67, "67", "Конденсаторы", [], null, null);
    var component = new ProjectComponentVm(new ComponentMatchEntry
    {
      Imported = new ImportedComponent { RawName = "К10-79", DetectedCategory = "Конденсатор", Positions = ["C1"] },
      MatchResult = new MatchResult { IsMatched = false },
    });
    var column = new FormColumnVm(component);
    state.SetParameters(parameters.Select(p => new FormParameterRowVm(p, state.Columns)).ToList());
    state.Columns.Add(column);
    return (state, column);
  }

  [Fact]
  public void Required_values_are_reported_with_form_and_row_navigation()
  {
    var (state, _) = Form(new FormParameter { Id = 1, RowNumber = 3, Name = "Напряжение", IsRequired = true });
    var issues = ProjectExportCheckService.CheckForm(state);
    Assert.Equal(2, issues.Count);
    Assert.All(issues, i =>
    {
      Assert.Contains("C1", i.Message);
      Assert.Contains("строка 3", i.Message);
      Assert.Equal(ExportIssueTarget.OwnForm, i.Target);
      Assert.Equal(67, i.FormId);
    });
  }

  [Fact]
  public void Filled_required_row_passes_check()
  {
    var (state, column) = Form(new FormParameter { Id = 1, RowNumber = 1, Name = "Напряжение", IsRequired = true });
    column.NtdValues[1] = "25";
    column.SetCellValue(1, "5");
    Assert.Empty(ProjectExportCheckService.CheckForm(state));
  }

  [Fact]
  public void Inactive_optional_row_is_ignored_but_active_row_requires_scheme_value()
  {
    var (state, column) = Form(new FormParameter { Id = 2, RowNumber = 2, Name = "Второе напряжение", IsOptional = true, IsRequired = true });
    Assert.Empty(ProjectExportCheckService.CheckForm(state));
    column.OptionalNtdValues[2] = "10";
    Assert.Single(ProjectExportCheckService.CheckForm(state));
    column.SetOptionalCellValue(2, "3");
    Assert.Empty(ProjectExportCheckService.CheckForm(state));
  }

  [Fact]
  public void Nonrequired_row_without_ntd_is_ignored()
  {
    var (state, _) = Form(new FormParameter { Id = 1, RowNumber = 1, Name = "Не применяется" });
    Assert.Empty(ProjectExportCheckService.CheckForm(state));
  }

  [Fact]
  public void Uncalculated_load_factor_is_reported_once()
  {
    var (_, column) = Form();
    var state = new OwnFormState(67, "67", "Конденсаторы", [], 2, null);
    state.SetParameters([new FormParameterRowVm(new FormParameter
    {
      Id = 2, RowNumber = 2, Name = "Коэффициент нагрузки", IsRequired = true, IsLoadFactorResult = true,
    }, state.Columns)]);
    state.Columns.Add(column);
    var issue = Assert.Single(ProjectExportCheckService.CheckForm(state));
    Assert.Contains("коэффициент нагрузки", issue.Message);
    column.LoadFactorValue = "0,5 (1)";
    Assert.Empty(ProjectExportCheckService.CheckForm(state));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("not json")]
  [InlineData("{}")]
  [InlineData("{\"componentsPerPage\":0,\"componentSlots\":[]}")]
  public async Task Missing_or_invalid_map_is_a_reported_issue_instead_of_a_failed_check(string? map)
  {
    var folder = Path.Combine(Path.GetTempPath(), "export-check-" + Guid.NewGuid().ToString("N"));
    var formFolder = Path.Combine(folder, "Form4");
    Directory.CreateDirectory(formFolder);
    try
    {
      await File.WriteAllTextAsync(Path.Combine(formFolder, "template.docx"), "template");
      if (map is not null) await File.WriteAllTextAsync(Path.Combine(formFolder, "map.json"), map);
      using var connection = new SqliteConnection("DataSource=:memory:");
      connection.Open();
      using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options);
      db.Database.EnsureCreated();
      var store = new ProjectStore();
      store.Load("Плата", folder, new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] });
      var factory = TestCatalogDbContextFactory.From(db);
      var service = new ProjectExportCheckService(store, factory, new OwnFormStateService(store, factory),
          new WordFormMapLoader(folder), Mock.Of<IRegistrationSheetService>());

      var issues = await service.CheckAsync();
      Assert.Contains(issues, i => i.Message.Contains("Форма 4:") && i.Message.Contains("карта заполнения"));
      Assert.Contains(issues, i => i.Target == ExportIssueTarget.TitlePage);
      Assert.False(store.IsDirty);
    }
    finally
    {
      File.Delete(Path.Combine(formFolder, "map.json"));
      File.Delete(Path.Combine(formFolder, "template.docx"));
      Directory.Delete(formFolder);
      Directory.Delete(folder);
    }
  }
}
