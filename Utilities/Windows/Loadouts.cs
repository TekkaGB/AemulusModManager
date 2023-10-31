using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AemulusModManager.Utilities.Windows
{
    public class Loadouts
    {
        public ObservableCollection<string> LoadoutItems;

        public Loadouts(string game)
        {
            LoadoutItems = new ObservableCollection<string>();
            LoadLoadouts(game);
        }

        public void LoadLoadouts(string game)
        {
            string configPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config";
            Utilities.ParallelLogger.Log($"[INFO] Loading loadouts for {game}");
            Directory.CreateDirectory($@"{configPath}\{game}");
            // If the old single loadout file existed, convert it to the new one with a name of default
            if (File.Exists($@"{configPath}\{game.Replace(" ", "")}Packages.xml") && !File.Exists($@"{configPath}\{game}\Default.xml"))
            {
                Utilities.ParallelLogger.Log("[INFO] Old loadout detected, converting to new one with name \"Default\"");
                File.Move($@"{configPath}\{game.Replace(" ", "")}Packages.xml", $@"{configPath}\{game}\Default.xml");
            }

            // Get all loadouts for the current game
            string[] loadoutFiles = Directory.GetFiles($@"{configPath}\{game}").Where((path) => Path.GetExtension(path) == ".xml").ToArray();
            
            // Create a default loadout if none exists
            if(loadoutFiles.Length == 0)
            {
                loadoutFiles = loadoutFiles.Append("Default").ToArray();
            }

            // Change the loadout items to the new ones
            LoadoutItems = new ObservableCollection<string>();
            foreach(string loadout in loadoutFiles)
            {
                LoadoutItems.Add(Path.GetFileNameWithoutExtension(loadout));
            }
            LoadoutItems.Add("Add new loadout");
        }

    }
}
