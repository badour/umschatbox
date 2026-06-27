using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

public static class ExpenseChatbotReasoningEngine
{
    public static string BuildAnalysis(ExpenseChatbotQueryType queryType, DataTable results, int totalRowCount)
    {
        if (results == null || results.Rows.Count == 0)
        {
            return string.Empty;
        }

        List<string> insights = BuildInsights(queryType, results, totalRowCount);
        return "Analysis:\n- " + string.Join("\n- ", insights.ToArray());
    }

    public static string BuildConversationalAnswer(ExpenseChatbotQueryType queryType, DataTable results, int totalRowCount, bool hasTimePeriod)
    {
        if (results == null || results.Rows.Count == 0)
        {
            return "I could not find data to analyze for this question.";
        }

        NumericColumnProfile primaryMetric = GetPrimaryMetricProfile(queryType, results);

        if (!hasTimePeriod && primaryMetric != null)
        {
            decimal value = primaryMetric.Values.Count == 1 ? primaryMetric.Values[0] : primaryMetric.Sum;
            return string.Format(
                "Answer:\nThe {0} is {1}.\n\nI found no time period in the question, so I returned the main calculated number only.",
                GetPrimaryMetricLabel(queryType),
                FormatDecimal(value));
        }

        List<string> insights = BuildInsights(queryType, results, totalRowCount);
        List<string> details = BuildRowDetails(queryType, results);

        string response = "Answer:\nBecause your question includes a period or a detailed scope, I reviewed the returned accounting data and summarized it in text.\n\nAnalysis:\n- "
            + string.Join("\n- ", insights.ToArray());

        if (details.Count > 0)
        {
            response += "\n\nDetails:\n- " + string.Join("\n- ", details.ToArray());
        }

        return response;
    }

