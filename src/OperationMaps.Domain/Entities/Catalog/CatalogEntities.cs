using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Domain.Entities.Catalog;

public class ComponentType
{
  public int Id { get; set; }
  public string Name { get; set; } = "";

  public List<TypeForm> TypeForms { get; set; } = new();
  public List<Family> Families { get; set; } = new();
  public List<FamilyParsingRule> ParsingRules { get; set; } = new();
}

public class TypeForm
{
  public int ComponentTypeId { get; set; }
  public ComponentType ComponentType { get; set; } = null!;
  public int FormId { get; set; }
  public Form Form { get; set; } = null!;
}

public class Family
{
  public int Id { get; set; }
  public string Name { get; set; } = "";
  public int ComponentTypeId { get; set; }
  public ComponentType ComponentType { get; set; } = null!;

  public List<Component> Components { get; set; } = new();
  public List<FamilyNtdValue> NtdValues { get; set; } = new();
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
