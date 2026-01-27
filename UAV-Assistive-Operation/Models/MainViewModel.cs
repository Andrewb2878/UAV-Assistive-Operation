using System.Collections.ObjectModel;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class MainViewModel
    {
        public DJITelemetryService Telemetry => App.DJITelemetryService;
        public ObservableCollection<LogEntryModel> EventLog => EventLogService.Instance.LogEntries;
    }
}
