namespace OperationMaps.Application.Services
{
  public interface IDialogService
  {
    void ShowError(string message, string? title = null);

    void ShowWarning(string message, string? title = null);

    void ShowInfo(string message, string? title = null);

    /// <summary>Yes/No/Cancel question: true = Yes, false = No, null = Cancel.</summary>
    bool? AskYesNoCancel(string message, string? title = null);
  }
}
