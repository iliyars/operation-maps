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
  [ObservableProperty] private string _value = "";
  [ObservableProperty] private string _regimeCondition = "";
  public string DefaultValue { get; private set; } = "";
  public string DisplayName => Unit is not null ? $"{Name}, {Unit}" : Name;
  public ObservableCollection<Form4.Form4NoteVm> Notes { get; } = [];
  public ObservableCollection<CatalogNoteChoice> AvailableNotes { get; } = [];
  [ObservableProperty] private string _selectionError = "";
  public string ValueHint => (string.IsNullOrWhiteSpace(DefaultValue) || DefaultValue is "—" or "-")
    && AvailableNotes.Any(n => OperationMaps.Application.Models.ReliefNote.IsMatch(n.Text))
    ? "Обычное значение не задано. Значение облегчённого режима доступно в примечаниях ниже." : "";
  public void AddReliefOption(CatalogNoteChoice option, string noteText)
  {
    foreach (var choice in AvailableNotes.Where(n => n.Text == noteText).ToArray()) AvailableNotes.Remove(choice);
    foreach (var note in Notes.Where(n => n.NoteText == noteText).ToArray()) Notes.Remove(note);
    AvailableNotes.Add(option);
    OnPropertyChanged(nameof(ValueHint));
  }
  public IEnumerable<Form4.Form4NoteVm> CustomNotes => Notes.Where(n => !AvailableNotes.Any(c => c.Text == n.NoteText));
  public void RestoreCatalogValue(string value)
  {
    if (!string.IsNullOrWhiteSpace(DefaultValue) && DefaultValue is not "—" and not "-") return;
    DefaultValue = value;
    Value = value;
  }

  public void AddCatalogNote(string text)
  {
    if (AvailableNotes.Any(n => n.Text == text)) return;
    var choice = new CatalogNoteChoice(text, () => Notes.Any(n => n.NoteText == text), selected =>
    {
      if (selected) AddNote(text);
      else foreach (var note in Notes.Where(n => n.NoteText == text).ToArray()) Notes.Remove(note);
      RecalculateGroupOrders?.Invoke();
    });
    Notes.CollectionChanged += (_, _) => choice.Refresh();
    AvailableNotes.Add(choice);
    OnPropertyChanged(nameof(CustomNotes));
    AddNote(text);
  }

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
    Notes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CustomNotes));
    ArgumentNullException.ThrowIfNull(ntdValue);
    RowNumber = ntdValue.FormParameter.RowNumber;
    FormParameterId = ntdValue.FormParameter.Id;
    Name = ntdValue.FormParameter.Name;
    Unit = ntdValue.FormParameter.Unit;
    Value = ntdValue.Value;
    DefaultValue = Value;
  }

  /// <summary>
  /// Builds a row directly from a FormParameter and an already-resolved
  /// display value (e.g. "—" for a parameter that has no FamilyNtdValue yet).
  /// Used by ProjectComponentVm.LoadNtdValuesAsync to show every Form-4
  /// parameter, not just the ones that happen to have a value in the DB.
  /// </summary>
  public NtdParameterVm(FormParameter parameter, string value)
  {
    Notes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CustomNotes));
    ArgumentNullException.ThrowIfNull(parameter);
    RowNumber = parameter.RowNumber;
    FormParameterId = parameter.Id;
    Name = parameter.Name;
    Unit = parameter.Unit;
    Value = value;
    DefaultValue = value;
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
    var note = AddNote(PendingNoteText);
    PendingNoteText = "";
    IsAddingNote = false;
    NoteSaved?.Invoke(note);
  }

  /// <summary>Appends a note to this row (also used when restoring a saved project).</summary>
  public Form4.Form4NoteVm AddNote(string text)
  {
    var note = new Form4.Form4NoteVm
    {
      FormParameterId = FormParameterId,
      NoteText = text,
      Order = Notes.Count + 1,
    };
    note.DeleteRequested += OnNoteDeleteRequested;
    Notes.Add(note);
    RecalculateGroupOrders?.Invoke();
    return note;
  }

  private void OnNoteDeleteRequested(Form4.Form4NoteVm note)
  {
    Notes.Remove(note);
    RecalculateGroupOrders?.Invoke();
  }
}
