using System;
using System.Collections.Generic;

namespace AemulusModManager.Utilities.BinaryPatching
{
    public class BinaryPatch
    {
        public string file { get; set; }
        public int? offset { get; set; }
        public string data { get; set; }
    }

    public class BinaryPatches
    {
        public int Version;
        public List<BinaryPatch> Patches { get; set; }
    }
}
