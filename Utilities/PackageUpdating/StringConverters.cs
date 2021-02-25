using System;

namespace AemulusModManager.Utilities.PackageUpdating
{
    class StringConverters
    {
        // Load all suffixes in an array  
        static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string FormatSize(long bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMinutes < 60)
            {
                return Math.Floor(timeSpan.TotalMinutes).ToString() + "min";
            }
            else if (timeSpan.TotalHours < 24)
            {
                return Math.Floor(timeSpan.TotalHours).ToString() + "hr";
            }
            else if (timeSpan.TotalDays < 7)
            {
                return Math.Floor(timeSpan.TotalDays).ToString() + "d";
            }
            else if (timeSpan.TotalDays < 30.4)
            {
                return Math.Floor(timeSpan.TotalDays / 7).ToString() + "wk";
            }
            else if (timeSpan.TotalDays < 365.25)
            {
                return Math.Floor(timeSpan.TotalDays / 30.4).ToString() + "mo";
            }
            else
            {
                return Math.Floor(timeSpan.TotalDays % 365.25).ToString() + "yr";
            }
        }
    }
}

