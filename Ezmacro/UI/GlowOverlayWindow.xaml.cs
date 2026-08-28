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

namespace Ezmacro
{
    /// <summary>
    /// Interaction logic for GlowOverlayWindow.xaml
    /// </summary>
    public partial class GlowOverlayWindow : Window
    {
        public GlowOverlayWindow()
        {
            InitializeComponent();

            // Stretch across the full primary screen dimensions
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        // Helper to change the glow color dynamically
        public void SetGlowColor(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            GlowBorder.BorderBrush = brush;
            GlowEffect.Color = color;
        }
    }
}
