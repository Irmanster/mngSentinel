namespace Sentinel.Filters
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    using Sentinel.Filters.Interfaces;
    using Sentinel.Interfaces;
    using Sentinel.Support.Mvvm;

    [DataContract]
    public class Filter : ViewModelBase, IFilter
    {
        /// <summary>
        /// Is the filter enabled?  If so, it will remove anything matching from the output.
        /// </summary>
        private bool enabled;

        private string name;

        private string pattern;

        private LogEntryField field;

        private MatchMode mode = MatchMode.Exact;

        private Regex regex;

        public Filter()
        {
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Field" || e.PropertyName == "Mode" || e.PropertyName == "Pattern")
                {
                    if (Mode == MatchMode.RegularExpression && Pattern != null) regex = new Regex(Pattern);
                    OnPropertyChanged("Description");
                }
            };
        }

        public Filter(string name, LogEntryField field, string pattern)
        {
            Name = name;
            Pattern = pattern;
            Field = field;
            regex = new Regex(pattern);

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Field" || e.PropertyName == "Mode" || e.PropertyName == "Pattern")
                {
                    if (Mode == MatchMode.RegularExpression && Pattern != null) regex = new Regex(Pattern);
                    OnPropertyChanged("Description");
                }
            };
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the filter is enabled.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        public string Pattern
        {
            get
            {
                return pattern;
            }

            set
            {
                if (pattern != value)
                {
                    pattern = value;
                    OnPropertyChanged("Pattern");
                }
            }
        }

        public LogEntryField Field
        {
            get
            {
                return field;
            }

            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged("Field");
                }
            }
        }

        public MatchMode Mode
        {
            get
            {
                return mode;
            }

            set
            {
                if (mode != value)
                {
                    mode = value;
                    OnPropertyChanged("Mode");
                }
            }
        }

        public string Description
        {
            get
            {
                var modeDescription = "Exact";
                switch (Mode)
                {
                    case MatchMode.RegularExpression:
                        modeDescription = "RegEx";
                        break;
                    case MatchMode.CaseSensitive:
                        modeDescription = "Case sensitive";
                        break;
                    case MatchMode.CaseInsensitive:
                        modeDescription = "Case insensitive";
                        break;
                }

                return string.Format("{0} match of {1} in the {2} field", modeDescription, Pattern, Field);
            }
        }

        public bool IsMatch(ILogEntry logEntry)
        {
            Debug.Assert(logEntry != null, "LogEntry can not be null.");

            if (logEntry == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Pattern))
            {
                return false;
            }

            string target;

            switch (Field)
            {
                case LogEntryField.None:
                    target = string.Empty;
                    break;
                case LogEntryField.Type:
                    target = logEntry.Type;
                    break;
                case LogEntryField.System:
                    target = logEntry.System;
                    break;
                case LogEntryField.Classification:
                    target = string.Empty;
                    break;
                case LogEntryField.Thread:
                    target = logEntry.Thread;
                    break;
                case LogEntryField.Source:
                    target = logEntry.Source;
                    break;
                case LogEntryField.Description:
                    target = logEntry.Description;
                    break;
                case LogEntryField.Host:
                    target = string.Empty;
                    break;
                default:
                    target = string.Empty;
                    break;
            }

            switch (Mode)
            {
                case MatchMode.Exact:
                    return target.Equals(Pattern);
                case MatchMode.CaseSensitive:
                    return target.Contains(Pattern);
                case MatchMode.CaseInsensitive:
                    return target.ToLower().Contains(Pattern.ToLower());
                case MatchMode.RegularExpression:
                    return regex != null && regex.IsMatch(target);
                default:
                    return false;
            }
        }

#if DEBUG
        public override string ToString()
        {
            return Description;
        }
#endif
    }
}