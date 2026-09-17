using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
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
public sealed class WordCatalogImportService : IWordCatalogImportService
{
  private readonly CatalogDbContext _db;
  private readonly IComponentEntryService _entryService;
  private readonly IComponentNameParser _nameParser;
  private readonly WordFormMapLoader _mapLoader;

  public WordCatalogImportService(
      CatalogDbContext db,
      IComponentEntryService entryService,
      IComponentNameParser nameParser,
      WordFormMapLoader mapLoader)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
    _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
  }

  public async Task<WordImportScanResult> ScanAsync(string documentPath, CancellationToken ct = default)
  {
    if (!File.Exists(documentPath))
      throw new FileNotFoundException("Document not found.", documentPath);

    var formNumbersInDoc = WordFormReader.DetectFormNumbers(documentPath);

    var allForms = await _db.Forms.Include(f => f.Parameters).ToListAsync(ct);
    var formsByNumber = allForms.ToDictionary(f => f.Number, StringComparer.OrdinalIgnoreCase);

    // ── Form 4 (family-level) — read once if present ──────────────────────
    WordFormData? form4Data = null;
    if (formsByNumber.ContainsKey("4") && TryLoadMap("4", out var form4Map))
      form4Data = WordFormReader.Read("4", documentPath, form4Map!);

    var componentTypes = await _db.ComponentTypes.ToListAsync(ct);
    var typesByName = componentTypes.ToDictionary(t => t.Name, StringComparer.Ordinal);

    var unsupported = new List<string>();
    var resolved = new List<WordImportedComponent>();

    foreach (var formNumber in formNumbersInDoc)
    {
      // Form 4 itself and the cover/TOC/changelog pages aren't own-forms.
      if (string.Equals(formNumber, "4", StringComparison.OrdinalIgnoreCase)) continue;

      if (!formsByNumber.TryGetValue(formNumber, out var form) || !TryLoadMap(formNumber, out var map))
      {
        unsupported.Add(formNumber);
        continue;
      }

      var formData = WordFormReader.Read(formNumber, documentPath, map!);

      foreach (var slot in formData.Components)
        resolved.Add(await ResolveComponentAsync(slot, form, form4Data, typesByName, ct));
    }

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
    item.ComponentTypeName = trimmedName;

    if (trimmedName.Length == 0)
    {
      item.ComponentTypeId = null;
      item.FamilyName = null;
      item.ExistingFamilyId = null;
      return;
    }

    var type = await _db.ComponentTypes.FirstOrDefaultAsync(t => t.Name == trimmedName, ct);
    item.ComponentTypeId = type?.Id;

    var parsed = _nameParser.Parse($"{trimmedName} {item.FullName}");
    var familyName = !string.IsNullOrWhiteSpace(parsed.Family) ? parsed.Family : parsed.Name;
    item.FamilyName = familyName;

    item.ExistingFamilyId = type is null
        ? null
        : (await _db.Families.FirstOrDefaultAsync(f => f.ComponentTypeId == type.Id && f.Name == familyName, ct))?.Id;
  }

  public async Task<Component> ImportAsync(WordImportedComponent item, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(item);

    if (string.IsNullOrWhiteSpace(item.ComponentTypeName))
      throw new InvalidOperationException($"Не указан тип компонента для «{item.FullName}».");

    // Re-resolve right before writing — covers both "the type came straight
    // from Form4 and was never touched by the review UI" and "the type text
    // was edited without a live re-resolve in between".
    await ResolveComponentTypeAsync(item, item.ComponentTypeName, ct);

    int componentTypeId;
    if (item.ComponentTypeId is { } existingTypeId)
    {
      componentTypeId = existingTypeId;
    }
    else
    {
      var newType = new ComponentType { Name = item.ComponentTypeName!.Trim() };
      _db.ComponentTypes.Add(newType);
      await _db.SaveChangesAsync(ct);
      componentTypeId = newType.Id;

      // A brand-new type has no existing Family under it by definition.
      item.ExistingFamilyId = null;
    }

    if (item.ExistingFamilyId is null && string.IsNullOrWhiteSpace(item.FamilyName))
      throw new InvalidOperationException($"Не указано семейство для «{item.FullName}».");

    var ownForm = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Id == item.OwnFormId, ct)
        ?? throw new InvalidOperationException($"Own form {item.OwnFormId} not found.");

    var ownFormRowToParamId = ownForm.Parameters.ToDictionary(p => p.RowNumber, p => p.Id);

    var input = new NewComponentInput
    {
      ComponentTypeId = componentTypeId,
      ExistingFamilyId = item.ExistingFamilyId,
      NewFamilyName = item.ExistingFamilyId is null ? item.FamilyName : null,
      FullName = item.FullName,
      Form4Values = await MapRowValuesToParamIdsAsync("4", item.Form4NtdValuesByRow, ct),
      OwnFormId = item.OwnFormId,
      OwnFormValues = MapRowValues(ownFormRowToParamId, item.OwnFormNtdValuesByRow),
      PinValues = MapRowValues(ownFormRowToParamId, item.OwnFormPinValuesByRow),
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
    var warnings = new List<string>();

    // Match this component's full designation against Form4's per-family
    // "Обозначение" slots by prefix — longest match wins in case more than
    // one Form4 slot's base designation happens to prefix-match. Two real
    // mismatches found on Ilya's own documents are compensated for:
    //  1. Look-alike/sound-alike characters (see NormalizeHomoglyphs) typed
    //     differently across the two form pages.
    //  2. Form4 sometimes includes a leading "ОС "/"ОСМ " (особая приёмка)
    //     quality-grade prefix that the own-form's "Наименование изделия"
    //     omits — tried as a fallback so the base designation still matches.
    var normalizedSlotName = NormalizeHomoglyphs(slot.Name);
    var form4Match = form4Data?.Components
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
        .FirstOrDefault();

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

      var parsed = _nameParser.Parse($"{typeName} {slot.Name}");
      familyName = !string.IsNullOrWhiteSpace(parsed.Family) ? parsed.Family : parsed.Name;

      if (componentTypeId is { } id)
      {
        var family = await _db.Families
            .FirstOrDefaultAsync(f => f.ComponentTypeId == id && f.Name == familyName, ct);
        existingFamilyId = family?.Id;
      }
    }

    var alreadyExists = await _db.Components.AnyAsync(c => c.FullName == slot.Name, ct);

    return new WordImportedComponent
    {
      FullName = slot.Name,
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
      Form4NtdValuesByRow = form4Match?.NtdValues ?? new Dictionary<int, string>(),
      OwnFormNtdValuesByRow = slot.NtdValues,
      OwnFormPinValuesByRow = slot.PinValues,
    };
  }

  private async Task<IReadOnlyDictionary<int, string>> MapRowValuesToParamIdsAsync(
      string formNumber, IReadOnlyDictionary<int, string> valuesByRow, CancellationToken ct)
  {
    if (valuesByRow.Count == 0) return new Dictionary<int, string>();

    var form = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Number == formNumber, ct);

    if (form is null) return new Dictionary<int, string>();

    var rowToParamId = form.Parameters.ToDictionary(p => p.RowNumber, p => p.Id);
    return MapRowValues(rowToParamId, valuesByRow);
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
