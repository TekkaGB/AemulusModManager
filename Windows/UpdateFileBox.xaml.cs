using AemulusModManager.Utilities.Windows;
using Microsoft.Win32;
using Octokit;
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Data;

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for UpdateFileBox.xaml
    /// </summary>
    public partial class UpdateFileBox : Window
    {
        public string chosenFileUrl;
        public string chosenFileName;
        private readonly string host;
        // GameBanana Files
        public UpdateFileBox(List<GameBananaItemFile> files, string packageName)
        {
            InitializeComponent();
            FileGrid.ItemsSource = files;
            FileGrid.SelectedIndex = 0;
            NameColumn.Binding = new Binding("FileName");
            UploadTimeColumn.Binding = new Binding("TimeSinceUpload");
            DescriptionColumn.Binding = new Binding("Description");
            Title = $"Aemulus Package Manager - {packageName}";
            host = "GameBanana";
            PlayNotificationSound();
        }

        // GitHub Files
        public UpdateFileBox(IReadOnlyList<ReleaseAsset> files, string packageName)
        {
            InitializeComponent();
            FileGrid.ItemsSource = files;
            FileGrid.SelectedIndex = 0;
            NameColumn.Binding = new Binding("Name");
            UploadTimeColumn.Binding = new Binding("UpdatedAt")
            {
                Converter = new TimeSinceConverter()
            };
            DescriptionColumn.Visibility = Visibility.Collapsed;
            Title = $"Aemulus Package Manager - {packageName}";
            host = "GitHub";
            PlayNotificationSound();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (host == "GameBanana")
            {
                GameBananaItemFile selectedItem = (GameBananaItemFile)FileGrid.SelectedItem;
                chosenFileUrl = selectedItem.DownloadUrl;
                chosenFileName = selectedItem.FileName;
            }
            else if (host == "GitHub")
            {
                ReleaseAsset selectedItem = (ReleaseAsset)FileGrid.SelectedItem;
                chosenFileUrl = selectedItem.BrowserDownloadUrl;
                chosenFileName = selectedItem.Name;
            }
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void PlayNotificationSound()
        {
            bool found = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(null); // pass null to get (Default)
                        if (o != null)
                        {
                            SoundPlayer theSound = new SoundPlayer((String)o);
                            theSound.Play();
                            found = true;
                        }
                    }
                }
            }
            catch
            { }
            if (!found)
                SystemSounds.Beep.Play(); // consolation prize
        }
    }

}