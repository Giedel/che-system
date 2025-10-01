//-- View_Model_Base.cs --

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace che_system.view_model
{
    public abstract class View_Model_Base : INotifyPropertyChanged
    {
        private string _searchText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    OnSearchTextChanged();
                }
            }
        }

        protected virtual void OnSearchTextChanged()
        {
            // Override in derived classes to implement specific search logic
        }

        protected void ClearSearch()
        {
            SearchText = string.Empty;
        }

        // Advanced search parsing
        protected List<SearchFilter> ParseSearchQuery(string query)
        {
            var filters = new List<SearchFilter>();
            if (string.IsNullOrWhiteSpace(query))
                return filters;

            // Split by logical operators
            var parts = Regex.Split(query.Trim(), @"\s+(AND|OR)\s+", RegexOptions.IgnoreCase);
            // ... (keep the logic but fix the call)

            // For simplicity, split by spaces and parse each part
            var words = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var trimmed = word.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Check for field-specific: "field:value"
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var field = trimmed.Substring(0, colonIndex).ToLower().Trim();
                    var value = trimmed.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(value))
                    {
                        // Check for operators in value
                        var opIndex = value.IndexOfAny(new char[] { '>', '<', '=', '!' });
                        if (opIndex == 0)
                        {
                            var op = value[0].ToString();
                            var val = value.Substring(1).Trim();
                            filters.Add(new SearchFilter(field, val, op));
                        }
                        // Check for range
                        else if (value.Contains("-") || value.Contains(" to "))
                        {
                            var rangeParts = value.Contains(" to ") ? value.Split(" to ") : value.Split('-');
                            if (rangeParts.Length == 2)
                            {
                                filters.Add(new SearchFilter(field, rangeParts[0].Trim(), rangeParts[1].Trim(), "range"));
                            }
                        }
                        else
                        {
                            filters.Add(new SearchFilter(field, value, "=", ""));
                        }
                        continue;
                    }
                }

                // Simple text search
                filters.Add(new SearchFilter("", trimmed, "contains", ""));
            }

            return filters;
        }

        // Enhanced filtering methods
        protected ObservableCollection<T> FilterCollection<T>(ObservableCollection<T> source, List<SearchFilter> filters) where T : class
        {
            if (filters.Count == 0)
                return new ObservableCollection<T>(source);

            return new ObservableCollection<T>(source.Where(item => ApplyFiltersToItem(item, filters)));
        }

        protected virtual bool ApplyFiltersToItem<T>(T item, List<SearchFilter> filters) where T : class
        {
            bool allMatch = true;
            foreach (var filter in filters)
            {
                bool match = false;
                if (string.IsNullOrEmpty(filter.Field) || filter.Op == "contains" || filter.Op == "phrase")
                {
                    // Global text search
                    var props = typeof(T).GetProperties()
                        .Where(p => p.PropertyType == typeof(string))
                        .Select(p => p.GetValue(item)?.ToString() ?? "");
                    if (filter.Op == "phrase")
                    {
                        match = props.Any(propValue => propValue.IndexOf(filter.Value, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    else
                    {
                        match = props.Any(propValue => propValue.IndexOf(filter.Value, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                }
                else
                {
                    match = ApplyFieldFilterToItem(item, filter);
                }
                allMatch &= match;
                if (!allMatch) break;
            }
            return allMatch;
        }

        protected virtual bool ApplyFieldFilterToItem<T>(T item, SearchFilter filter) where T : class
        {
            // Override in derived classes to map field names to properties
            // For numeric/date, use TryParse or DateTime.TryParse
            return false; // Default - no match
        }

        // Simple numeric filter helper for derived classes
        protected bool ApplyNumericFilter(object value, string op, string filterValue)
        {
            if (value == null) return false;
            if (!double.TryParse(value.ToString(), out double itemValue) || !double.TryParse(filterValue, out double filterVal))
                return false;

            return op switch
            {
                ">" => itemValue > filterVal,
                "<" => itemValue < filterVal,
                ">=" => itemValue >= filterVal,
                "<=" => itemValue <= filterVal,
                "=" => itemValue == filterVal,
                "range" => throw new ArgumentException("Use ApplyRangeFilter for ranges"),
                _ => false
            };
        }

        protected bool ApplyRangeFilter(object value, string low, string high)
        {
            if (value == null) return false;
            if (!double.TryParse(value.ToString(), out double itemValue) ||
                !double.TryParse(low, out double lowVal) || !double.TryParse(high, out double highVal))
                return false;

            return itemValue >= lowVal && itemValue <= highVal;
        }

        protected bool ApplyDateFilter(object value, string op, string filterValue)
        {
            if (value == null) return false;
            if (!DateTime.TryParse(value.ToString(), out DateTime itemDate) || !DateTime.TryParse(filterValue, out DateTime filterDate))
                return false;

            return op switch
            {
                ">" => itemDate > filterDate,
                "<" => itemDate < filterDate,
                ">=" => itemDate >= filterDate,
                "<=" => itemDate <= filterDate,
                "=" => itemDate.Date == filterDate.Date,
                "range" => throw new ArgumentException("Use ApplyDateRangeFilter for ranges"),
                _ => false
            };
        }

        protected bool ApplyDateRangeFilter(object value, string low, string high)
        {
            if (value == null) return false;
            if (!DateTime.TryParse(value.ToString(), out DateTime itemDate) ||
                !DateTime.TryParse(low, out DateTime lowDate) || !DateTime.TryParse(high, out DateTime highDate))
                return false;

            return itemDate >= lowDate && itemDate <= highDate;
        }

        // Basic text filter methods (backward compatibility)
        protected ObservableCollection<T> FilterCollection<T>(ObservableCollection<T> source, Func<T, bool> predicate) where T : class
        {
            return new ObservableCollection<T>(source.Where(predicate ?? (t => true)));
        }

        protected ObservableCollection<T> FilterCollection<T>(ObservableCollection<T> source, string searchText, Func<T, string> getProperty) where T : class
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new ObservableCollection<T>(source);

            var filters = ParseSearchQuery(searchText);
            return FilterCollection(source, filters);
        }

        protected ObservableCollection<T> FilterCollection<T>(ObservableCollection<T> source, string searchText, params Func<T, string>[] properties) where T : class
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new ObservableCollection<T>(source);

            var filters = ParseSearchQuery(searchText);
            return FilterCollection(source, filters);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SearchFilter
    {
        public string Field { get; set; } = "";
        public string Value { get; set; } = "";
        public string Op { get; set; } = "contains";
        public string Value2 { get; set; } = ""; // for range
        public bool IsTextSearch { get; set; } = false;

        public SearchFilter(string field, string value, string op = "=", string value2 = "")
        {
            Field = field;
            Value = value;
            Op = op;
            Value2 = value2;
            if (op == "contains") IsTextSearch = true;
        }

        public SearchFilter(string op, string value)
        {
            Op = op;
            Value = value;
            Field = "";
            IsTextSearch = op == "contains";
        }
    }
}
