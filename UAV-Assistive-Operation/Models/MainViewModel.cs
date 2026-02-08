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
            int nextIndex = _currentRemapIndex + 1;
            
            while (nextIndex < RemapRows.Count && RemapRows[nextIndex].AssignedInput != ControlRemapRowViewModel.DefaultWaitingText)
                nextIndex++;

            if (nextIndex >= RemapRows.Count)
                return false;

            _currentRemapIndex = nextIndex;
            HighlightCurrentRow();
            return true;
        }

        public AlertService Alerts => App.AlertService;
        public DJITelemetryService Telemetry => App.DJITelemetryService;
        public ObservableCollection<LogEntryModel> EventLog => EventLogService.Instance.LogEntries;
    }
}
