using Microsoft.Win32;
using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for ChangelogBox.xaml
    /// </summary>
    public partial class ChangelogBox : Window
    {
        public bool YesNo = false;
        private DisplayedMetadata row;
        private string version;
        private string path;
        public ChangelogBox(GameBananaItemUpdate update, string packageName, string text, DisplayedMetadata row, string version, string path, bool OK = true)
        {
            this.row = row;
            this.version = version;
            this.path = path;
            InitializeComponent();
            ChangesGrid.ItemsSource = update.Changes;
            Title = $"{packageName} Changelog";
            VersionLabel.Content = update.Title;
            if (update.Version != null)
                VersionLabel.Content += $" ({update.Version})";
            Text.Text = text;
            if (OK)
            {
                OkButton.Visibility = Visibility.Visible;
            }
            else
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                SkipButton.Visibility = Visibility.Visible;
            }
            PlayNotificationSound();
        }

        // An instance where you can't skip the update (only yes or no)
        public ChangelogBox(GameBananaItemUpdate update, string packageName, string text, bool OK = true)
        {
            InitializeComponent();
            ChangesGrid.ItemsSource = update.Changes;
            Title = $"{packageName} Changelog";
            VersionLabel.Content = update.Title;
            if (update.Version != null)
                VersionLabel.Content += $" ({update.Version})";
            Text.Text = text;
            if (OK)
            {
                OkButton.Visibility = Visibility.Visible;
            }
            else
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(YesButton, 2);
                Grid.SetColumnSpan(NoButton, 2);
            }
            PlayNotificationSound();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Skip_Button_Click(object sender, RoutedEventArgs e)
        {
            // Write that the update should be skipped to the package metadata
            Metadata m = new Metadata();
            m.name = row.name;
            m.author = row.author;
            m.id = row.id;
            m.version = row.version;
            m.link = row.link;
            m.description = row.description;
            m.skippedVersion = version;
            try
            {
                using (FileStream streamWriter = File.Create(path))
                {
                    try
                    {
                        XmlSerializer xsp = new XmlSerializer(typeof(Metadata));
                        xsp.Serialize(streamWriter, m);
                    }
                    catch (Exception ex)
                    {
                        Utilities.ParallelLogger.Log($@"[ERROR] Couldn't serialize {path} ({ex.Message})");
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error trying to set skipped version for {row.name}: {ex.Message}");
            }
            Close();
        }
        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            YesNo = true;
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
