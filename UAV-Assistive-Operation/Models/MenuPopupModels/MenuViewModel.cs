using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;

namespace UAV_Assistive_Operation.Models
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        //States
        private bool _menuActive = false;
        private int _currentIndex = 0;
        public MenuRowViewModel SimulatorRow => GetRow(MenuRowOptions.simulatorMode);

        //Properties
        public ObservableCollection<MenuRowViewModel> MenuRows { get; }


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

        public int SelectedIndex
        {
            get => _currentIndex;
            set
            {
                if (Set(ref _currentIndex, value))
                    HighlightCurrent();
            }
        }

        public MenuRowViewModel GetRow(MenuRowOptions option)
        {
            return MenuRows.FirstOrDefault(row => row.MenuOption == option);
        }


        //Initialization
        public MenuViewModel() 
        {
            var options = Enum.GetValues(typeof(MenuRowOptions)).Cast<MenuRowOptions>();
            MenuRows = new ObservableCollection<MenuRowViewModel>(options.Select(option => new MenuRowViewModel(option)));

            HighlightCurrent();
        }


        //Navigation methods
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
            ExitError = null;

            OnPropertyChanged(nameof(IsExitHighlighted));
        }


        //Exit button methods
        private bool _isExitPressed;
        public bool IsExitPressed
        {
            get => _isExitPressed;
            set => Set(ref _isExitPressed, value);
        }

        private string _exitError;
        public string ExitError
        {
            get => _exitError;
            set => Set(ref _exitError, value);
        }


        //Error handling
        public void SetRowError(int index, string message)
        {
            if (index < 0 || index > MenuRows.Count)
            {
                return;
            }
            else if (index == MenuRows.Count)
            {
                ExitError = message;
                return;
            }
            MenuRows[index].Error = message;
        }


        public bool IsExitHighlighted => SelectedIndex == MenuRows.Count;


        //Selection processing
        public event Action<MenuCommand, int> CommandRequested;

        public async void Select()
        {
            //Handling exit button
            if (SelectedIndex == MenuRows.Count)
            {
                IsExitPressed = true;
                await Task.Delay(150);
                IsExitPressed = false;

                CommandRequested?.Invoke(MenuCommand.ExitApplication, SelectedIndex);
                return;
            }

            //Handling menu rows
            var row = MenuRows[SelectedIndex];
            row.Error = null;

            row.IsPressed = true;
            await Task.Delay(150);
            row.IsPressed = false;

            switch (SelectedIndex)
            {
                case 0:
                    CommandRequested?.Invoke(MenuCommand.ReconfigureController, SelectedIndex); break;
                case 1:
                    CommandRequested?.Invoke(MenuCommand.ToggleSimulator, SelectedIndex); break;
            }
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
