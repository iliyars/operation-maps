using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Infrastructure.Commands
{
  public sealed class UndoRedoStack : ObservableObject
  {
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string UndoDescription => _undoStack.TryPeek(out var cmd) ? cmd.Description : "";
    public string RedoDescription => _redoStack.TryPeek(out var cmd) ? cmd.Description : "";


    public void Execute(IUndoableCommand command)
    {
      ArgumentNullException.ThrowIfNull(command);

      command.Execute();
      _undoStack.Push(command);
      _redoStack.Clear();

      NotifyChanged();
    }

    public bool Undo()
    {
      if (!CanUndo) return false;

      var command = _undoStack.Pop();
      command.Undo();
      _redoStack.Push(command);

      NotifyChanged();
      return true;
    }

    public bool Redo()
    {
      if (!CanRedo) return false;

      var command = _redoStack.Pop();
      command.Execute();
      _undoStack.Push(command);

      NotifyChanged();
      return true;
    }

    public void Clear()
    {
      _undoStack.Clear();
      _redoStack.Clear();
      NotifyChanged();
    }

    private void NotifyChanged()
    {
      OnPropertyChanged(nameof(CanUndo));
      OnPropertyChanged(nameof(CanRedo));
      OnPropertyChanged(nameof(UndoDescription));
      OnPropertyChanged(nameof(RedoDescription));
    }
  }
}
