using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ezmacro
{
    public class MacroEvent
    {
        public string ActionType { get; set; } // e.g., "KeyPress", "MouseClick"
        public string Detail { get; set; }     // e.g., "Space", "LeftButton"
        public int X { get; set; }             // Mouse X coordinate
        public int Y { get; set; }             // Mouse Y coordinate
        public long Delay { get; set; }        // Milliseconds since last action
    }
}
