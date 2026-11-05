using System.ComponentModel;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Features.ExportCheck;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class ReportOpeningTests : IDisposable
{
  private readonly string _folder = Path.Combine(Path.GetTempPath(), $"report-opening-{Guid.NewGuid():N}");
  private readonly SqliteConnection _connection = new("Data Source=:memory:");
  private readonly CatalogDbContext _db;
  private readonly ServiceProvider _provider;
  private readonly Mock<IDialogService> _dialogs = new();
  private readonly Mock<IDocumentLauncher> _launcher = new();
  private readonly ProjectStore _store;
  private readonly ShellViewModel _shell;

  public ReportOpeningTests()
  {
    _connection.Open();
    _db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
    _db.Forms.AddRange(new Form { Number = "3", Title = "Платы" }, new Form { Number = "4", Title = "Форма 4" });
    _db.SaveChanges();
    var navigation = new Mock<INavigationService>();
    navigation.Setup(n => n.NavigateAsync<ExportCheckViewModel>(It.IsAny<object>(), true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(UserPreferences.CreateInMemory());
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(TestCatalogDbContextFactory.From(_db));
    services.AddSingleton(_dialogs.Object);
    services.AddSingleton(_launcher.Object);
    services.AddSingleton(navigation.Object);
    _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    _store = _provider.GetRequiredService<ProjectStore>();
    var result = new ProjectMatchResult
    {
      Matched = [new ComponentMatchEntry
      {
        Imported = new ImportedComponent { RawName = "Плата", DetectedCategory = "Плата", Positions = ["A1"] },
        MatchResult = new MatchResult { IsMatched = true },
      }], Unresolved = [], Warnings = [],
    };
    _store.Load("Плата", _folder, result);
    _store.DocumentNumber = "АБВГ.123456.001";
    _store.DocumentTitle = "Плата";
    _store.HeadName = "Иванов И.И.";
    _store.HeadOccupation = "Руководитель";
    _shell = _provider.GetRequiredService<ShellViewModel>();
    _shell.OnProjectLoaded("Плата", result);
    _dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), "Проверка перед экспортом")).Returns(true);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  [InlineData(null)]
  public async Task Successful_export_offers_opening_and_launches_only_after_yes(bool? answer)
  {
    _dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), "Итоговый документ"))
        .Callback((string message, string? _) => Assert.True(File.Exists(_store.ReportDocumentPath), message))
        .Returns(answer);
    _launcher.Setup(l => l.Open(It.IsAny<string>())).Callback((string path) =>
    {
      Assert.Equal(_store.ReportDocumentPath, path);
      using var document = WordprocessingDocument.Open(path, false);
      Assert.NotNull(document.MainDocumentPart);
    });

    await _shell.ExportReportCommand.ExecuteAsync(null);

    Assert.True(File.Exists(_store.ReportDocumentPath));
    _dialogs.Verify(d => d.AskYesNoCancel(It.Is<string>(m => m.Contains("Открыть документ в Word?")
        && m.Contains(_store.ReportDocumentPath!)), "Итоговый документ"), Times.Once);
    _launcher.Verify(l => l.Open(_store.ReportDocumentPath!), answer == true ? Times.Once() : Times.Never());
    _dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    Assert.True(_shell.Operation.IsIdle);
  }

  [Fact]
  public async Task Opening_failure_does_not_turn_saved_export_into_a_failure()
  {
    _dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), "Итоговый документ")).Returns(true);
    _launcher.Setup(l => l.Open(It.IsAny<string>())).Throws(new Win32Exception("Нет приложения для .docx"));
    await _shell.ExportReportCommand.ExecuteAsync(null);
    using var document = WordprocessingDocument.Open(_store.ReportDocumentPath!, false);
    Assert.NotNull(document.MainDocumentPart);
    _dialogs.Verify(d => d.ShowWarning(It.Is<string>(m => m.Contains("Документ сохранён")
        && m.Contains(_store.ReportDocumentPath!)), "Открытие документа"), Times.Once);
    _dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    Assert.False(_shell.IsExportingReport);
  }

  [Fact]
  public async Task Locked_previous_report_does_not_offer_or_launch_a_document()
  {
    Directory.CreateDirectory(_store.FormsFolder!);
    await File.WriteAllBytesAsync(_store.ReportDocumentPath!, [1, 2, 3]);
    using (File.Open(_store.ReportDocumentPath!, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      await _shell.ExportReportCommand.ExecuteAsync(null);
    _dialogs.Verify(d => d.AskYesNoCancel(It.IsAny<string>(), "Итоговый документ"), Times.Never);
    _launcher.Verify(l => l.Open(It.IsAny<string>()), Times.Never);
    Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(_store.ReportDocumentPath!));
  }

  public void Dispose()
  {
    _provider.Dispose(); _db.Dispose(); _connection.Dispose();
    if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
  }
}
