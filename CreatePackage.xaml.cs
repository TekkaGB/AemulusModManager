using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CreatePackage : Window
    {
        public Metadata metadata;
        public CreatePackage()
        {
            InitializeComponent();
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NameBox.Text.Length > 0)
                CreateButton.IsEnabled = true;
            else
                CreateButton.IsEnabled = false;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            metadata = new Metadata();
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
                Version v;
                string version = VersionBox.Text;
                if (Version.TryParse(version, out v))
                    metadata.version = VersionBox.Text;
                else
                {
                    Console.WriteLine("[ERROR] Invalid version number, no version number created");
                    metadata.version = "";
                }
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
