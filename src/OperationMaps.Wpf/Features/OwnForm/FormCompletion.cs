namespace OperationMaps.Wpf.Features.OwnForm;

/// <summary>Common rules for progress, filtering and navigation to missing values.</summary>
public static class FormCompletion
{
  public static bool IsMissing(string? value)
      => string.IsNullOrWhiteSpace(value) || value.Trim() is "-" or "—";

  public static IEnumerable<FormParameterRowVm> Relevant(FormColumnVm column,
      IReadOnlyList<FormParameterRowVm> parameters)
      => parameters.Where(p => p.IsOptional
          ? column.HasOptionalRow(p.FormParameterId)
          : p.IsRequired || p.IsLoadFactorResult || !IsMissing(column.GetNtdValue(p.FormParameterId)));

  public static bool IsFilled(FormColumnVm column, FormParameterRowVm parameter)
      => !IsMissing(parameter.IsOptional
          ? column.GetOptionalCellValue(parameter.FormParameterId)
          : column.GetCellValue(parameter.FormParameterId));

  public static IEnumerable<FormParameterRowVm> Missing(FormColumnVm column,
      IReadOnlyList<FormParameterRowVm> parameters)
      => Relevant(column, parameters).Where(p => !IsFilled(column, p));
}
