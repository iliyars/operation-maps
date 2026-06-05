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
    public ComponentMatchEntry Entry { get; }

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
    /// Lazily loads NTD values from the catalog DB.
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
        var values = await db.FamilyNtdValues
            .Include(v => v.FormParameter)
            .Where(v => v.FamilyId == familyId)
            .OrderBy(v => v.FormParameter.RowNumber)
            .ToListAsync(ct);

        NtdValues = values
            .Select(v => new NtdParameterVm(v))
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


  }
}
