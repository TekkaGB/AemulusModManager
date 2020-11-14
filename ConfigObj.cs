using System;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Xml.Serialization;

namespace AemulusModManager
{
    public class Config
    {
        public ObservableCollection<Package> package { get; set; }
        public string modDir { get; set; }
        public string exePath { get; set; }
        public string reloadedPath { get; set; }
        public bool emptySND { get; set; }
        public bool tbl { get; set; }
    }
    public class Package
    {
        public string name { get; set; }
        public string path { get; set; }
        public bool enabled { get; set; }
        public string id { get; set; }
    }

    public class Metadata
    {
        public string name { get; set; }
        public string id { get; set; }
        public string author { get; set; }
        public string version { get; set; }
        public string link { get; set; }
        public string description { get; set; }
    }

    [Serializable, XmlRoot("Mod")]
    public class ModXmlMetadata
    {
        public string Id { get; set; }
        public string Game { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Date { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public string UpdateUrl { get; set; }
    }

    public class DisplayedMetadata
    {
        public string name { get; set; }
        public string id { get; set; }
        public string author { get; set; }
        public bool enabled { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public string link { get; set; }
        public string path { get; set; }
    }

}
