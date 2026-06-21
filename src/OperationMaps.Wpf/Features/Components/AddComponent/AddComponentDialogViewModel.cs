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
/// Mirrors OwnForm's ParameterDetailVm feature set (pins, optional row)
/// so the same Form 64 layout works in both places.
/// </summary>
public sealed partial class WizardParameterRowVm : ObservableObject
{
  public int FormParameterId { get; }
  public int RowNumber { get; }
  public string DisplayName { get; }

  [ObservableProperty] private string _value = "";

  // ── Pins (Form 64 only) ───────────────────────────────────────────────────

  /// <summary>Whether the own-form being filled has a "номера выводов" column.</summary>
  public bool ShowPins { get; init; }

  [ObservableProperty] private string _pinsValue = "";

  // ── Optional second-value row (e.g. Form 64's second supply voltage) ─────

  /// <summary>True for the ONE parameter that has an optional counterpart (e.g. RowNumber=1 in Form 64).</summary>
  public bool CanHaveOptionalRow { get; init; }

  /// <summary>FormParameterId of the optional counterpart, when <see cref="CanHaveOptionalRow"/> is true.</summary>
  public int OptionalFormParameterId { get; init; }

  [ObservableProperty] private bool _isOptionalRowVisible;
  [ObservableProperty] private string _optionalValue = "";
  [ObservableProperty] private string _optionalPinsValue = "";

  [RelayCommand]
  private void AddOptionalRow() => IsOptionalRowVisible = true;

  [RelayCommand]
  private void RemoveOptionalRow()
  {
    IsOptionalRowVisible = false;
    OptionalValue = "";
    OptionalPinsValue = "";
  }

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
/// and fill its parameters — including pin numbers and an optional
/// second-value row when the chosen form supports them (Form 64).
/// On confirm, creates Family (if needed) + Component via
/// <see cref="IComponentEntryService"/>, then updates the originating
/// <see cref="ProjectComponentVm"/> in place.
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

  /// <summary>False when step 1 was skipped (family already existed) — "Назад" is hidden in that case.</summary>
  public bool HasStep1 { get; private set; } = true;

  // ── Header info (read-only, shown on both steps) ───────────────────────────

  public string ComponentRawName => _target.Entry.Imported.RawName;
  public string ComponentPositions => _target.Positions;
  public string ComponentTypeName => _target.ComponentTypeName;

  // ── Step 1: Family ────────────────────────────────────────────────────────

  [ObservableProperty] private bool _familyAlreadyKnown;
  [ObservableProperty] private string _familyName = "";

  public ObservableCollection<WizardParameterRowVm> Form4Rows { get; } = [];

  // ── Step 2: Own form ──────────────────────────────────────────────────────

  public ObservableCollection<OwnFormOption> OwnFormOptions { get; } = [];

  [ObservableProperty] private OwnFormOption? _selectedOwnForm;

  /// <summary>Whether the currently selected own form has a "номера выводов" column (Form 64).</summary>
  [ObservableProperty] private bool _ownFormShowsPins;

  public ObservableCollection<WizardParameterRowVm> OwnFormRows { get; } = [];

  // ── Result ────────────────────────────────────────────────────────────────

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
        // component's name — not the raw full name — so a later
        // re-import can find this family again (ComponentMatcher
        // looks up Family by parsed.Family).
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
        var existingValues = existingFamily is null
            ? []
            : await _db.FamilyNtdValues
                .Where(v => v.FamilyId == existingFamily.Id)
                .ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);

        foreach (var param in form4.Parameters.Where(p => !p.IsOptional).OrderBy(p => p.RowNumber))
        {
          var row = new WizardParameterRowVm(param);
          if (existingValues.TryGetValue(param.Id, out var existing))
            row.Value = existing;
          Form4Rows.Add(row);
        }
      }

      // Own form options — all forms except Form 4
      var forms = await _db.Forms
          .Where(f => f.Number != "4")
          .OrderBy(f => f.Number)
          .ToListAsync(ct);

      OwnFormOptions.Clear();
      foreach (var f in forms)
        OwnFormOptions.Add(new OwnFormOption(f.Id, f.Number, f.Title));

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

  /// <summary>
  /// Loads parameters for the selected own form, wiring up pin-number
  /// support and the optional second-value row exactly like OwnFormView:
  /// the parameter that has an IsOptional counterpart gets
  /// CanHaveOptionalRow=true and a reference to that counterpart's id;
  /// the counterpart parameter itself is excluded from the visible list
  /// (it only ever renders via the primary row's "+ Добавить" affordance).
  /// </summary>
  private async Task LoadOwnFormParametersAsync(OwnFormOption? option)
  {
    OwnFormRows.Clear();
    OwnFormShowsPins = false;
    if (option is null) return;

    var form = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Id == option.Id);

    if (form is null) return;

    // Pin numbers are currently unique to Form 64.
    OwnFormShowsPins = form.Number == "64";

    var allParams = form.Parameters.OrderBy(p => p.RowNumber).ToList();

    foreach (var param in allParams.Where(p => !p.IsOptional))
    {
      var counterpart = allParams.FirstOrDefault(
          p => p.IsOptional && p.OptionalForRowNumber == param.RowNumber);

      OwnFormRows.Add(new WizardParameterRowVm(param)
      {
        ShowPins = OwnFormShowsPins,
        CanHaveOptionalRow = counterpart is not null,
        OptionalFormParameterId = counterpart?.Id ?? 0,
      });
    }
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
      // Optional-row values (e.g. second supply voltage) go into the
      // SAME OwnFormValues dictionary, keyed by the OPTIONAL parameter's
      // id — ComponentEntryService stores them as regular
      // ComponentNtdValue rows; the export service is what treats them
      // specially via map.json's "optionalRows" lookup.
      var ownFormValues = OwnFormRows.ToDictionary(r => r.FormParameterId, r => r.Value);
      var pinValues = new Dictionary<int, string>();

      foreach (var row in OwnFormRows)
      {
        if (row.ShowPins && !string.IsNullOrWhiteSpace(row.PinsValue))
          pinValues[row.FormParameterId] = row.PinsValue;

        if (row.CanHaveOptionalRow && row.IsOptionalRowVisible)
        {
          if (!string.IsNullOrWhiteSpace(row.OptionalValue))
            ownFormValues[row.OptionalFormParameterId] = row.OptionalValue;

          if (row.ShowPins && !string.IsNullOrWhiteSpace(row.OptionalPinsValue))
            pinValues[row.OptionalFormParameterId] = row.OptionalPinsValue;
        }
      }

      var input = new NewComponentInput
      {
        ComponentTypeId = _componentTypeId,
        ExistingFamilyId = _existingFamilyId,
        NewFamilyName = _existingFamilyId is null ? FamilyName : null,
        FullName = _target.Entry.Imported.RawName,
        Form4Values = Form4Rows.ToDictionary(r => r.FormParameterId, r => r.Value),
        OwnFormId = SelectedOwnForm.Id,
        OwnFormValues = ownFormValues,
        PinValues = pinValues,
      };

      var component = await _entryService.CreateComponentAsync(input, ct);

      var newResult = new MatchResult
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
