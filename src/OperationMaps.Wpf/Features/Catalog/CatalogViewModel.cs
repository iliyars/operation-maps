using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Catalog;

/// <summary>
/// "Справочник ЭРИ" — read-only browser of the catalog database, available
/// to every user: types → families → components. A family shows its Form 4
/// ("по НТД") values; a component shows the values of its own form
/// (Form 67, 68, ...). Editing is reserved for the admin mode (later).
/// </summary>
public sealed partial class CatalogViewModel : ScreenViewModelBase, INavigatedTo
{
  private const string NoValue = "—";

  private readonly CatalogDbContext _db;

  public IReadOnlyList<CatalogTypeNode> Types { get; private set; } = [];

  [ObservableProperty] private string _searchText = "";
  [ObservableProperty] private bool _isLoading;

  [ObservableProperty] private CatalogFamilyNode? _selectedFamily;
  [ObservableProperty] private CatalogComponentItem? _selectedComponent;
  [ObservableProperty] private IReadOnlyList<CatalogComponentItem> _visibleComponents = [];
  [ObservableProperty] private CatalogCard? _card;

  public int FamilyCount => Types.Sum(t => t.AllFamilies.Count);
  public int ComponentCount => Types.Sum(t => t.AllFamilies.Sum(f => f.Components.Count));

  public CatalogViewModel(CatalogDbContext db)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
  {
    IsLoading = true;
    try
    {
      await LoadTreeAsync(cancellationToken);
    }
    finally
    {
      IsLoading = false;
    }
  }

  // ── Tree ──────────────────────────────────────────────────────────────────

  private async Task LoadTreeAsync(CancellationToken ct)
  {
    // Fresh read on every visit — the catalog changes while the app runs
    // (the "add component" wizard, Word import).
    var types = await _db.ComponentTypes
        .AsNoTracking()
        .Include(t => t.Families).ThenInclude(f => f.Components).ThenInclude(c => c.OwnForm)
        .OrderBy(t => t.Name)
        .ToListAsync(ct);

    Types = types
        .Where(t => t.Families.Count > 0)
        .Select(t => new CatalogTypeNode
        {
          Name = t.Name,
          AllFamilies = t.Families
              .OrderBy(f => f.Name)
              .Select(f => new CatalogFamilyNode
              {
                Id = f.Id,
                Name = f.Name,
                Components = f.Components
                    .OrderBy(c => c.FullName)
                    .Select(c => new CatalogComponentItem
                    {
                      Id = c.Id,
                      FullName = c.FullName,
                      NeedsReview = c.NeedsAdminReview,
                      OwnFormNumber = c.OwnForm?.Number,
                    })
                    .ToList(),
              })
              .ToList(),
        })
        .ToList();

    OnPropertyChanged(nameof(Types));
    OnPropertyChanged(nameof(FamilyCount));
    OnPropertyChanged(nameof(ComponentCount));
    ApplySearch();
  }

  partial void OnSearchTextChanged(string value) => ApplySearch();

  private void ApplySearch()
  {
    var search = SearchText.Trim();
    foreach (var type in Types)
    {
      type.ApplyFilter(search);
      // While searching, open every type that has hits; otherwise keep the user's choice.
      if (search.Length > 0)
        type.IsExpanded = type.Families.Count > 0;
    }
    UpdateVisibleComponents();
  }

