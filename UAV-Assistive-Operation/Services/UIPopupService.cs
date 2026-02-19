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
        private ContentDialog _aircraftRequired;
        private ContentDialog _menu;

        private bool _allowClose = false;


        public void RegisterPopups(ContentDialog controllerRequired, ContentDialog controllerRemap,
                                     ContentDialog uavRequired, ContentDialog menu)
        {
            _controllerRequired = controllerRequired;
            _controllerRemap = controllerRemap;
            _aircraftRequired = uavRequired;
            _menu = menu;

            _controllerRequired.Closing += OnPopupClosing;
            _controllerRemap.Closing += OnPopupClosing;
            _aircraftRequired.Closing += OnPopupClosing;
            _menu.Closing += OnPopupClosing;
        }

        private void OnPopupClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (!_allowClose)
            {
                args.Cancel = true;
            }
        }

        public async void ShowPopup(UIPopups popup)
        {
            if (_currentPopup == popup)
                return;

            await HidePopup();
            _currentPopup = popup;
            _allowClose = false;

            await App.RunOnUIThread(() =>
            {
                GetPopup(popup)?.ShowAsync();
            });
        }

        public async Task HidePopup()
        {
            await App.RunOnUIThread(() =>
            {
                var popup = GetPopup(_currentPopup);
                if (popup != null)
                {
                    _allowClose = true;
                    popup.Hide();
                }                
                _currentPopup = UIPopups.None;
                _allowClose = false;
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
                case UIPopups.AircraftRequired:
                    return _aircraftRequired;
                case UIPopups.Menu:
                    return _menu;
                default: 
                    return null;
            };
        }
    }
}
