using OperationMaps.Wpf.Shared.Dialogs;

namespace OperationMaps.Tests.Wpf;

public sealed class MessageDialogContentTests
{
  [Theory]
  [InlineData("\n")]
  [InlineData("\r\n")]
  public void Confirmation_question_stays_visible_separately_from_scrollable_details(string newline)
  {
    var content = new MessageDialogContent("Проверка", $"Замечания:{newline}• Нет значения{newline}{newline}Продолжить экспорт?",
        MessageDialogKind.Question, 500);
    Assert.Equal($"Замечания:\n• Нет значения", content.MessageBody);
    Assert.Equal("Продолжить экспорт?", content.QuestionPrompt);
    Assert.True(content.HasQuestionPrompt);
    Assert.True(content.BodyMaxHeight < content.MessageMaxHeight);
  }

  [Theory]
  [InlineData("Сохранить изменения?")]
  [InlineData("Первый абзац\n\nПоследний абзац без вопроса.")]
  public void Messages_without_a_separate_final_question_keep_all_their_text(string message)
  {
    var content = new MessageDialogContent("Подтверждение", message, MessageDialogKind.Question, 500);
    Assert.Null(content.QuestionPrompt);
    Assert.Equal(message, content.MessageBody);
  }

  [Fact]
  public void Informational_message_is_not_reinterpreted_as_a_confirmation()
  {
    const string message = "Подробности\n\nВопрос в тексте?";
    var content = new MessageDialogContent("Готово", message, MessageDialogKind.Information, 500);
    Assert.True(content.IsNotice);
    Assert.False(content.IsQuestion);
    Assert.Equal(message, content.MessageBody);
    Assert.Null(content.QuestionPrompt);
  }
}
