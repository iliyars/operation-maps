using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using System.Collections.ObjectModel;

namespace OperationMaps.Wpf.Features.Components;

/// <summary>
/// Represents one row in the NTD parameters table of the detail panel.
/// </summary>
public sealed partial class NtdParameterVm : ObservableObject
{
  public int RowNumber { get; }
  public int FormParameterId { get; }
  public string Name { get; }
  public string? Unit { get; }
  public string Value { get; }
  public string DisplayName => Unit is not null ? $"{Name}, {Unit}" : Name;
  public ObservableCollection<Form4.Form4NoteVm> Notes { get; } = [];

  [ObservableProperty]
  private bool _isAddingNote;

  [ObservableProperty]
  private string _pendingNoteText = "";

  /// <summary>
  /// Callback to parent group for cross-parameter order recalculation.
  /// Set by Form4ViewModel when building NtdValues.
  /// </summary>
  public Action? RecalculateGroupOrders { get; set; }

  public event Action<Form4.Form4NoteVm>? NoteSaved;

  public NtdParameterVm(FamilyNtdValue ntdValue)
  {
    ArgumentNullException.ThrowIfNull(ntdValue);
    RowNumber = ntdValue.FormParameter.RowNumber;
    FormParameterId = ntdValue.FormParameter.Id;
    Name = ntdValue.FormParameter.Name;
    Unit = ntdValue.FormParameter.Unit;
    Value = ntdValue.Value;
  }

  /// <summary>
  /// Builds a row directly from a FormParameter and an already-resolved
  /// display value (e.g. "—" for a parameter that has no FamilyNtdValue yet).
  /// Used by ProjectComponentVm.LoadNtdValuesAsync to show every Form-4
  /// parameter, not just the ones that happen to have a value in the DB.
  /// </summary>
  public NtdParameterVm(FormParameter parameter, string value)
  {
    ArgumentNullException.ThrowIfNull(parameter);
    RowNumber = parameter.RowNumber;
    FormParameterId = parameter.Id;
    Name = parameter.Name;
    Unit = parameter.Unit;
    Value = value;
  }

  [RelayCommand]
  private void StartAddNote()
  {
    PendingNoteText = "";
    IsAddingNote = true;
  }

  [RelayCommand]
  private void CancelAddNote()
  {
    PendingNoteText = "";
    IsAddingNote = false;
  }

  [RelayCommand]
  private void ConfirmAddNote()
  {
    if (string.IsNullOrWhiteSpace(PendingNoteText)) return;
    var note = new Form4.Form4NoteVm
    {
      FormParameterId = FormParameterId,
      NoteText = PendingNoteText,
      Order = 1,
    };
    note.DeleteRequested += OnNoteDeleteRequested;
    Notes.Add(note);
    PendingNoteText = "";
    IsAddingNote = false;
    RecalculateGroupOrders?.Invoke();
    NoteSaved?.Invoke(note);
  }

  private void OnNoteDeleteRequested(Form4.Form4NoteVm note)
  {
    Notes.Remove(note);
    RecalculateGroupOrders?.Invoke();
  }
}
