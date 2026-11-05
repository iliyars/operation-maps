using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Features.TitlePage;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.ExportCheck;

public enum ExportIssueTarget { None, Components, TitlePage, OwnForm }

public sealed record ExportIssue(string Message, ExportIssueTarget Target = ExportIssueTarget.None, int? FormId = null)
{
  public bool CanNavigate => Target != ExportIssueTarget.None;
}

/// <summary>Checks the same project-lifetime form states that report export uses.</summary>
public sealed class ProjectExportCheckService(
    ProjectStore store,
    IDbContextFactory<CatalogDbContext> dbFactory,
    OwnFormStateService ownForms,
    WordFormMapLoader maps,
    IRegistrationSheetService registration)
{
  public async Task<IReadOnlyList<ExportIssue>> CheckAsync(CancellationToken ct = default)
  {
    var issues = new List<ExportIssue>();
    if (!store.HasProject)
    {
      issues.Add(new("Откройте или импортируйте проект."));
      return issues;
    }
    if (store.Components.Count == 0)
      issues.Add(new("В проекте нет компонентов.", ExportIssueTarget.Components));
    if (!TitlePageFile.IsComplete(store))
      issues.Add(new("Титульный лист: заполните обозначение, название изделия, ФИО и должность руководителя.", ExportIssueTarget.TitlePage));
    if (!ContentsPageFile.CanWrite(store))
      issues.Add(new("Содержание: не указано обозначение документа.", ExportIssueTarget.TitlePage));

    foreach (var component in store.Components)
    {
      if (Form3DataBuilder.IsBoard(component.Entry.Imported)) continue;
      if (!component.IsMatched)
        issues.Add(new($"{component.Positions} — {component.Name}: компонент не сопоставлен с каталогом.", ExportIssueTarget.Components));
      else if (component.Entry.MatchResult.MatchedComponent?.OwnFormId is null)
        issues.Add(new($"{component.Positions} — {component.Name}: не назначена собственная форма.", ExportIssueTarget.Components));
    }

    var forms = store.Components.SelectMany(c => c.Entry.MatchResult.RequiredForms)
        .Where(f => f.Number is not ("3" or "4")).DistinctBy(f => f.Id).OrderBy(f => f.Number).ToList();
    if (store.Components.Any(c => Form3DataBuilder.IsBoard(c.Entry.Imported)) && await CheckTemplateAsync("3", ct) is { } form3Issue)
      issues.Add(form3Issue);
    if (await CheckTemplateAsync("4", ct) is { } form4Issue) issues.Add(form4Issue);
    foreach (var form in forms)
    {
      ct.ThrowIfCancellationRequested();
      if (await CheckTemplateAsync(form.Number, ct) is { } templateIssue) issues.Add(templateIssue);
      var state = await ownForms.GetAsync(form.Id, ct);
      if (state is null)
      {
        issues.Add(new($"Форма {form.Number}: отсутствует в каталоге.", ExportIssueTarget.Components));
        continue;
      }
      issues.AddRange(CheckForm(state));
    }

    await using var db = await dbFactory.CreateDbContextAsync(ct);
    var ids = store.Components.Where(c => c.Entry.MatchResult.MatchedComponent is not null)
        .Select(c => c.Entry.MatchResult.MatchedComponent!.Id).Distinct().ToArray();
    var requiredEdits = await NormalRegimeDataCheck.FindAsync(db, ids, ct);
    foreach (var component in store.Components.Where(c => c.Entry.MatchResult.MatchedComponent is not null))
    {
      component.RequiredEditReasons = requiredEdits.GetValueOrDefault(component.Entry.MatchResult.MatchedComponent!.Id) ?? [];
      foreach (var reason in component.RequiredEditReasons)
        issues.Add(new($"{component.Positions} — {component.Name}: необходимо редактирование. {reason}", ExportIssueTarget.Components));
    }
    var requiredForm4Rows = await db.FormParameters.AsNoTracking()
        .Where(p => p.Form.Number == "4" && p.IsRequired)
        .Select(p => p.RowNumber).ToListAsync(ct);
    foreach (var component in store.Components.Where(c => c.HasFamily && !Form3DataBuilder.IsBoard(c.Entry.Imported)))
    {
      await component.LoadNtdValuesAsync(db, ct);
      var missing = component.NtdValues.Where(r => requiredForm4Rows.Contains(r.RowNumber) && Missing(r.Value))
          .Select(r => r.RowNumber).ToList();
      if (missing.Count > 0)
        issues.Add(new($"Форма 4, {component.Positions}: не заполнены данные НТД в строках {string.Join(", ", missing)}.", ExportIssueTarget.Components));
    }
    if (!registration.TemplateExists)
      issues.Add(new("Лист регистрации изменений: отсутствует шаблон Word. Проверьте установку приложения."));
    return issues;
  }

  public static IReadOnlyList<ExportIssue> CheckForm(OwnFormState state)
  {
    var issues = new List<ExportIssue>();
    foreach (var column in state.Columns)
    {
      var prefix = $"Форма {state.FormNumber}, {column.Positions}";
      if (state.LoadFactorResultParameterId.HasValue && string.IsNullOrWhiteSpace(column.LoadFactorValue))
        issues.Add(new($"{prefix}: коэффициент нагрузки не рассчитан — проверьте базовый параметр и значения НТД / в схеме.", ExportIssueTarget.OwnForm, state.FormId));
      foreach (var row in FormCompletion.Relevant(column, state.Parameters).Where(p => !p.IsLoadFactorResult))
      {
        var ntd = row.IsOptional ? column.OptionalNtdValues.GetValueOrDefault(row.FormParameterId) : column.GetNtdValue(row.FormParameterId);
        var value = row.IsOptional ? column.GetOptionalCellValue(row.FormParameterId) : column.GetCellValue(row.FormParameterId);
        if (Missing(value))
          issues.Add(new($"{prefix}, строка {row.RowNumber} «{row.Name}»: не заполнено значение в схеме.", ExportIssueTarget.OwnForm, state.FormId));
        if (row.IsRequired && Missing(ntd))
          issues.Add(new($"{prefix}, строка {row.RowNumber} «{row.Name}»: не заполнено значение по НТД.", ExportIssueTarget.OwnForm, state.FormId));
      }
    }
    return issues;
  }

  public Task<ExportIssue?> CheckTemplateAsync(string number, CancellationToken ct = default)
      => Task.Run(() => CheckTemplate(number), ct);

  public ExportIssue? CheckTemplate(string number)
  {
    try
    {
      maps.ValidateTemplate(number);
      return null;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
    {
      return new($"Форма {number}: отсутствует шаблон Word либо недоступна / повреждена карта заполнения. Проверьте установку приложения.\n{ex.Message}");
    }
  }

  private static bool Missing(string? value) => string.IsNullOrWhiteSpace(value) || value.Trim() is "—" or "-";
}
