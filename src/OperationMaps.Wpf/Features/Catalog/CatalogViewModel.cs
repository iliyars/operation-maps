using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components.AddComponent;
using OperationMaps.Wpf.Stores;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Catalog;

/// <summary>
/// "Справочник ЭРИ" — read-only browser of the catalog database, available
/// to every user: types → families → components. A family shows its Form 4
/// ("по НТД") values; a component shows the values of its own form
/// (Form 67, 68, ...). In admin mode the catalog can also be extended:
/// a component into the selected family, or a new family with its first
/// component — both through the same wizard as in a project, but with the
/// name typed in and the result counted as verified. Admins can also edit
/// the selected record in place (name + values) and delete it.
/// </summary>
public sealed partial class CatalogViewModel : ScreenViewModelBase, INavigatedTo
{
  private const string NoValue = "—";

  private readonly CatalogDbContext _db;
  private readonly SessionStore _session;
  private readonly IComponentEntryService _entryService;
  private readonly IComponentNameParser _nameParser;
  private readonly ICatalogEditService _editService;
  private readonly IDialogService _dialogs;

  public IReadOnlyList<CatalogTypeNode> Types { get; private set; } = [];

  [ObservableProperty] private string _searchText = "";

  /// <summary>Admin's work queue: only families/components still awaiting review.</summary>
  [ObservableProperty] private bool _onlyUnverified;

