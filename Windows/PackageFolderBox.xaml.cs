using Microsoft.Win32;
using System;
using System.Media;
using System.Windows;

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for UpdateFileBox.xaml
    /// </summary>
    public partial class PackageFolderBox : Window
    {
        public string chosenFolder;
        public PackageFolderBox(string[] folders, string packageName)
        {
            InitializeComponent();
            FileGrid.ItemsSource = folders;
            FileGrid.SelectedIndex = 0;
            Title = $"Aemulus Package Manager - {packageName}";
            PlayNotificationSound();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {

            string selectedItem = (string)FileGrid.SelectedItem;
            chosenFolder = selectedItem;
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
