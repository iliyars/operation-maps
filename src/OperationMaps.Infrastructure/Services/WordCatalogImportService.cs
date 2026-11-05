using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using DocumentFormat.OpenXml.Packaging;
using OperationMaps.Application.Models;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Infrastructure.Services;

/// <summary>
/// Reads a filled work-order Word document — which may bundle Form4 plus
/// several own-forms (Form67, Form68, ...), each possibly spread across
/// multiple pages — and resolves each own-form component slot against the
/// catalog, ready to hand to <see cref="IComponentEntryService"/>.
/// <para>
/// Form4 is family-level data (shared across all components of one base
/// part number): each own-form component's full designation is matched
/// against Form4's "Обозначение" slots by prefix (e.g. the own-form's
/// "К10-84в 3216М-50 В-Н90-0,15 мкФ-N-A" is matched to Form4's
/// "К10-84в"), since that's the only thing linking the two forms' data —
/// there's no shared id between them in the source document.
/// </para>
/// </summary>
public sealed class WordCatalogImportService : IWordCatalogImportService, IWordCatalogImportProgress
{
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;
  private readonly IComponentEntryService _entryService;
  private readonly IComponentNameParser _nameParser;
  private readonly WordFormMapLoader _mapLoader;

  public WordCatalogImportService(
      IDbContextFactory<CatalogDbContext> dbFactory,
      IComponentEntryService entryService,
      IComponentNameParser nameParser,
      WordFormMapLoader mapLoader)
  {
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
    _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
  }

  public Task<WordImportScanResult> ScanAsync(string documentPath, CancellationToken ct = default)
    => Task.Run(() => ScanCoreAsync(documentPath, null, ct), ct);

  public Task<WordImportScanResult> ScanAsync(string documentPath, IProgress<WordImportProgress> progress, CancellationToken ct = default)
    => Task.Run(() => ScanCoreAsync(documentPath, progress, ct), ct);

