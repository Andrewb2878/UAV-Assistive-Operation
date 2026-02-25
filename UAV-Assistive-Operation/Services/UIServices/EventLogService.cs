using System;
using System.Collections.ObjectModel;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    public class EventLogService
    {
        private static EventLogService _instance;


        public static EventLogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventLogService();
                }
                return _instance;
            }
        }

        public ObservableCollection<LogEntryModel> LogEntries { get;  } = new ObservableCollection<LogEntryModel>();


        private EventLogService() { }

        public void Log(LogEventType eventType, String message)
        {
            var entry = new LogEntryModel
            {
                Time = DateTime.Now,
                EventType = eventType,
                Message = message
            };

            _ = App.RunOnUIThread(() =>
                {
                    LogEntries.Add(entry);
                });
                
        }
    }
}
