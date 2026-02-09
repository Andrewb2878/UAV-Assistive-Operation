using System.Collections.ObjectModel;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class MainViewModel
    {
        public ControllerConfigurationViewModel ControllerConfiguration { get; }

        public AlertService Alerts => App.AlertService;
        public DJITelemetryService Telemetry => App.DJITelemetryService;
        public ObservableCollection<LogEntryModel> EventLog => EventLogService.Instance.LogEntries;


        public MainViewModel(ControllerMappingService mappingService)
        {
            ControllerConfiguration = new ControllerConfigurationViewModel(mappingService);
        }
    }
}
