using AemulusModManager.Utilities.PackageUpdating;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace AemulusModManager
{
    public class GameBananaAPIV4
    {
        [JsonProperty("_sName")]
        public string Title { get; set; }
        [JsonProperty("_aGame")]
        public GameBananaGame Game { get; set; }
        [JsonIgnore]
        public Uri Image => Media.Where(x => x.Type == "image").ToList().Count > 0 ? new Uri($"{Media[0].Base}/{Media[0].File}")
            : new Uri("https://images.gamebanana.com/static/img/DefaultEmbeddables/Sound.jpg");
        [JsonProperty("_aPreviewMedia")]
        public List<GameBananaMedia> Media { get; set; }
        [JsonProperty("_aSubmitter")]
        public GameBananaMember Owner { get; set; }
        [JsonProperty("_aFiles")]
        public List<GameBananaItemFile> Files { get; set; }
        [JsonProperty("_aAlternateFileSources")]
        public List<GameBananaAlternateFileSource> AlternateFileSources { get; set; }
        [JsonProperty("_bHasUpdates")]
        public bool? HasUpdates { get; set; }
        [JsonProperty("_aLatestUpdates")]
        public GameBananaItemUpdate[] Updates { get; set; }
        [JsonProperty("_aModManagerIntegrations")]
        public Dictionary<string, List<GameBananaModManagerIntegration>> ModManagerIntegrations { get; set; }
    }
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
        [JsonProperty("_sName")]
        public string Name { get; set; }
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
        [JsonProperty("_sName")]
        public string Title { get; set; }
        [JsonProperty("_aGame")]
        public GameBananaGame Game { get; set; }
        [JsonProperty("_sProfileUrl")]
        public Uri Link { get; set; }
        [JsonProperty("_aAlternateFileSources")]
        public List<GameBananaAlternateFileSource> AlternateFileSources { get; set; }
        [JsonIgnore]
        public bool HasAltLinks => AlternateFileSources != null;
        [JsonProperty("_aModManagerIntegrations")]
        public Dictionary<string, List<GameBananaModManagerIntegration>> ModManagerIntegrations { get; set; }
        [JsonIgnore]
        public Uri Image => Media.Where(x => x.Type == "image").ToList().Count > 0 ? new Uri($"{Media[0].Base}/{Media[0].File}")
            : SoundImage(Game.ID);
        [JsonProperty("_aPreviewMedia")]
        public List<GameBananaMedia> Media { get; set; }
        [JsonProperty("_sDescription")]
        public string Description { get; set; }
        [JsonIgnore]
        public bool HasDescription => Description.Length > 100;
        [JsonProperty("_sText")]
        public string Text { get; set; }
        [JsonProperty("_sObsolescenceNotice")]
        public string ObsolescenceNotice { get; set; }
        [JsonIgnore]
        public string ConvertedObsolesenceNotice => String.IsNullOrEmpty(ObsolescenceNotice) ?
            "This Mod has been marked as obsolete. It may no longer work and/or contain outdated information. It is retained for archival purposes." 
            : ConvertHtmlToText(ObsolescenceNotice);
        [JsonProperty("_bIsObsolete")]
        public bool IsObsolete { get; set; }
        [JsonIgnore]
        public string ConvertedText => ConvertHtmlToText(Text);
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
        public List<GameBananaItemFile> Files => AllFiles.Where(x => ModManagerIntegrations.ContainsKey(x.ID)
            && !x.Description.Contains(".disable-modbrowser")).ToList();
        [JsonProperty("_aCategory")]
        public GameBananaCategory Category { get; set; }
        [JsonProperty("_aRootCategory")]
        public GameBananaCategory RootCategory { get; set; }
        [JsonIgnore]
        public string CategoryName => StringConverters.FormatSingular(RootCategory.Name, Category.Name);
        [JsonIgnore]
        public bool HasLongCategoryName => CategoryName.Length > 30;
        [JsonIgnore]
        public bool HasDownloads => AllFiles != null && Files.Count > 0;
        [JsonIgnore]
        public bool Compatible => HasAltLinks || HasDownloads;

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
        private Uri SoundImage(int game)
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
        private string ConvertHtmlToText(string html)
        {
            // Newlines
            html = html.Replace("<br>", "\n");
            html = html.Replace(@"</li>", "\n");
            html = html.Replace(@"</h3>", "\n");
            html = html.Replace(@"</h2>", "\n");
            html = html.Replace(@"</h1>", "\n");
            html = html.Replace("<ul>", "\n");
            // Bullet point
            html = html.Replace("<li>", "• ");
            // Unique spaces
            html = html.Replace("&nbsp;", " ");
            html = html.Replace(@"\u00a0", " ");
            // Unique characters
            html = html.Replace("&amp;", "&");
            html = html.Replace("&gt;", ">");
            // Remove tabs
            html = html.Replace("\t", string.Empty);
            // Remove all unaccounted html tags
            html = Regex.Replace(html, "<.*?>", string.Empty);
            // Convert newlines of 3 or more to 2 newlines
            html = Regex.Replace(html, "[\\r\\n]{3,}", "\n\n", RegexOptions.Multiline);
            // Trim extra whitespace at start and end
            return html.Trim();
        }
    }
    public class GameBananaAlternateFileSource
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; } = "Mirror";
    }
    public class GameBananaModList
    {
        public ObservableCollection<GameBananaRecord> Records { get; set; }
        public int TotalPages { get; set; }
        public DateTime TimeFetched = DateTime.UtcNow;
        public bool IsValid => (DateTime.UtcNow - TimeFetched).TotalMinutes < 30;
    }
    public class GameBananaMedia
    {
        [JsonProperty("_sType")]
        public string Type { get; set; }
        [JsonProperty("_sUrl")]
        public Uri Audio { get; set; }
        [JsonProperty("_sBaseUrl")]
        public Uri Base { get; set; }
        [JsonProperty("_sFile")]
        public Uri File { get; set; }
        [JsonProperty("_sCaption")]
        public string Caption { get; set; }
    }
}
