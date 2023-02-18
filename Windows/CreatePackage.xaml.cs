using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
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
        private string skippedVersion = "";
        public CreatePackage(Metadata m)
        {
            InitializeComponent();
            if (m != null)
            {
                Title = $"Edit {m.name}";
                NameBox.Text = m.name;
                AuthorBox.Text = m.author;
                IDBox.Text = m.id;
                VersionBox.Text = m.version;
                LinkBox.Text = m.link;
                DescBox.Text = m.description;
                skippedVersion = m.skippedVersion;
                editing = true;
                if (PackageUpdatable())
                {
                    AllowUpdates.IsEnabled = true;
                    if (PackageUpdatable())
                    {
                        AllowUpdates.IsEnabled = true;
                        if (skippedVersion == "all")
                            AllowUpdates.IsChecked = false;
                        else
                            AllowUpdates.IsChecked = true;
                    }
                    else
                    {
                        AllowUpdates.IsEnabled = false;
                        AllowUpdates.IsChecked = false;
                    }
                }
                else
                {
                    AllowUpdates.IsEnabled = false;
                    AllowUpdates.IsChecked = false;
                }
                if (IDBox.Text != AuthorBox.Text.Replace(" ", "").ToLower() + "."
                        + NameBox.Text.Replace(" ", "").ToLower() && IDBox.Text.Length > 0)
                    edited = true;
                if (IDBox.Text == NameBox.Text.Replace(" ", "").ToLower())
                    edited = false;
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
            if (!edited && !editing)
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
                dirName = $@"{NameBox.Text} {VersionBox.Text}";
            else
                dirName = NameBox.Text;
            dirName = $@"Packages\{string.Join("_", dirName.Split(Path.GetInvalidFileNameChars()))}";
            if (!Directory.Exists(dirName) || editing)
            {
                if ((bool)!AllowUpdates.IsChecked)
                    metadata.skippedVersion = "all";
                else
                    metadata.skippedVersion = null;
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
                Utilities.ParallelLogger.Log($"[ERROR] Package name {NameBox.Text} already exists, try another one.");
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
            if (!edited && !editing)
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
            openPng.Filters.Add(new CommonFileDialogFilter("Preview", "*.*"));
            openPng.EnsurePathExists = true;
            openPng.EnsureValidNames = true;
            openPng.Title = "Select Preview";
            if (openPng.ShowDialog() == CommonFileDialogResult.Ok)
            {
                PreviewBox.Text = openPng.FileName;
                thumbnailPath = openPng.FileName;
            }
            // Bring Create Package window back to foreground after closing dialog
            this.Activate();
        }

        private void LinkBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PackageUpdatable())
            {
                AllowUpdates.IsEnabled = true;
            }
            else
            {
                AllowUpdates.IsEnabled = false;
                AllowUpdates.IsChecked = false;
            }
        }

        private bool PackageUpdatable()
        {
            if (LinkBox.Text == "")
                return false;
            string host = UrlConverter.Convert(LinkBox.Text);
            return (host == "GameBanana" || host == "GitHub") && VersionBox.Text != "";
        }
    }
}
