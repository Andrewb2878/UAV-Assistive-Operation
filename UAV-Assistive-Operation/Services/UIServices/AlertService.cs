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

        //Current aircraft flight status - displayed when no alerts are active or alert cycling
        private  string _flightStatus = "---";

        private bool _showAlert = true;
        

        public event PropertyChangedEventHandler PropertyChanged;


        /// <summary>
        /// Manages the alert banner in the application UI
        /// 
        /// Tracks all active alerts
        /// Determines the visibility of alerts based on priority
        /// 
        /// Alerts with lower priority numbers act as higher priority (0 higher priority than 1) 
        /// </summary>
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

        //Current message that should be displayed in the alert banner
        public string DisplayMessage => CalculateDisplayMessage();

        //Used to tell if any alerts are currently active
        public bool AlertBannerActive => _activeAlerts.Any();


        /// <summary>
        /// Updates the current aircraft flight status
        /// </summary>
        /// <param name="status">Current flight state</param>
        public void FlightStatus(string status)
        {
            _flightStatus = status;
            NotifyUI();
        }

        /// <summary>
        /// Adds or removes an alert from the active alert list
        /// </summary>
        /// <param name="alertId">Unique alert identifier</param>
        /// <param name="alertActive">Controls the visibility of the alert, false to remove an existing alert</param>
        /// <param name="message">Alert message to display</param>
        /// <param name="priority">Alert priority (lower value = higher priority)</param>
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
                //Remove alert if it exists
                if (_activeAlerts.Remove(alertId))
                {
                    changed = true;
                }
            }

            //Update UI and timer if alert state changed
            if (changed)
            {
                TimerState();
                NotifyUI();
            }       
        }

        /// <summary>
        /// Clears active alerts
        /// </summary>
        public void ClearAlerts()
        {
            if (!_activeAlerts.Any())
                return;
            
            _activeAlerts.Clear();
            _ = App.RunOnUIThread(() =>
            {
                if (_cycleTimer.IsEnabled)
                    _cycleTimer.Stop();

                _showAlert = true;
            });
            NotifyUI();
        }

        /// <summary>
        /// Calculates which message should be shown in the alert banner.
        /// 
        /// No alerts - displays flight status
        /// Critical alert - always show alert message
        /// Non-critical alert - alternate between flight state and alert
        /// </summary>
        /// <returns></returns>
        private string CalculateDisplayMessage()
        {
            if (!_activeAlerts.Any())
                return _flightStatus;

            var topAlert = _activeAlerts.Values.OrderBy(a => a.Priority).First();

            if (topAlert.IsCritical)
                return topAlert.Message;

            return _showAlert ? topAlert.Message : _flightStatus;
        }

        /// <summary>
        /// Controls the timer, only running if there is an active non-critical alert
        /// </summary>
        private void TimerState()
        {
            _ = App.RunOnUIThread(() =>
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
