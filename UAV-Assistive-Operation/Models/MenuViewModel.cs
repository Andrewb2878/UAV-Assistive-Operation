using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private readonly DJIFlightDataService _flightDataService;

        private bool _menuActive = false;
        private bool _simulatorEnabled;
        private int _currentIndex = 0;

        public ObservableCollection<MenuRowViewModel> MenuRows { get; }


        public MenuViewModel(DJIFlightDataService flightDataService) 
        {
            _flightDataService = flightDataService;

            var options = Enum.GetValues(typeof(MenuOptions)).Cast<MenuOptions>();
            MenuRows = new ObservableCollection<MenuRowViewModel>(options.Select(option => new MenuRowViewModel(option)));

            HighlightCurrent();
        }


        public bool MenuActive
        {
            get => _menuActive;
            set
            {
                if (Set(ref _menuActive, value) && !value)
                {
                    foreach (var row in MenuRows)
                        row.Error = null;
                }
            }
            
        }

        public string ToggleButtonText => IsSimulatorEnabled ? "Disable" : "Enable";

        public bool IsSimulatorEnabled
        {
            get => _simulatorEnabled;
            set
            {
                if (Set(ref _simulatorEnabled, value))
                    OnPropertyChanged(nameof(ToggleButtonText));
            }
        }

        public int SelectedIndex
        {
            get => _currentIndex;
            set
            {
                if (Set(ref _currentIndex, value))
                    HighlightCurrent();
            }
        }

        public void MoveUp()
        {
            if (SelectedIndex > 0)
                SelectedIndex--;
        }

        public void MoveDown()
        {
            if (SelectedIndex < MenuRows.Count)
                SelectedIndex++;
        }

        private void HighlightCurrent()
        {
            for (int index = 0; index < MenuRows.Count; index++)
            {
                MenuRows[index].IsHighlighted = (index == SelectedIndex);

                if (index != SelectedIndex)
                    MenuRows[index].Error = null;
            }
            OnPropertyChanged(nameof(IsExitHighlighted));
            OnPropertyChanged(nameof(ExitError));
        }

        public bool CanExit => !_flightDataService.IsFlying;
        public string ExitError => _flightDataService.IsFlying ? "Cannot exit while aircraft is flying." : null;
        public bool IsExitHighlighted => SelectedIndex == MenuRows.Count;


        public event Action<int> ItemSelected;

        public void Select()
        {
            if (SelectedIndex == MenuRows.Count)
            {
                if (CanExit)
                {
                    Environment.Exit(0);
                }
                return;
            }

            var row = MenuRows[SelectedIndex];
            row.Error = null;

            if (_flightDataService.IsFlying)
            {
                row.Error = $"Cannot {row.DisplayName} during flight";
                return;
            }

            if (row.MenuOption == MenuOptions.simulatorMode)
            {
                IsSimulatorEnabled = !IsSimulatorEnabled;
            }

            ItemSelected?.Invoke(SelectedIndex);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
