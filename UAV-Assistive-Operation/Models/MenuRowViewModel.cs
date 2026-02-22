using System.ComponentModel;
using System.Runtime.CompilerServices;
using UAV_Assistive_Operation.Configuration;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Helpers;

namespace UAV_Assistive_Operation.Models
{
    public class MenuRowViewModel : INotifyPropertyChanged
    {
        public MenuOptions MenuOption { get; }
        public string DisplayName => MenuOption.GetDisplayName();
        public bool IsSimulatorRow => MenuOption == MenuOptions.simulatorMode;


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

        public MenuRowViewModel(MenuOptions menuOption)
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
