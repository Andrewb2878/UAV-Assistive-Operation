using DJI.WindowsSDK;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class FlightModeModel : INotifyPropertyChanged
    {
        private FCFlightMode? _flightMode;


        public FCFlightMode? FlightMode
        {
            get => _flightMode; 
            set
            {
                if (_flightMode != value)
                {
                    _flightMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        public string DisplayText
        {
            get
            {
                if (!FlightMode.HasValue)
                    return "---";

                switch (FlightMode.Value)
                {
                    case FCFlightMode.MANUAL:
                        return "Manual mode";
                    case FCFlightMode.ATTI:
                        return "Attitude mode";
                    case FCFlightMode.ATTI_CL:
                        return "Attitude course-lock mode";
                    case FCFlightMode.ATTI_HOVER:
                        return "Attitude hover mode";
                    case FCFlightMode.HOVER:
                        return "Hover mode";
                    case FCFlightMode.GPS_BRAKE:
                        return "Brake mode";
                    case FCFlightMode.GPS_ATTI:
                        return "GPS Attitude mode";
                    case FCFlightMode.GPS_CL:
                        return "GPS course-lock mode";
                    case FCFlightMode.GPS_HOMELOCK:
                        return "GPS home-lock mode";
                    case FCFlightMode.GPS_HOTPOINT:
                        return "GPS hotpoint mode";
                    case FCFlightMode.ASSISTED_TAKE_OFF:
                        return "Assisted takeoff mode";
                    case FCFlightMode.AUTO_TAKE_OFF:
                        return "Auto takeoff mode";
                    case FCFlightMode.AUTO_LANDING:
                        return "Auto landing mode";
                    case FCFlightMode.ATTI_LANDING:
                        return "Attitude landing mode";
                    case FCFlightMode.NAVI_GO:
                        return "GPS waypoint mode";
                    case FCFlightMode.GO_HOME:
                        return "Go home mode";
                    case FCFlightMode.JOYSTICK:
                        return "Joystick mode";
                    case FCFlightMode.CINEMATIC:
                        return "Cinematic mode";
                    case FCFlightMode.ATTI_LIMITED:
                        return "Attitude fly limited mode";
                    case FCFlightMode.DRAW:
                        return "Draw mode";
                    case FCFlightMode.FOLLOW_ME:
                        return "Follow-me mode";
                    case FCFlightMode.ACTIVE_TRACK:
                        return "ActiveTrack mode";
                    case FCFlightMode.TAP_FLY:
                        return "TapFly mode";
                    case FCFlightMode.PANO:
                        return "Pano mode";
                    case FCFlightMode.FARMING:
                        return "Farming mode";
                    case FCFlightMode.FPV:
                        return "FPV mode";
                    case FCFlightMode.GPS_SPORT:
                        return "Sport mode";
                    case FCFlightMode.GPS_NOVICE:
                        return "Novice mode";
                    case FCFlightMode.CONFIRM_LANDING:
                        return "Confirm landing mode";
                    case FCFlightMode.NOE:
                        return "NOE mode";
                    case FCFlightMode.GESTURE_CONTROL:
                        return "Gesture control mode";
                    case FCFlightMode.TRIPOD_GPS:
                        return "Tripod mode";
                    case FCFlightMode.ACTIVE_TRACK_COURSE_LOCK:
                        return "ActiveTrack course-lock mode";
                    case FCFlightMode.MOTOR_START:
                        return "Motors just started";
                    case FCFlightMode.FIXED_WING:
                        return "Fixed wing mode";
                    case FCFlightMode.APAS:
                        return "APAS mode";
                    case FCFlightMode.PALM_LAUNCH:
                        return "Palm launch mode";
                    case FCFlightMode.TIME_LAPSE:
                        return "Time-Lapse";
                    case FCFlightMode.UNKNOWN:
                        return "Unknown";
                    default:
                        return "---";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
