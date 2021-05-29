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
        P3,
        P4G,
        P5,
        P5S
    }
    public enum FeedFilter
    {
        Featured,
        Recent,
        Popular
    }
    public enum TypeFilter
    {
        Mods,
        WiPs,
        Sounds
    }
    public static class FeedGenerator
    {
        private static Dictionary<string, GameBananaModList> feed;
        public static bool error;
        public static Exception exception;
        public static async Task<ObservableCollection<GameBananaRecord>> GetFeed(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage)
        {
            error = false;
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();
            // Remove oldest key if more than 15 pages are cached
            if (feed.Count > 15)
                feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);
            using (var httpClient = new HttpClient())
            {
                var requestUrl = GenerateUrl(page, game, type, filter, category, subcategory, perPage);
                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                    return feed[requestUrl].Records;
                string responseString = "";
                try
                {
                    responseString = await httpClient.GetStringAsync(requestUrl);
                    // Fix for member becoming a list when empty instead of a dictionary
                    responseString = responseString.Replace(@"""_aModManagerIntegrations"": []", @"""_aModManagerIntegrations"": {}");
                }
                catch (Exception e)
                {
                    error = true;
                    exception = e;
                    return null;
                }
                var response = JsonConvert.DeserializeObject<GameBananaModList>(responseString);
                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, response);
                else
                    feed[requestUrl] = response;
                return response.Records;
            }
        }
        private static string GenerateUrl(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage)
        {
            // Base
            var url = "https://gamebanana.com/apiv3/";
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
            }
            // Different starting endpoint if requesting all mods instead of specific category
            if (category.ID != null)
                url += "ByCategory?";
            else
            {
                url += $"ByGame?_aGameRowIds[]=";
                switch (game)
                {
                    case GameFilter.P3:
                        url += "8502&";
                        break;
                    case GameFilter.P4G:
                        url += "8263&";
                        break;
                    case GameFilter.P5:
                        url += "7545&";
                        break;
                    case GameFilter.P5S:
                        url += "9099&";
                        break;
                }
            }
            // Consistent args
            url += $"&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy&_bReturnMetadata=true&_nPerpage={perPage}";
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
                    url += "&_sOrderBy=_nDownloadCount,DESC";
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
        public static GameBananaMetadata GetMetadata(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage)
        {
            var url = GenerateUrl(page, game, type, filter, category, subcategory, perPage);
            if (feed.ContainsKey(url))
                return feed[url].Metadata;
            else
                return null;
        }
    }
}
