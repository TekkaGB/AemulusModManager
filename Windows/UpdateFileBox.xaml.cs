using AemulusModManager.Utilities.PackageUpdating;
using AemulusModManager.Utilities.Windows;
using Microsoft.Win32;
using Octokit;
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Controls;
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
        public string host;
        // GameBanana Files
        public UpdateFileBox(List<GameBananaItemFile> files, string packageName)
        {
            InitializeComponent();
            FileList.ItemsSource = files;
            TitleBox.Text = packageName;
            host = "gamebanana";
        }
        // GitHub Files
        public UpdateFileBox(IReadOnlyList<ReleaseAsset> files, string packageName)
        {
            InitializeComponent();

            TitleBox.Text = packageName;
            var convList = new List<GithubFile>();
            foreach (var file in files)
            {
                convList.Add(new GithubFile()
                {
                    FileName = file.Name,
                    Downloads = file.DownloadCount,
                    Filesize = file.Size,
                    Description = file.Label,
                    DateAdded = file.UpdatedAt.DateTime,
                    DownloadUrl = file.BrowserDownloadUrl
                });
            }
            FileList.ItemsSource = convList;
            host = "github";
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (host.Equals("gamebanana"))
            {
                var item = button.DataContext as GameBananaItemFile;
                chosenFileUrl = item.DownloadUrl;
                chosenFileName = item.FileName;
            }
            else if (host.Equals("github"))
            {
                var item = button.DataContext as GithubFile;
                chosenFileUrl = item.DownloadUrl;
                chosenFileName = item.FileName;
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