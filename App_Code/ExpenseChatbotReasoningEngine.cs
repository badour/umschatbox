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

        AddTypeSpecificInsights(insights, queryType, results, numericProfiles, dateProfile);
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

            case ExpenseChatbotQueryType.StudentRevenueSummary:
                return "I treated this as a student revenue analysis and reviewed " + scope + " for income totals, receipt averages, and academic-year patterns.";

            case ExpenseChatbotQueryType.FileLinks:
                return "I treated this as a file/document lookup and reviewed " + scope + " for link availability, document types, and recency.";

            case ExpenseChatbotQueryType.InvoiceExpenseCode:
                return "I treated this as an invoice expense-code lookup and reviewed " + scope + " for codes, amounts, and status patterns.";

            case ExpenseChatbotQueryType.PersonExpenses:
                return "I treated this as a person-expense lookup and reviewed " + scope + " for spend totals, date coverage, and category patterns.";

            default:
                return "I reviewed " + scope + " before listing them, looking for useful totals, dates, and unusual values.";
        }
    }

    private static void AddTypeSpecificInsights(
        List<string> insights,
        ExpenseChatbotQueryType queryType,
        DataTable results,
        List<NumericColumnProfile> numericProfiles,
        DateColumnProfile dateProfile)
    {
        switch (queryType)
        {
            case ExpenseChatbotQueryType.FileLinks:
                AddFileLinkInsights(insights, results, dateProfile);
                break;

            case ExpenseChatbotQueryType.InvoiceExpenseCode:
                AddInvoiceExpenseCodeInsights(insights, results, numericProfiles);
                break;

            case ExpenseChatbotQueryType.PersonExpenses:
                AddPersonExpenseInsights(insights, results, numericProfiles, dateProfile);
                break;

            case ExpenseChatbotQueryType.StudentRevenueSummary:
                AddStudentRevenueInsights(insights, results, numericProfiles);
                break;
        }
    }

    private static void AddFileLinkInsights(List<string> insights, DataTable results, DateColumnProfile dateProfile)
    {
        DataColumn linkColumn = GetFirstColumnByName(results, "link", "url", "path", "filepath", "file_path");

        if (linkColumn != null)
        {
            int rowsWithLinks = CountNonEmptyValues(results, linkColumn);
            insights.Add(string.Format(
                "{0} of {1} row(s) include a file/link value in {2}.",
                rowsWithLinks,
                results.Rows.Count,
                FormatColumnName(linkColumn.ColumnName)));
        }

        DataColumn documentTypeColumn = GetFirstColumnByName(results, "doctype", "doc type", "documenttype", "filetype", "type");
        if (documentTypeColumn != null)
        {
            AddTopValueInsight(
                insights,
                results,
                documentTypeColumn,
                "The most common document type is '{0}' with {1} file(s).");
        }

        DataColumn documentNumberColumn = GetFirstColumnByName(results, "docnum", "doc num", "documentnumber", "docnumber", "number");
        if (documentNumberColumn != null)
        {
            int distinctDocumentCount = CountDistinctValues(results, documentNumberColumn);
            insights.Add(string.Format(
                "The result covers {0} distinct document number(s).",
                distinctDocumentCount));
        }

        if (dateProfile != null && dateProfile.Values.Count > 0)
        {
            insights.Add(string.Format(
                "The newest matching file/document date is {0}.",
                dateProfile.Max.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }
    }

    private static void AddInvoiceExpenseCodeInsights(List<string> insights, DataTable results, List<NumericColumnProfile> numericProfiles)
    {
        DataColumn expenseCodeColumn = GetFirstColumnByName(results, "expensecode", "expense code", "code");
        if (expenseCodeColumn != null)
        {
            int distinctCodeCount = CountDistinctValues(results, expenseCodeColumn);
            insights.Add(string.Format(
                "I found {0} distinct expense code(s) in the invoice results.",
                distinctCodeCount));

            AddTopValueInsight(
                insights,
                results,
                expenseCodeColumn,
                "The most repeated expense code is '{0}' with {1} row(s).");
        }

        DataColumn statusColumn = GetFirstColumnByName(results, "status", "state");
        if (statusColumn != null)
        {
            AddTopValueInsight(
                insights,
                results,
                statusColumn,
                "The most common invoice status is '{0}' with {1} row(s).");
        }

        NumericColumnProfile amountProfile = GetBestAmountProfile(numericProfiles);
        if (amountProfile != null)
        {
            insights.Add(string.Format(
                "The invoice-related {0} total is {1}.",
                FormatColumnName(amountProfile.Column.ColumnName),
                FormatDecimal(amountProfile.Sum)));
        }
    }

    private static void AddPersonExpenseInsights(
        List<string> insights,
        DataTable results,
        List<NumericColumnProfile> numericProfiles,
        DateColumnProfile dateProfile)
    {
        DataColumn personColumn = GetFirstColumnByName(results, "person", "displayname", "employee", "username", "name");
        if (personColumn != null)
        {
            int distinctPersonCount = CountDistinctValues(results, personColumn);
            insights.Add(string.Format(
                "The result includes expense rows for {0} distinct person value(s).",
                distinctPersonCount));
        }

        NumericColumnProfile amountProfile = GetBestAmountProfile(numericProfiles);
        if (amountProfile != null)
        {
            insights.Add(string.Format(
                "For this person search, {0} totals {1} across the returned rows.",
                FormatColumnName(amountProfile.Column.ColumnName),
                FormatDecimal(amountProfile.Sum)));
        }

        DataColumn categoryColumn = GetFirstColumnByName(results, "expensecode", "expense code", "category", "description", "doctype");
        if (categoryColumn != null)
        {
            AddTopValueInsight(
                insights,
                results,
                categoryColumn,
                "The most frequent expense category/code is '{0}' with {1} row(s).");
        }

        if (dateProfile != null && dateProfile.Values.Count > 0)
        {
            insights.Add(string.Format(
                "The latest expense activity in these rows is {0}.",
                dateProfile.Max.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }
    }

    private static void AddStudentRevenueInsights(List<string> insights, DataTable results, List<NumericColumnProfile> numericProfiles)
    {
        NumericColumnProfile revenueProfile = numericProfiles
            .OrderByDescending(profile => GetRevenuePriority(profile.Column.ColumnName))
            .ThenBy(profile => profile.Column.Ordinal)
            .FirstOrDefault();

        if (revenueProfile != null)
        {
            insights.Add(string.Format(
                "Student revenue total based on {0} is {1}.",
                FormatColumnName(revenueProfile.Column.ColumnName),
                FormatDecimal(revenueProfile.Sum)));
        }

        DataColumn academicYearColumn = GetFirstColumnByName(results, "academicyear", "academic year", "studyyear", "schoolyear", "year");
        if (academicYearColumn != null)
        {
            int distinctYearCount = CountDistinctValues(results, academicYearColumn);
            insights.Add(string.Format(
                "The result covers {0} academic year value(s).",
                distinctYearCount));

            AddTopValueInsight(
                insights,
                results,
                academicYearColumn,
                "The academic year with the most returned receipt rows is '{0}' with {1} row(s).");
        }

        NumericColumnProfile receiptCountProfile = numericProfiles
            .FirstOrDefault(profile => NormalizeColumnName(profile.Column.ColumnName).Contains("receiptcount"));

        if (receiptCountProfile != null)
        {
            insights.Add(string.Format(
                "The analysis is based on {0} receipt row(s).",
                FormatDecimal(receiptCountProfile.Sum)));
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

    private static NumericColumnProfile GetBestAmountProfile(List<NumericColumnProfile> numericProfiles)
    {
        return numericProfiles
            .OrderByDescending(profile => GetAmountPriority(profile.Column.ColumnName))
            .ThenBy(profile => profile.Column.Ordinal)
            .FirstOrDefault();
    }

    private static DataColumn GetFirstColumnByName(DataTable results, params string[] nameParts)
    {
        if (results == null || nameParts == null)
        {
            return null;
        }

        foreach (DataColumn column in results.Columns)
        {
            string normalizedColumnName = NormalizeColumnName(column.ColumnName);

            for (int index = 0; index < nameParts.Length; index++)
            {
                string normalizedNamePart = NormalizeColumnName(nameParts[index]);
                if (!string.IsNullOrWhiteSpace(normalizedNamePart) && normalizedColumnName.Contains(normalizedNamePart))
                {
                    return column;
                }
            }
        }

        return null;
    }

    private static void AddTopValueInsight(List<string> insights, DataTable results, DataColumn column, string messageTemplate)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in results.Rows)
        {
            string value = Convert.ToString(row[column], CultureInfo.CurrentCulture);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!counts.ContainsKey(value))
            {
                counts[value] = 0;
            }

            counts[value]++;
        }

        if (counts.Count == 0)
        {
            return;
        }

        KeyValuePair<string, int> topValue = counts.OrderByDescending(pair => pair.Value).First();
        insights.Add(string.Format(messageTemplate, topValue.Key, topValue.Value));
    }

    private static int CountNonEmptyValues(DataTable results, DataColumn column)
    {
        int count = 0;

        foreach (DataRow row in results.Rows)
        {
            string value = Convert.ToString(row[column], CultureInfo.CurrentCulture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountDistinctValues(DataTable results, DataColumn column)
    {
        HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in results.Rows)
        {
            string value = Convert.ToString(row[column], CultureInfo.CurrentCulture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.Count;
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

        if (queryType == ExpenseChatbotQueryType.StudentRevenueSummary)
        {
            priority += GetRevenuePriority(name);
        }

        return priority;
    }

    private static int GetRevenuePriority(string columnName)
    {
        string name = (columnName ?? string.Empty).ToLowerInvariant();

        if (name.Contains("totalstudentrevenue") || name.Contains("studentrevenue") || name.Contains("revenue") || name.Contains("income"))
        {
            return 120;
        }

        if (name.Contains("inputvalue") || name.Contains("receipt") || name.Contains("value"))
        {
            return 90;
        }

        return 0;
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

    private static string NormalizeColumnName(string columnName)
    {
        return (columnName ?? string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
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
