using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Wpf.Features.Components.Commands
{
    public class MergeComponentsCommand : IUndoableCommand
    {
        private readonly ObservableCollection<ProjectComponentVm> _list;
        private readonly ProjectComponentVm _first;
        private readonly ProjectComponentVm _second;

        private ProjectComponentVm? _merged;
        private int _firstIndex;
        private int _secondIndex;

        public string Description => $"Объединить {_first.Name} + {_second.Name}";

        public MergeComponentsCommand(
            ObservableCollection<ProjectComponentVm> list,
            ProjectComponentVm first,
            ProjectComponentVm second)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public void Execute()
        {
            _firstIndex = _list.IndexOf(_first);
            _secondIndex = _list.IndexOf(_second);

            var mergedPositions = _first.Entry.Imported.Positions
                .Concat(_second.Entry.Imported.Positions)
                .Distinct()
                .OrderBy(p => p, PositionComparer.Instance)
                .ToList();

            var mergedImported = new ImportedComponent
            {
                RawName = _first.Entry.Imported.RawName,
                DetectedCategory = _first.Entry.Imported.DetectedCategory,
                Positions = mergedPositions,
                RawPositions = string.Join(", ", mergedPositions),
            };

            var mergedEntry = new ComponentMatchEntry
            {
                Imported = mergedImported,
                MatchResult = _first.Entry.MatchResult,
            };

            _merged = new ProjectComponentVm(mergedEntry);

            var insertAt = Math.Min(_firstIndex, _secondIndex);
            _list.Remove(_first);
            _list.Remove(_second);
            _list.Insert(insertAt, _merged);
        }

        public void Undo()
        {
            if (_merged is null) return;

            var index = _list.IndexOf(_merged);
            _list.Remove(_merged);

            var insertFirst = Math.Min(_firstIndex, _secondIndex);
            var insertSecond = Math.Max(_firstIndex, _secondIndex);

            _list.Insert(insertFirst, _firstIndex < _secondIndex ? _first : _second);
            _list.Insert(insertSecond, _secondIndex > _firstIndex ? _second : _merged);
        }
    }
}
