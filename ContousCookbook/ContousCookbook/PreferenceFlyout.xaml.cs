using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace ContousCookbook
{
    public sealed partial class PreferenceFlyout : SettingsFlyout
    {
        public PreferenceFlyout()
        {
            this.InitializeComponent();
            // Initialize the ToggleSwitch from roaming settings
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("Remember"))
                Remember.IsOn = (bool)ApplicationData.Current.RoamingSettings.Values["Remember"];

        }
        private void OnToggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.RoamingSettings.Values["Remember"] = Remember.IsOn;
        }
    }
}
