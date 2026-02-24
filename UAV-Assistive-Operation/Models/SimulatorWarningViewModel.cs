using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace UAV_Assistive_Operation.Models
{
    public class SimulatorWarningViewModel : INotifyPropertyChanged
    {
        //States
        private bool _simulatorActive = false;


        public bool MenuActive
        {
            get => _simulatorActive;
            set => Set(ref _simulatorActive, value);
        }

        private bool _isContinuePressed;
        public bool IsContinuePressed
        {
            get => _isContinuePressed;
            set => Set(ref _isContinuePressed, value);
        }


        //Selection processing
        public event Action CommandRequested;

        public async void Select()
        {
            IsContinuePressed = true;
            await Task.Delay(150);
            IsContinuePressed = false;

            CommandRequested?.Invoke();
            return;
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
    }
}
