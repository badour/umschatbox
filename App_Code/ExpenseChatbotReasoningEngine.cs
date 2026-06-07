using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

public static class ExpenseChatbotReasoningEngine
{
    private const int MaxNumericInsights = 3;

    public static string BuildAnalysis(ExpenseChatbotQueryType queryType, DataTable results, int totalRowCount)
    {
        if (results == null || results.Rows.Count == 0)
        {
            return string.Empty;
        }

        List<string> insights = new List<string>();
        List<NumericColumnProfile> numericProfiles = GetNumericProfiles(results);
        DateColumnProfile dateProfile = GetBestDateProfile(results);

        insights.Add(BuildIntro(queryType, results.Rows.Count, totalRowCount));

        AddAggregateInsights(insights, queryType, numericProfiles);
        AddDateRangeInsight(insights, dateProfile);
        AddTrendInsight(insights, results, numericProfiles, dateProfile);
        AddCategoryInsight(insights, results, numericProfiles);
        AddAnomalyInsight(insights, numericProfiles);

        if (insights.Count == 1)
        {
            insights.Add("I did not find enough numeric or date detail for deeper trend analysis, so I am showing the matching records for review.");
        }

        return "Analysis:\n- " + string.Join("\n- ", insights.ToArray());
    }

    private static string BuildIntro(ExpenseChatbotQueryType queryType, int analyzedRows, int totalRowCount)
    {
        string scope = totalRowCount > analyzedRows
            ? string.Format("the first {0} displayed row(s) out of {1} returned row(s)", analyzedRows, totalRowCount)
            : string.Format("{0} returned row(s)", analyzedRows);

        switch (queryType)
        {
            case ExpenseChatbotQueryType.ExpenseNetValue:
                return "I treated this as an accounting summary question and reviewed " + scope + " for totals, averages, and period coverage.";

            case ExpenseChatbotQueryType.FixedAssetsByAccount:
                return "I treated this as a fixed-assets analysis question and reviewed " + scope + " for account-level concentration and net values.";

            case ExpenseChatbotQueryType.PersonPaymentTotal:
                return "I treated this as a payee/person payment analysis and reviewed " + scope + " for total paid amount and date coverage.";

            default:
                return "I reviewed " + scope + " before listing them, looking for useful totals, dates, and unusual values.";
        }
    }

    private static void AddAggregateInsights(List<string> insights, ExpenseChatbotQueryType queryType, List<NumericColumnProfile> numericProfiles)
    {
        List<NumericColumnProfile> selectedProfiles = numericProfiles
            .OrderByDescending(profile => GetNumericColumnPriority(profile.Column.ColumnName, queryType))
            .ThenBy(profile => profile.Column.Ordinal)
            .Take(MaxNumericInsights)
            .ToList();

        for (int index = 0; index < selectedProfiles.Count; index++)
        {
            NumericColumnProfile profile = selectedProfiles[index];

            if (profile.Values.Count == 1)
            {
                insights.Add(string.Format(
                    "{0} is {1}.",
                    FormatColumnName(profile.Column.ColumnName),
                    FormatDecimal(profile.Sum)));
            }
            else
            {
                insights.Add(string.Format(
                    "{0}: total {1}, average {2}, minimum {3}, maximum {4}.",
                    FormatColumnName(profile.Column.ColumnName),
                    FormatDecimal(profile.Sum),
                    FormatDecimal(profile.Average),
                    FormatDecimal(profile.Min),
                    FormatDecimal(profile.Max)));
            }
        }
    }

