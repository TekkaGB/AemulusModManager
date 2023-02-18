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
        public bool deleteLoadout = false;
        private string originalName;
        public CreateLoadout(string game, string currentName = null, bool noDelete = false)
        {
            this.game = game;
            originalName = currentName;
            InitializeComponent();
            // Change title and text if editing an existing loadout
            if (currentName != null)
            {
                Title = $"Edit {currentName} loadout";
                name = currentName;
                NameBox.Text = currentName;
                // Remove copy loadou
                CopyLoadout.IsEnabled = false;
                CopyLoadout.Visibility = Visibility.Collapsed;
                Height = 120;
            }
            // Make the delete button invisible as this is a new loadout or renaming an imported one
            if(currentName == null || noDelete)
            {
                DeleteButton.Visibility = Visibility.Collapsed;
                DeleteButton.IsEnabled = false;
            }
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
                Utilities.ParallelLogger.Log("[ERROR] Invalid loadout name, try another one.");
                NotificationBox notification = new NotificationBox($"Invalid loadout name, try another one.");
                notification.ShowDialog();
            }
            else if (!File.Exists($@"{System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{NameBox.Text}.xml"))
            {
                Close();
            }
            else
            {
                Utilities.ParallelLogger.Log($"[ERROR] Loadout name {NameBox.Text} already exists, try another one.");
                NotificationBox notification = new NotificationBox($"Loadout name {NameBox.Text} already exists, try another one.");
                notification.ShowDialog();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            name = "";
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if there are more loadouts (you can't delete the last one)
            string configPath = $@"{System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config";
            string[] loadoutFiles = Directory.GetFiles($@"{configPath}\{game}").Where((path) => System.IO.Path.GetExtension(path) == ".xml").ToArray();
            if(loadoutFiles.Length == 1)
            {
                NotificationBox notification = new NotificationBox($"You cannot delete the last loadout");
                Utilities.ParallelLogger.Log("[ERROR] You cannot delete the last loadout");
                notification.ShowDialog();
            } 
            // Confirm that the user wants to delete the loadout
            else
            {
                NotificationBox notification = new NotificationBox($"Are you sure you want to delete {originalName} loadout?\nThis cannot be undone.", false);
                notification.ShowDialog();
                if (notification.YesNo)
                {
                    deleteLoadout = true;
                    Close();
                }

            }

        }
    }
}
