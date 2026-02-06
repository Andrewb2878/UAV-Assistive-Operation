using System;
using System.Collections.ObjectModel;
using System.Linq;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class MainViewModel
    {
        public ObservableCollection<ControlRemapRowViewModel> RemapRows { get; }

        private int _currentRemapIndex = 0;


        public MainViewModel()
        {
            RemapRows = new ObservableCollection<ControlRemapRowViewModel>(
                Enum.GetValues(typeof(ApplicationControls)).Cast<ApplicationControls>()
                    .Select(control => new ControlRemapRowViewModel(control)));

            HighlightCurrentRow();

        }

        private void HighlightCurrentRow()
        {
            for (int index = 0;  index < RemapRows.Count; index++)
                RemapRows[index].IsHighlighted = index == _currentRemapIndex;
        }

        public ControlRemapRowViewModel CurrentRow =>
            _currentRemapIndex < RemapRows.Count ? RemapRows[_currentRemapIndex] : null;

        public bool AdvanceToNext()
        {
            if (_currentRemapIndex >= RemapRows.Count - 1)
                return false;

            _currentRemapIndex++;
            HighlightCurrentRow();
            return true;
        }

        public AlertService Alerts => App.AlertService;
        public DJITelemetryService Telemetry => App.DJITelemetryService;
        public ObservableCollection<LogEntryModel> EventLog => EventLogService.Instance.LogEntries;
    }
}
