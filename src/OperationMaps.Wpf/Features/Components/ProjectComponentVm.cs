using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Importing;

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ProjectComponentVm : ObservableObject
  {
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Unique identifier within the current project session.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>The underlying match data from the import pipeline.</summary>
    public ComponentMatchEntry Entry { get; }

    // ── Display properties ────────────────────────────────────────────────────

    /// <summary>Comma-separated position designators, e.g. "R1, R2, R3".</summary>
    public string Positions => string.Join(", ", Entry.Imported.Positions);

    /// <summary>Component name — from catalog if matched, raw name otherwise.</summary>
    public string Name => Entry.MatchResult.MatchedComponent?.FullName
                       ?? Entry.Imported.RawName;

    public string? TypeName => Entry.MatchResult.MatchedType?.Name;
    public string? FamilyName => Entry.MatchResult.MatchedFamily?.Name;
    public bool IsMatched => Entry.MatchResult.IsMatched;

    // ── UI state ──────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isSelected;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ProjectComponentVm(ComponentMatchEntry entry)
    {
      Entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    // ── Mutation (used by undo/redo commands) ─────────────────────────────────

    /// <summary>
    /// Replaces the position list. Called by Split/Merge commands.
    /// Raises property-changed for <see cref="Positions"/>.
    /// </summary>
    public void SetPositions(IReadOnlyList<string> positions)
    {
      Entry.Imported.Positions = positions.ToList();
      OnPropertyChanged(nameof(Positions));
    }
  }
}
