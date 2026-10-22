using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// Hands out the project-lifetime <see cref="OwnFormState"/> for a form:
  /// built from the catalog the first time the form is needed (opened on
  /// screen or exported), then kept in <see cref="ProjectStore"/> so user
  /// edits survive navigation.
  /// </summary>
  public sealed class OwnFormStateService
  {
    private readonly ProjectStore _store;
    private readonly CatalogDbContext _db;

    public OwnFormStateService(ProjectStore store, CatalogDbContext db)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Returns the cached state for <paramref name="formId"/>, building it on
    /// first use. Null when the form doesn't exist in the catalog.
    /// </summary>
    public async Task<OwnFormState?> GetAsync(int formId, CancellationToken ct = default)
    {
      if (_store.OwnForms.TryGetValue(formId, out var cached))
      {
        await SyncColumnsAsync(cached, ct);
        return cached;
      }

      var state = await LoadAsync(formId, ct);
      if (state is not null)
        _store.AddOwnForm(state);

      return state;
    }

    // ── Initial build ─────────────────────────────────────────────────────────

    private async Task<OwnFormState?> LoadAsync(int formId, CancellationToken ct)
    {
      var form = await _db.Forms
          .Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Id == formId, ct);

      if (form is null) return null;

      var parameters = form.Parameters.OrderBy(p => p.RowNumber).ToList();
      var loadFactor = LoadFactorSetup.For(form, parameters);

      var state = new OwnFormState(
          formId: form.Id,
          formNumber: form.Number,
          formTitle: form.Title,
          loadFactorOptions: loadFactor.Candidates
              .Select(p => new LoadFactorOptionVm(p.Id, p.RowNumber, p.Name))
              .ToList(),
          loadFactorResultParameterId: loadFactor.Result?.Id,
          defaultLoadFactorParameterId: loadFactor.DefaultBaseId);

      // Rows reference state.Columns live, so they can be created before
      // the columns are added.
      var rows = parameters.Select(p => new FormParameterRowVm(p, state.Columns)
      {
        // Also covers a result row recognized by its name, not just by the flag.
        IsLoadFactorResult = p.Id == loadFactor.Result?.Id,
      }).ToList();

      // Link each "primary" parameter to its optional counterpart, if any.
      // The counterpart is identified by IsOptional=true + OptionalForRowNumber
      // pointing back at the primary's RowNumber.
      foreach (var primary in rows.Where(p => !p.IsOptional))
      {
        var counterpart = rows.FirstOrDefault(
            p => p.IsOptional && p.OptionalForRowNumber == primary.RowNumber);
        if (counterpart is not null)
          primary.OptionalCounterpartId = counterpart.FormParameterId;
      }

      state.SetParameters(rows);

      var columns = await BuildColumnsAsync(state, parameters, RelevantComponents(formId), ct);
      foreach (var col in columns)
        state.Columns.Add(col);

      // Initial calculation in case values were already loaded (e.g. via
      // Word import) before the user touches anything.
      foreach (var col in state.Columns)
        state.RecalculateLoadFactor(col);

      return state;
    }

    // ── Keeping a cached state in line with the project ───────────────────────

    /// <summary>
    /// Components can be (re)matched after the form state was built — e.g. an
    /// unresolved component gets catalog data via the "add component" wizard
    /// and now belongs to this form. Adds columns for newly relevant
    /// components and drops columns whose components left the form, without
    /// touching the user's edits in every other column.
    /// </summary>
    private async Task SyncColumnsAsync(OwnFormState state, CancellationToken ct)
    {
      var relevant = RelevantComponents(state.FormId);

      // Split/merge replace columns with clones holding the same positions,
      // so "is this component represented?" is answered by positions, with
      // reference equality as a fallback for components without positions.
      var relevantPositions = relevant
          .SelectMany(c => c.Entry.Imported.Positions)
          .ToHashSet();

      var stale = state.Columns
          .Where(col => !relevant.Contains(col.Component)
                     && !col.Component.Entry.Imported.Positions.Any(relevantPositions.Contains))
          .ToList();

      var coveredPositions = state.Columns
          .SelectMany(col => col.Component.Entry.Imported.Positions)
          .ToHashSet();

      var missing = relevant
          .Where(c => !state.Columns.Any(col => col.Component == c)
                   && !c.Entry.Imported.Positions.Any(coveredPositions.Contains))
          .ToList();

      if (stale.Count == 0 && missing.Count == 0) return;

      foreach (var col in stale)
        state.Columns.Remove(col);

      if (missing.Count > 0)
      {
        var parameters = await _db.FormParameters
            .Where(p => p.FormId == state.FormId)
            .ToListAsync(ct);

        foreach (var col in await BuildColumnsAsync(state, parameters, missing, ct))
        {
          state.Columns.Add(col);
          state.RecalculateLoadFactor(col);
        }
      }

      // Recorded split/merge commands refer to the old set of columns —
      // replaying them now could resurrect removed columns.
      state.History.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<ProjectComponentVm> RelevantComponents(int formId)
        => _store.Components
            .Where(c => c.Entry.MatchResult.MatchedComponent?.OwnFormId == formId
                     || c.Entry.MatchResult.MatchedComponent?.OwnForm?.Id == formId)
            .ToList();

    private async Task<List<FormColumnVm>> BuildColumnsAsync(
        OwnFormState state,
        IReadOnlyCollection<FormParameter> parameters,
        IReadOnlyList<ProjectComponentVm> components,
        CancellationToken ct)
    {
      var formId = state.FormId;

      var componentIds = components
          .Select(c => c.Entry.MatchResult.MatchedComponent?.Id)
          .Where(id => id is not null)
          .Select(id => id!.Value)
          .ToHashSet();

      // Component.LoadFactorParameterId — per-component override of which
      // base parameter feeds the load-factor calculation.
      var loadFactorOverrideByComponent = componentIds.Count > 0
          ? await _db.Components
              .Where(c => componentIds.Contains(c.Id))
              .ToDictionaryAsync(c => c.Id, c => c.LoadFactorParameterId, ct)
          : [];

      var ntdByComponent = componentIds.Count > 0
          ? (await _db.ComponentNtdValues
              .Where(v => componentIds.Contains(v.ComponentId)
                       && v.FormParameter.FormId == formId)
              .ToListAsync(ct))
              .GroupBy(v => v.ComponentId)
              .ToDictionary(g => g.Key, g => g.ToList())
          : [];

      // Pin numbers (Form 64's "номера выводов" column) — catalog data,
      // entered once per Component via the "add component" wizard.
      var pinsByComponent = componentIds.Count > 0
          ? (await _db.ComponentPinValues
              .Where(v => componentIds.Contains(v.ComponentId)
                       && v.FormParameter.FormId == formId)
              .ToListAsync(ct))
              .GroupBy(v => v.ComponentId)
              .ToDictionary(g => g.Key, g => g.ToList())
          : [];

      // Optional parameters (e.g. the second supply voltage) don't represent
      // a normal table row — their NTD values go into OptionalNtdValues.
      var optionalParamIds = parameters.Where(p => p.IsOptional).Select(p => p.Id).ToHashSet();

      var columns = new List<FormColumnVm>();

      foreach (var component in components)
      {
        var column = new FormColumnVm(component);
        var componentId = component.Entry.MatchResult.MatchedComponent?.Id;

        if (componentId is not null)
        {
          if (ntdByComponent.TryGetValue(componentId.Value, out var ntdValues))
            foreach (var ntd in ntdValues)
              if (optionalParamIds.Contains(ntd.FormParameterId))
                column.OptionalNtdValues[ntd.FormParameterId] = ntd.Value;
              else
                column.NtdValues[ntd.FormParameterId] = ntd.Value;

          if (pinsByComponent.TryGetValue(componentId.Value, out var pinValues))
            foreach (var pin in pinValues)
              column.PinValues[pin.FormParameterId] = pin.Pins;

          // Explicit Component.LoadFactorParameterId if set, otherwise the form's default.
          loadFactorOverrideByComponent.TryGetValue(componentId.Value, out var explicitOverride);
          column.LoadFactorBaseParameterId = explicitOverride ?? state.DefaultLoadFactorParameterId;
        }

        columns.Add(column);
      }

      return columns;
    }
  }
}
