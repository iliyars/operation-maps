namespace OperationMaps.Application.Services
{
  public interface IDialogService
  {
    void ShowError(string message, string? title = null);

    void ShowWarning(string message, string? title = null);
  }
}
