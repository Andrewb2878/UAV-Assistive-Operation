using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class ControllerConfigurationViewModel : INotifyPropertyChanged
    {
        //Services
        private readonly ControllerMappingService _mappingService;

        //States
        private int _currentRemapIndex = 0;
        public int TotalControls => RemapRows.Count;
        public int ConfiguredControls => RemapRows.Count(rows => rows.AssignedInput != ControllerConfigurationRowViewModel.DefaultWaitingText);
        public bool IsFullyRemapped => _mappingService.IsFullyRemapped;

        //Properties
        public ObservableCollection<ControllerConfigurationRowViewModel> RemapRows { get; }
        public ControllerConfigurationRowViewModel CurrentRow => _currentRemapIndex < RemapRows.Count ? RemapRows[_currentRemapIndex] : null;


        //Initialization
        public ControllerConfigurationViewModel(ControllerMappingService mappingService)
        {
            _mappingService = mappingService;
            _mappingService.RemappingStateChanged += RemappingStateChanged;

            var controls = Enum.GetValues(typeof(ApplicationControls)).Cast<ApplicationControls>();
            RemapRows = new ObservableCollection<ControllerConfigurationRowViewModel>(
                controls.Select(control => new ControllerConfigurationRowViewModel(control)));

            HighlightCurrentRow();
        }


        //Reset method
        public void Reset()
        {
            _currentRemapIndex = 0;

            foreach (var row in RemapRows) 
            {
                row.AssignedInput = ControllerConfigurationRowViewModel.DefaultWaitingText;
                row.Error = null;
                row.IsHighlighted = false;
            }
            OnPropertyChanged(nameof(CurrentRow));
            OnPropertyChanged(nameof(ProgressText));
            HighlightCurrentRow();
        }


        //Input handling
        public bool HandleInput(InputBindingModel binding)
        {
            var row = CurrentRow;
            if (row == null)
                return true;

            if (_mappingService.TryAssignBinding(row.Controls, binding, out string error, out var autoAssigned))
            {
                row.AssignedInput = _mappingService.DescribeBinding(binding);
                row.Error = null;

                if (autoAssigned.HasValue)
                {
                    var oppositeBinding = new InputBindingModel
                    {
                        Type = binding.Type,
                        Index = binding.Index,
                        Polarity = binding.Polarity,
                        Direction = -binding.Direction,
                    };

                    _mappingService.TryAssignBinding(autoAssigned.Value, oppositeBinding, out _, out _);

                    var autoRow = RemapRows.FirstOrDefault(r => r.Controls == autoAssigned.Value);
                    if (autoRow != null)
                    {
                        autoRow.AssignedInput = _mappingService.DescribeBinding(oppositeBinding);
                        autoRow.Error = null;
                    }
                }
                return !AdvanceToNext();
            }

            row.Error = error;
            return false;
        }


        //Navigation methods
        private bool AdvanceToNext()
        {
            int nextIndex = _currentRemapIndex + 1;
            while (nextIndex < RemapRows.Count && RemapRows[nextIndex].AssignedInput != ControllerConfigurationRowViewModel.DefaultWaitingText)
                nextIndex++;

            if (nextIndex >= RemapRows.Count)
                return false;

            _currentRemapIndex = nextIndex;

            OnPropertyChanged(nameof(CurrentRow));
            OnPropertyChanged(nameof(ProgressText));
            HighlightCurrentRow();
            return true;
        }

        private void HighlightCurrentRow()
        {
            foreach (var row in RemapRows)
                row.IsHighlighted = row == CurrentRow;
        }

        private void RemappingStateChanged()
        {
            OnPropertyChanged(nameof(IsFullyRemapped));
        }


        //UI
        public string ProgressText => $"{ConfiguredControls} / {TotalControls} controls configured";


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
