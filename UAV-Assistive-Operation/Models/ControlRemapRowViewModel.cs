using System.ComponentModel;
using System.Runtime.CompilerServices;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Helpers;

namespace UAV_Assistive_Operation.Models
{
    public class ControlRemapRowViewModel : INotifyPropertyChanged
    {
        public ApplicationControls Controls { get; }

        public string DisplayName => Controls.GetDisplayName();

        private string _assignedInput;
        public string AssignedInput
        {
            get => _assignedInput;
            set
            {
                _assignedInput = value;
                OnPropertyChanged();
            }
        }

        private string _error;
        public string Error 
        {
            get => _error;
            set
            {
                _error = value;
                OnPropertyChanged();
            }
        }

        private bool _isHighlighted;
        public bool IsHighlighted 
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                OnPropertyChanged();
            }
        }

        public ControlRemapRowViewModel(ApplicationControls controls)
        {
            Controls = controls;
            AssignedInput = "Waiting for input...";
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
