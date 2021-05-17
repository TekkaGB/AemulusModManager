using AemulusModManager.Utilities.PackageUpdating;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace AemulusModManager
{
    /* Disclaimer: These classes are taken (slightly modified) from Reloaded II's Gamebanana Resolver*/
    public class GameBananaItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("Owner().name")]
        public string Owner { get; set; }
        [JsonProperty("Game().name")]
        public string Game { get; set; }
        [JsonProperty("Updates().bSubmissionHasUpdates()")]
        public bool HasUpdates { get; set; }

        [JsonProperty("Updates().aGetLatestUpdates()")]
        public GameBananaItemUpdate[] Updates { get; set; }

        [JsonProperty("Files().aFiles()")]
        public Dictionary<string, GameBananaItemFile> Files { get; set; }
        [JsonProperty("Preview().sSubFeedImageUrl()")]
        public Uri SubFeedImage { get; set; }
        [JsonProperty("Preview().sStructuredDataFullsizeUrl()")]
        public Uri EmbedImage { get; set; }

    }
    public class GameBananaCategory
    {
        [JsonProperty("_idRow")]
        public int? ID { get; set; }
        [JsonProperty("_idParentCategoryRow")]
        public int? RootID { get; set; }
        [JsonProperty("_sModelName")]
        public string Model { get; set; }
        [JsonProperty("_sName")]
        public string Name { get; set; }
        [JsonProperty("_sIconUrl")]
        public Uri Icon { get; set; }
        [JsonIgnore]
        public bool HasIcon => Icon.OriginalString.Length > 0;
    }
    public class GameBananaMember
    {
        [JsonProperty("_sName")]
        public string Name { get; set; }
        [JsonProperty("_sAvatarUrl")]
        public Uri Avatar { get; set; }
        [JsonProperty("_sUpicUrl")]
        public Uri Upic { get; set; }
        [JsonIgnore]
        public bool HasUpic => Upic.OriginalString.Length > 0;
    }
    public class GameBananaGame
    {
        [JsonProperty("_idRow")]
        public int ID { get; set; }
    }
    public class GameBananaModManagerIntegration
    {
        [JsonProperty("_sInstallerName")]
        public string Name { get; set; }
        [JsonProperty("_sInstallerUrl")]
        public Uri Url { get; set; }
        [JsonProperty("_sIconClasses")]
        public string Icon { get; set; }
        [JsonProperty("_sDownloadUrl")]
        public string DownloadUrl { get; set; }
    }
    public class GameBananaRecord
    {
        public Uri SoundImage(int game)
        {
            // Get different Sound thumbnail per game
            switch (game)
            {
                case 8502:
                    return new Uri("https://media.discordapp.net/attachments/792245872259235850/842426607712993351/P3FSound.png");
                case 8263:
                    return new Uri("https://media.discordapp.net/attachments/792245872259235850/842426608882679818/P4GSound.png");
                case 7545:
                    return new Uri("https://media.discordapp.net/attachments/792245872259235850/842426604789170236/P5Sound.png");
                case 9099:
                    return new Uri("https://media.discordapp.net/attachments/792245872259235850/842426607490170891/P5SSound.png");
                default:
                    return new Uri("https://images.gamebanana.com/static/img/DefaultEmbeddables/Sound.jpg");
            }
        }
        [JsonProperty("_sName")]
        public string Title { get; set; }
        [JsonProperty("_aGame")]
        public GameBananaGame Game { get; set; }
        [JsonProperty("_sProfileUrl")]
        public Uri Link { get; set; }
        [JsonProperty("_aModManagerIntegrations")]
        public Dictionary<string, List<GameBananaModManagerIntegration>> ModManagerIntegrations { get; set; }
        [JsonIgnore]
        public Uri Image => Media.Count > 0 ? new Uri($"{Media[0].Base}/{Media[0].File}")
            : SoundImage(Game.ID);
        [JsonProperty("_aPreviewMedia")]
        public List<GameBananaImage> Media { get; set; }
        [JsonProperty("_sDescription")]
        public string Description { get; set; }
        [JsonIgnore]
        public bool HasDescription => Description.Length > 100;
        [JsonProperty("_sText")]
        public string Text { get; set; }
        [JsonIgnore]
        public string ConvertedText => Regex.Replace(Regex.Replace(Text.Replace("<br>", "\n").Replace("&nbsp;", " ")
            .Replace("<ul>", "\n").Replace("<li>", "• ").Replace(@"\u00a0", " ").Replace(@"</li>", "\n").Replace("&amp;", "&")
            .Replace(@"</h3>", "\n").Replace(@"</h2>", "\n").Replace(@"</h1>", "\n"), "<.*?>", string.Empty),
            "[\\r\\n]{3,}", "\n\n", RegexOptions.Multiline).Trim();
        [JsonProperty("_nViewCount")]
        public int Views { get; set; }
        [JsonProperty("_nLikeCount")]
        public int Likes { get; set; }
        [JsonProperty("_nDownloadCount")]
        public int Downloads { get; set; }
        [JsonIgnore]
        public string DownloadString => StringConverters.FormatNumber(Downloads);
        [JsonIgnore]
        public string ViewString => StringConverters.FormatNumber(Views);
        [JsonIgnore]
        public string LikeString => StringConverters.FormatNumber(Likes);
        [JsonProperty("_aSubmitter")]
        public GameBananaMember Owner { get; set; }
        [JsonProperty("_aFiles")]
        public List<GameBananaItemFile> AllFiles { get; set; }
        [JsonIgnore]
        public List<GameBananaItemFile> Files => AllFiles.Where(x => ModManagerIntegrations.ContainsKey(x.ID)).ToList();
        [JsonProperty("_aCategory")]
        public GameBananaCategory Category { get; set; }
        [JsonProperty("_aRootCategory")]
        public GameBananaCategory RootCategory { get; set; }
        [JsonIgnore]
        public string CategoryName => StringConverters.FormatSingular(RootCategory.Name, Category.Name);
        [JsonIgnore]
        public bool HasLongCategoryName => CategoryName.Length > 30;
        [JsonIgnore]
        public bool Compatible => Files.Count > 0 && Category.ID != 3827 && Category.ID != 959;

        [JsonProperty("_tsDateUpdated")]
        public long DateUpdatedLong { get; set; }
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        [JsonIgnore]
        public DateTime DateUpdated => Epoch.AddSeconds(DateUpdatedLong);
        [JsonProperty("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);
        [JsonIgnore]
        public string DateAddedFormatted => $"Added {StringConverters.FormatTimeAgo(DateTime.UtcNow - DateAdded)}";
        [JsonIgnore]
        public bool HasUpdates => DateAdded.CompareTo(DateUpdated) != 0;
        [JsonIgnore]
        public string DateUpdatedAgo => $"Updated {StringConverters.FormatTimeAgo(DateTime.UtcNow - DateUpdated)}";
    }
    public class GameBananaModList
    {
        [JsonProperty("_aMetadata")]
        public GameBananaMetadata Metadata { get; set; }
        [JsonProperty("_aRecords")]
        public ObservableCollection<GameBananaRecord> Records { get; set; }
        [JsonIgnore]
        public DateTime TimeFetched = DateTime.UtcNow;
        [JsonIgnore]
        public bool IsValid => (DateTime.UtcNow - TimeFetched).TotalMinutes < 30;
    }
    public class GameBananaCategories
    {
        [JsonProperty("_aMetadata")]
        public GameBananaMetadata Metadata { get; set; }
        [JsonProperty("_aRecords")]
        public List<GameBananaCategory> Categories { get; set; }
    }
    public class GameBananaMetadata
    {
        [JsonProperty("_nRecordCount")]
        public int Records { get; set; }
        [JsonProperty("_nTotalRecordCount")]
        public int TotalRecords { get; set; }
        [JsonProperty("_nPageCount")]
        public int TotalPages { get; set; }
    }
    public class GameBananaImage
    {
        [JsonProperty("_sBaseUrl")]
        public Uri Base { get; set; }
        [JsonProperty("_sFile")]
        public Uri File { get; set; }
        [JsonProperty("_sCaption")]
        public string Caption { get; set; }
    }
}