    private static void AddDateRangeInsight(List<string> insights, DateColumnProfile dateProfile)
    {
        if (dateProfile == null || dateProfile.Values.Count == 0)
        {
            return;
        }

        insights.Add(string.Format(
            "The date range in {0} runs from {1} to {2}.",
            FormatColumnName(dateProfile.Column.ColumnName),
            dateProfile.Min.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dateProfile.Max.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }

    private static void AddTrendInsight(List<string> insights, DataTable results, List<NumericColumnProfile> numericProfiles, DateColumnProfile dateProfile)
    {
        if (dateProfile == null)
        {
            return;
        }

        NumericColumnProfile amountProfile = numericProfiles
            .OrderByDescending(profile => GetAmountPriority(profile.Column.ColumnName))
            .ThenBy(profile => profile.Column.Ordinal)
            .FirstOrDefault();

        if (amountProfile == null)
        {
            return;
        }

        Dictionary<int, decimal> totalsByYear = new Dictionary<int, decimal>();

        foreach (DataRow row in results.Rows)
        {
            DateTime dateValue;
            decimal amountValue;

            if (!TryGetDate(row[dateProfile.Column], out dateValue) || !TryGetDecimal(row[amountProfile.Column], out amountValue))
            {
                continue;
            }

            int year = dateValue.Year;
            if (!totalsByYear.ContainsKey(year))
            {
                totalsByYear[year] = 0M;
            }

            totalsByYear[year] += amountValue;
        }

        if (totalsByYear.Count < 2)
        {
            return;
        }

        List<int> years = totalsByYear.Keys.OrderBy(year => year).ToList();
        int firstYear = years.First();
        int lastYear = years.Last();
        decimal firstTotal = totalsByYear[firstYear];
        decimal lastTotal = totalsByYear[lastYear];
        decimal difference = lastTotal - firstTotal;

        if (difference == 0M)
        {
            insights.Add(string.Format(
                "The yearly total for {0} is stable between {1} and {2}.",
                FormatColumnName(amountProfile.Column.ColumnName),
                firstYear,
                lastYear));
            return;
        }

        string direction = difference > 0M ? "increased" : "decreased";
        insights.Add(string.Format(
            "Trend: {0} {1} from {2} in {3} to {4} in {5}.",
            FormatColumnName(amountProfile.Column.ColumnName),
            direction,
            FormatDecimal(firstTotal),
            firstYear,
            FormatDecimal(lastTotal),
            lastYear));
    }

    private static void AddCategoryInsight(List<string> insights, DataTable results, List<NumericColumnProfile> numericProfiles)
    {
        DataColumn categoryColumn = GetBestCategoryColumn(results);
        if (categoryColumn == null)
        {
            return;
        }

        NumericColumnProfile amountProfile = numericProfiles
            .OrderByDescending(profile => GetAmountPriority(profile.Column.ColumnName))
            .ThenBy(profile => profile.Column.Ordinal)
            .FirstOrDefault();

        if (amountProfile == null)
        {
            AddCategoryCountInsight(insights, results, categoryColumn);
            return;
        }

        Dictionary<string, decimal> totalsByCategory = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in results.Rows)
        {
            string category = Convert.ToString(row[categoryColumn], CultureInfo.CurrentCulture);
            decimal amount;

            if (string.IsNullOrWhiteSpace(category) || !TryGetDecimal(row[amountProfile.Column], out amount))
            {
                continue;
            }

            if (!totalsByCategory.ContainsKey(category))
            {
                totalsByCategory[category] = 0M;
            }

            totalsByCategory[category] += amount;
        }

        if (totalsByCategory.Count == 0)
        {
            return;
        }

        KeyValuePair<string, decimal> topCategory = totalsByCategory.OrderByDescending(pair => Math.Abs(pair.Value)).First();
        decimal totalAbsoluteValue = totalsByCategory.Values.Sum(value => Math.Abs(value));
        string shareText = totalAbsoluteValue == 0M
            ? "the largest absolute value"
            : string.Format("{0:P0} of the category total", Math.Abs(topCategory.Value) / totalAbsoluteValue);

        insights.Add(string.Format(
            "The largest category by {0} is '{1}' with {2}, representing {3}.",
            FormatColumnName(amountProfile.Column.ColumnName),
            topCategory.Key,
            FormatDecimal(topCategory.Value),
            shareText));
    }

    private static void AddCategoryCountInsight(List<string> insights, DataTable results, DataColumn categoryColumn)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in results.Rows)
        {
            string category = Convert.ToString(row[categoryColumn], CultureInfo.CurrentCulture);
            if (string.IsNullOrWhiteSpace(category))
            {
                continue;
            }

            if (!counts.ContainsKey(category))
            {
                counts[category] = 0;
            }

            counts[category]++;
        }

        if (counts.Count == 0)
        {
            return;
        }

