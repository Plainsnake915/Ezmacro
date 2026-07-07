using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using SharpHook;

namespace Ezmacro
{
    public partial class MainWindow : Window
    {
        // An ObservableCollection automatically updates the DataGrid UI when items change
        public ObservableCollection<MacroEvent> MacroActions { get; set; } = new ObservableCollection<MacroEvent>();
        private TaskPoolGlobalHook _hook;
        private Stopwatch _stopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            // Bind our collection to the DataGrid UI
            MacroDataGrid.ItemsSource = MacroActions;
        }

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            BtnRecord.IsEnabled = false;
            BtnStop.IsEnabled = true;
            MacroActions.Clear();
            _stopwatch.Restart();

            // TODO: Start SharpHook global listening here
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            BtnRecord.IsEnabled = true;
            BtnStop.IsEnabled = false;
            _stopwatch.Stop();

            // TODO: Stop SharpHook listening here
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Iterate through MacroActions and simulate inputs
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Serialize MacroActions to JSON
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Deserialize JSON back into MacroActions
        }

        protected override void OnClosed(EventArgs e)
        {
            _hook?.Dispose();
            base.OnClosed(e);
        }
    }
    public class MacroEvent
    {
        public string ActionType { get; set; } // e.g., "KeyPress", "MouseClick"
        public string Detail { get; set; }     // e.g., "Space", "LeftButton"
        public int X { get; set; }             // Mouse X coordinate
        public int Y { get; set; }             // Mouse Y coordinate
        public long Delay { get; set; }        // Milliseconds since last action
    }
}