  private async Task<WordImportScanResult> ScanCoreAsync(string documentPath, IProgress<WordImportProgress>? progress, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    progress?.Report(new("Открытие документа…"));
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    if (!File.Exists(documentPath))
      throw new FileNotFoundException("Document not found.", documentPath);

    using var document = WordprocessingDocument.Open(documentPath, false);
    var formNumbersInDoc = WordFormReader.DetectFormNumbers(document, ct);
    progress?.Report(new("Чтение формы 4 и загрузка каталога…", 0, formNumbersInDoc.Count));

    var allForms = await db.Forms.Include(f => f.Parameters).ToListAsync(ct);
    var formsByNumber = allForms.ToDictionary(f => f.Number, StringComparer.OrdinalIgnoreCase);

    // ── Form 4 (family-level) — read once if present ──────────────────────
    WordFormData? form4Data = null;
    if (formsByNumber.ContainsKey("4") && TryLoadMap("4", out var form4Map))
      form4Data = WordFormReader.Read("4", document, form4Map!, ct);

    // Older catalogs start at resource (row 1); current maps include the two
    // list-availability rows first. Keep catalog IDs and stored projects intact.
    if (form4Data is not null && formsByNumber.TryGetValue("4", out var catalogForm4)
        && catalogForm4.Parameters.Any(p => p.RowNumber == 1 && p.Name == "Показатель ресурса"))
      form4Data = new WordFormData
      {
        FormNumber = "4",
        Components = form4Data.Components.Select(c => new WordComponentData
        {
          Name = c.Name, ComponentTypeName = c.ComponentTypeName, Quantity = c.Quantity, Note = c.Note,
          NtdValues = c.NtdValues.Where(v => v.Key >= 3).ToDictionary(v => v.Key - 2, v => v.Value)
        }).ToList()
      };

    var componentTypes = await db.ComponentTypes.ToListAsync(ct);
    var typesByName = componentTypes.ToDictionary(t => t.Name, StringComparer.Ordinal);

    var unsupported = new List<string>();
    var resolved = new List<WordImportedComponent>();

    // The same full designation legitimately appears in several own-form
    // slots — e.g. positions C1 and C2 use the identical capacitor value,
    // each tracked as its own slot so their "в схеме" values can differ —
    // Repeated slots still produce one catalog component. Keep their values:
    // unmarked values may supply a default, while marked values describe
    // alternative regimes of that same component.
    var seenFullNames = new Dictionary<string, WordImportedComponent>(StringComparer.Ordinal);

    var processedForms = 0;
    foreach (var formNumber in formNumbersInDoc)
    {
      ct.ThrowIfCancellationRequested();
      progress?.Report(new($"Чтение формы {formNumber}…", processedForms++, formNumbersInDoc.Count));
      // Form 4 itself and the cover/TOC/changelog pages aren't own-forms.
      if (formNumber.ToUpperInvariant() is "2А" or "2A" or "3" or "4") continue;

      if (!formsByNumber.TryGetValue(formNumber, out var form) || !TryLoadMap(formNumber, out var map))
      {
        unsupported.Add(formNumber);
        continue;
      }

      var formData = WordFormReader.Read(formNumber, document, map!, ct);

      foreach (var slot in formData.Components)
      {
        ct.ThrowIfCancellationRequested();
        var item = await ResolveComponentAsync(slot, form, form4Data, typesByName, ct);
        if (seenFullNames.TryGetValue(item.FullName, out var previous))
        {
          previous.ImportErrors.AddRange(item.ImportErrors);
          if (previous.OwnFormId == item.OwnFormId)
          {
            if (previous.LoadFactorBaseRow is { } previousRow && item.LoadFactorBaseRow is { } nextRow && previousRow != nextRow)
              throw new InvalidOperationException($"Для «{item.FullName}» указаны разные базовые параметры коэффициента нагрузки.");
            previous.LoadFactorBaseRow ??= item.LoadFactorBaseRow;
            previous.AdditionalVariants.Add(new(item.Form4NtdValuesByRow, item.OwnFormNtdValuesByRow,
              item.Form4NotesByRow, item.OwnFormNotesByRow));
            previous.AdditionalVariants.AddRange(item.AdditionalVariants);
          }
          else
            previous.ImportErrors.Add($"Для одного компонента указаны разные собственные формы: {previous.OwnFormNumber} и {item.OwnFormNumber}. Уточните документ.");
          continue;
        }
        seenFullNames.Add(item.FullName, item);
        resolved.Add(item);
      }
    }

    progress?.Report(new($"Прочитано компонентов: {resolved.Count}", formNumbersInDoc.Count, formNumbersInDoc.Count));
    return new WordImportScanResult
    {
      Components = resolved,
      UnsupportedFormNumbers = unsupported,
    };
  }

  public async Task ResolveComponentTypeAsync(WordImportedComponent item, string componentTypeName, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(item);

    var trimmedName = componentTypeName?.Trim() ?? "";

    if (trimmedName.Length == 0)
    {
      ct.ThrowIfCancellationRequested();
      item.ComponentTypeName = trimmedName;
      item.ComponentTypeId = null;
      item.FamilyName = null;
      item.ExistingFamilyId = null;
      return;
    }

    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var type = await db.ComponentTypes.FirstOrDefaultAsync(t => t.Name == trimmedName, ct);

    var parsed = _nameParser.Parse($"{trimmedName} {item.FullName}");
    var familyName = WordComponentName.PreserveFamilyPrefix(item.FullName,
      !string.IsNullOrWhiteSpace(parsed.Family) ? parsed.Family : parsed.Name);
    var familyId = type is null
        ? null
        : (await db.Families.FirstOrDefaultAsync(f => f.ComponentTypeId == type.Id && f.Name == familyName, ct))?.Id;
    ct.ThrowIfCancellationRequested();
    item.ComponentTypeName = trimmedName;
    item.ComponentTypeId = type?.Id;
    item.FamilyName = familyName;
    item.ExistingFamilyId = familyId;
  }

