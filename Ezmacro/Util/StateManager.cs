using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ezmacro
{
    static class StateManager
    {
        public static Stopwatch _stopwatch { get; set; } = new Stopwatch();
        public static Stopwatch _mouseSampleTimer { get; set; } = new Stopwatch();
        public static bool _isRecording { get; set; } = false;
        public static bool _isPlaying { get; set; } = false;
    }
}