  private void UpdateVisibleComponents()
  {
    var search = SearchText.Trim();
    if (SelectedFamily is null)
      VisibleComponents = [];
    else if (search.Length == 0 || SelectedFamily.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
      VisibleComponents = SelectedFamily.Components;
    else
      VisibleComponents = SelectedFamily.Components.Where(c => c.Matches(search)).ToList();
  }

  [RelayCommand]
  private void ToggleType(CatalogTypeNode type) => type.IsExpanded = !type.IsExpanded;

  // ── Selection ─────────────────────────────────────────────────────────────

  [RelayCommand]
  private async Task SelectFamilyAsync(CatalogFamilyNode family, CancellationToken ct)
  {
    if (SelectedFamily is not null) SelectedFamily.IsSelected = false;
    SelectComponentItem(null);

    SelectedFamily = family;
    family.IsSelected = true;
    UpdateVisibleComponents();

    Card = await BuildFamilyCardAsync(family, ct);
  }

  [RelayCommand]
  private async Task SelectComponentAsync(CatalogComponentItem component, CancellationToken ct)
  {
    SelectComponentItem(component);
    Card = await BuildComponentCardAsync(component, ct);
  }

  private void SelectComponentItem(CatalogComponentItem? component)
  {
    if (SelectedComponent is not null) SelectedComponent.IsSelected = false;
    SelectedComponent = component;
    if (component is not null) component.IsSelected = true;
  }

  // ── Cards ─────────────────────────────────────────────────────────────────

  private async Task<CatalogCard> BuildFamilyCardAsync(CatalogFamilyNode family, CancellationToken ct)
  {
    var form4Params = await _db.FormParameters
        .AsNoTracking()
        .Where(p => p.Form.Number == "4")
        .OrderBy(p => p.RowNumber)
        .ToListAsync(ct);

    var values = await _db.FamilyNtdValues
        .AsNoTracking()
        .Where(v => v.FamilyId == family.Id)
        .ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);

    var notes = await _db.FamilyNotes
        .AsNoTracking()
        .Where(n => n.FamilyId == family.Id)
        .Select(n => new { n.FormParameter.RowNumber, n.FormParameter.Name, n.Note.Text })
        .ToListAsync(ct);

    var forms = await _db.FamilyForms
        .AsNoTracking()
        .Where(ff => ff.FamilyId == family.Id)
        .Select(ff => ff.Form.Number)
        .ToListAsync(ct);

    var subtitle = $"Семейство · компонентов: {family.Components.Count}";
    if (forms.Count > 0)
      subtitle += " · формы: " + string.Join(", ", forms.OrderBy(FormNumberOrder));

    return new CatalogCard
    {
      Title = family.Name,
      Subtitle = subtitle,
      ParametersHeader = "ФОРМА 4 — ПО НТД",
      Parameters = form4Params
          .Select(p => new CatalogParamRow
          {
            RowNumber = p.RowNumber,
            Name = DisplayName(p),
            Value = values.TryGetValue(p.Id, out var v) && !string.IsNullOrWhiteSpace(v) ? v : NoValue,
          })
          .ToList(),
      Notes = notes
          .OrderBy(n => n.RowNumber)
          .Select(n => new CatalogNoteRow { Parameter = $"{n.RowNumber}. {n.Name}", Text = n.Text })
          .ToList(),
    };
  }

  private async Task<CatalogCard> BuildComponentCardAsync(CatalogComponentItem component, CancellationToken ct)
  {
    var entity = await _db.Components
        .AsNoTracking()
        .Include(c => c.Family)
        .Include(c => c.OwnForm)
        .FirstOrDefaultAsync(c => c.Id == component.Id, ct);

    if (entity is null)
      return new CatalogCard
      {
        Title = component.FullName,
        Subtitle = "",
        ParametersHeader = "",
        EmptyMessage = "Компонент удалён из справочника.",
      };

    var subtitle = $"Семейство: {entity.Family.Name}";

    if (entity.OwnForm is null)
      return new CatalogCard
      {
        Title = entity.FullName,
        Subtitle = subtitle,
        NeedsReview = entity.NeedsAdminReview,
        ParametersHeader = "",
        EmptyMessage = "Собственная форма для компонента не задана.",
      };

    var formParams = await _db.FormParameters
        .AsNoTracking()
        .Where(p => p.FormId == entity.OwnForm.Id)
        .OrderBy(p => p.RowNumber)
        .ToListAsync(ct);

    var values = await _db.ComponentNtdValues
        .AsNoTracking()
        .Where(v => v.ComponentId == entity.Id)
        .ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);

    var pins = await _db.ComponentPinValues
        .AsNoTracking()
        .Where(v => v.ComponentId == entity.Id)
        .ToDictionaryAsync(v => v.FormParameterId, v => v.Pins, ct);

    var notes = await _db.ComponentNotes
        .AsNoTracking()
        .Where(n => n.ComponentId == entity.Id)
        .Select(n => new { n.FormParameter.RowNumber, n.FormParameter.Name, n.Note.Text })
        .ToListAsync(ct);

    return new CatalogCard
    {
      Title = entity.FullName,
      Subtitle = $"{subtitle} · форма {entity.OwnForm.Number}",
      NeedsReview = entity.NeedsAdminReview,
      ParametersHeader = $"ФОРМА {entity.OwnForm.Number} — ПО НТД",
      Parameters = formParams
          // An optional second row (e.g. second supply voltage) exists only
          // for components that actually have it.
          .Where(p => !p.IsOptional || values.ContainsKey(p.Id))
          .Select(p => new CatalogParamRow
          {
            RowNumber = p.RowNumber,
            Name = DisplayName(p),
            Value = values.TryGetValue(p.Id, out var v) && !string.IsNullOrWhiteSpace(v) ? v : NoValue,
            Pins = pins.TryGetValue(p.Id, out var pn) && !string.IsNullOrWhiteSpace(pn) ? pn : null,
          })
          .ToList(),
      Notes = notes
          .OrderBy(n => n.RowNumber)
          .Select(n => new CatalogNoteRow { Parameter = $"{n.RowNumber}. {n.Name}", Text = n.Text })
          .ToList(),
    };
  }

  private static string DisplayName(FormParameter p)
      => p.Unit is null ? p.Name : $"{p.Name}, {p.Unit}";

  /// <summary>Numeric order for form numbers ("4" before "67"), text ones last.</summary>
  private static int FormNumberOrder(string number)
      => int.TryParse(number, out var n) ? n : int.MaxValue;
}