    private static List<string> BuildInsights(ExpenseChatbotQueryType queryType, DataTable results, int totalRowCount)
    {
        List<string> insights = new List<string>();
        NumericColumnProfile primaryMetric = GetPrimaryMetricProfile(queryType, results);
        DateColumnProfile dateProfile = GetBestDateProfile(results);

        insights.Add(BuildIntro(queryType, results.Rows.Count, totalRowCount));

        if (primaryMetric != null)
        {
            insights.Add(string.Format(
                "{0}: total {1}, average {2}, minimum {3}, maximum {4}.",
                FormatColumnName(primaryMetric.Column.ColumnName),
                FormatDecimal(primaryMetric.Sum),
                FormatDecimal(primaryMetric.Average),
                FormatDecimal(primaryMetric.Min),
                FormatDecimal(primaryMetric.Max)));
        }

        AddOptionalNumericInsight(insights, results, "TotalDebetValue");
        AddOptionalNumericInsight(insights, results, "TotalCreditValue");

        if (dateProfile != null)
        {
            insights.Add(string.Format(
                "The accounting date range runs from {0} to {1}.",
                dateProfile.Min.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                dateProfile.Max.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        AddCategoryInsight(insights, results);

        return insights;
    }

    private static string BuildIntro(ExpenseChatbotQueryType queryType, int analyzedRows, int totalRowCount)
    {
        string scope = totalRowCount > analyzedRows
            ? string.Format("the first {0} row(s) out of {1} returned row(s)", analyzedRows, totalRowCount)
            : string.Format("{0} returned row(s)", analyzedRows);

        switch (queryType)
        {
            case ExpenseChatbotQueryType.AccountRelatedExpenses:
                return "I treated this as expenses related to a specific accounting account/category and reviewed " + scope + " from ExpencesAccDocSum.";

            case ExpenseChatbotQueryType.FixedAssetsTotal:
                return "I treated this as total fixed assets for account prefix 1 and reviewed " + scope + " from ExpencesAccDocSum.";

            case ExpenseChatbotQueryType.BuildingsTotal:
                return "I treated this as total buildings for account prefix 112 and reviewed " + scope + " from ExpencesAccDocSum.";

            case ExpenseChatbotQueryType.AccountingExpensesTotal:
                return "I treated this as total accounting expenses for account prefix 3, a specific account code, or a person/name filter and reviewed " + scope + " from ExpencesAccDocSum.";

            default:
                return "I reviewed " + scope + " from ExpencesAccDocSum.";
        }
    }

    private static NumericColumnProfile GetPrimaryMetricProfile(ExpenseChatbotQueryType queryType, DataTable results)
    {
        List<NumericColumnProfile> profiles = GetNumericProfiles(results);

        return profiles
            .OrderByDescending(profile => GetPrimaryMetricPriority(queryType, profile.Column.ColumnName))
            .ThenBy(profile => profile.Column.Ordinal)
            .FirstOrDefault();
    }

    private static int GetPrimaryMetricPriority(ExpenseChatbotQueryType queryType, string columnName)
    {
        string name = NormalizeColumnName(columnName);
        int priority = 0;

        if (name.Contains("totalaccountingvalue")
            || name.Contains("totalfixedassetsvalue")
            || name.Contains("totalbuildingsvalue")
            || name.Contains("totalexpensesvalue"))
        {
            priority += 100;
        }

        if (name.Contains("netaccountingvalue")
            || name.Contains("netfixedassetsvalue")
            || name.Contains("netbuildingsvalue")
            || name.Contains("netexpensesvalue"))
        {
            priority += 90;
        }

        if (name.Contains("totaldebetvalue") || name.Contains("totalcreditvalue"))
        {
            priority += 60;
        }

        if (name.Contains("count") || name.Contains("period") || name.Contains("prefix"))
        {
            priority -= 50;
        }

        return priority;
    }

    private static string GetPrimaryMetricLabel(ExpenseChatbotQueryType queryType)
    {
        switch (queryType)
        {
            case ExpenseChatbotQueryType.AccountRelatedExpenses:
                return "total account-related expense value";

            case ExpenseChatbotQueryType.FixedAssetsTotal:
                return "total fixed assets value";

            case ExpenseChatbotQueryType.BuildingsTotal:
                return "total buildings value";

            case ExpenseChatbotQueryType.AccountingExpensesTotal:
                return "total expenses value";

            default:
                return "total value";
        }
    }

    private static void AddOptionalNumericInsight(List<string> insights, DataTable results, string columnName)
    {
        DataColumn column = GetFirstColumnByName(results, columnName);
        if (column == null)
        {
            return;
        }

        decimal total = 0M;
        bool hasValue = false;

        foreach (DataRow row in results.Rows)
        {
            decimal value;
            if (TryGetDecimal(row[column], out value))
            {
                total += value;
                hasValue = true;
            }
        }

        if (hasValue)
        {
            insights.Add(FormatColumnName(column.ColumnName) + " is " + FormatDecimal(total) + ".");
        }
    }

    private static void AddCategoryInsight(List<string> insights, DataTable results)
    {
        DataColumn accountCodeColumn = GetFirstColumnByName(results, "FromAccountID", "AccountPrefix", "TertiaryAccountCode");
        DataColumn accountNameColumn = GetFirstColumnByName(results, "ToAccountName", "AccountName", "AnalysisType");

        if (accountCodeColumn != null)
        {
            insights.Add("The accounting prefix/category in the result is " + FirstNonEmpty(results, accountCodeColumn) + ".");
        }

        if (accountNameColumn != null)
        {
            insights.Add("The accounting name/type in the result is " + FirstNonEmpty(results, accountNameColumn) + ".");
        }
    }

    private static List<string> BuildRowDetails(ExpenseChatbotQueryType queryType, DataTable results)
    {
        List<string> details = new List<string>();
        DataColumn labelColumn = GetFirstColumnByName(results, "ToAccountName", "AnalysisType", "AccountPrefix", "FromAccountID", "SearchText");
        NumericColumnProfile primaryMetric = GetPrimaryMetricProfile(queryType, results);

        int maxRows = Math.Min(results.Rows.Count, 8);
        for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
        {
            DataRow row = results.Rows[rowIndex];
            string label = labelColumn == null ? "Result " + (rowIndex + 1).ToString(CultureInfo.InvariantCulture) : Convert.ToString(row[labelColumn], CultureInfo.CurrentCulture);

            if (string.IsNullOrWhiteSpace(label))
            {
                label = "Result " + (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
            }

            if (primaryMetric != null)
            {
                decimal value;
                if (TryGetDecimal(row[primaryMetric.Column], out value))
                {
                    details.Add(label + ": " + FormatColumnName(primaryMetric.Column.ColumnName) + " " + FormatDecimal(value));
                    continue;
                }
            }

            details.Add(label);
        }

        if (results.Rows.Count > maxRows)
        {
            details.Add(string.Format("There are {0} additional row(s) not shown in this text summary.", results.Rows.Count - maxRows));
        }

        return details;
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

            if (values.Count > 0)
            {
                profiles.Add(new NumericColumnProfile(column, values));
            }
        }

        return profiles;
    }

    private static DateColumnProfile GetBestDateProfile(DataTable results)
    {
        foreach (DataColumn column in results.Columns)
        {
            if (!NormalizeColumnName(column.ColumnName).Contains("period") && !NormalizeColumnName(column.ColumnName).Contains("date"))
            {
                continue;
            }

            List<DateTime> values = new List<DateTime>();

            foreach (DataRow row in results.Rows)
            {
                DateTime value;
                if (TryGetDate(row[column], out value))
                {
                    values.Add(value);
                }
            }

            if (values.Count > 0)
            {
                return new DateColumnProfile(column, values);
            }
        }

        return null;
    }

    private static DataColumn GetFirstColumnByName(DataTable results, params string[] nameParts)
    {
        foreach (DataColumn column in results.Columns)
        {
            string normalizedColumnName = NormalizeColumnName(column.ColumnName);

            for (int index = 0; index < nameParts.Length; index++)
            {
                string normalizedNamePart = NormalizeColumnName(nameParts[index]);
                if (normalizedColumnName.Contains(normalizedNamePart))
                {
                    return column;
                }
            }
        }

        return null;
    }

    private static string FirstNonEmpty(DataTable results, DataColumn column)
    {
        foreach (DataRow row in results.Rows)
        {
            string value = Convert.ToString(row[column], CultureInfo.CurrentCulture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "not specified";
    }

    private static bool TryGetDecimal(object value, out decimal result)
    {
        result = 0M;

        if (value == null || value == DBNull.Value)
        {
            return false;
        }

        try
        {
            result = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
            return true;
        }
        catch (FormatException)
        {
            string text = Convert.ToString(value, CultureInfo.CurrentCulture);
            return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out result)
                || decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result);
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

        DateTime parsed;
        if (DateTime.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), out parsed))
        {
            result = parsed.Date;
            return true;
        }

        return false;
    }

    private static string FormatColumnName(string columnName)
    {
        return string.IsNullOrWhiteSpace(columnName) ? "Value" : columnName.Replace("_", " ");
    }

    private static string NormalizeColumnName(string columnName)
    {
        return (columnName ?? string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
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
        }

        public DataColumn Column { get; private set; }

        public List<decimal> Values { get; private set; }

        public decimal Sum { get; private set; }

        public decimal Average { get; private set; }

        public decimal Min { get; private set; }

        public decimal Max { get; private set; }
    }

    private sealed class DateColumnProfile
    {
        public DateColumnProfile(DataColumn column, List<DateTime> values)
        {
            Column = column;
            Min = values.Min();
            Max = values.Max();
        }

        public DataColumn Column { get; private set; }

        public DateTime Min { get; private set; }

        public DateTime Max { get; private set; }
    }
}
