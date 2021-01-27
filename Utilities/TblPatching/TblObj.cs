using System;
using System.Collections.Generic;

namespace AemulusModManager.Utilities.TblPatching
{
    public class NameSection
    {
        public int namesSize;
        public int pointersSize;
        public List<byte[]> names;
        public List<UInt16> pointers;
    }

    public class Table
    {
        public string tableName;
        public List<Section> sections;
        public List<NameSection> nameSections;
    }

    public class Section
    {
        public int size;
        public byte[] data;
    }

    public class TablePatch
    {
        public string tbl { get; set; }
        public int? section { get; set; }
        public int? offset { get; set; }
        public string data { get; set; }
        public int? index { get; set; }
        public string name { get; set; }
    }

    public class TablePatches
    {
        public int Version;
        public List<TablePatch> Patches { get; set; }
    }
}
