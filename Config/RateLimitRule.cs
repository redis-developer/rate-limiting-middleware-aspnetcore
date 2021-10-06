using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RateLimitingMiddleware.Config
{
    public class RateLimitRule
    {
        private static readonly Regex TimePattern = new ("([0-9]+(s|m|d|h))"); 
        private enum TimeUnit
        {
            s = 1,
            m = 60,
            h = 3600,
            d = 86400
        }

        internal string PathKey => string.IsNullOrEmpty(Path) ? Path : PathRegex;
        public string Path { get; set; }
        public string PathRegex { get; set; }
        public string Window { get; set; }
        public int MaxRequests { get; set; }
        private int _windowSeconds = 0;
        internal int WindowSeconds
        {
            get
            {
                if (_windowSeconds < 1)
                {
                    _windowSeconds = ParseTime(Window);
                }
                return _windowSeconds;
            }
        }
        
        public bool MatchPath(string path)
        {
            if (!string.IsNullOrEmpty(Path))
            {
                return  path.Equals(Path, StringComparison.InvariantCultureIgnoreCase);
            }
            if (!string.IsNullOrEmpty(PathRegex))
            {
                return Regex.IsMatch(path, PathRegex);
            }
            return false;
        }
        
        private static int ParseTime(string timeStr)
        {
            var match = TimePattern.Match(timeStr);
            if (string.IsNullOrEmpty(match.Value))
                throw new ArgumentException("Rate limit window was not provided or was not " +
                                            "properly formatted, must be of the form ([0-9]+(s|m|d|h))");
            var unit = Enum.Parse<TimeUnit>(match.Value.Last().ToString());
            var num = int.Parse(match.Value.Substring(0, match.Value.Length - 1));
            return num * (int) unit;
        }
    }
}