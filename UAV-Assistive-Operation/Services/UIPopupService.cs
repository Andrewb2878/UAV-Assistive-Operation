using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using Windows.UI.Xaml.Controls;

namespace UAV_Assistive_Operation.Services
{
    internal class UIPopupService
    {
        private UIPopups _currentPopup = UIPopups.None;

        private ContentDialog _controllerRequired;
        private ContentDialog _controllerRemap;
        private ContentDialog _uavRequired;


        public void RegisterPopups(ContentDialog controllerRequired, ContentDialog controllerRemap,
                                     ContentDialog uavRequired)
        {
            _controllerRequired = controllerRequired;
            _controllerRemap = controllerRemap;
            _uavRequired = uavRequired;
        }

        public async void ShowPopup(UIPopups popup)
        {
            if (_currentPopup == popup)
                return;

            await HidePopup();
            _currentPopup = popup;
            await App.RunOnUIThread(() =>
            {
                GetPopup(popup)?.ShowAsync();
            });
        }

        public async Task HidePopup()
        {
            await App.RunOnUIThread(() =>
            {
                GetPopup(_currentPopup)?.Hide();
                _currentPopup = UIPopups.None;
            });
        }

        private ContentDialog GetPopup(UIPopups popup)
        {
            switch (popup)
            {
                case UIPopups.ControllerRequired:
                    return _controllerRequired;
                case UIPopups.ControllerRemapping:
                    return _controllerRemap;
                case UIPopups.UAVRequired:
                    return _uavRequired;
                default: 
                    return null;
            };
        }
    }
}
