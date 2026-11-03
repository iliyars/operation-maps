using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Domain.Entities.Catalog;

public sealed class ComponentRegime
{
  public int Id { get; set; }
  public int ComponentId { get; set; }
  public Component Component { get; set; } = null!;
  public string Name { get; set; } = "";
  public string Condition { get; set; } = "";
  public List<ComponentRegimeValue> Values { get; set; } = [];
}

public sealed class ComponentRegimeValue
{
  public int ComponentRegimeId { get; set; }
  public ComponentRegime ComponentRegime { get; set; } = null!;
  public int FormParameterId { get; set; }
  public FormParameter FormParameter { get; set; } = null!;
  public string Value { get; set; } = "";
}
