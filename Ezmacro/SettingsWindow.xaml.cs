using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SharpHook.Data;

namespace Ezmacro
{
    public partial class SettingsWindow : Window
    {
        private MacroSettings _settings;

        public SettingsWindow(MacroSettings currentSettings)
        {
            InitializeComponent();
            _settings = currentSettings;

            // Populate the dropdown boxes with all available keyboard keys
            var allKeys = Enum.GetValues(typeof(KeyCode));
            
            ComboRecordKey.ItemsSource = allKeys;
            ComboPlayKey.ItemsSource = allKeys;

            // Select the keys currently stored in our settings state
            ComboRecordKey.SelectedItem = _settings.RecordStopKey;
            ComboPlayKey.SelectedItem = _settings.PlaybackKey;
            CheckBoxMouseRecording.IsChecked = _settings.MousetrackingEnabled;
            SliderSampling.Value = _settings.Samplingrate;
            CheckBoxShowMouse.IsChecked = !_settings.HideMouseMoves;
            CheckBoxContinuosPlayback.IsChecked = _settings.ContinuosPlayback;
            CheckBoxMinimize.IsChecked = _settings.AutoMinimize;
            CheckBoxHideGlow.IsChecked = _settings.HideGlow;
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Save choices back to the configuration object
            _settings.RecordStopKey = (KeyCode)ComboRecordKey.SelectedItem;
            _settings.PlaybackKey = (KeyCode)ComboPlayKey.SelectedItem;
            _settings.MousetrackingEnabled = CheckBoxMouseRecording.IsChecked ?? true;
            _settings.Samplingrate = (long)SliderSampling.Value;
            _settings.HideMouseMoves = !(CheckBoxShowMouse.IsChecked ?? true);
            _settings.ContinuosPlayback = CheckBoxContinuosPlayback.IsChecked ?? true;
            _settings.AutoMinimize = CheckBoxMinimize.IsChecked ?? true;
            _settings.HideGlow = CheckBoxHideGlow.IsChecked ?? true;

            _settings.Save(); // Persist the settings to disk



            this.DialogResult = true; // Closes the window
            CollectionViewSource.GetDefaultView(((MainWindow)Owner).MacroActions).Refresh(); // Refresh the DataGrid filter in the main window
        }
    }
}
