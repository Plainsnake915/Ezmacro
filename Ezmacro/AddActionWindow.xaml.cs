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
    /// <summary>
    /// Interaction logic for AddActionWindow.xaml
    /// </summary>
    public partial class AddActionWindow : Window
    {
        public MacroEvent CreatedAction { get; private set; }

        public AddActionWindow()
        {
            InitializeComponent();
            CmbActionType.SelectedIndex = 0;
        }

        private void CmbActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDetail == null) return;

            string selectedType = CmbActionType.SelectedItem as string;

            switch (selectedType)
            {
                case "KeyPress":
                case "KeyRelease":
                    CmbDetail.ItemsSource = Enum.GetValues(typeof(KeyCode));
                    CmbDetail.SelectedIndex = 0;
                    CmbDetail.IsEnabled = true;
                    break;

                case "MousePress":
                case "MouseRelease":
                    CmbDetail.ItemsSource = Enum.GetValues(typeof(SharpHook.Data.MouseButton));
                    CmbDetail.SelectedIndex = 0;
                    CmbDetail.IsEnabled = true;
                    break;

                case "MouseMove":
                    CmbDetail.ItemsSource = null;
                    CmbDetail.IsEnabled = false;
                    break;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(TxtX.Text, out int x);
            int.TryParse(TxtY.Text, out int y);
            long.TryParse(TxtDelay.Text, out long delay);

            CreatedAction = new MacroEvent
            {
                ActionType = CmbActionType.SelectedItem?.ToString() ?? "KeyPress",
                Detail = CmbDetail.IsEnabled ? CmbDetail.Text : "",
                X = x,
                Y = y,
                Delay = delay
            };

            DialogResult = true; // Closes window and signals success
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Closes window
        }
    }
}
