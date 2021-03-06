namespace Sentinel.Highlighters
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Data;

    using Common.Logging;

    using Sentinel.Highlighters.Interfaces;
    using Sentinel.Interfaces;

    public class HighlighterConverter : IValueConverter
    {
        private static readonly ILog Log = LogManager.GetLogger<HighlighterConverter>();

        private readonly IHighlighter highlighter;

        public HighlighterConverter(IHighlighter highlighter)
        {
            this.highlighter = highlighter;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var match = false;
            if (value != null)
            {
                var entry = value as ILogEntry;
                if (entry == null)
                {
                    Log.WarnFormat("Expected 'value' to be an ILogEntry but found {0}", value);
                }
                else
                {
                    match = highlighter.Enabled && highlighter.IsMatch(entry);
                }
            }

            return match ? "Match" : "Not Match";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}