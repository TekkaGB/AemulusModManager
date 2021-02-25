using System.Collections.Generic;

namespace AemulusModManager.Utilities.BmdPatching
{
    public class Msg
    {
        public string title;
        public string message;
    }

    public class BmdPatch
    {
        public string bmdPath { get; set; }
        public string title { get; set; }
        public string message { get; set; }
        public int index { get; set; }
    }
    public class Bmd_Patches
    {
        public int Version { get; set; }
        public List<BmdPatch> BmdPatches { get; set; }
    }
}
