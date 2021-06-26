using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for CreateLoadout.xaml
    /// </summary>
    public partial class CreateLoadout : Window
    {
        private string game;
        public string name = "";
        public CreateLoadout(string game)
        {
            this.game = game;
            InitializeComponent();
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            name = NameBox.Text;
            if (!string.IsNullOrWhiteSpace(NameBox.Text))
                CreateButton.IsEnabled = true;
            else
                CreateButton.IsEnabled = false;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO check for invalid symbols
            if(NameBox.Text == "Add new loadout")
            {
                Console.WriteLine("[ERROR] Invalid loadout name, try another one.");
            }
            else if (!Directory.Exists($@"{System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{NameBox.Text}.xml"))
            {
                Close();
            }
            else
            {
                Console.WriteLine($"[ERROR] Loadout name {NameBox.Text} already exists, try another one.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            name = "";
            Close();
        }
    }
}
