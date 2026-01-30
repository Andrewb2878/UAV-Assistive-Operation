using System.Collections.ObjectModel;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class MainViewModel
    {

        public AlertService Alerts => App.AlertService;
        public DJITelemetryService Telemetry => App.DJITelemetryService;
        public ObservableCollection<LogEntryModel> EventLog => EventLogService.Instance.LogEntries;
    }
}
