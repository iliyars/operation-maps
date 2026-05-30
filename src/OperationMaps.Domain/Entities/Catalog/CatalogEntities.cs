using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Domain.Entities.Catalog;

public class ComponentType
{
  public int Id { get; set; }
  public string Name { get; set; } = "";

  public List<Family> Families { get; set; } = new();
  public List<FamilyParsingRule> ParsingRules { get; set; } = new();
}

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ComponentTypeId { get; set; }
    public ComponentType ComponentType { get; set; } = null!;

    public List<Component> Components { get; set; } = new();
    public List<FamilyForm> FamilyForms { get; set; } = new();
    public List<FamilyNtdValue> NtdValues { get; set; } = new();
    public List<FamilyNote> FamilyNotes { get; set; } = new();
}

public class FamilyForm
{
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;

    public int FormId { get; set; }
    public Form Form { get; set; } = null!;
}



public class FamilyParsingRule
{
  public int Id { get; set; }
  public int ComponentTypeId { get; set; }
  public ComponentType ComponentType { get; set; } = null!;
  public int Priority { get; set; }
  public string Pattern { get; set; } = "";
  public string? Example { get; set; }
}

public class Component
{
  public int Id { get; set; }
  public string FullName { get; set; } = "";
  public string? Designation { get; set; }
  public int FamilyId { get; set; }
  public Family Family { get; set; } = null!;

  public List<ComponentNtdValue> NtdValues { get; set; } = new();
  public List<ComponentPinValue> PinValues { get; set; } = new();
  public List<ComponentNote> ComponentNotes { get; set; } = new();
}

public class FamilyNtdValue
{
  public int Id { get; set; }
  public int FamilyId { get; set; }
  public Family Family { get; set; } = null!;
  public int FormParameterId { get; set; }
  public FormParameter FormParameter { get; set; } = null!;
  public string Value { get; set; } = "";
}

public class ComponentNtdValue
{
  public int Id { get; set; }
  public int ComponentId { get; set; }
  public Component Component { get; set; } = null!;
  public int FormParameterId { get; set; }
  public FormParameter FormParameter { get; set; } = null!;
  public string Value { get; set; } = "";
}

public class ComponentPinValue
{
  public int Id { get; set; }
  public int ComponentId { get; set; }
  public Component Component { get; set; } = null!;
  public int FormParameterId { get; set; }
  public FormParameter FormParameter { get; set; } = null!;
  public string Pins { get; set; } = "";
}

// ── Примечания ───────────────────────────────────────────────────────────────

/// <summary>Справочник переиспользуемых примечаний.</summary>
public class Note
{
  public int Id { get; set; }
  public string Text { get; set; } = "";

  public List<FamilyNote> FamilyNotes { get; set; } = new();
  public List<ComponentNote> ComponentNotes { get; set; } = new();
}

/// <summary>
/// Привязка примечания к конкретному параметру семейства.
/// PK: (FamilyId, FormParameterId, NoteId).
/// </summary>
public class FamilyNote
{
  public int FamilyId { get; set; }
  public Family Family { get; set; } = null!;

  public int FormParameterId { get; set; }          // ★ к какому параметру
  public FormParameter FormParameter { get; set; } = null!;

  public int NoteId { get; set; }
  public Note Note { get; set; } = null!;
}

/// <summary>
/// Привязка примечания к конкретному параметру компонента.
/// PK: (ComponentId, FormParameterId, NoteId).
/// </summary>
public class ComponentNote
{
  public int ComponentId { get; set; }
  public Component Component { get; set; } = null!;

  public int FormParameterId { get; set; }          // ★ к какому параметру
  public FormParameter FormParameter { get; set; } = null!;

  public int NoteId { get; set; }
  public Note Note { get; set; } = null!;
}
