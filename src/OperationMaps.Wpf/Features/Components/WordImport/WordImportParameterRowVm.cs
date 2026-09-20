using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Wpf.Features.Components.WordImport;

/// <summary>
/// One editable "по НТД" parameter row in the detail panel, for the
/// currently selected <see cref="WordImportRowVm"/>. Edits write straight
/// back into the underlying <see cref="WordImportedComponent"/>'s
/// row-keyed dictionaries, so importing always uses whatever is currently
/// shown here — no separate "apply edits" step.
/// </summary>
public sealed partial class WordImportParameterRowVm : ObservableObject
{
  private readonly WordImportedComponent _model;

  public int RowNumber { get; }
  public string DisplayName { get; }

  /// <summary>Whether this own-form has a "номера выводов" column (Form 64).</summary>
  public bool ShowPins { get; }

  [ObservableProperty] private string _value = "";
  [ObservableProperty] private string _pinsValue = "";

  public WordImportParameterRowVm(FormParameter parameter, WordImportedComponent model, bool showPins)
  {
    _model = model;
    RowNumber = parameter.RowNumber;
    DisplayName = parameter.Unit is { Length: > 0 }
        ? $"{parameter.Name}, {parameter.Unit}"
        : parameter.Name;
    ShowPins = showPins;

    // Direct field assignment (not the property setter) so the
    // OnXChanged handlers below don't immediately write the same value
    // straight back into the model it just came from.
    model.OwnFormNtdValuesByRow.TryGetValue(RowNumber, out var value);
    _value = value ?? "";

    if (showPins)
    {
      model.OwnFormPinValuesByRow.TryGetValue(RowNumber, out var pins);
      _pinsValue = pins ?? "";
    }
  }

  partial void OnValueChanged(string value) => _model.OwnFormNtdValuesByRow[RowNumber] = value;
  partial void OnPinsValueChanged(string value) => _model.OwnFormPinValuesByRow[RowNumber] = value;
}

/// <summary>Read-only Form-4 (family-level) value shown alongside the editable own-form parameters, for context.</summary>
public sealed record Form4SummaryRowVm(string DisplayName, string Value);
