namespace OperationMaps.Wpf.Shared.Dialogs;

public enum MessageDialogKind { Information, Warning, Error, Question }

public sealed record MessageDialogContent(string Title, string Message, MessageDialogKind Kind, double MessageMaxHeight)
{
  public bool IsQuestion => Kind == MessageDialogKind.Question;
  public bool IsNotice => !IsQuestion;
  private string NormalizedMessage => Message.Replace("\r\n", "\n").Replace('\r', '\n');
  private int QuestionStart
  {
    get
    {
      var text = NormalizedMessage.TrimEnd();
      var separator = text.LastIndexOf("\n\n", StringComparison.Ordinal);
      return IsQuestion && text.EndsWith('?') && separator >= 0 && text.Length - separator <= 300
          ? separator : -1;
    }
  }
  public string MessageBody => QuestionStart is var index && index >= 0 ? NormalizedMessage[..index] : Message;
  public string? QuestionPrompt => QuestionStart is var index && index >= 0 ? NormalizedMessage[(index + 2)..].Trim() : null;
  public bool HasQuestionPrompt => QuestionPrompt is not null;
  public double BodyMaxHeight => Math.Max(80, MessageMaxHeight - (HasQuestionPrompt ? 100 : 0));
  public string Symbol => Kind switch
  {
    MessageDialogKind.Error => "\uEA39",
    MessageDialogKind.Warning => "\uE7BA",
    MessageDialogKind.Question => "\uE897",
    _ => "\uE946",
  };
}
