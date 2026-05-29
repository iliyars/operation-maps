using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Infrastructure.Commands
{
  public interface IUndoableCommand
  {
    string Description { get; }

    void Execute();
    void Undo();
  }
}
