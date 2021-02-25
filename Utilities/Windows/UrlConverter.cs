using System;
using System.Windows.Data;

namespace AemulusModManager
{
    // Used to convert url to displayable domain name
    class UrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value.ToString().Length == 0)
                return null;
            string url = value.ToString();
            Uri uri;
            if ((Uri.TryCreate(url, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Use validated URI here
                string host = uri.DnsSafeHost;
                switch (host)
                {
                    case "www.gamebanana.com":
                    case "gamebanana.com":
                        return "GameBanana";
                    case "nexusmods.com":
                    case "www.nexusmods.com":
                        return "Nexus";
                    case "www.shrinefox.com":
                    case "shrinefox.com":
                        return "ShrineFox";
                    case "www.github.com":
                    case "github.com":
                        return "GitHub";
                    default:
                        return "Other";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string Convert(string url)
        {
            if (url == null || url.Length == 0)
                return null;
            Uri uri;
            if ((Uri.TryCreate(url, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Use validated URI here
                string host = uri.DnsSafeHost;
                switch (host)
                {
                    case "www.gamebanana.com":
                    case "gamebanana.com":
                        return "GameBanana";
                    case "nexusmods.com":
                    case "www.nexusmods.com":
                        return "Nexus";
                    case "www.shrinefox.com":
                    case "shrinefox.com":
                        return "ShrineFox";
                    case "www.github.com":
                    case "github.com":
                        return "GitHub";
                    default:
                        return "Other";
                }
            }
            return null;
        }

    }
}
