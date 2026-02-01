using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UAV_Assistive_Operation.Models;
using Windows.UI.Xaml;

namespace UAV_Assistive_Operation.Services
{
    public class AlertService : INotifyPropertyChanged
    {
        private readonly Dictionary<string, ActiveAlertModel> _activeAlerts = new Dictionary<string, ActiveAlertModel>();
        private readonly DispatcherTimer _cycleTimer;
        private  string _flightStatus = "---";
        private bool _showAlert = true;
        

        public event PropertyChangedEventHandler PropertyChanged;


        public AlertService() 
        { 
            _cycleTimer = new DispatcherTimer 
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _cycleTimer.Tick += (sender, args) =>
            {
                _showAlert = !_showAlert;
                NotifyUI();
            };
        }

        public string DisplayMessage => CalculateDisplayMessage();

        public bool AlertBannerActive => _activeAlerts.Any();


        public void FlightStatus(string status)
        {
            _flightStatus = status;
            NotifyUI();
        }

        public void AlertState(string alertId, bool alertActive, string message, int priority)
        {
            bool changed = false;
            if (alertActive)
            {
                if (!_activeAlerts.ContainsKey(alertId))
                {
                    _activeAlerts[alertId] = new ActiveAlertModel { Message = message, Priority = priority };
                    changed = true;
                }
            }
            else
            {
                if (_activeAlerts.Remove(alertId))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                TimerState();
                NotifyUI();
            }       
        }

        public void ClearAlerts()
        {
            if (!_activeAlerts.Any())
                return;
            
            _activeAlerts.Clear();
            App.RunOnUIThread(() =>
            {
                if (_cycleTimer.IsEnabled)
                    _cycleTimer.Stop();

                _showAlert = true;
            });
            NotifyUI();
        }


        private string CalculateDisplayMessage()
        {
            if (!_activeAlerts.Any())
                return _flightStatus;

            var topAlert = _activeAlerts.Values.OrderBy(a => a.Priority).First();

            if (topAlert.IsCritical)
                return topAlert.Message;

            return _showAlert ? topAlert.Message : _flightStatus;
        }

        private void TimerState()
        {
            App.RunOnUIThread(() =>
            {
                bool nonCriticalAlert = _activeAlerts.Values.Any(a => !a.IsCritical);

                if (nonCriticalAlert && !_cycleTimer.IsEnabled)
                {
                    _showAlert = true;
                    _cycleTimer.Start();
                }
                else if (!nonCriticalAlert && _cycleTimer.IsEnabled)
                {
                    _cycleTimer.Stop();
                    _showAlert = true;
                }
            });
        }

        private async void NotifyUI()
        {
            if (App.UIDispatcher == null)
                return;

            if (App.UIDispatcher.HasThreadAccess)
            {
                RaisePropertyChanged();
            }
            else
            {
                await App.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, RaisePropertyChanged);
                
            }
        }

        private void RaisePropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayMessage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlertBannerActive)));
        }
    }
}
