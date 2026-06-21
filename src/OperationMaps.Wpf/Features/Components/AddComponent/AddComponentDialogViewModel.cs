using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components;
using System.Collections.ObjectModel;

namespace OperationMaps.Wpf.Features.Components.AddComponent;

/// <summary>
/// One editable Form-4 / own-form parameter row in the wizard.
/// </summary>
public sealed partial class WizardParameterRowVm : ObservableObject
{
  public int FormParameterId { get; }
  public int RowNumber { get; }
  public string DisplayName { get; }

  [ObservableProperty] private string _value = "";

  public WizardParameterRowVm(FormParameter parameter)
  {
    FormParameterId = parameter.Id;
    RowNumber = parameter.RowNumber;
    DisplayName = parameter.Unit is { Length: > 0 }
        ? $"{parameter.Name}, {parameter.Unit}"
        : parameter.Name;
  }
}

/// <summary>Selectable item for "choose own form" (Form67/68/...) on step 2.</summary>
public sealed record OwnFormOption(int Id, string Number, string Title)
{
  public string Display => $"Форма {Number} — {Title}";
}

/// <summary>
/// Two-step wizard for entering NTD values for a component that wasn't
/// found in the catalog: step 1 collects Form-4 (family-level) values,
/// step 2 lets the user pick the component's own form (Form67/68/...)
/// and fill its parameters. On confirm, creates Family (if needed) +
/// Component via <see cref="IComponentEntryService"/>, then updates the
/// originating <see cref="ProjectComponentVm"/> in place.
/// </summary>
public sealed partial class AddComponentDialogViewModel : ObservableObject
{
  private readonly CatalogDbContext _db;
  private readonly IComponentEntryService _entryService;
  private readonly IComponentNameParser _nameParser;
  private readonly ProjectComponentVm _target;

  private int _componentTypeId;
  private int? _existingFamilyId;

