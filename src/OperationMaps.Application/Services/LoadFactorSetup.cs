using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Application.Services;

/// <summary>
/// How a form's load factor (коэффициент нагрузки) is calculated: which row
/// shows the result, which parameters the user may pick as its base, and the
/// default base.
/// <para>
/// Explicit catalog flags win (Form 67/68: <see cref="FormParameter.IsLoadFactorResult"/>,
/// <see cref="FormParameter.CanBeLoadFactorBase"/>). For every other form the
/// right base parameter isn't known yet, so: the result row is recognized by
/// its printed name, any other parameter may be chosen as the base per
/// component, and the default comes only from
/// <see cref="Form.DefaultLoadFactorParameterId"/>, which an admin sets once
/// it's clarified.
/// </para>
/// </summary>
public sealed record LoadFactorSetup(
    FormParameter? Result,
    IReadOnlyList<FormParameter> Candidates,
    int? DefaultBaseId)
{
  public const string ResultRowName = "Коэффициент нагрузки";

  public static LoadFactorSetup For(Form form, IReadOnlyList<FormParameter> parameters)
  {
    var result = parameters.FirstOrDefault(p => p.IsLoadFactorResult)
        ?? parameters.FirstOrDefault(p =>
            string.Equals(p.Name.Trim(), ResultRowName, StringComparison.OrdinalIgnoreCase));

    // Without a result row there's nothing to calculate into.
    if (result is null)
      return new LoadFactorSetup(null, [], null);

    var flagged = parameters.Where(p => p.CanBeLoadFactorBase).OrderBy(p => p.RowNumber).ToList();
    var candidates = flagged.Count > 0
        ? flagged
        // Derived rows stay in: a sum like Form 67's "Суммарное" (row1+row2+row3)
        // is exactly the kind of value a load factor is taken from.
        : parameters
            .Where(p => p.Id != result.Id && !p.IsOptional)
            .OrderBy(p => p.RowNumber)
            .ToList();

    // A default must be one of the offered choices; with exactly one
    // candidate (e.g. Form 68) that candidate is the obvious default — the
    // picker isn't even shown then.
    var defaultId = candidates.Any(c => c.Id == form.DefaultLoadFactorParameterId)
        ? form.DefaultLoadFactorParameterId
        : candidates.Count == 1 ? candidates[0].Id : null;

    return new LoadFactorSetup(result, candidates, defaultId);
  }
}