  public async Task<Component> ImportAsync(WordImportedComponent item, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    if (item.ImportErrors.Count > 0)
      throw new InvalidOperationException("Исправьте ошибки в исходном Word-документе перед импортом:\n" + string.Join("\n", item.ImportErrors.Distinct()));
    await using var db = await _dbFactory.CreateDbContextAsync(ct);

    if (string.IsNullOrWhiteSpace(item.ComponentTypeName))
      throw new InvalidOperationException($"Не указан тип компонента для «{item.FullName}».");

    // Re-resolve right before writing — covers both "the type came straight
    // from Form4 and was never touched by the review UI" and "the type text
    // was edited without a live re-resolve in between".
    await ResolveComponentTypeAsync(item, item.ComponentTypeName, ct);

    if (item.ExistingFamilyId is null && string.IsNullOrWhiteSpace(item.FamilyName))
      throw new InvalidOperationException($"Не указано семейство для «{item.FullName}».");

    var ownForm = await db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Id == item.OwnFormId, ct)
        ?? throw new InvalidOperationException($"Own form {item.OwnFormId} not found.");

    var ownFormRowToParamId = ownForm.Parameters.ToDictionary(p => p.RowNumber, p => p.Id);
    int? loadFactorParameterId = null;
    if (item.LoadFactorBaseRow is { } baseRow)
    {
      var parameter = ownForm.Parameters.FirstOrDefault(p => p.RowNumber == baseRow && !p.IsOptional
        && !p.IsLoadFactorResult && p.Name != "Коэффициент нагрузки");
      if (parameter is null)
        throw new InvalidOperationException($"В форме {ownForm.Number} нет базового параметра № {baseRow} для коэффициента нагрузки.");
      loadFactorParameterId = parameter.Id;
    }

    var form4Values = (await MapRowValuesToParamIdsAsync("4", item.Form4NtdValuesByRow, ct)).ToDictionary();
    var ownValues = MapRowValues(ownFormRowToParamId, item.OwnFormNtdValuesByRow).ToDictionary();
    var form4Notes = (await MapNotesAsync("4", item.Form4NotesByRow, ct)).ToDictionary();
    var ownNotes = (await MapNotesAsync(item.OwnFormNumber, item.OwnFormNotesByRow, ct)).ToDictionary();
    var conditions = new Dictionary<string, Dictionary<int, string>>(StringComparer.Ordinal);
    ExtractConditions(form4Values, form4Notes);
    ExtractConditions(ownValues, ownNotes);
    foreach (var variant in item.AdditionalVariants)
    {
      var extraForm4 = (await MapRowValuesToParamIdsAsync("4", variant.Form4Values, ct)).ToDictionary();
      var extraOwn = MapRowValues(ownFormRowToParamId, variant.OwnValues).ToDictionary();
      var extraForm4Notes = (await MapNotesAsync("4", variant.Form4Notes, ct)).ToDictionary();
      var extraOwnNotes = (await MapNotesAsync(item.OwnFormNumber, variant.OwnNotes, ct)).ToDictionary();
      ExtractConditions(extraForm4, extraForm4Notes);
      ExtractConditions(extraOwn, extraOwnNotes);
      MergeVariant(form4Values, form4Notes, extraForm4, extraForm4Notes);
      MergeVariant(ownValues, ownNotes, extraOwn, extraOwnNotes);
    }

    void ExtractConditions(Dictionary<int, string> values, Dictionary<int, List<string>> notes)
    {
      foreach (var (id, texts) in notes.ToArray())
      {
        var selected = texts.Where(ReliefNote.IsMatch).Distinct().ToArray();
        if (selected.Length == 0) continue;
        if (selected.Length > 1)
          throw new InvalidOperationException("У значения несколько условий. Уточните их совместное применение перед импортом.");
        if (!values.TryGetValue(id, out var value) || string.IsNullOrWhiteSpace(value))
          throw new InvalidOperationException("Условие должно быть связано с непустым значением параметра.");
        // All explanations on a conditional value remain mandatory with that value.
        var condition = string.Join("\n", texts.Distinct());
        if (!conditions.TryGetValue(condition, out var changes)) conditions[condition] = changes = [];
        if (changes.TryGetValue(id, out var previous) && previous != value)
          throw new InvalidOperationException($"Условие «{condition}» задаёт разные значения одного параметра.");
        changes[id] = value;
        values.Remove(id);
        notes.Remove(id);
      }
    }

