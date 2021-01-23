using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public NotificationBox(string message)
        {
            InitializeComponent();
            Notification.Text = message;
            if (message.Length > 30)
                Notification.TextAlignment = TextAlignment.Left;
            PlayNotificationSound();
            taskBarItem.ProgressState = TaskbarItemProgressState.Indeterminate;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
