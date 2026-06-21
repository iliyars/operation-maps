using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
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
    public string Name => Entry.MatchResult.MatchedComponent?.FullName
                              ?? Entry.Imported.RawName;
    public string? TypeName => Entry.Imported.DetectedCategory;
    public string? FamilyName => Entry.MatchResult.MatchedFamily?.Name;

    /// <summary>
    /// Тип компонента для Формы 4: "Конденсатор", "Резистор" и т.д.
    /// Всегда берётся из XML (DetectedCategory) — первое слово наименования.
    /// </summary>
    public string ComponentTypeName => Entry.Imported.DetectedCategory;

    public ComponentMatchStatus MatchStatus => Entry.MatchResult switch
    {
      { MatchedComponent: not null } => ComponentMatchStatus.Matched,
      { MatchedFamily: not null } => ComponentMatchStatus.FamilyFound,
      _ => ComponentMatchStatus.Unresolved,
    };

    public bool IsMatched => MatchStatus == ComponentMatchStatus.Matched;

    public bool HasFamily => MatchStatus != ComponentMatchStatus.Unresolved;

    // ── UI state ──────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isSelected;

    // ── NTD values (lazy) ─────────────────────────────────────────────────────

    [ObservableProperty] private IReadOnlyList<NtdParameterVm> _ntdValues = [];
    [ObservableProperty] private bool _isLoadingNtd;
    [ObservableProperty] private bool _ntdLoaded;

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
      if (NtdLoaded || IsLoadingNtd) return;

      var familyId = Entry.MatchResult.MatchedFamily?.Id;
      if (familyId is null)
      {
        NtdLoaded = true;
        return;
      }

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

        NtdValues = form4Parameters
            .Select(p => new NtdParameterVm(
                p,
                existingValues.TryGetValue(p.Id, out var value) ? value : "—"))
            .ToList();

        NtdLoaded = true;
      }
      finally
      {
        IsLoadingNtd = false;
      }
    }

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

      return new ProjectComponentVm(clonedEntry);
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
      OnPropertyChanged(nameof(HasFamily));
    }
  }
}
