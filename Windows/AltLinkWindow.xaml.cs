using AemulusModManager.Utilities.PackageUpdating;
using AemulusModManager.Utilities.Windows;
using Microsoft.Win32;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Windows.Data;

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for UpdateFileBox.xaml
    /// </summary>
    public partial class AltLinkWindow : Window
    {
        public AltLinkWindow(List<GameBananaAlternateFileSource> files, string packageName, string game, bool update = false)
        {
            InitializeComponent();
            FileList.ItemsSource = files;
            TitleBox.Text = packageName;
            Description.Text = update ? $"Links from the Alternate File Sources section were found. You can " +
                $"select one to manually download.\nTo update, hit refresh after extracting the downloaded archive into:"
                : $"Links from the Alternate File Sources section were found. You can " +
                $"select one to manually download.\nTo install, hit refresh after extracting the downloaded archive into:";
            PathText.Text = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Mods\{game}";
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            var item = button.DataContext as GameBananaAlternateFileSource;
            Process.Start(item.Url.AbsoluteUri);
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