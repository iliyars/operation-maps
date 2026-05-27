using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Wpf.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
  private readonly IDbContextFactory<OperationMapsDbContext> _dbFactory;

  public CatalogViewModel(IDbContextFactory<OperationMapsDbContext> dbFactory)
  {
    _dbFactory = dbFactory;
  }

  // ── Панель 1: Типы ───────────────────────────────────────────────────────

  public ObservableCollection<ComponentTypeVm> ComponentTypes { get; } = new();

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasSelectedType))]
  [NotifyCanExecuteChangedFor(nameof(DeleteTypeCommand))]
  [NotifyCanExecuteChangedFor(nameof(AddFamilyCommand))]
  private ComponentTypeVm? _selectedType;

  public bool HasSelectedType => SelectedType != null;

  partial void OnSelectedTypeChanged(ComponentTypeVm? value)
      => _ = LoadFamiliesAsync(value?.Id);

  // ── Панель 2: Семейства ──────────────────────────────────────────────────

  public ObservableCollection<FamilyVm> Families { get; } = new();

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasSelectedFamily))]
  [NotifyCanExecuteChangedFor(nameof(DeleteFamilyCommand))]
  [NotifyCanExecuteChangedFor(nameof(SaveNtdCommand))]
  [NotifyCanExecuteChangedFor(nameof(AddNoteToFamilyCommand))]
  private FamilyVm? _selectedFamily;

  public bool HasSelectedFamily => SelectedFamily != null;

  partial void OnSelectedFamilyChanged(FamilyVm? value)
  {
    _ = LoadNtdAsync(value?.Id);
    _ = LoadFamilyNotesAsync(value?.Id);
  }

  // ── Панель 3: NTD ────────────────────────────────────────────────────────

  public ObservableCollection<NtdRowVm> NtdRows { get; } = new();

  // ── Секция примечаний семейства ──────────────────────────────────────────

  /// <summary>Примечания, уже привязанные к семейству.</summary>
  public ObservableCollection<FamilyNoteVm> FamilyNotes { get; } = new();

  /// <summary>Все параметры текущей формы — для выпадающего списка.</summary>
  public ObservableCollection<FormParameter> AvailableParameters { get; } = new();

  /// <summary>Все примечания из справочника — для выпадающего списка.</summary>
  public ObservableCollection<Note> AvailableNotes { get; } = new();

  /// <summary>Выбранное примечание для добавления.</summary>
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(AddNoteToFamilyCommand))]
  private Note? _newNote;

  /// <summary>Выбранный параметр для добавления.</summary>
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(AddNoteToFamilyCommand))]
  private FormParameter? _newNoteParameter;

  private bool CanAddNote =>
      HasSelectedFamily && NewNote != null && NewNoteParameter != null;

  // ── Статус ───────────────────────────────────────────────────────────────

  [ObservableProperty] private string _statusMessage = "";
  [ObservableProperty] private bool _isBusy;

  // ── Загрузка ─────────────────────────────────────────────────────────────

  [RelayCommand]
  public async Task LoadAsync()
  {
    IsBusy = true;
    try
    {
      await using var db = await _dbFactory.CreateDbContextAsync();

      var types = await db.ComponentTypes.OrderBy(t => t.Name).ToListAsync();
      ComponentTypes.Clear();
      foreach (var t in types)
        ComponentTypes.Add(new ComponentTypeVm { Id = t.Id, Name = t.Name });

      // Загружаем справочник примечаний и параметры один раз
      var notes = await db.Notes.OrderBy(n => n.Text).ToListAsync();
      AvailableNotes.Clear();
      foreach (var n in notes) AvailableNotes.Add(n);

      StatusMessage = $"Загружено типов: {ComponentTypes.Count}";
    }
    catch (Exception ex) { StatusMessage = $"Ошибка загрузки: {ex.Message}"; }
    finally { IsBusy = false; }
  }

  // ── Типы: CRUD ───────────────────────────────────────────────────────────

  [RelayCommand]
  private async Task AddTypeAsync()
  {
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = new ComponentType { Name = "Новый тип" };
    db.ComponentTypes.Add(entity);
    await db.SaveChangesAsync();

    var vm = new ComponentTypeVm { Id = entity.Id, Name = entity.Name };
    ComponentTypes.Add(vm);
    SelectedType = vm;
    StatusMessage = "Тип добавлен";
  }

  [RelayCommand(CanExecute = nameof(HasSelectedType))]
  private async Task DeleteTypeAsync()
  {
    if (SelectedType is null) return;
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = await db.ComponentTypes.FindAsync(SelectedType.Id);
    if (entity is null) return;
    db.ComponentTypes.Remove(entity);
    await db.SaveChangesAsync();
    ComponentTypes.Remove(SelectedType);
    SelectedType = null;
    StatusMessage = "Тип удалён";
  }

  [RelayCommand(CanExecute = nameof(HasSelectedType))]
  private async Task RenameTypeAsync()
  {
    if (SelectedType is null) return;
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = await db.ComponentTypes.FindAsync(SelectedType.Id);
    if (entity is null) return;
    entity.Name = SelectedType.Name;
    await db.SaveChangesAsync();
    StatusMessage = $"Тип «{entity.Name}» сохранён";
  }

  // ── Семейства: загрузка ──────────────────────────────────────────────────

  private async Task LoadFamiliesAsync(int? typeId)
  {
    Families.Clear();
    NtdRows.Clear();
    FamilyNotes.Clear();
    if (typeId is null) return;

    IsBusy = true;
    try
    {
      await using var db = await _dbFactory.CreateDbContextAsync();
      var families = await db.Families
          .Where(f => f.ComponentTypeId == typeId)
          .OrderBy(f => f.Name)
          .ToListAsync();
      foreach (var f in families)
        Families.Add(new FamilyVm { Id = f.Id, ComponentTypeId = f.ComponentTypeId, Name = f.Name });
    }
    finally { IsBusy = false; }
  }

  // ── Семейства: CRUD ──────────────────────────────────────────────────────

  [RelayCommand(CanExecute = nameof(HasSelectedType))]
  private async Task AddFamilyAsync()
  {
    if (SelectedType is null) return;
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = new Family { Name = "Новое семейство", ComponentTypeId = SelectedType.Id };
    db.Families.Add(entity);
    await db.SaveChangesAsync();
    var vm = new FamilyVm { Id = entity.Id, ComponentTypeId = entity.ComponentTypeId, Name = entity.Name };
    Families.Add(vm);
    SelectedFamily = vm;
    StatusMessage = "Семейство добавлено";
  }

  [RelayCommand(CanExecute = nameof(HasSelectedFamily))]
  private async Task DeleteFamilyAsync()
  {
    if (SelectedFamily is null) return;
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = await db.Families.FindAsync(SelectedFamily.Id);
    if (entity is null) return;
    db.Families.Remove(entity);
    await db.SaveChangesAsync();
    Families.Remove(SelectedFamily);
    SelectedFamily = null;
    StatusMessage = "Семейство удалено";
  }

  [RelayCommand(CanExecute = nameof(HasSelectedFamily))]
  private async Task RenameFamilyAsync()
  {
    if (SelectedFamily is null) return;
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = await db.Families.FindAsync(SelectedFamily.Id);
    if (entity is null) return;
    entity.Name = SelectedFamily.Name;
    await db.SaveChangesAsync();
    StatusMessage = $"Семейство «{entity.Name}» сохранено";
  }

  // ── NTD: загрузка ────────────────────────────────────────────────────────

  private async Task LoadNtdAsync(int? familyId)
  {
    NtdRows.Clear();
    AvailableParameters.Clear();
    if (familyId is null) return;

    IsBusy = true;
    try
    {
      await using var db = await _dbFactory.CreateDbContextAsync();

      var parameters = await db.FormParameters
          .OrderBy(p => p.RowNumber)
          .ToListAsync();

      // Заполняем список параметров для выпадающего списка примечаний
      foreach (var p in parameters) AvailableParameters.Add(p);

      var savedValues = await db.FamilyNtdValues
          .Where(v => v.FamilyId == familyId)
          .ToDictionaryAsync(v => v.FormParameterId);

      foreach (var param in parameters)
      {
        savedValues.TryGetValue(param.Id, out var saved);
        var row = new NtdRowVm
        {
          Id = saved?.Id ?? 0,
          FormParameterId = param.Id,
          RowNumber = param.RowNumber,
          ParameterName = param.Name,
          Unit = param.Unit,
          Value = saved?.Value ?? "",
        };
        row.MarkClean();
        NtdRows.Add(row);
      }
    }
    finally { IsBusy = false; }
  }

  // ── NTD: сохранение ──────────────────────────────────────────────────────

  [RelayCommand(CanExecute = nameof(HasSelectedFamily))]
  private async Task SaveNtdAsync()
  {
    if (SelectedFamily is null) return;
    IsBusy = true;
    try
    {
      await using var db = await _dbFactory.CreateDbContextAsync();
      foreach (var row in NtdRows.Where(r => r.IsDirty))
      {
        if (row.Id == 0)
        {
          var entity = new FamilyNtdValue
          {
            FamilyId = SelectedFamily.Id,
            FormParameterId = row.FormParameterId,
            Value = row.Value,
          };
          db.FamilyNtdValues.Add(entity);
          await db.SaveChangesAsync();
          row.Id = entity.Id;
        }
        else
        {
          var entity = await db.FamilyNtdValues.FindAsync(row.Id);
          if (entity is not null) entity.Value = row.Value;
        }
        row.MarkClean();
      }
      await db.SaveChangesAsync();
      StatusMessage = $"NTD семейства «{SelectedFamily.Name}» сохранены";
    }
    catch (Exception ex) { StatusMessage = $"Ошибка сохранения NTD: {ex.Message}"; }
    finally { IsBusy = false; }
  }

  // ── Примечания семейства: загрузка ───────────────────────────────────────

  private async Task LoadFamilyNotesAsync(int? familyId)
  {
    FamilyNotes.Clear();
    if (familyId is null) return;

    await using var db = await _dbFactory.CreateDbContextAsync();
    var notes = await db.FamilyNotes
        .Where(fn => fn.FamilyId == familyId)
        .Include(fn => fn.Note)
        .Include(fn => fn.FormParameter)
        .ToListAsync();

    foreach (var fn in notes)
    {
      FamilyNotes.Add(new FamilyNoteVm
      {
        NoteId = fn.NoteId,
        FamilyId = fn.FamilyId,
        NoteText = fn.Note.Text,
        SelectedParameterId = fn.FormParameterId,
        SelectedParameterName = fn.FormParameter.Name,
      });
    }
  }

  // ── Примечания семейства: добавить ───────────────────────────────────────

  [RelayCommand(CanExecute = nameof(CanAddNote))]
  private async Task AddNoteToFamilyAsync()
  {
    if (SelectedFamily is null || NewNote is null || NewNoteParameter is null) return;

    await using var db = await _dbFactory.CreateDbContextAsync();

    // Проверяем дубликат
    var exists = await db.FamilyNotes.AnyAsync(fn =>
        fn.FamilyId == SelectedFamily.Id &&
        fn.FormParameterId == NewNoteParameter.Id &&
        fn.NoteId == NewNote.Id);

    if (exists)
    {
      StatusMessage = "Это примечание уже привязано к данному параметру";
      return;
    }

    var entity = new FamilyNote
    {
      FamilyId = SelectedFamily.Id,
      FormParameterId = NewNoteParameter.Id,
      NoteId = NewNote.Id,
    };
    db.FamilyNotes.Add(entity);
    await db.SaveChangesAsync();

    FamilyNotes.Add(new FamilyNoteVm
    {
      NoteId = NewNote.Id,
      FamilyId = SelectedFamily.Id,
      NoteText = NewNote.Text,
      SelectedParameterId = NewNoteParameter.Id,
      SelectedParameterName = NewNoteParameter.Name,
    });

    NewNote = null;
    NewNoteParameter = null;
    StatusMessage = "Примечание добавлено";
  }

  // ── Примечания семейства: удалить ────────────────────────────────────────

  [RelayCommand]
  private async Task RemoveNoteFromFamilyAsync(FamilyNoteVm vm)
  {
    await using var db = await _dbFactory.CreateDbContextAsync();
    var entity = await db.FamilyNotes.FindAsync(vm.FamilyId, vm.SelectedParameterId, vm.NoteId);
    if (entity is null) return;

    db.FamilyNotes.Remove(entity);
    await db.SaveChangesAsync();

    FamilyNotes.Remove(vm);
    StatusMessage = "Примечание удалено";
  }

  // ── Справочник примечаний: создать новое ────────────────────────────────

  [RelayCommand]
  private async Task CreateNoteAsync(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return;

    await using var db = await _dbFactory.CreateDbContextAsync();

    // Не дублируем если такой текст уже есть
    var existing = await db.Notes.FirstOrDefaultAsync(n => n.Text == text);
    if (existing is not null)
    {
      NewNote = existing;
      StatusMessage = "Примечание уже существует в справочнике";
      return;
    }

    var entity = new Note { Text = text };
    db.Notes.Add(entity);
    await db.SaveChangesAsync();

    AvailableNotes.Add(entity);
    NewNote = entity;
    StatusMessage = "Примечание создано в справочнике";
  }
}
