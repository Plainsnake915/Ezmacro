using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SharpHook;
using SharpHook.Data;

namespace Ezmacro
{
    public partial class MainWindow : Window
    {
        // An ObservableCollection automatically updates the DataGrid UI when items change
        public ObservableCollection<MacroEvent> MacroActions { get; set; } = new ObservableCollection<MacroEvent>();
        private TaskPoolGlobalHook _hook;
        private Stopwatch _stopwatch = new Stopwatch();
        private Stopwatch _mouseSampleTimer = new Stopwatch();
        private bool _isRecording = false;
        private bool _isPlaying = false;
        private GlowOverlayWindow _overlay;
        public MacroSettings Settings { get; set; } = new MacroSettings();
        private void ShowGlow(Color color)
        {
            if(Settings.HideGlow) { return; }
            if (_overlay == null)
            {
                _overlay = new GlowOverlayWindow();

            }
            _overlay.SetGlowColor(color);
            _overlay.Show();
        }
        private void HideGlow()
        {
            if (_overlay != null)
            {
                _overlay.Hide();
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            SetupDataGridFilter();
            Settings = MacroSettings.Load(); // Load settings from disk


            // Bind our collection to the DataGrid UI
            MacroDataGrid.ItemsSource = MacroActions;
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnGlobalKeyPressed;
            _hook.KeyReleased += OnGlobalKeyReleased;

            _hook.MousePressed += OnGlobalMousePressed;
            _hook.MouseReleased += OnGlobalMouseReleased;
            _hook.MouseMoved += OnGlobalMouseMoved;


            Task.Run(() => _hook.Run());

        }
        private void SetupDataGridFilter()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(MacroActions);
            view.Filter = item =>
            {
                if (item is MacroEvent macroEvent)
                {
                    if (macroEvent.ActionType == "MouseMove" && Settings.HideMouseMoves)
                    {
                        return false; // Hide mouse move events if the setting is enabled
                    }
                }
                return true; // Show all other events

            };
        }
        private void OnGlobalMouseMoved(object sender, MouseHookEventArgs e)
        {
            // Optional: Handle mouse movement if needed
            if (!_isRecording || !Settings.MousetrackingEnabled) return;
            if (!_mouseSampleTimer.IsRunning || _mouseSampleTimer.ElapsedMilliseconds >= Settings.Samplingrate)
            {
                _mouseSampleTimer.Restart();
                long elapsed = _stopwatch.ElapsedMilliseconds;
                _stopwatch.Restart();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MacroActions.Add(new MacroEvent
                    {
                        ActionType = "MouseMove",
                        X = e.Data.X,
                        Y = e.Data.Y,
                        Delay = elapsed
                    });
                });
            }



        }
        private void OnGlobalKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (!_isRecording) { return; } // Ignore key presses if we're not recording
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
            if (e.Data.KeyCode == Settings.RecordStopKey && !_isPlaying)
            {
                if (_isRecording)
                {
                    BtnStop_Click(sender, null); // Stop recording if the designated key is released
                    return;
                }
                else
                {
                    BtnRecord_Click(sender, null); // Start recording if the designated key is released
                    return;
                }
            }
            if (e.Data.KeyCode == Settings.PlaybackKey && !_isRecording)
            {
                if (_isPlaying)
                {
                    _isPlaying = false; // Stop playback if the designated key is released
                    return;
                }
                else
                {
                    BtnPlay_Click(sender, null); // Start playback if the designated key is released
                    return;
                }
            }
            if (!_isRecording) { return; } // Ignore key releases if we're not recording
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
            if (!_isRecording) { return; }
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

            if (!_isRecording) { return; }
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


        private async void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                BtnRecord.IsEnabled = false;
                BtnStop.IsEnabled = true;
                BtnPlay.IsEnabled = false;
                await Task.Delay(100); // Small delay to ensure the button state updates before recording starts
                if (Settings.AutoMinimize) this.WindowState = WindowState.Minimized;
                ShowGlow(Colors.Red); // Show red glow during recording
                _isRecording = true;
                MacroActions.Clear();
                _stopwatch.Restart();
            });
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                BtnRecord.IsEnabled = true;
                BtnStop.IsEnabled = false;
                BtnPlay.IsEnabled = true;
                HideGlow(); // Hide the glow overlay when recording stops
                _isRecording = false;
                _stopwatch.Stop();
                this.WindowState = WindowState.Normal;
                this.Topmost = true; // Bring the window to the front
                this.Activate(); // Ensure the window is active
                this.Topmost = false; // Reset Topmost to allow other windows to be on top
                if (_isPlaying)
                {
                    _isPlaying = false; // Stop playback if the user has stopped it
                    return;
                }
                int lastIndex = MacroActions.Count - 1;
                MacroActions[lastIndex].ActionType = "wait";
                MacroActions[lastIndex].Detail = "delay";
                MacroDataGrid.Items.Refresh(); // Refresh the DataGrid to show the updated last row
            });



        }


        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                if (MacroActions.Count == 0)
                {
                    MessageBox.Show("No macro recorded yet! Record something first.", "Playback Info");
                    return;
                }


                BtnRecord.IsEnabled = false;
                BtnPlay.IsEnabled = false;
                BtnStop.IsEnabled = true;


                if (Settings.AutoMinimize) this.WindowState = WindowState.Minimized;
                _isPlaying = true;
                await Task.Delay(300);
                ShowGlow(Colors.Green); // Show green glow during playback
                playBack();







            });


        }
        private async void playBack()
        {


            var simulator = new EventSimulator();


            await Task.Run(async () =>
            {
                while (_isPlaying)
                {
                    foreach (var action in MacroActions)
                    {
                        if (!_isPlaying) break; // Stop playback if the user has stopped it

                        if (action.Delay > 0)
                        {
                            await Task.Delay((int)action.Delay);
                        }

                        try
                        {
                            // Execute action based on its type
                            if (action.ActionType == "wait")
                            {
                                continue;
                            }
                            if (action.ActionType == "KeyPress")
                            {
                                if (Enum.TryParse(action.Detail, out KeyCode code))
                                {
                                    simulator.SimulateKeyPress(code);
                                }
                            }
                            else if (action.ActionType == "KeyRelease")
                            {
                                if (Enum.TryParse(action.Detail, out KeyCode code))
                                {
                                    simulator.SimulateKeyRelease(code);
                                }
                            }

                            else if (action.ActionType == "MousePress")
                            {
                                if (Enum.TryParse(action.Detail, out SharpHook.Data.MouseButton button))
                                {
                                    simulator.SimulateMouseMovement((short)action.X, (short)action.Y);
                                    simulator.SimulateMousePress(button);
                                }
                            }
                            else if (action.ActionType == "MouseRelease")
                            {
                                if (Enum.TryParse(action.Detail, out SharpHook.Data.MouseButton button))
                                {
                                    simulator.SimulateMouseMovement((short)action.X, (short)action.Y);
                                    simulator.SimulateMouseRelease(button);
                                }
                            }
                            else if (action.ActionType == "MouseMove")
                            {
                                simulator.SimulateMouseMovement((short)action.X, (short)action.Y);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to simulate action: {ex.Message}");
                        }
                    }
                    if (!Settings.ContinuosPlayback) _isPlaying = false;
                }
            });
           

            HideGlow(); // Hide the glow overlay after playback
            this.WindowState = WindowState.Normal;
            this.Topmost = true; // Bring the window to the front
            this.Activate(); // Ensure the window is active
            this.Topmost = false; // Reset Topmost to allow other windows to be on top
            BtnRecord.IsEnabled = true;
            BtnPlay.IsEnabled = true;
            BtnStop.IsEnabled = false;
            
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
                string dedicatedFolderPath = Path.Combine(documentsPath, "Ezmacro", "MyMacros");

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
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Pass the main window's settings instance over to the settings popup window
            SettingsWindow settingsWindow = new SettingsWindow(this.Settings);
            settingsWindow.Owner = this; // Makes it pop up directly over the main window
            settingsWindow.ShowDialog();
        }

        private void MacroDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MacroEvent)))
            {
                var droppedData = e.Data.GetData(typeof(MacroEvent)) as MacroEvent;
                var targetRow = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);

                if (droppedData != null && targetRow != null)
                {
                    var targetData = targetRow.Item as MacroEvent;
                    int oldIndex = MacroActions.IndexOf(droppedData);
                    int newIndex = MacroActions.IndexOf(targetData);

                    if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
                    {
                        MacroActions.Move(oldIndex, newIndex);
                        MacroDataGrid.SelectedItem = droppedData;
                    }
                }
            }
        }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }
        private DataGridRow _currentDropTargetRow;

        private void DataGridRow_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;

            var targetRow = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);

            if (_currentDropTargetRow != null && _currentDropTargetRow != targetRow)
            {
                // Clear previous highlight
                _currentDropTargetRow.BorderThickness = new Thickness(0);
            }

            if (targetRow != null)
            {
                _currentDropTargetRow = targetRow;
                Point pos = e.GetPosition(targetRow);

                // Highlight top or bottom border depending on insertion point
                if (pos.Y < targetRow.ActualHeight / 2)
                {
                    targetRow.BorderThickness = new Thickness(0, 2, 0, 0); // Line above
                    targetRow.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                }
                else
                {
                    targetRow.BorderThickness = new Thickness(0, 0, 0, 2); // Line below
                    targetRow.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                }
            }
        }
        private void DataGridRow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is DataGridRow row)
            {

                Point currentPos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPos;

                // Check if user moved far enough to trigger drag
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {



                    var draggedItem = row.Item as MacroEvent;
                    if (draggedItem != null)
                    {
                        row.Opacity = 0.5;
                        DragDrop.DoDragDrop(row, draggedItem, DragDropEffects.Move);
                        row.Opacity = 1.0;
                    }
                }
                
            }
        }
        private Point _dragStartPoint;

        // Capture mouse position on row click
        private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                _dragStartPoint = e.GetPosition(null);
            }
        }
        private void BtnOpenAddWindow_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddActionWindow
            {
                Owner = this // Blocks parent window until closed
            };

            if (addWindow.ShowDialog() == true && addWindow.CreatedAction != null)
            {
                var newAction = addWindow.CreatedAction;

                // Insert after currently selected row, or append to end
                if (MacroDataGrid.SelectedItem is MacroEvent selected)
                {
                    int index = MacroActions.IndexOf(selected);
                    MacroActions.Insert(index + 1, newAction);
                }
                else
                {
                    MacroActions.Add(newAction);
                }

                MacroDataGrid.SelectedItem = newAction;
                MacroDataGrid.ScrollIntoView(newAction);
            }
        }
        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the item bound to the row containing the clicked button
            if (sender is Button button && button.DataContext is MacroEvent eventToDelete)
            {
                MacroActions.Remove(eventToDelete);
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            _hook?.Dispose();
            base.OnClosed(e);
        }
        

    }
    
    

    

}
