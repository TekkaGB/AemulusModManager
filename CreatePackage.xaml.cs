using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for CreatePackage.xaml
    /// </summary>
    public partial class CreatePackage : Window
    {
        public Metadata metadata;
        public string thumbnailPath;
        private bool focused = false;
        private bool edited = false;
        private bool editing = false;
        public CreatePackage(Metadata m)
        {
            InitializeComponent();
            if (m != null)
            {
                NameBox.Text = m.name;
                AuthorBox.Text = m.author;
                IDBox.Text = m.id;
                VersionBox.Text = m.version;
                LinkBox.Text = m.link;
                DescBox.Text = m.description;
                editing = true;
                if (IDBox.Text != AuthorBox.Text.Replace(" ", "").ToLower() + "."
                        + NameBox.Text.Replace(" ", "").ToLower() && IDBox.Text.Length > 0)
                    edited = true;
            }
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
            if (!Directory.Exists(dirName) || editing)
            {
                metadata.name = NameBox.Text;
                if (AuthorBox.Text != null)
                    metadata.author = AuthorBox.Text;
                else
                    metadata.author = "";
                if (VersionBox.Text != null)
                    metadata.version = VersionBox.Text;
                else
                    metadata.version = "";
                if (IDBox.Text != null)
                    metadata.id = IDBox.Text;
                else
                    metadata.id = "";
                if (LinkBox.Text != null)
                    metadata.link = LinkBox.Text;
                else
                    metadata.link = "";
                if (DescBox.Text != null)
                    metadata.description = DescBox.Text;
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

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var openPng = new CommonOpenFileDialog();
            openPng.Filters.Add(new CommonFileDialogFilter("Thumbnail", "*.png"));
            openPng.EnsurePathExists = true;
            openPng.EnsureValidNames = true;
            openPng.Title = "Select .png for thumbnail";
            if (openPng.ShowDialog() == CommonFileDialogResult.Ok)
            {
                PreviewBox.Text = openPng.FileName;
                thumbnailPath = openPng.FileName;
            }
            // Bring Create Package window back to foreground after closing dialog
            this.Activate();
        }
    }
}
