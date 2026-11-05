using OperationMaps.Wpf.Services;

namespace OperationMaps.Tests.Wpf;

public sealed class OperationStatusTests
{
  [Fact]
  public async Task Completing_older_operation_does_not_unlock_workspace_or_clear_current_message()
  {
    var status = new OperationStatus();
    var older = status.Begin("Чтение");
    using var newer = status.Begin("Экспорт");
    older.Dispose();
    older.Dispose();
    await older.ReportAsync("Запоздалое обновление");
    Assert.False(status.IsIdle);
    Assert.Equal("Экспорт", status.Message);
    newer.Dispose();
    Assert.True(status.IsIdle);
  }

  [Fact]
  public async Task Overlapping_operations_keep_latest_status_until_it_finishes()
  {
    var status = new OperationStatus();
    using var first = status.Begin("Чтение XML");
    var second = status.Begin("Экспорт Word");
    await first.ReportAsync("Сопоставление");
    Assert.True(status.IsBusy);
    Assert.Equal("Экспорт Word", status.Message);
    second.Dispose();
    Assert.Equal("Сопоставление", status.Message);
    first.Dispose();
    Assert.False(status.IsBusy);
    Assert.Equal("Готово", status.Message);
    await first.ReportAsync("Устаревшее сообщение");
    Assert.Equal("Готово", status.Message);
  }

  [Fact]
  public void Failure_clears_status_and_notifies_bindings()
  {
    var status = new OperationStatus();
    var changed = new List<string?>();
    status.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
    Assert.Throws<InvalidOperationException>((Action)(() =>
    {
      using var operation = status.Begin("Экспорт");
      throw new InvalidOperationException();
    }));
    Assert.False(status.IsBusy);
    Assert.Equal("Готово", status.Message);
    Assert.Contains(nameof(OperationStatus.IsBusy), changed);
    Assert.Contains(nameof(OperationStatus.Message), changed);
  }
}
