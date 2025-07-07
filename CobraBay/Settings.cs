namespace CobraBay.Properties
{
    // This class allows you to handle specific events on the settings class:
    // The SettingChanging event is raised before a setting's value is changed.
    // The PropertyChanged event is raised after a setting's value is changed.
    // The SettingsLoaded event is raised after the setting values are loaded.
    // The SettingsSaving event is raised before the setting values are saved.
    public sealed partial class Settings
    {
        public Settings()
        {
            // Uncomment these lines if you want to handle specific events.
            //this.SettingChanging += SettingChangingEventHandler;
            //this.SettingsSaving += SettingsSavingEventHandler;
        }

        // This method is called before a setting value is changed.
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) =>
            // Implement your code to handle the event here.
            System.Diagnostics.Debug.WriteLine($"Setting changing: {e.SettingName} to {e.NewValue}");

        // This method is called before the settings are saved.
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) =>
            // Implement your code to handle the event here.
            System.Diagnostics.Debug.WriteLine("Settings are being saved.");
    }
}
