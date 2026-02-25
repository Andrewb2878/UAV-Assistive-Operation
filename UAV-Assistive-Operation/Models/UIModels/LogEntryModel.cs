using System;
using UAV_Assistive_Operation.Enums;

namespace UAV_Assistive_Operation.Models
{
    public class LogEntryModel
    {
        public DateTime Time { get; set; }
        public LogEventType EventType { get; set; }
        public string Message { get; set; }

        public string TimeDisplay => Time.ToString("HH:mm:ss");
    }
}
