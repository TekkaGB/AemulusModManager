using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CreatePackage : Window
    {
        public Metadata metadata;
        private bool focused = false;
        private bool edited = false;
        public CreatePackage()
        {
            InitializeComponent();
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameBox.Text))
                CreateButton.IsEnabled = true;
            else
                CreateButton.IsEnabled = false;
            // Change bool back to false if they deleted both changed entries
            if (NameBox.Text.Length == 0 
                && AuthorBox.Text.Length == 0 
                && IDBox.Text.Length == 0)
                edited = false;
            if (!edited)
            {
                if (NameBox.Text.Length > 0 && AuthorBox.Text.Length > 0)
                    IDBox.Text = AuthorBox.Text.Replace(" ", "").ToLower() + "."
                        + NameBox.Text.Replace(" ", "").ToLower();
                else if (NameBox.Text.Length > 0)
                    IDBox.Text = NameBox.Text.Replace(" ", "").ToLower();
                else
                    IDBox.Text = AuthorBox.Text.Replace(" ", "").ToLower();
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            metadata = new Metadata();
            string dirName;
            if (VersionBox.Text != null)
                dirName = $@"Packages\{NameBox.Text} {VersionBox.Text}";
            else
                dirName = $@"Packages\{NameBox.Text}";
            if (!Directory.Exists(dirName))
            {
                metadata.name = NameBox.Text;
                if (AuthorBox.Text != null)
                {
                    AuthorBox.Text.Replace("&", "&amp;");
                    metadata.author = AuthorBox.Text;
                }
                else
                    metadata.author = "";
                if (VersionBox.Text != null)
                {
                    VersionBox.Text.Replace("&", "&amp;");
                    metadata.version = VersionBox.Text;
                }
                else
                    metadata.version = "";
                if (IDBox.Text != null)
                {
                    IDBox.Text.Replace("&", "&amp;");
                    metadata.id = IDBox.Text;
                }
                else
                    metadata.id = "";
                if (LinkBox.Text != null)
                {
                    LinkBox.Text.Replace("&", "&amp;");
                    metadata.link = LinkBox.Text;
                }
                else
                    metadata.link = "";
                if (DescBox.Text != null)
                {
                    // Replace illegal characters
                    DescBox.Text.Replace("&", "&amp;");
                    DescBox.Text.Replace("\"", "\\\"");
                    DescBox.Text.Replace("\\", "\\\\");
                    DescBox.Text.Replace("\'", "\\\'");
                    metadata.description = DescBox.Text;
                }
                else
                    metadata.description = "";
                Close();
            }
            else
            {
                Console.WriteLine($"[ERROR] Package name {NameBox.Text} already exists, try another one.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void IDBox_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            focused = !focused;
        }

        private void IDBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (focused && !edited)
                edited = true;
        }

        private void AuthorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Change bool back to false if they deleted both changed entries
            if (NameBox.Text.Length == 0
                && AuthorBox.Text.Length == 0
                && IDBox.Text.Length == 0)
                edited = false;
            if (!edited)
            {
                if (NameBox.Text.Length > 0 && AuthorBox.Text.Length > 0)
                    IDBox.Text = AuthorBox.Text.Replace(" ", "").ToLower() + "."
                        + NameBox.Text.Replace(" ", "").ToLower();
                else if (AuthorBox.Text.Length > 0)
                    IDBox.Text = AuthorBox.Text.Replace(" ", "").ToLower();
                else
                    IDBox.Text = NameBox.Text.Replace(" ", "").ToLower();
            }
        }
    }
}
