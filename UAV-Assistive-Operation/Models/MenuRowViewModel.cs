using System.ComponentModel;
using System.Runtime.CompilerServices;
using UAV_Assistive_Operation.Configuration;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Helpers;

namespace UAV_Assistive_Operation.Models
{
    public class MenuRowViewModel : INotifyPropertyChanged
    {
        public MenuRowOptions MenuOption { get; }
        public string DisplayName => MenuOption.GetDisplayName();
        public bool IsSimulatorRow => MenuOption == MenuRowOptions.simulatorMode;


        public bool IsToggleButton => MenuRules.Rules.TryGetValue(MenuOption, out var rule) && rule.IsToggleButton;
        public string ButtonText => MenuRules.Rules.TryGetValue(MenuOption, out var rule) ? rule.ButtonText : "Select";


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

        private bool _isPressed;
        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    OnPropertyChanged(nameof(IsPressed));
                }
            }
        }

        public MenuRowViewModel(MenuRowOptions menuOption)
        {
            MenuOption = menuOption;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
