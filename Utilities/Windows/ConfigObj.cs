﻿using System;
using System.Collections.ObjectModel;
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
        public bool updateAemulus { get; set; } = true;
        public bool darkMode { get; set; } = true;
        public ConfigP3F p3fConfig { get; set; }
        public ConfigP3P p3pConfig { get; set; }
        public ConfigP3PSwitch p3pSwitchConfig { get; set; }
        public ConfigP4G p4gConfig { get; set; }
        public ConfigP4GVita p4gVitaConfig { get; set; }
        public ConfigP5 p5Config { get; set; }
        public ConfigP5R p5rConfig { get; set; }
        public ConfigP5RSwitch p5rSwitchConfig { get; set; }
        public ConfigP5S p5sConfig { get; set; }
        public ConfigPQ pqConfig { get; set; }
        public ConfigPQ2 pq2Config { get; set; }
        public ConfigP1PSP p1pspConfig { get; set; }
        public double? LeftGridWidth { get; set; }
        public double? RightGridWidth { get; set; }
        public double? TopGridHeight { get; set; }
        public double? BottomGridHeight { get; set; }
        public double? RightTopGridHeight { get; set; }
        public double? RightBottomGridHeight { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public bool Maximized { get; set; }
    }

    public class ConfigP4G
    {
        public string modDir { get; set; }
        public string exePath { get; set; }
        public string reloadedPath { get; set; }
        public bool emptySND { get; set; }
        public bool useCpk { get; set; }
        public string cpkLang { get; set; }
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }
    public class ConfigP4GVita
    {
        public string modDir { get; set; }
        public string cpkName { get; set; } = "m0.cpk";
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }

    public class ConfigP1PSP
    {
        public string modDir { get; set; }
        public string texturesPath { get; set; }
        public string cheatsPath { get; set; }
        public string isoPath { get; set; }
        public string launcherPath { get; set; }
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public bool createIso { get; set; } = false;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }

    public class ConfigP3F
    {
        public string modDir { get; set; }
        public string isoPath { get; set; }
        public string elfPath { get; set; }
        public string launcherPath { get; set; }
        public string cheatsPath { get; set; }
        public string cheatsWSPath { get; set; }
        public string texturesPath { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool advancedLaunchOptions { get; set; }
        public bool deleteOldVersions { get; set; }
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }

    }
    public class ConfigP3P
    {
        public string modDir { get; set; }
        public string texturesPath { get; set; }
        public string cheatsPath { get; set; }
        public string isoPath { get; set; }
        public string cpkName { get; set; } = "mod.cpk";
        public string launcherPath { get; set; }
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }

    public class ConfigP3PSwitch
    {
        public string modDir { get; set; }
        public string gamePath { get; set; }
        public string cpkName { get; set; } = "mod.cpk";
        public string launcherPath { get; set; }
        public string language { get; set; } = "English";
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }

    public class ConfigP5
    {
        public string modDir { get; set; }
        public string gamePath { get; set; }
        public string launcherPath { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool deleteOldVersions { get; set; }
        public bool updatesEnabled { get; set; } = true;
        public string CpkName { get; set; } = "mod";
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }

    }
    public class ConfigP5R
    {
        public string modDir { get; set; }
        public string cpkName { get; set; } = "mod.cpk";
        public string language { get; set; } = "English";
        public string version { get; set; } = "1.02";
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }
    public class ConfigP5RSwitch
    {
        public string modDir { get; set; }
        public string gamePath { get; set; }
        public string launcherPath { get; set; }
        public string language { get; set; } = "English";
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }
    public class ConfigPQ2
    {
        public string modDir { get; set; }
        public string ROMPath { get; set; }
        public string launcherPath { get; set; }
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }
    public class ConfigPQ
    {
        public string modDir { get; set; }
        public string ROMPath { get; set; }
        public string launcherPath { get; set; }
        public bool deleteOldVersions { get; set; }
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
        public string lastUnpacked { get; set; }
    }

    public class ConfigP5S
    {
        public string modDir { get; set; } = "";
        public bool buildWarning { get; set; } = true;
        public bool buildFinished { get; set; } = true;
        public bool updateConfirm { get; set; } = true;
        public bool updateChangelog { get; set; } = true;
        public bool updateAll { get; set; } = true;
        public bool deleteOldVersions { get; set; }
        public bool updatesEnabled { get; set; } = true;
        public string loadout { get; set; }
    }
    public class Packages
    {
        public ObservableCollection<Package> packages { get; set; }
        public bool showHiddenPackages { get; set; } = true;
    }
    public class Package
    {
        public string name { get; set; }
        public string path { get; set; }
        public bool enabled { get; set; }
        public string id { get; set; }
        public bool hidden { get; set; } = false;
        public string link { get; set; }
    }

    public class Metadata
    {
        public string name { get; set; }
        public string id { get; set; }
        public string author { get; set; }
        public string version { get; set; }
        public string link { get; set; }
        public string description { get; set; }
        public string skippedVersion { get; set; }
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
        public string skippedVersion { get; set; }
        public bool hidden { get; set; } = false;
    }
}