  public int UnverifiedCount => Types.Sum(t => t.AllFamilies.Sum(f => f.Components.Count(c => c.NeedsReview)));
  [ObservableProperty] private bool _isLoading;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(AddComponentCommand))]
  private CatalogFamilyNode? _selectedFamily;
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
  private CatalogComponentItem? _selectedComponent;
  [ObservableProperty] private IReadOnlyList<CatalogComponentItem> _visibleComponents = [];
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
  [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
  private CatalogCard? _card;

  // ── Edit mode (admin) ─────────────────────────────────────────────────────

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
  [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
  [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
  private bool _isEditing;

  [ObservableProperty] private string _editName = "";
  [ObservableProperty] private string? _editError;

  public int FamilyCount => Types.Sum(t => t.AllFamilies.Count);
  public int ComponentCount => Types.Sum(t => t.AllFamilies.Sum(f => f.Components.Count));

  public bool IsAdmin => _session.IsAdmin;

  public CatalogViewModel(
      CatalogDbContext db,
      SessionStore session,
      IComponentEntryService entryService,
      IComponentNameParser nameParser,
      ICatalogEditService editService,
      IDialogService dialogs)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _session = session ?? throw new ArgumentNullException(nameof(session));
    _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
    _editService = editService ?? throw new ArgumentNullException(nameof(editService));
    _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    // Signing in/out while the catalog is open shows/hides the admin buttons.
    _session.PropertyChanged += OnSessionChanged;
  }

  private void OnSessionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (e.PropertyName != nameof(SessionStore.IsAdmin)) return;
    OnPropertyChanged(nameof(IsAdmin));
    AddComponentCommand.NotifyCanExecuteChanged();
    AddFamilyCommand.NotifyCanExecuteChanged();
    VerifyCommand.NotifyCanExecuteChanged();
    StartEditCommand.NotifyCanExecuteChanged();
    DeleteCommand.NotifyCanExecuteChanged();
    if (!IsAdmin)
    {
      OnlyUnverified = false;
      CancelEdit();
    }
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
                TypeName = t.Name,
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
    OnPropertyChanged(nameof(UnverifiedCount));
    ApplySearch();
  }

  partial void OnSearchTextChanged(string value) => ApplySearch();
  partial void OnOnlyUnverifiedChanged(bool value) => ApplySearch();

  private void ApplySearch()
  {
    var search = SearchText.Trim();
    foreach (var type in Types)
    {
      type.ApplyFilter(search, OnlyUnverified);
      // While filtering, open every type that has hits; otherwise keep the user's choice.
      if (search.Length > 0 || OnlyUnverified)
        type.IsExpanded = type.Families.Count > 0;
    }
    UpdateVisibleComponents();
  }

  private void UpdateVisibleComponents()
  {
    var search = SearchText.Trim();
    if (SelectedFamily is null)
    {
      VisibleComponents = [];
      return;
    }

    IEnumerable<CatalogComponentItem> components = SelectedFamily.Components;
    if (search.Length > 0 && !SelectedFamily.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
      components = components.Where(c => c.Matches(search));
    if (OnlyUnverified)
      components = components.Where(c => c.NeedsReview);
    VisibleComponents = components.ToList();
  }

  [RelayCommand]
  private void ToggleType(CatalogTypeNode type) => type.IsExpanded = !type.IsExpanded;

  // ── Selection ─────────────────────────────────────────────────────────────

  [RelayCommand]
  private async Task SelectFamilyAsync(CatalogFamilyNode family, CancellationToken ct)
  {
    CancelEdit();
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
    CancelEdit();
    SelectComponentItem(component);
    Card = await BuildComponentCardAsync(component, ct);
  }

  private void SelectComponentItem(CatalogComponentItem? component)
  {
    if (SelectedComponent is not null) SelectedComponent.IsSelected = false;
    SelectedComponent = component;
    if (component is not null) component.IsSelected = true;
  }

  // ── Admin: adding to the catalog ──────────────────────────────────────────

  [RelayCommand(CanExecute = nameof(CanAddComponent))]
  private Task AddComponentAsync()
      => RunWizardAsync(AddComponentTarget.ForCatalog(SelectedFamily!.Id, SelectedFamily.Name, SelectedFamily.TypeName));

  private bool CanAddComponent => IsAdmin && SelectedFamily is not null;

  [RelayCommand(CanExecute = nameof(IsAdmin))]
  private Task AddFamilyAsync()
      => RunWizardAsync(AddComponentTarget.ForCatalog(null, null, SelectedFamily?.TypeName));

  private async Task RunWizardAsync(AddComponentTarget target)
  {
    var vm = new AddComponentDialogViewModel(_db, _entryService, _nameParser, target);
    var dialog = new AddComponentDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };

    if (dialog.ShowDialog() != true || vm.CreatedComponent is not { } created) return;

    // Show the result right away.
    await ReloadAndSelectAsync(created.FamilyId, created.Id);
  }

  /// <summary>Re-reads the catalog and re-selects a family (and a component in it), if they still exist.</summary>
  private async Task ReloadAndSelectAsync(int? familyId, int? componentId)
  {
    SelectComponentItem(null);
    SelectedFamily = null;
    Card = null;

    await LoadTreeAsync(CancellationToken.None);

    var family = Types.SelectMany(t => t.AllFamilies).FirstOrDefault(f => f.Id == familyId);
    if (family is null) return;

    Types.First(t => t.AllFamilies.Contains(family)).IsExpanded = true;
    await SelectFamilyAsync(family, CancellationToken.None);

    var item = family.Components.FirstOrDefault(c => c.Id == componentId);
    if (item is not null)
      await SelectComponentAsync(item, CancellationToken.None);
  }

  // ── Admin: editing and deleting ───────────────────────────────────────────

  [RelayCommand(CanExecute = nameof(CanEditCard))]
  private void StartEdit()
  {
    if (Card is null) return;

    EditName = Card.Title;
    EditError = null;
    foreach (var row in Card.Parameters)
    {
      row.EditValue = row.StoredValue;
      row.EditPins = row.Pins ?? "";
    }
    IsEditing = true;
  }

  private bool CanEditCard => IsAdmin && !IsEditing && Card is { EntityId: > 0 };

  [RelayCommand]
  private async Task SaveEditAsync()
  {
    if (Card is null) return;

    EditError = null;
    var values = Card.Parameters.ToDictionary(r => r.FormParameterId, r => r.EditValue);

    try
    {
      if (Card.Kind == CatalogCardKind.Family)
        await _editService.UpdateFamilyAsync(Card.EntityId, EditName, values);
      else
        await _editService.UpdateComponentAsync(
            Card.EntityId,
            EditName,
            values,
            Card.ShowsPins ? Card.Parameters.ToDictionary(r => r.FormParameterId, r => r.EditPins) : new Dictionary<int, string>());
    }
    catch (InvalidOperationException ex)
    {
      EditError = ex.Message;
      return;
    }

    IsEditing = false;
    // Names may have changed — the tree and search need a fresh read.
    await ReloadAndSelectAsync(SelectedFamily?.Id, Card.Kind == CatalogCardKind.Component ? Card.EntityId : null);
  }

  [RelayCommand]
  private void CancelEdit()
  {
    IsEditing = false;
    EditError = null;
  }

  [RelayCommand(CanExecute = nameof(CanEditCard))]
  private async Task DeleteAsync()
  {
    if (Card is null || SelectedFamily is not { } family) return;

    try
    {
      if (Card.Kind == CatalogCardKind.Component)
      {
        if (_dialogs.AskYesNoCancel(
                $"Удалить компонент «{Card.Title}» из справочника вместе со всеми его данными?\n\nОтменить это действие нельзя.",
                "Удаление компонента") != true)
          return;

        var familyEmpty = await _editService.DeleteComponentAsync(Card.EntityId);

        // Microcircuits & co.: family and component share a name — an empty
        // family left behind is almost always just clutter.
        if (familyEmpty && _dialogs.AskYesNoCancel(
                $"В семействе «{family.Name}» больше нет компонентов. Удалить и семейство?",
                "Удаление семейства") == true)
        {
          await _editService.DeleteFamilyAsync(family.Id);
          await ReloadAndSelectAsync(null, null);
          return;
        }

        await ReloadAndSelectAsync(family.Id, null);
      }
      else
      {
        if (family.Components.Count > 0)
        {
          _dialogs.ShowWarning(
              $"В семействе «{family.Name}» есть компоненты ({family.Components.Count}). Сначала удалите их.",
              "Удаление семейства");
          return;
        }

        if (_dialogs.AskYesNoCancel(
                $"Удалить семейство «{family.Name}» вместе с его данными Формы 4?\n\nОтменить это действие нельзя.",
                "Удаление семейства") != true)
          return;

        await _editService.DeleteFamilyAsync(family.Id);
        await ReloadAndSelectAsync(null, null);
      }
    }
    catch (InvalidOperationException ex)
    {
      _dialogs.ShowWarning(ex.Message, "Удаление");
    }
  }

  // ── Admin: confirming user-added components ───────────────────────────────

  [RelayCommand(CanExecute = nameof(CanVerify))]
  private async Task VerifyAsync()
  {
    if (SelectedComponent is not { } item) return;

    await _entryService.MarkVerifiedAsync(item.Id);

    item.NeedsReview = false;
    SelectedFamily?.NotifyReviewChanged();
    OnPropertyChanged(nameof(UnverifiedCount));
    VerifyCommand.NotifyCanExecuteChanged();
    Card = await BuildComponentCardAsync(item, CancellationToken.None);
  }

  private bool CanVerify => IsAdmin && !IsEditing && SelectedComponent is { NeedsReview: true };

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
      Kind = CatalogCardKind.Family,
      EntityId = family.Id,
      Title = family.Name,
      Subtitle = subtitle,
      ParametersHeader = "ФОРМА 4 — ПО НТД",
      Parameters = form4Params
          .Select(p => new CatalogParamRow
          {
            FormParameterId = p.Id,
            RowNumber = p.RowNumber,
            Name = DisplayName(p),
            Value = values.TryGetValue(p.Id, out var v) && !string.IsNullOrWhiteSpace(v) ? v : NoValue,
            StoredValue = values.GetValueOrDefault(p.Id) ?? "",
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
        Kind = CatalogCardKind.Component,
        Title = component.FullName,
        Subtitle = "",
        ParametersHeader = "",
        EmptyMessage = "Компонент удалён из справочника.",
      };

    var subtitle = $"Семейство: {entity.Family.Name}";

    if (entity.OwnForm is null)
      return new CatalogCard
      {
        Kind = CatalogCardKind.Component,
        EntityId = entity.Id,
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
      Kind = CatalogCardKind.Component,
      EntityId = entity.Id,
      ShowsPins = entity.OwnForm.Number == "64", // pin numbers are unique to Form 64
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
            FormParameterId = p.Id,
            RowNumber = p.RowNumber,
            Name = DisplayName(p),
            Value = values.TryGetValue(p.Id, out var v) && !string.IsNullOrWhiteSpace(v) ? v : NoValue,
            StoredValue = values.GetValueOrDefault(p.Id) ?? "",
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
