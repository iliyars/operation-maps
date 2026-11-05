using Moq;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Features.Components.WordImport;

namespace OperationMaps.Tests.Wpf;

public sealed class WordImportTypeResolutionTests
{
  [Fact]
  public async Task Latest_request_wins_even_when_old_request_ignores_cancellation_and_mutates_its_input()
  {
    var (row, requests) = Create();
    row.TypeText = "Первый";
    var first = row.TypeResolutionTask;
    row.TypeText = "Второй";
    var second = row.TypeResolutionTask;
    Assert.True(requests[0].Token.IsCancellationRequested);
    Assert.False(row.IsSelected);
    Assert.False(row.CanToggleSelection);
    Assert.NotSame(row.Model, requests[0].Draft);
    requests[1].Complete(2);
    await second;
    Assert.Equal("Второй", row.Model.ComponentTypeName);
    Assert.Equal(2, row.Model.ComponentTypeId);
    Assert.Equal("Семейство 2", row.FamilyDisplay);
    requests[0].Complete(1);
    await first;
    Assert.Equal("Второй", row.Model.ComponentTypeName);
    Assert.Equal(2, row.Model.ComponentTypeId);
    Assert.Equal(20, row.Model.ExistingFamilyId);
    Assert.Equal("Семейство 2", row.FamilyDisplay);
    Assert.True(row.IsSelected);
    Assert.True(row.CanToggleSelection);
  }

  [Fact]
  public async Task Returning_to_same_text_does_not_accept_the_first_matching_request()
  {
    var (row, requests) = Create();
    row.TypeText = "A";
    var first = row.TypeResolutionTask;
    row.TypeText = "B";
    var second = row.TypeResolutionTask;
    row.TypeText = "A";
    var third = row.TypeResolutionTask;
    requests[2].Complete(3);
    await third;
    requests[0].Complete(1);
    requests[1].Ready.SetException(new IOException("Устаревшая ошибка"));
    await Task.WhenAll(first, second);
    Assert.Equal(3, row.Model.ComponentTypeId);
    Assert.Null(row.ImportError);
    Assert.True(row.IsSelected);
  }

  [Fact]
  public async Task Resolution_failure_is_observed_and_blocks_import_until_next_success()
  {
    var (row, requests) = Create();
    row.TypeText = "Ошибка";
    requests[0].Ready.SetException(new IOException("Нет доступа"));
    await row.TypeResolutionTask;
    Assert.Contains("Нет доступа", row.ImportError);
    Assert.False(row.IsSelected);
    Assert.False(row.CanToggleSelection);
    Assert.False(row.IsResolvingType);
    row.TypeText = "Новый";
    requests[1].Complete(2);
    await row.TypeResolutionTask;
    Assert.Null(row.ImportError);
    Assert.True(row.IsSelected);
  }

  [Fact]
  public async Task Leaving_screen_prevents_late_resolution_from_selecting_or_changing_row()
  {
    var (row, requests) = Create();
    row.TypeText = "Поздний";
    var pending = row.TypeResolutionTask;
    row.CancelTypeResolution();
    requests[0].Complete(1);
    await pending;
    Assert.True(requests[0].Token.IsCancellationRequested);
    Assert.Null(row.Model.ComponentTypeId);
    Assert.False(row.IsSelected);
  }

  private static (WordImportRowVm Row, List<Request> Requests) Create()
  {
    var requests = new List<Request>();
    var service = new Mock<IWordCatalogImportService>();
    service.Setup(s => s.ResolveComponentTypeAsync(It.IsAny<WordImportedComponent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Returns((WordImportedComponent draft, string name, CancellationToken ct) =>
      {
        var request = new Request(draft, name, ct);
        requests.Add(request);
        return request.Ready.Task;
      });
    return (new(new WordImportedComponent
    {
      FullName = "Компонент", OwnFormNumber = "67", OwnFormTitle = "Форма 67", OwnFormId = 67,
    }, service.Object), requests);
  }

  private sealed record Request(WordImportedComponent Draft, string Name, CancellationToken Token)
  {
    public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Complete(int id)
    {
      Draft.ComponentTypeName = Name;
      Draft.ComponentTypeId = id;
      Draft.FamilyName = $"Семейство {id}";
      Draft.ExistingFamilyId = id * 10;
      Ready.SetResult();
    }
  }
}
