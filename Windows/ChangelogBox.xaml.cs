using Microsoft.Win32;
using System;
using System.Media;
using System.Windows;

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for ChangelogBox.xaml
    /// </summary>
    public partial class ChangelogBox : Window
    {
        public bool YesNo = false;
        public ChangelogBox(GameBananaItemUpdate update, string packageName, string text, bool OK = true)
        {
            InitializeComponent();
            ChangesGrid.ItemsSource = update.Changes;
            Title = $"{packageName} Changelog";
            VersionLabel.Content = update.Title;
            Text.Text = text;
            if (OK)
            {
                OkButton.Visibility = Visibility.Visible;
            }
            else
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
            }
            PlayNotificationSound();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
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