  // ── Step state ────────────────────────────────────────────────────────────

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(IsStep1))]
  [NotifyPropertyChangedFor(nameof(IsStep2))]
  private int _step = 1; // 1 or 2
  [ObservableProperty] private bool _isLoading = true;
  [ObservableProperty] private bool _isSaving;
  [ObservableProperty] private string? _errorMessage;

  public bool IsStep1 => Step == 1;
  public bool IsStep2 => Step == 2;

  // ── Header info (read-only, shown on both steps) ───────────────────────────

  public string ComponentRawName => _target.Entry.Imported.RawName;
  public string ComponentPositions => _target.Positions;
  public string ComponentTypeName => _target.ComponentTypeName;

  // ── Step 1: Family ────────────────────────────────────────────────────────

  /// <summary>True when MatchedFamily was already resolved by the parser.</summary>
  [ObservableProperty] private bool _familyAlreadyKnown;

  /// <summary>Editable when family wasn't resolved by the parser.</summary>
  [ObservableProperty] private string _familyName = "";

  public ObservableCollection<WizardParameterRowVm> Form4Rows { get; } = [];

  // ── Step 2: Own form ──────────────────────────────────────────────────────

  public ObservableCollection<OwnFormOption> OwnFormOptions { get; } = [];

  [ObservableProperty] private OwnFormOption? _selectedOwnForm;

  public ObservableCollection<WizardParameterRowVm> OwnFormRows { get; } = [];

  // ── Result ────────────────────────────────────────────────────────────────

  /// <summary>Set to true once the dialog has successfully saved — view closes on this.</summary>
  [ObservableProperty] private bool _isCompleted;

  public AddComponentDialogViewModel(
      CatalogDbContext db,
      IComponentEntryService entryService,
      IComponentNameParser nameParser,
      ProjectComponentVm target)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
    _target = target ?? throw new ArgumentNullException(nameof(target));
  }

  // ── Initialization ───────────────────────────────────────────────────────

  public async Task InitializeAsync(CancellationToken ct = default)
  {
    IsLoading = true;
    try
    {
      var typeName = _target.Entry.Imported.DetectedCategory;
      var type = await _db.ComponentTypes
          .FirstOrDefaultAsync(t => t.Name == typeName, ct);

      if (type is null)
      {
        ErrorMessage = $"Тип компонента «{typeName}» не найден в справочнике.";
        return;
      }
      _componentTypeId = type.Id;

      var existingFamily = _target.Entry.MatchResult.MatchedFamily;
      if (existingFamily is not null)
      {
        _existingFamilyId = existingFamily.Id;
        FamilyAlreadyKnown = true;
        FamilyName = existingFamily.Name;

        // Family already exists — nothing to collect on step 1.
        // Skip straight to step 2 (own-form parameters).
        HasStep1 = false;
        Step = 2;
      }
      else
      {
        // Family wasn't resolved by the parser. Pre-fill with the SAME
        // value the parser would compute for "Family" given this
        // component's name — NOT the raw full name. This matters because
        // ComponentMatcher looks up Family by parsed.Family on every
        // import; if we saved something else here (e.g. the full raw
        // name), a later re-import would never find this family again
        // and the component would stay "unresolved" forever.
        // The field stays editable in case the user wants a different
        // grouping name.
        var parsed = _nameParser.Parse(
            $"{_target.Entry.Imported.DetectedCategory} {_target.Entry.Imported.RawName}");

        FamilyAlreadyKnown = false;
        FamilyName = !string.IsNullOrWhiteSpace(parsed.Family)
            ? parsed.Family
            : _target.Entry.Imported.RawName;
      }

      // Form 4 — always Form.Number == "4"
      var form4 = await _db.Forms
          .Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Number == "4", ct);

      Form4Rows.Clear();
      if (form4 is not null)
      {
        // If the family already has some Form-4 values, pre-fill them
        // (read-only intent: user can still edit, but won't lose existing data
        // because the service skips overwriting non-empty existing rows).
        var existingValues = existingFamily is null
            ? []
            : await _db.FamilyNtdValues
                .Where(v => v.FamilyId == existingFamily.Id)
                .ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);

        foreach (var param in form4.Parameters.OrderBy(p => p.RowNumber))
        {
          var row = new WizardParameterRowVm(param);
          if (existingValues.TryGetValue(param.Id, out var existing))
            row.Value = existing;
          Form4Rows.Add(row);
        }
      }

      // Own form options — all forms except Form 4 (Form4 is universal/handled separately)
      var forms = await _db.Forms
          .Where(f => f.Number != "4")
          .OrderBy(f => f.Number)
          .ToListAsync(ct);

      OwnFormOptions.Clear();
      foreach (var f in forms)
        OwnFormOptions.Add(new OwnFormOption(f.Id, f.Number, f.Title));

      // Pre-select if the component type type already suggests a form via family
      if (existingFamily is not null)
      {
        var linkedFormId = await _db.FamilyForms
            .Where(ff => ff.FamilyId == existingFamily.Id)
            .Select(ff => ff.FormId)
            .FirstOrDefaultAsync(ct);

        if (linkedFormId != 0)
          SelectedOwnForm = OwnFormOptions.FirstOrDefault(o => o.Id == linkedFormId);
      }
    }
    finally
    {
      IsLoading = false;
    }
  }

  // ── Step navigation ───────────────────────────────────────────────────────

  public bool CanGoNext => IsStep1
      && (FamilyAlreadyKnown || !string.IsNullOrWhiteSpace(FamilyName));

  [RelayCommand(CanExecute = nameof(CanGoNext))]
  private void GoNext()
  {
    if (!CanGoNext) return;
    Step = 2;
  }

  partial void OnFamilyNameChanged(string value) => GoNextCommand.NotifyCanExecuteChanged();
  partial void OnFamilyAlreadyKnownChanged(bool value) => GoNextCommand.NotifyCanExecuteChanged();
  partial void OnStepChanged(int value)
  {
    GoNextCommand.NotifyCanExecuteChanged();
    SaveCommand.NotifyCanExecuteChanged();
  }

  /// <summary>
  /// False when step 1 was skipped (family already existed) — in that case
  /// "Назад" from step 2 has nowhere useful to go, so the button is hidden.
  /// </summary>
  public bool HasStep1 { get; private set; } = true;

  [RelayCommand]
  private void GoBack()
  {
    if (Step > 1 && HasStep1) Step--;
  }

  partial void OnSelectedOwnFormChanged(OwnFormOption? value)
  {
    _ = LoadOwnFormParametersAsync(value);
    SaveCommand.NotifyCanExecuteChanged();
  }

  partial void OnIsSavingChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

  private async Task LoadOwnFormParametersAsync(OwnFormOption? option)
  {
    OwnFormRows.Clear();
    if (option is null) return;

    var form = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Id == option.Id);

    if (form is null) return;

    foreach (var param in form.Parameters.OrderBy(p => p.RowNumber))
      OwnFormRows.Add(new WizardParameterRowVm(param));
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  public bool CanSave => IsStep2 && SelectedOwnForm is not null && !IsSaving;

  [RelayCommand(CanExecute = nameof(CanSave))]
  private async Task SaveAsync(CancellationToken ct = default)
  {
    if (!CanSave || SelectedOwnForm is null) return;

    IsSaving = true;
    ErrorMessage = null;
    try
    {
      var input = new NewComponentInput
      {
        ComponentTypeId = _componentTypeId,
        ExistingFamilyId = _existingFamilyId,
        NewFamilyName = _existingFamilyId is null ? FamilyName : null,
        FullName = _target.Entry.Imported.RawName,
        Form4Values = Form4Rows.ToDictionary(r => r.FormParameterId, r => r.Value),
        OwnFormId = SelectedOwnForm.Id,
        OwnFormValues = OwnFormRows.ToDictionary(r => r.FormParameterId, r => r.Value),
      };

      var component = await _entryService.CreateComponentAsync(input, ct);

      var newResult = new OperationMaps.Application.Importing.MatchResult
      {
        IsMatched = true,
        MatchedType = await _db.ComponentTypes.FindAsync([_componentTypeId], ct),
        MatchedFamily = component.Family,
        MatchedComponent = component,
        RequiredForms = component.OwnForm is not null ? [component.OwnForm] : [],
        Warning = null,
      };

      _target.ApplyNewMatch(newResult);

      IsCompleted = true;
    }
    catch (Exception ex)
    {
      ErrorMessage = $"Не удалось сохранить: {ex.Message}";
    }
    finally
    {
      IsSaving = false;
    }
  }
}
