using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Word;
using OperationMaps.Application.Models;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ProjectComponentVm : ObservableObject
  {
    // ── Identity ──────────────────────────────────────────────────────────────

    public Guid Id { get; } = Guid.NewGuid();
    public ComponentMatchEntry Entry { get; private set; }

    // ── Display properties ────────────────────────────────────────────────────

    public string Positions => string.Join(", ", Entry.Imported.Positions);
    public string Name => Entry.Imported.RawName;
    public string? TypeName => Entry.Imported.DetectedCategory;
    public string? FamilyName => Entry.MatchResult.MatchedFamily?.Name;
    public string Classification => IsBoard ? "Плата · Форма 3 · без каталога"
      : $"{TypeName} · {FamilyName ?? "семейство не определено"}";

    /// <summary>
    /// Тип компонента для Формы 4: "Конденсатор", "Резистор" и т.д.
    /// Всегда берётся из XML (DetectedCategory) — первое слово наименования.
    /// </summary>
    public string ComponentTypeName => Entry.Imported.DetectedCategory;

    public bool IsBoard => Form3DataBuilder.IsBoard(Entry.Imported);
    public ComponentRegimeSelection? ActiveRegime { get; private set; }
    private IReadOnlyList<ComponentRegimeSelection> _reliefVariants = [];
    public string RegimeName => ActiveRegime?.Name ?? "Основной";

    public void SelectRegime(ComponentRegimeSelection? regime)
    {
      if (Equals(ActiveRegime, regime)) return;
      ActiveRegime = regime;
      ApplyRegimeValues();
      OnPropertyChanged(nameof(ActiveRegime));
      OnPropertyChanged(nameof(RegimeName));
      Edited?.Invoke();
    }

    private void ApplyRegimeValues()
    {
      foreach (var row in NtdValues)
      {
        var value = ActiveRegime?.Values.FirstOrDefault(v => v.FormNumber == "4" && v.RowNumber == row.RowNumber);
        row.Value = value?.Value ?? row.DefaultValue;
        row.RegimeCondition = value is null ? "" : value.Condition ?? ActiveRegime!.Condition;
      }
    }

    public bool ApplyAllReliefNotes()
    {
      var available = _reliefVariants.Where(v => v.Values.Any(p => p.FormNumber == "4")).ToArray();
      if (available.Length == 0) return false;
      if (available.All(ReliefSelected)) return true;
      var selected = ActiveRegime is { } active
        ? (active.Conditions.Count > 0 ? active.Conditions : new[] { active }).ToList() : [];
      foreach (var variant in available)
        if (!selected.Any(c => c.Name == variant.Name && c.Condition == variant.Condition)) selected.Add(variant);
      // Compose first: conflicting alternatives must not partially change the component.
      var combined = ComponentRegimeSelection.Combine(selected);
      SelectRegime(combined);
      foreach (var row in NtdValues)
      {
        row.SelectionError = "";
        foreach (var note in row.AvailableNotes) note.Refresh();
      }
      return true;
    }

    public ComponentMatchStatus MatchStatus => IsBoard ? ComponentMatchStatus.Automatic : Entry.MatchResult switch
    {
      { MatchedComponent: not null } => ComponentMatchStatus.Matched,
      { MatchedFamily: not null } => ComponentMatchStatus.FamilyFound,
      _ => ComponentMatchStatus.Unresolved,
    };

    public bool IsMatched => IsBoard || MatchStatus == ComponentMatchStatus.Matched;

    public bool HasFamily => !IsBoard && MatchStatus != ComponentMatchStatus.Unresolved;

    // ── UI state ──────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isSelected;

    // ── NTD values (lazy) ─────────────────────────────────────────────────────

    [ObservableProperty] private IReadOnlyList<NtdParameterVm> _ntdValues = [];
    [ObservableProperty] private bool _isLoadingNtd;
    [ObservableProperty] private bool _ntdLoaded;
    private readonly SemaphoreSlim _ntdLoading = new(1, 1);

    /// <summary>
    /// Raised when the user adds/removes a Form 4 note on one of this
    /// component's NTD rows — drives the project's "unsaved changes" flag.
    /// </summary>
    public event Action? Edited;

    partial void OnNtdValuesChanged(IReadOnlyList<NtdParameterVm> value)
    {
      foreach (var row in value)
        row.Notes.CollectionChanged += (_, _) => Edited?.Invoke();
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public ProjectComponentVm(ComponentMatchEntry entry)
    {
      Entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    // ── NTD loading ───────────────────────────────────────────────────────────

    /// <summary>
    /// Lazily loads NTD values from the catalog DB for the Form-4 view:
    /// shows EVERY Form-4 parameter (not just the ones that have a value),
    /// filling missing ones with an em dash so the user can see at a glance
    /// what's still unfilled for this family.
    /// Safe to call multiple times — loads only once.
    /// </summary>
    public async Task LoadNtdValuesAsync(
        CatalogDbContext db,
        CancellationToken ct = default)
    {
      await _ntdLoading.WaitAsync(ct);
      try { await LoadNtdCoreAsync(db, ct); }
      finally { _ntdLoading.Release(); }
    }

    private async Task LoadNtdCoreAsync(CatalogDbContext db, CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      if (NtdLoaded || IsBoard) return;
      var entry = Entry;

      var familyId = entry.MatchResult.MatchedFamily?.Id;

      IsLoadingNtd = true;

      try
      {
        // All Form-4 parameters, in display order — this is the full list
        // we want to show, regardless of whether a value exists yet.
        var form4Parameters = await db.Forms
            .Where(f => f.Number == "4")
            .SelectMany(f => f.Parameters)
            .OrderBy(p => p.RowNumber)
            .ToListAsync(ct);

        // Existing values for this family — sparse, may be missing rows.
        var existingValues = await db.FamilyNtdValues
            .Where(v => v.FamilyId == familyId)
            .ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);

        var rows = form4Parameters
            .Select(p => new NtdParameterVm(
                p,
                existingValues.TryGetValue(p.Id, out var value) ? value : "—"))
            .ToList();

        var catalogNotes = await db.FamilyNotes.Include(n => n.Note)
          .Where(n => n.FamilyId == familyId && n.FormParameter.Form.Number == "4")
          .OrderBy(n => n.NoteId).ToListAsync(ct);
        foreach (var note in catalogNotes)
          rows.FirstOrDefault(r => r.FormParameterId == note.FormParameterId)?.AddCatalogNote(note.Note.Text);
        var componentId = entry.MatchResult.MatchedComponent?.Id;
        var storedVariants = await db.ComponentRegimes.AsNoTracking()
          .Include(r => r.Values).ThenInclude(v => v.FormParameter).ThenInclude(p => p.Form)
          .Where(r => r.ComponentId == componentId).ToListAsync(ct);
        foreach (var ordinary in storedVariants.Where(r => !ReliefNote.IsMatch(r.Condition)))
          foreach (var value in ordinary.Values.Where(v => v.FormParameter.Form.Number == "4"))
          {
            var row = rows.FirstOrDefault(r => r.FormParameterId == value.FormParameterId);
            if (row is null) continue;
            row.RestoreCatalogValue(value.Value);
            row.AddCatalogNote(ordinary.Condition);
          }
        var variants = storedVariants.Where(r => ReliefNote.IsMatch(r.Condition))
          .Select(r => new ComponentRegimeSelection(r.Name, r.Condition,
            r.Values.Select(v => new RegimeParameterValue(v.FormParameter.Form.Number, v.FormParameter.RowNumber, v.Value)).ToArray())).ToList();
        var savedVariants = ActiveRegime is { } saved
          ? saved.Conditions.Count > 0 ? saved.Conditions : new[] { saved } : [];
        foreach (var savedVariant in savedVariants.Where(v => ReliefNote.IsMatch(v.Condition)))
        {
          variants.RemoveAll(v => v.Name == savedVariant.Name && v.Condition == savedVariant.Condition);
          variants.Add(savedVariant);
        }
        foreach (var variant in variants)
          foreach (var value in variant.Values.Where(v => v.FormNumber == "4"))
          {
            var row = rows.FirstOrDefault(r => r.RowNumber == value.RowNumber);
            if (row is null) continue;
            var option = new CatalogNoteChoice($"{variant.Condition} — {value.Value}",
              () => ReliefSelected(variant), selected =>
              {
                try
                {
                  var choices = ActiveRegime is { } active
                    ? (active.Conditions.Count > 0 ? active.Conditions : new[] { active }).ToList() : [];
                  choices.RemoveAll(c => c.Name == variant.Name && c.Condition == variant.Condition);
                  if (selected) choices.Add(variant);
                  SelectRegime(ComponentRegimeSelection.Combine(choices));
                  row.SelectionError = "";
                }
                catch (InvalidOperationException)
                { row.SelectionError = "Сначала снимите другое примечание, задающее иное значение этого параметра."; }
                foreach (var parameter in NtdValues)
                  foreach (var choice in parameter.AvailableNotes) choice.Refresh();
              });
            row.AddReliefOption(option, variant.Condition);
          }
        ct.ThrowIfCancellationRequested();
        if (!ReferenceEquals(entry, Entry)) return;
        _reliefVariants = variants;
        NtdValues = rows;
        ApplyRegimeValues();

        NtdLoaded = true;
      }
      finally
      {
        IsLoadingNtd = false;
      }
    }

    private bool ReliefSelected(ComponentRegimeSelection variant)
      => ActiveRegime is { } active && (active.Conditions.Count > 0 ? active.Conditions : new[] { active })
        .Any(c => c.Name == variant.Name && c.Condition == variant.Condition);

    // ── Mutation (used by undo/redo commands) ─────────────────────────────────

    public void SetPositions(IReadOnlyList<string> positions)
    {
      Entry.Imported.Positions = positions.ToList();
      OnPropertyChanged(nameof(Positions));
    }

    public ProjectComponentVm CloneWithPositions(IReadOnlyList<string> positions)
    {
      var clonedImported = new ImportedComponent
      {
        ImportIndex = Entry.Imported.ImportIndex,
        RawName = Entry.Imported.RawName,
        DetectedCategory = Entry.Imported.DetectedCategory,
        Positions = positions.ToList(),
        RawPositions = string.Join(", ", positions),
      };

      var clonedEntry = new ComponentMatchEntry
      {
        Imported = clonedImported,
        MatchResult = Entry.MatchResult,
      };

      var clone = new ProjectComponentVm(clonedEntry);
      clone.SelectRegime(ActiveRegime);
      return clone;
    }

    // ── Re-matching after manual component creation ───────────────────────────

    /// <summary>
    /// Replaces <see cref="Entry"/>'s MatchResult with a freshly built one
    /// (e.g. after the user filled in NTD values for a previously unresolved
    /// component via the "add component" wizard). Updates the in-memory
    /// project state directly so the row moves from "Unresolved" to
    /// "Matched" immediately, without restarting the app.
    /// Resets the lazily-loaded NTD cache so it's reloaded for the new family.
    /// </summary>
    public void ApplyNewMatch(MatchResult newResult)
    {
      ArgumentNullException.ThrowIfNull(newResult);

      Entry = new ComponentMatchEntry
      {
        Imported = Entry.Imported,
        MatchResult = newResult,
      };

      // Force NTD values to reload from DB for the (possibly new) family.
      NtdLoaded = false;
      NtdValues = [];

      OnPropertyChanged(nameof(Name));
      OnPropertyChanged(nameof(FamilyName));
      OnPropertyChanged(nameof(MatchStatus));
      OnPropertyChanged(nameof(IsMatched));
      OnPropertyChanged(nameof(Classification));
      OnPropertyChanged(nameof(HasFamily));
    }
  }
}
