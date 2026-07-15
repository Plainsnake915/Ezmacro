using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using SharpHook;
using SharpHook.Data;
using SharpHook.Native;

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
        private void OnGlobalKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart(); // Reset timer for the next sequential object

            // Thread marshaling: Safely pass the data object to the Main UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Instantiate a new data object (Instantiation) and populate its properties
                MacroEvent keyEvent = new MacroEvent
                {
                    ActionType = "KeyPress",
                    Detail = e.Data.KeyCode.ToString(),
                    X = 0,
                    Y = 0,
                    Delay = elapsed
                };

                // Add the newly created object to our collection state
                MacroActions.Add(keyEvent);
            });
        }
        private void OnGlobalKeyReleased(object sender, KeyboardHookEventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MacroEvent keyUpEvent = new MacroEvent
                {
                    ActionType = "KeyRelease",
                    Detail = e.Data.KeyCode.ToString(),
                    X = 0,
                    Y = 0,
                    Delay = elapsed
                };
                MacroActions.Add(keyUpEvent);
            });
        }

        private void OnGlobalMousePressed(object sender, MouseHookEventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MacroEvent mouseDownEvent = new MacroEvent
                {
                    ActionType = "MousePress",
                    Detail = e.Data.Button.ToString(),
                    X = e.Data.X,
                    Y = e.Data.Y,
                    Delay = elapsed
                };
                MacroActions.Add(mouseDownEvent);
            });
        }

        private void OnGlobalMouseReleased(object sender, MouseHookEventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MacroEvent mouseUpEvent = new MacroEvent
                {
                    ActionType = "MouseRelease",
                    Detail = e.Data.Button.ToString(),
                    X = e.Data.X,
                    Y = e.Data.Y,
                    Delay = elapsed
                };
                MacroActions.Add(mouseUpEvent);
            });
        }


        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            BtnRecord.IsEnabled = false;
            BtnStop.IsEnabled = true;
            BtnPlay.IsEnabled = false;
            MacroActions.Clear();
            _stopwatch.Restart();

            // TODO: Start SharpHook global listening here
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnGlobalKeyPressed;
            _hook.KeyReleased += OnGlobalKeyReleased;

            _hook.MousePressed += OnGlobalMousePressed;
            _hook.MouseReleased += OnGlobalMouseReleased;
            


            Task.Run(() => _hook.Run());
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            BtnRecord.IsEnabled = true;
            BtnStop.IsEnabled = false;
            BtnPlay.IsEnabled = true;
            _stopwatch.Stop();

            // TODO: Stop SharpHook listening here
            if (_hook != null)
            {
                // OOP Cleanup: Unsubscribe from the event streams to prevent memory leaks
                _hook.KeyPressed -= OnGlobalKeyPressed;
                _hook.KeyReleased -= OnGlobalKeyReleased;
                _hook.MouseClicked -= OnGlobalMousePressed;
                _hook.MouseReleased -= OnGlobalMouseReleased;

                // Destroy the hook instance and release OS resources
                _hook.Dispose();
                _hook = null;
            }
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Iterate through MacroActions and simulate inputs
            // 1. Check if we actually have any actions recorded to play back
            if (MacroActions.Count == 0)
            {
                MessageBox.Show("No macro recorded yet! Record something first.", "Playback Info");
                return;
            }

            // 2. Disable UI buttons so the user can't click things while the macro is running
            BtnRecord.IsEnabled = false;
            BtnPlay.IsEnabled = false;

            // 3. Minimize our app window so the macro inputs go to the desktop/other apps instead of clicking inside our own UI!
            this.WindowState = WindowState.Minimized;

            // Give the window a brief moment (300ms) to finish minimizing before starting
            await Task.Delay(300);

            var simulator = new EventSimulator();

            // 5. Loop through our encapsulated MacroEvent data objects on a background thread
            await Task.Run(async () =>
            {
                foreach (var action in MacroActions)
                {
                    // Wait for the exact recorded delay before performing the action
                    if (action.Delay > 0)
                    {
                        await Task.Delay((int)action.Delay);
                    }

                    try
                    {
                        // Execute action based on its type
                        if (action.ActionType == "KeyPress")
                        {
                            if (Enum.TryParse(action.Detail, out KeyCode code))
                            {
                                simulator.SimulateKeyPress(code);
                            }
                        }
                        else if(action.ActionType == "KeyRelease")
                        {
                            if (Enum.TryParse(action.Detail, out KeyCode code))
                            {
                                simulator.SimulateKeyRelease(code);
                            }
                        }
                        
                        else if(action.ActionType == "MousePress")
                        {
                            if (Enum.TryParse(action.Detail, out MouseButton button))
                            {
                                simulator.SimulateMouseMovement((short)action.X, (short)action.Y);
                                simulator.SimulateMousePress(button);
                            }
                        }
                        else if (action.ActionType == "MouseRelease")
                        {
                            if (Enum.TryParse(action.Detail, out MouseButton button))
                            {
                                simulator.SimulateMouseMovement((short)action.X, (short)action.Y);
                                simulator.SimulateMouseRelease(button);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to simulate action: {ex.Message}");
                    }
                }
            });

            // 6. Restore the application window when playback finishes
            this.WindowState = WindowState.Normal;
            BtnRecord.IsEnabled = true;
            BtnPlay.IsEnabled = true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Serialize MacroActions to JSON
            // 1. Guard clause: Don't save empty macros
            if (MacroActions.Count == 0)
            {
                MessageBox.Show("There are no recorded actions to save!", "Save Macro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Define our master folder path: Documents\MyMacros
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string dedicatedFolderPath = Path.Combine(documentsPath,"Ezmacro", "MyMacros");

                // 3. Make sure the folder physically exists before opening the window
                if (!Directory.Exists(dedicatedFolderPath))
                {
                    Directory.CreateDirectory(dedicatedFolderPath);
                }

                // 4. Configure the Save Dialog Box
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON files (*.json)|*.json";
                saveFileDialog.DefaultExt = "json";
                saveFileDialog.Title = "Name Your Macro File";

                // CRUCIAL: Force the dialog window to open directly inside our dedicated folder
                saveFileDialog.InitialDirectory = dedicatedFolderPath;

                // 5. If the user types a name and clicks "Save"
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Even if they try to browse away, we can ensure it saves to our folder by grabbing just the file name
                    string justFileName = Path.GetFileName(saveFileDialog.FileName);
                    string finalLockPath = Path.Combine(dedicatedFolderPath, justFileName);

                    // 6. Serialize and write the file
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(MacroActions, options);

                    File.WriteAllText(finalLockPath, jsonString);

                    MessageBox.Show($"Saved successfully to your macro folder!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save macro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Deserialize JSON back into MacroActions
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dedicatedFolderPath = Path.Combine(documentsPath, "Ezmacro", "MyMacros");

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json";
            openFileDialog.Title = "Load a Macro File";

            // Automatically open up inside our specific folder!
            if (Directory.Exists(dedicatedFolderPath))
            {
                openFileDialog.InitialDirectory = dedicatedFolderPath;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonString = File.ReadAllText(openFileDialog.FileName);
                    var loadedActions = JsonSerializer.Deserialize<ObservableCollection<MacroEvent>>(jsonString);

                    if (loadedActions != null)
                    {
                        MacroActions.Clear();
                        foreach (var action in loadedActions)
                        {
                            MacroActions.Add(action);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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