using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace AemulusModManager.Utilities
{

    public enum GameFilter
    {
        P1PSP,
        P3,
        P3P,
        P4G,
        P4GVita,
        P5,
        P5R,
        P5RSwitch,
        P5S,
        PQ,
        PQ2
    }
    public enum FeedFilter
    {
        Featured,
        Recent,
        Popular,
        None
    }
    public enum TypeFilter
    {
        Mods,
        WiPs,
        Sounds,
        Tools,
        Tutorials
    }
    public static class FeedGenerator
    {
        private static Dictionary<string, GameBananaModList> feed;
        public static bool error;
        public static Exception exception;
        public static GameBananaModList CurrentFeed = new GameBananaModList();
        public static double GetHeader(this HttpResponseMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues(key, out keys))
                return -1;
            return Double.Parse(keys.First());
        }
        public static async Task GetFeed(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, string search)
        {
            error = false;
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();
            // Remove oldest key if more than 15 pages are cached
            if (feed.Count > 15)
                feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);
            using (var httpClient = new HttpClient())
            {
                var requestUrl = GenerateUrl(page, game, type, filter, category, subcategory, perPage, search);
                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                {
                    CurrentFeed = feed[requestUrl];
                    return;
                }
                try
                {
                    var response = await httpClient.GetAsync(requestUrl);
                    var responseString = await response.Content.ReadAsStringAsync();
                    responseString = responseString.Replace(@"""_aModManagerIntegrations"": []", @"""_aModManagerIntegrations"": {}");
                    var records = JsonConvert.DeserializeObject<ObservableCollection<GameBananaRecord>>(responseString);
                    CurrentFeed = new GameBananaModList();
                    CurrentFeed.Records = records;
                    // Get record count from header
                    var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
                    if (numRecords != -1)
                    {
                        var totalPages = Convert.ToInt32(Math.Ceiling(numRecords / Convert.ToDouble(perPage)));
                        if (totalPages == 0)
                            totalPages = 1;
                        CurrentFeed.TotalPages = totalPages;
                    }
                }
                catch (Exception e)
                {
                    error = true;
                    exception = e;
                    return ;
                }
                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, CurrentFeed);
                else
                    feed[requestUrl] = CurrentFeed;
            }
        }
        private static string GenerateUrl(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, string search)
        {
            // Base
            var url = "https://gamebanana.com/apiv6/";
            switch (type)
            {
                case TypeFilter.Mods:
                    url += "Mod/";
                    break;
                case TypeFilter.Sounds:
                    url += "Sound/";
                    break;
                case TypeFilter.WiPs:
                    url += "Wip/";
                    break;
                case TypeFilter.Tools:
                    url += "Tool/";
                    break;
                case TypeFilter.Tutorials:
                    url += "Tutorial/";
                    break;
            }
            // Different starting endpoint if requesting all mods instead of specific category
            if (search != null)
            {
                url += $"ByName?_sName=*{search}*&_idGameRow=";
                switch (game)
                {
                    case GameFilter.P1PSP:
                        url += "12961&";
                        break;
                    case GameFilter.P3:
                        url += "8502&";
                        break;
                    case GameFilter.P3P:
                        url += "8583&";
                        break;
                    case GameFilter.P4G:
                        url += "8263&";
                        break;
                    case GameFilter.P4GVita:
                        url += "15703&";
                        break;
                    case GameFilter.P5:
                        url += "7545&";
                        break;
                    case GameFilter.P5R:
                        url += "8464&";
                        break;
                    case GameFilter.P5RSwitch:
                        url += "17354&";
                        break;
                    case GameFilter.P5S:
                        url += "9099&";
                        break;
                    case GameFilter.PQ:
                        url += "14377&";
                        break;
                    case GameFilter.PQ2:
                        url += "9561&";
                        break;
                }
            }
            else if (category.ID != null)
                url += "ByCategory?";
            else
            {
                url += $"ByGame?_aGameRowIds[]=";
                switch (game)
                {
                    case GameFilter.P1PSP:
                        url += "12961&";
                        break;
                    case GameFilter.P3:
                        url += "8502&";
                        break;
                    case GameFilter.P3P:
                        url += "8583&";
                        break;
                    case GameFilter.P4G:
                        url += "8263&";
                        break;
                    case GameFilter.P4GVita:
                        url += "15703&";
                        break;
                    case GameFilter.P5:
                        url += "7545&";
                        break;
                    case GameFilter.P5R:
                        url += "8464&";
                        break;
                    case GameFilter.P5RSwitch:
                        url += "17354&";
                        break;
                    case GameFilter.P5S:
                        url += "9099&";
                        break;
                    case GameFilter.PQ:
                        url += "14377&";
                        break;
                    case GameFilter.PQ2:
                        url += "9561&";
                        break;
                }
            }
            var extraProps = type == TypeFilter.Tutorials ? String.Empty : ",_aAlternateFileSources,_nDownloadCount,_aFiles,_aModManagerIntegrations";
            if (type != TypeFilter.WiPs)
                extraProps += ",_bIsObsolete,_sObsolescenceNotice";
            // Consistent args
            url += $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded,_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
                $"_nLikeCount{extraProps}&_aArgs[]=_sbIsNsfw = false&_nPerpage={perPage}";
            // Sorting filter
            switch (filter)
            {
                case FeedFilter.Recent:
                    url += "&_sOrderBy=_tsDateUpdated,DESC";
                    break;
                case FeedFilter.Featured:
                    url += "&_aArgs[]=_sbWasFeatured = true&_sOrderBy=_tsDateAdded,DESC";
                    break;
                case FeedFilter.Popular:
                    if (type != TypeFilter.Tutorials)
                        url += "&_sOrderBy=_nDownloadCount,DESC";
                    else
                        url += "&_sOrderBy=_nViewCount,DESC";
                    break;
            }
            // Choose subcategory or category
            if (subcategory.ID != null)
                url += $"&_aCategoryRowIds[]={subcategory.ID}";
            else if (category.ID != null)
                url += $"&_aCategoryRowIds[]={category.ID}";
            // Get page number
            url += $"&_nPage={page}";
            return url;
        }
    }
}