    static void MergeVariant(Dictionary<int, string> values, Dictionary<int, List<string>> notes,
      Dictionary<int, string> extraValues, IReadOnlyDictionary<int, List<string>> extraNotes)
    {
      foreach (var (id, value) in extraValues)
      {
        if (values.TryGetValue(id, out var previous))
        {
          // Repeated position groups often leave unused NTD cells as dashes.
          // These placeholders must not replace a known catalog value.
          if (IsMissing(value)) continue;
          if (!IsMissing(previous) && previous != value)
            throw new InvalidOperationException($"В документе указаны разные значения одного параметра: «{previous}» и «{value}». Уточните данные перед импортом.");
        }
        values[id] = value;
      }
      foreach (var (id, texts) in extraNotes)
      {
        if (!notes.TryGetValue(id, out var existing)) notes[id] = existing = [];
        foreach (var text in texts)
          if (!existing.Contains(text)) existing.Add(text);
      }

      static bool IsMissing(string text) => text.Trim() is "" or "—" or "–" or "-";
    }
    var input = new NewComponentInput
    {
      ComponentTypeId = item.ComponentTypeId ?? 0,
      NewComponentTypeName = item.ComponentTypeId is null ? item.ComponentTypeName : null,
      ExistingFamilyId = item.ExistingFamilyId,
      NewFamilyName = item.ExistingFamilyId is null ? item.FamilyName : null,
      FullName = item.FullName,
      Form4Values = form4Values,
      OwnFormId = item.OwnFormId,
      LoadFactorParameterId = loadFactorParameterId,
      OwnFormValues = ownValues,
      PinValues = MapRowValues(ownFormRowToParamId, item.OwnFormPinValuesByRow),
      Form4Notes = form4Notes,
      OwnFormNotes = ownNotes,
      Regimes = conditions.Select(c => new NewComponentRegimeInput(c.Key, c.Key, c.Value)).ToArray(),
    };

