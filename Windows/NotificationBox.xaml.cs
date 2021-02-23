using Microsoft.Win32;
using System;
using System.Media;
using System.Windows;
using System.Windows.Shell;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for NotificationBox.xaml
    /// </summary>
    public partial class NotificationBox : Window
    {
        public bool YesNo = false;
        public NotificationBox(string message, bool OK = true)
        {
            InitializeComponent();
            Notification.Text = message;
            if (OK)
            {
                OkButton.Visibility = Visibility.Visible;
                PlayNotificationSound();
                taskBarItem.ProgressState = TaskbarItemProgressState.Indeterminate;
            }
            else
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                PlayNotificationSound();
            }
            if (message.Length > 40)
                Notification.TextAlignment = TextAlignment.Left;
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
