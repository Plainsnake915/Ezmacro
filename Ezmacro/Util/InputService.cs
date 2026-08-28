using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpHook;

namespace Ezmacro
{
    internal class InputService
    {
        private int cumx = 0;
        private  int cumy = 0;
        private MainWindow _mainWindow;

        public InputService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void OnMouseDeltaReceived(int x, int y)
        {
            if (!StateManager._isRecording || !_mainWindow.Settings.MousetrackingEnabled) return;
            cumx += x;
            cumy += y;
            if (!StateManager._mouseSampleTimer.IsRunning || StateManager._mouseSampleTimer.ElapsedMilliseconds >= _mainWindow.Settings.Samplingrate)
            {
                StateManager._mouseSampleTimer.Restart();
                long elapsed = StateManager._stopwatch.ElapsedMilliseconds;
                StateManager._stopwatch.Restart();
                int deltaXToRecord = cumx;
                int deltaYToRecord = cumy;
                cumx = 0;
                cumy = 0;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _mainWindow.MacroActions.Add(new MacroEvent
                    {
                        ActionType = "MouseMove",
                        X = deltaXToRecord,
                        Y = deltaYToRecord,
                        Delay = elapsed
                    });
                });
            }
        }

        public void OnGlobalKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (e.Data.KeyCode == _mainWindow.Settings.RecordStopKey && !StateManager._isPlaying)
            {
                if (StateManager._isRecording)
                {
                    _mainWindow.BtnStop_Click(sender, null); // Stop recording if the designated key is released
                    return;
                }
                else
                {
                    _mainWindow.BtnRecord_Click(sender, null); // Start recording if the designated key is released
                    return;
                }
            }
            if (e.Data.KeyCode == _mainWindow.Settings.PlaybackKey && !StateManager._isRecording)
            {
                if (StateManager._isPlaying)
                {
                    StateManager._isPlaying = false; // Stop playback if the designated key is released
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.HideGlow();
                    });
                    return;
                }
                else
                {
                    _mainWindow.BtnPlay_Click(sender, null); // Start playback if the designated key is released
                    return;
                }
            }
            if (!StateManager._isRecording) { return; } // Ignore key presses if we're not recording
            long elapsed = StateManager._stopwatch.ElapsedMilliseconds;
            StateManager._stopwatch.Restart(); // Reset timer for the next sequential object

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
                _mainWindow.MacroActions.Add(keyEvent);
            });
        }
        public void OnGlobalKeyReleased(object sender, KeyboardHookEventArgs e)
        {

            if (!StateManager._isRecording) { return; } // Ignore key releases if we're not recording
            long elapsed = StateManager._stopwatch.ElapsedMilliseconds;
            StateManager._stopwatch.Restart();
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
                _mainWindow.MacroActions.Add(keyUpEvent);
            });
        }

        public void OnGlobalMousePressed(object sender, MouseHookEventArgs e)
        {
            if (!StateManager._isRecording) { return; }
            long elapsed = StateManager._stopwatch.ElapsedMilliseconds;
            StateManager._stopwatch.Restart();
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
                _mainWindow.MacroActions.Add(mouseDownEvent);
            });
        }

        public void OnGlobalMouseReleased(object sender, MouseHookEventArgs e)
        {

            if (!StateManager._isRecording) { return; }
            long elapsed = StateManager._stopwatch.ElapsedMilliseconds;
            StateManager._stopwatch.Restart();
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
                _mainWindow.MacroActions.Add(mouseUpEvent);
            });
        }

       
    }
}
