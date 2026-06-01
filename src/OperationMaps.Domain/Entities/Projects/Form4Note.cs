using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Domain.Entities.Projects
{
  public class Form4Note
  {
    public int Id { get; set; }

    /// <summary>Set for RLC groups (grouped by family).</summary>
    public int? FamilyId { get; set; }

    /// <summary>Set for component groups (grouped by full name).</summary>
    public int? ComponentId { get; set; }

    public int FormParameterId { get; set; }
    public FormParameter FormParameter { get; set; } = null!;

    /// <summary>Note text, e.g. "Обеспечивается конструкцией".</summary>
    public string NoteText { get; set; } = "";

    /// <summary>
    /// Display order within this group's parameter.
    /// 1 = *, 2 = **, 3 = ***.
    /// </summary>
    public int Order { get; set; }
  }
}