    return await _entryService.CreateComponentAsync(input, ct);
  }

  // ── Resolution ────────────────────────────────────────────────────────────

  private async Task<WordImportedComponent> ResolveComponentAsync(
      WordComponentData slot,
      Form ownForm,
      WordFormData? form4Data,
      IReadOnlyDictionary<string, ComponentType> typesByName,
      CancellationToken ct)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var warnings = new List<string>();
    var fullName = ForeignComponentName.WithoutManufacturer(slot.Name);

    // Match this component's full designation against Form4's per-family
    // "Обозначение" slots by prefix — longest match wins in case more than
    // one Form4 slot's base designation happens to prefix-match. Two real
    // mismatches found on Ilya's own documents are compensated for:
    //  1. Look-alike/sound-alike characters (see NormalizeHomoglyphs) typed
    //     differently across the two form pages.
    //  2. Form4 sometimes includes a leading "ОС "/"ОСМ " (особая приёмка)
    //     quality-grade prefix that the own-form's "Наименование изделия"
    //     omits — tried as a fallback so the base designation still matches.
    var normalizedSlotName = NormalizeHomoglyphs(StripKnownDesignationPrefix(fullName));
    var form4Matches = form4Data?.Components
        .Where(f => !string.IsNullOrWhiteSpace(f.ComponentTypeName))
        .Select(f => new
        {
          Component = f,
          Designation = NormalizeHomoglyphs(f.ComponentTypeName.Trim()),
        })
        .Where(x => normalizedSlotName.StartsWith(x.Designation, StringComparison.OrdinalIgnoreCase)
            || normalizedSlotName.StartsWith(StripKnownDesignationPrefix(x.Designation), StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(x => x.Designation.Length)
        .Select(x => x.Component)
        .ToList() ?? [];
    var form4Match = form4Matches.FirstOrDefault();

    string? typeName = form4Match?.Name?.Trim();
    int? componentTypeId = null;
    string? familyName = null;
    int? existingFamilyId = null;

    if (form4Match is null)
    {
      warnings.Add("Не найдено соответствие в Форме 4 — укажите тип компонента вручную.");
    }
    else
    {
      // The type text comes straight from Word regardless of whether it's
      // already in the catalog — if it isn't, ImportAsync creates it rather
      // than blocking the user with a "pick from the existing list" wall.
      if (typesByName.TryGetValue(typeName!, out var type))
        componentTypeId = type.Id;
      else
        warnings.Add($"Тип «{typeName}» отсутствует в справочнике — будет создан новый.");

      var parsed = _nameParser.Parse($"{typeName} {fullName}");
      familyName = WordComponentName.PreserveFamilyPrefix(fullName,
        !string.IsNullOrWhiteSpace(parsed.Family) ? parsed.Family : parsed.Name);

      if (componentTypeId is { } id)
      {
        var family = await db.Families
            .FirstOrDefaultAsync(f => f.ComponentTypeId == id && f.Name == familyName, ct);
        existingFamilyId = family?.Id;
      }
    }

    var alreadyExists = await db.Components.AnyAsync(c => c.FullName == fullName, ct);
    var form4Values = form4Match is null ? new Dictionary<int, string>() : new Dictionary<int, string>(form4Match.NtdValues);
    var ownValues = new Dictionary<int, string>(slot.NtdValues);
    var noteErrors = new List<string>();
    Dictionary<int, List<string>> ParseNotes(string number, string text, Dictionary<int, string> values)
    {
      var errors = new List<string>();
      var notes = WordNoteParser.Extract(text, values, errors);
      noteErrors.AddRange(errors.Select(e => $"Форма {number}: {e}"));
      return notes;
    }
    var form4Notes = ParseNotes("4", form4Match?.Note ?? "", form4Values);
    var ownNotes = ParseNotes(ownForm.Number, slot.Note, ownValues);
    int? loadFactorBaseRow = null;
    foreach (var parameter in ownForm.Parameters.Where(p => p.IsLoadFactorResult || p.Name == "Коэффициент нагрузки"))
      foreach (var values in new[] { ownValues, slot.SchemeValues })
      {
        if (!values.TryGetValue(parameter.RowNumber, out var text)) continue;
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\(\s*(\d+)\s*\)");
        if (!match.Success) continue;
        if (!int.TryParse(match.Groups[1].Value, out var row) || row < 1)
          throw new InvalidOperationException("Некорректный номер базового параметра коэффициента нагрузки.");
        if (loadFactorBaseRow is { } existingRow && existingRow != row)
          throw new InvalidOperationException("В строке коэффициента нагрузки указаны разные базовые параметры.");
        loadFactorBaseRow = row;
        if (ReferenceEquals(values, ownValues)) ownValues[parameter.RowNumber] = text.Remove(match.Index, match.Length).Trim();
      }
    if (WordNoteParser.HasContent(slot.Note) && ownNotes.Count == 0)
      warnings.Add("Примечания собственной формы прочитаны, но не привязаны к строкам НТД: проверьте маркеры в ячейках.");
    if (WordNoteParser.HasContent(form4Match?.Note) && form4Notes.Count == 0)
      warnings.Add("Примечания формы 4 прочитаны, но не привязаны к строкам НТД: проверьте маркеры в ячейках.");

    var resolved = new WordImportedComponent
    {
      FullName = fullName,
      OwnFormNumber = ownForm.Number,
      OwnFormTitle = ownForm.Title,
      OwnFormId = ownForm.Id,
      DetectedTypeName = typeName,
      ComponentTypeName = typeName,
      ComponentTypeId = componentTypeId,
      FamilyName = familyName,
      ExistingFamilyId = existingFamilyId,
      AlreadyInCatalog = alreadyExists,
      Form4DataFound = form4Match is not null,
      Warnings = warnings,
      Form4NtdValuesByRow = form4Values,
      OwnFormNtdValuesByRow = ownValues,
      Form4NotesByRow = form4Notes,
      OwnFormNotesByRow = ownNotes,
      Form4NoteText = form4Match?.Note ?? "",
      OwnFormNoteText = slot.Note,
      OwnFormPinValuesByRow = new Dictionary<int, string>(slot.PinValues),
      LoadFactorBaseRow = loadFactorBaseRow,
    };
    if (form4Match is not null)
      foreach (var alternative in form4Matches.Skip(1).Where(f =>
        StripKnownDesignationPrefix(NormalizeHomoglyphs(f.ComponentTypeName.Trim())) ==
        StripKnownDesignationPrefix(NormalizeHomoglyphs(form4Match.ComponentTypeName.Trim()))))
      {
        var alternativeValues = new Dictionary<int, string>(alternative.NtdValues);
        var alternativeNotes = ParseNotes("4", alternative.Note, alternativeValues);
        resolved.AdditionalVariants.Add(new(alternativeValues, [], alternativeNotes, []));
      }
    resolved.ImportErrors.AddRange(noteErrors.Distinct());
    return resolved;
  }

  private async Task<IReadOnlyDictionary<int, string>> MapRowValuesToParamIdsAsync(
      string formNumber, IReadOnlyDictionary<int, string> valuesByRow, CancellationToken ct)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    if (valuesByRow.Count == 0) return new Dictionary<int, string>();

    var form = await db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Number == formNumber, ct);

    if (form is null) return new Dictionary<int, string>();

    var rowToParamId = form.Parameters.ToDictionary(p => p.RowNumber, p => p.Id);
    return MapRowValues(rowToParamId, valuesByRow);
  }

  private async Task<IReadOnlyDictionary<int, List<string>>> MapNotesAsync(
    string formNumber, Dictionary<int, List<string>> notes, CancellationToken ct)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var ids = await db.FormParameters.Where(p => p.Form.Number == formNumber)
      .ToDictionaryAsync(p => p.RowNumber, p => p.Id, ct);
    return notes.Where(n => ids.ContainsKey(n.Key)).ToDictionary(n => ids[n.Key], n => n.Value);
  }

  private static IReadOnlyDictionary<int, string> MapRowValues(
      IReadOnlyDictionary<int, int> rowToParamId, IReadOnlyDictionary<int, string> valuesByRow)
  {
    var result = new Dictionary<int, string>();
    foreach (var (row, value) in valuesByRow)
      if (rowToParamId.TryGetValue(row, out var paramId))
        result[paramId] = value;
    return result;
  }

  // Characters that get typed for the wrong reason across two different
  // form pages of the same hand-filled document — either because a Latin
  // letter LOOKS like a Cyrillic one (classic homograph set, same one used
  // for phishing-domain detection), or because a Cyrillic letter SOUNDS
  // like a Latin one and gets typed phonetically (e.g. "Б" -> "B" — a real
  // case found on Ilya's own document, "2Д906A2/ББ" vs "2Д906A2/BB"; "Б"
  // doesn't visually resemble "B" at all, but is folded into the same
  // bucket as visual "В" anyway since both are observed real mistakes for
  // different reasons, and mismatching them just means an extra row to
  // review, while missing a real match means unnecessary manual work).
  private static readonly Dictionary<char, char> LatinToCyrillicHomoglyphs = new()
  {
    ['A'] = 'А', ['a'] = 'а',
    ['B'] = 'В',
    ['Б'] = 'В', // phonetic mistake, folded into the same bucket as visual "B"
    ['C'] = 'С', ['c'] = 'с',
    ['E'] = 'Е', ['e'] = 'е',
    ['H'] = 'Н',
    ['K'] = 'К', ['k'] = 'к',
    ['M'] = 'М',
    ['O'] = 'О', ['o'] = 'о',
    ['P'] = 'Р', ['p'] = 'р',
    ['T'] = 'Т',
    ['X'] = 'Х', ['x'] = 'х',
    ['Y'] = 'У', ['y'] = 'у',
  };

  private static string NormalizeHomoglyphs(string s)
  {
    var chars = s.ToCharArray();
    for (int i = 0; i < chars.Length; i++)
      if (LatinToCyrillicHomoglyphs.TryGetValue(chars[i], out var cyrillic))
        chars[i] = cyrillic;
    return new string(chars);
  }

  private static readonly string[] KnownDesignationPrefixes = ["ОСМ ", "ОС "];

  private static string StripKnownDesignationPrefix(string s)
  {
    foreach (var prefix in KnownDesignationPrefixes)
      if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return s[prefix.Length..];
    return s;
  }

  private bool TryLoadMap(string formNumber, out WordFormMap? map)
  {
    try
    {
      map = _mapLoader.Load(formNumber);
      return true;
    }
    catch (FileNotFoundException)
    {
      map = null;
      return false;
    }
  }
}
