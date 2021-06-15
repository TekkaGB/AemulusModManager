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
        public AltLinkWindow(List<GameBananaAlternateFileSource> links, string packageName, string game)
        {
            InitializeComponent();
            FileList.ItemsSource = links;
            TitleBox.Text = packageName;
            Description.Text = $"Links from the Alternate File Sources section were found. You can " +
                $"select one to manually download.\nTo install, extract the downloaded archive into:";
            PathText.Text = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}";
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