        KeyValuePair<string, int> topCategory = counts.OrderByDescending(pair => pair.Value).First();
        insights.Add(string.Format(
            "The most frequent {0} is '{1}' with {2} row(s).",
            FormatColumnName(categoryColumn.ColumnName),
            topCategory.Key,
            topCategory.Value));
    }

    private static void AddAnomalyInsight(List<string> insights, List<NumericColumnProfile> numericProfiles)
    {
        NumericColumnProfile profile = numericProfiles
            .Where(item => item.Values.Count >= 4)
            .OrderByDescending(item => GetAmountPriority(item.Column.ColumnName))
            .ThenBy(item => item.Column.Ordinal)
            .FirstOrDefault();

        if (profile == null || profile.StandardDeviation <= 0M)
        {
            return;
        }

        decimal threshold = profile.Average + (2M * profile.StandardDeviation);
        if (profile.Max <= threshold)
        {
            return;
        }

        insights.Add(string.Format(
            "Possible anomaly: {0} has a high value of {1}, which is well above the average of {2}.",
            FormatColumnName(profile.Column.ColumnName),
            FormatDecimal(profile.Max),
            FormatDecimal(profile.Average)));
    }

    private static List<NumericColumnProfile> GetNumericProfiles(DataTable results)
    {
        List<NumericColumnProfile> profiles = new List<NumericColumnProfile>();

        foreach (DataColumn column in results.Columns)
        {
            List<decimal> values = new List<decimal>();

            foreach (DataRow row in results.Rows)
            {
                decimal value;
                if (TryGetDecimal(row[column], out value))
                {
                    values.Add(value);
                }
            }

            if (values.Count == 0)
            {
                continue;
            }

            bool nativeNumeric = IsNativeNumericType(column.DataType);
            bool enoughParsedValues = values.Count >= Math.Max(1, results.Rows.Count / 2);

            if (nativeNumeric || enoughParsedValues)
            {
                profiles.Add(new NumericColumnProfile(column, values));
            }
        }

        return profiles;
    }

    private static DateColumnProfile GetBestDateProfile(DataTable results)
    {
        List<DateColumnProfile> profiles = new List<DateColumnProfile>();

        foreach (DataColumn column in results.Columns)
        {
            List<DateTime> values = new List<DateTime>();

            foreach (DataRow row in results.Rows)
            {
                DateTime value;
                if (TryGetDate(row[column], out value))
                {
                    values.Add(value);
                }
            }

            if (values.Count == 0)
            {
                continue;
            }

            bool nativeDate = column.DataType == typeof(DateTime) || column.DataType == typeof(DateTimeOffset);
            bool enoughParsedValues = values.Count >= Math.Max(1, results.Rows.Count / 2);

            if (nativeDate || enoughParsedValues || IsDateColumnName(column.ColumnName))
            {
                profiles.Add(new DateColumnProfile(column, values));
            }
        }

        return profiles
            .OrderByDescending(profile => IsDateColumnName(profile.Column.ColumnName) ? 1 : 0)
            .ThenBy(profile => profile.Column.Ordinal)
            .FirstOrDefault();
    }

    private static DataColumn GetBestCategoryColumn(DataTable results)
    {
        List<DataColumn> candidates = new List<DataColumn>();

        foreach (DataColumn column in results.Columns)
        {
            if (IsNativeNumericType(column.DataType) || column.DataType == typeof(DateTime))
            {
                continue;
            }

            int nonEmptyCount = 0;
            HashSet<string> distinctValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in results.Rows)
            {
                string value = Convert.ToString(row[column], CultureInfo.CurrentCulture);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                nonEmptyCount++;
                distinctValues.Add(value);
            }

            if (nonEmptyCount == 0 || distinctValues.Count == 0 || distinctValues.Count > Math.Max(12, results.Rows.Count / 2))
            {
                continue;
            }

            candidates.Add(column);
        }

        return candidates
            .OrderByDescending(column => GetCategoryColumnPriority(column.ColumnName))
            .ThenBy(column => column.Ordinal)
            .FirstOrDefault();
    }

    private static bool TryGetDecimal(object value, out decimal result)
    {
        result = 0M;

        if (value == null || value == DBNull.Value)
        {
            return false;
        }

        if (value is decimal)
        {
            result = (decimal)value;
            return true;
        }

        if (value is int || value is long || value is short || value is byte || value is double || value is float)
        {
            try
            {
                result = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        string text = Convert.ToString(value, CultureInfo.CurrentCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Replace(",", string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out result)
            || decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryGetDate(object value, out DateTime result)
    {
        result = DateTime.MinValue;

        if (value == null || value == DBNull.Value)
        {
            return false;
        }

        if (value is DateTime)
        {
            result = ((DateTime)value).Date;
            return true;
        }

        string text = Convert.ToString(value, CultureInfo.CurrentCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        DateTime parsed;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            result = parsed.Date;
            return true;
        }

        return false;
    }

    private static bool IsNativeNumericType(Type type)
    {
        return type == typeof(decimal)
            || type == typeof(double)
            || type == typeof(float)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(short)
            || type == typeof(byte);
    }

    private static int GetNumericColumnPriority(string columnName, ExpenseChatbotQueryType queryType)
    {
        string name = (columnName ?? string.Empty).ToLowerInvariant();
        int priority = GetAmountPriority(name);

        if (queryType == ExpenseChatbotQueryType.ExpenseNetValue && name.Contains("net"))
        {
            priority += 20;
        }

        if (queryType == ExpenseChatbotQueryType.FixedAssetsByAccount && name.Contains("asset"))
        {
            priority += 20;
        }

        if (queryType == ExpenseChatbotQueryType.PersonPaymentTotal && (name.Contains("paid") || name.Contains("payment")))
        {
            priority += 20;
        }

        return priority;
    }

    private static int GetAmountPriority(string columnName)
    {
        string name = (columnName ?? string.Empty).ToLowerInvariant();

        if (name.Contains("net") || name.Contains("total") || name.Contains("amount") || name.Contains("value") || name.Contains("balance"))
        {
            return 100;
        }

        if (name.Contains("debit") || name.Contains("credit") || name.Contains("cost") || name.Contains("paid") || name.Contains("payment"))
        {
            return 80;
        }

        if (name.Contains("count") || name.Contains("qty") || name.Contains("quantity"))
        {
            return 40;
        }

        if (name.Contains("id") || name.Contains("num") || name.Contains("number") || name.Contains("code"))
        {
            return 5;
        }

        return 20;
    }

    private static int GetCategoryColumnPriority(string columnName)
    {
        string name = (columnName ?? string.Empty).ToLowerInvariant();

        if (name.Contains("account") || name.Contains("type") || name.Contains("category") || name.Contains("status") || name.Contains("person") || name.Contains("name"))
        {
            return 100;
        }

        if (name.Contains("code") || name.Contains("description") || name.Contains("doc"))
        {
            return 60;
        }

        return 10;
    }

    private static bool IsDateColumnName(string columnName)
    {
        string name = (columnName ?? string.Empty).ToLowerInvariant();
        return name.Contains("date") || name.Contains("day") || name.Contains("period");
    }

    private static string FormatColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return "Value";
        }

        return columnName.Replace("_", " ");
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("N2", CultureInfo.CurrentCulture);
    }

    private sealed class NumericColumnProfile
    {
        public NumericColumnProfile(DataColumn column, List<decimal> values)
        {
            Column = column;
            Values = values;
            Sum = values.Sum();
            Average = values.Average();
            Min = values.Min();
            Max = values.Max();
            StandardDeviation = CalculateStandardDeviation(values, Average);
        }

        public DataColumn Column { get; private set; }

        public List<decimal> Values { get; private set; }

        public decimal Sum { get; private set; }

        public decimal Average { get; private set; }

        public decimal Min { get; private set; }

        public decimal Max { get; private set; }

        public decimal StandardDeviation { get; private set; }

        private static decimal CalculateStandardDeviation(List<decimal> values, decimal average)
        {
            if (values.Count < 2)
            {
                return 0M;
            }

            double variance = values
                .Select(value => Math.Pow((double)(value - average), 2D))
                .Average();

            return (decimal)Math.Sqrt(variance);
        }
    }

    private sealed class DateColumnProfile
    {
        public DateColumnProfile(DataColumn column, List<DateTime> values)
        {
            Column = column;
            Values = values;
            Min = values.Min();
            Max = values.Max();
        }

        public DataColumn Column { get; private set; }

        public List<DateTime> Values { get; private set; }

        public DateTime Min { get; private set; }

        public DateTime Max { get; private set; }
    }
}
