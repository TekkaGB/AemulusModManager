﻿using System;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Xml.Serialization;

namespace AemulusModManager
{
    // Old config
    public class Config
    {
        // Keep to transfer data to new config
        public ObservableCollection<Package> package { get; set; }
        public string modDir { get; set; }
        public string exePath { get; set; }
        public string reloadedPath { get; set; }
        public bool emptySND { get; set; }
        public bool useCpk { get; set; }
        public string cpkLang { get; set; }

    }

    public class AemulusConfig
    {
        public string game { get; set; }
        public bool bottomUpPriority { get; set; }
        public ConfigP3F p3fConfig { get; set; }
        public ConfigP4G p4gConfig { get; set; }
        public ConfigP5 p5Config { get; set; }

    }

    public class ConfigP4G
    {
        public string modDir { get; set; }
        public string exePath { get; set; }
        public string reloadedPath { get; set; }
        public bool emptySND { get; set; }
        public bool useCpk { get; set; }
        public bool disableMessageBox { get; set; }
        public string cpkLang { get; set; }
    }

    public class ConfigP3F
    {
        public string modDir { get; set; }
        public string isoPath { get; set; }
        public string elfPath { get; set; }
        public string launcherPath { get; set; }
        public bool disableMessageBox { get; set; }
    }
    public class ConfigP5
    {
        public string modDir { get; set; }
        public string gamePath { get; set; }
        public string launcherPath { get; set; }
        public bool disableMessageBox { get; set; }
    }

    public class Packages
    {
        public ObservableCollection<Package> packages { get; set; }
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
