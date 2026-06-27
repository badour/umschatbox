using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML;

public sealed class ExpenseChatbotNaturalLanguageRequest
{
    public ExpenseChatbotNaturalLanguageRequest(
        ExpenseChatbotQueryType queryType,
        string searchValue,
        string originalQuestion,
        bool usedNaturalLanguage,
        double confidence)
    {
        QueryType = queryType;
        SearchValue = searchValue;
        OriginalQuestion = originalQuestion;
        UsedNaturalLanguage = usedNaturalLanguage;
        Confidence = confidence;
    }

    public ExpenseChatbotQueryType QueryType { get; private set; }

    public string SearchValue { get; private set; }

    public string OriginalQuestion { get; private set; }

    public bool UsedNaturalLanguage { get; private set; }

    public double Confidence { get; private set; }
}

public sealed class ExpenseChatbotNaturalLanguageParser
{
    private readonly ExpenseChatbotIntentClassifier _classifier;

    public ExpenseChatbotNaturalLanguageParser()
        : this(new ExpenseChatbotIntentClassifier())
    {
    }

    public ExpenseChatbotNaturalLanguageParser(ExpenseChatbotIntentClassifier classifier)
    {
        if (classifier == null)
        {
            throw new ArgumentNullException("classifier");
        }

        _classifier = classifier;
    }

    public ExpenseChatbotNaturalLanguageRequest Parse(string question, ExpenseChatbotQueryType fallbackQueryType)
    {
        string normalizedQuestion = NormalizeQuestion(question);
        bool looksNaturalLanguage = LooksLikeNaturalLanguage(normalizedQuestion);
        ExpenseChatbotQueryType queryType = fallbackQueryType;
        double confidence = 0D;

        if (looksNaturalLanguage)
        {
            ExpenseChatbotQueryType ruleQueryType;
            if (TryClassifyByRules(normalizedQuestion, out ruleQueryType))
            {
                queryType = ruleQueryType;
            }
            else
            {
                ExpenseChatbotIntentResult intent = _classifier.Classify(normalizedQuestion);
                confidence = intent.Confidence;

                if (intent.IsConfident)
                {
                    queryType = intent.QueryType;
                }
            }
        }

        string searchValue = ExtractSearchValue(queryType, normalizedQuestion);

        if (string.IsNullOrWhiteSpace(searchValue))
        {
            searchValue = normalizedQuestion;
        }

        return new ExpenseChatbotNaturalLanguageRequest(
            queryType,
            searchValue,
            normalizedQuestion,
            looksNaturalLanguage,
            confidence);
    }

    private static bool LooksLikeNaturalLanguage(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        string normalized = question.ToLowerInvariant();
        return normalized.Contains(" ")
            && ContainsAny(
                normalized,
                "expense",
                "expenses",
                "account",
                "fixed",
                "asset",
                "assets",
                "building",
                "buildings",
                "total",
                "sum",
                "person",
                "مصروف",
                "مصروفات",
                "مصاريف",
                "المصاريف",
                "المصروفات",
                "حساب",
                "تبويب",
                "باب",
                "محاسبي",
                "موجودات",
                "الموجودات",
                "ثابتة",
                "الثابتة",
                "الثابته",
                "مباني",
                "المباني",
                "مجموع",
                "الكلي",
                "الكلية",
                "شخص",
                "صيانة",
                "سيارات");
    }

    private static bool TryClassifyByRules(string question, out ExpenseChatbotQueryType queryType)
    {
        if (IsBuildingsTotalQuestion(question))
        {
            queryType = ExpenseChatbotQueryType.BuildingsTotal;
            return true;
        }

        if (IsFixedAssetsTotalQuestion(question))
        {
            queryType = ExpenseChatbotQueryType.FixedAssetsTotal;
            return true;
        }

        if (IsAccountRelatedExpensesQuestion(question))
        {
            queryType = ExpenseChatbotQueryType.AccountRelatedExpenses;
            return true;
        }

        if (IsAccountingExpensesTotalQuestion(question))
        {
            queryType = ExpenseChatbotQueryType.AccountingExpensesTotal;
            return true;
        }

        queryType = ExpenseChatbotQueryType.AccountingExpensesTotal;
        return false;
    }

    private static string ExtractSearchValue(ExpenseChatbotQueryType queryType, string question)
    {
        switch (queryType)
        {
            case ExpenseChatbotQueryType.AccountRelatedExpenses:
                return ExtractAccountRelatedExpensesSearchValue(question);

            case ExpenseChatbotQueryType.FixedAssetsTotal:
            case ExpenseChatbotQueryType.BuildingsTotal:
            case ExpenseChatbotQueryType.AccountingExpensesTotal:
                return ExtractAccountingAnalyticsSearchValue(question);

            default:
                return question;
        }
    }

    private static string ExtractAccountRelatedExpensesSearchValue(string question)
    {
        string accountCode = ExtractAccountCodeToken(question);
        if (!string.IsNullOrWhiteSpace(accountCode))
        {
            return accountCode;
        }

        string value = ExtractAfterPhrase(
            question,
            "expenses related to account",
            "expenses for account",
            "المصاريف المتعلقة بحساب",
            "المصاريف المتعلقة ب",
            "المصروفات المتعلقة بحساب",
            "المصروفات المتعلقة ب",
            "المصاريف المتعلقة",
            "المصروفات المتعلقة",
            "تبويب محاسبي",
            "باب محاسبي",
            "الباب المحاسبي",
            "حساب");

        return string.IsNullOrWhiteSpace(value) ? question : value;
    }

    private static string ExtractAccountingAnalyticsSearchValue(string question)
    {
        string value = ExtractFromYearValue(question);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        value = ExtractLastYearsValue(question);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string accountCode = ExtractAccountCodeToken(question);
        if (!string.IsNullOrWhiteSpace(accountCode))
        {
            return accountCode;
        }

        value = ExtractAfterPhrase(
            question,
            "expenses for account",
            "total for account",
            "account total for",
            "account name",
            "account",
            "expenses for person",
            "expenses for",
            "for person",
            "المصروفات لحساب",
            "المصاريف لحساب",
            "المجموع الكلي لحساب",
            "مجموع حساب",
            "اسم الحساب",
            "حساب",
            "لتبويب محاسبي",
            "لتبويب",
            "تبويب محاسبي",
            "المصروفات لشخص",
            "المصاريف لشخص",
            "المصروفات للشخص",
            "المصاريف للشخص",
            "لشخص",
            "للشخص",
            "باسم",
            "اسم");

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (ContainsAny(
            question,
            "all years",
            "all",
            "لكل السنوات",
            "لكل الاعوام",
            "لكل الأعوام",
            "كل السنوات",
            "كل الاعوام",
            "كل الأعوام",
            "جميع السنوات",
            "جميع الاعوام",
            "جميع الأعوام"))
        {
            return "all years";
        }

        return question;
    }

    private static bool IsAccountRelatedExpensesQuestion(string question)
    {
        bool hasExpenseWord = ContainsAny(question, "expense", "expenses", "مصروف", "مصروفات", "مصاريف", "المصاريف", "المصروفات");
        bool hasRelatedWord = ContainsAny(question, "related", "account", "category", "maintenance", "المتعلقة", "المتعلقه", "حساب", "تبويب", "باب", "صيانة", "الصيانة");
        bool hasAccountReference = !string.IsNullOrWhiteSpace(ExtractAccountCodeToken(question))
            || ContainsAny(question, "account", "حساب", "تبويب", "باب", "صيانة", "الصيانة");

        return hasExpenseWord && hasRelatedWord && hasAccountReference;
    }

    private static bool IsFixedAssetsTotalQuestion(string question)
    {
        bool hasAssetWord = ContainsAny(question, "fixed assets", "fixed asset", "موجودات", "الموجودات", "اصول", "أصول");
        bool hasFixedWord = ContainsAny(question, "fixed", "ثابت", "ثابتة", "الثابته", "الثابتة");
        bool hasTotalWord = ContainsAny(question, "total", "sum", "all", "مجموع", "الكلي", "الكلية", "اجمالي", "إجمالي", "كل");

        return hasAssetWord && hasFixedWord && hasTotalWord;
    }

    private static bool IsBuildingsTotalQuestion(string question)
    {
        bool hasBuildingWord = ContainsAny(question, "building", "buildings", "مباني", "المباني", "بناية", "ابنية", "أبنية");
        bool hasTotalWord = ContainsAny(question, "total", "sum", "all", "مجموع", "الكلي", "الكلية", "اجمالي", "إجمالي", "كل");

        return hasBuildingWord && hasTotalWord;
    }

    private static bool IsAccountingExpensesTotalQuestion(string question)
    {
        bool hasExpenseWord = ContainsAny(question, "expense", "expenses", "مصروف", "مصروفات", "مصاريف", "المصاريف", "المصروفات");
        bool hasTotalWord = ContainsAny(question, "total", "sum", "net", "value", "amount", "مجموع", "الكلي", "الكلية", "اجمالي", "إجمالي", "قيمة", "مبالغ");
        bool hasScopeWord = ContainsAny(question, "person", "account", "category", "شخص", "اسم", "حساب", "تبويب", "باب", "لشخص", "لتبويب", "المحاسبي");

        return hasExpenseWord && (hasTotalWord || hasScopeWord);
    }

    private static string ExtractAccountCodeToken(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        Match match = Regex.Match(question, @"\b(?<code>\d{3,10})\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["code"].Value : string.Empty;
    }

    private static string ExtractLastYearsValue(string question)
    {
        Match englishMatch = Regex.Match(
            question,
            @"\b(?:last|past)\s+(?<years>\d+)\s+years?\b",
            RegexOptions.IgnoreCase);

        if (englishMatch.Success)
        {
            return "last " + englishMatch.Groups["years"].Value + " years";
        }

        Match arabicMatch = Regex.Match(
            question,
            @"(?:اخر|آخر)\s+(?<years>\d+)\s+(?:سنة|سنوات|اعوام|أعوام)",
            RegexOptions.IgnoreCase);

        if (arabicMatch.Success)
        {
            return "last " + arabicMatch.Groups["years"].Value + " years";
        }

        Match arabicWordMatch = Regex.Match(
            question,
            @"(?:اخر|آخر|السنوات|للسنوات|سنوات|لمدة)\s+(?<word>الثلاثة|الثلاثاة|ثلاثة|ثلاث|الثلاث|الثلاثه|ثلاثه)\s+(?:سنوات|سنة|اعوام|أعوام|الاخيرة|الأخيرة|الاخير|الأخير)?",
            RegexOptions.IgnoreCase);

        if (arabicWordMatch.Success && ContainsAny(arabicWordMatch.Groups["word"].Value, "ثلاث"))
        {
            return "last 3 years";
        }

        return string.Empty;
    }

    private static string ExtractFromYearValue(string question)
    {
        Match englishMatch = Regex.Match(question, @"\bfrom\s+(?<year>20\d{2}|19\d{2})\b", RegexOptions.IgnoreCase);
        if (englishMatch.Success)
        {
            return "from " + englishMatch.Groups["year"].Value;
        }

        Match arabicMatch = Regex.Match(question, @"\bمن\s+(?<year>20\d{2}|19\d{2})\b", RegexOptions.IgnoreCase);
        if (arabicMatch.Success)
        {
            return "from " + arabicMatch.Groups["year"].Value;
        }

        return string.Empty;
    }

    private static string ExtractAfterPhrase(string question, params string[] phrases)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        for (int phraseIndex = 0; phraseIndex < phrases.Length; phraseIndex++)
        {
            string phrase = phrases[phraseIndex];
            Match match = Regex.Match(
                question,
                @"(?:^|\s)" + Regex.Escape(phrase) + @"(?:\s|[:#-])+\s*(?<value>.+)$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return CleanExtractedValue(match.Groups["value"].Value);
            }
        }

        return string.Empty;
    }

    private static string CleanExtractedValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string cleanedValue = Regex.Replace(value, @"[?.!,;]+$", string.Empty).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"^(to|for|mr\.?|mrs\.?|ms\.?|person|account|category)\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"^(الى|إلى|ل|لل|السيد|السيح|سيد|دكتور|الدكتور|أستاذ|استاذ|شخص|حساب|تبويب|باب|محاسبي)\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"\s+(please|pls|من فضلك|لو سمحت|رجاء)$", string.Empty, RegexOptions.IgnoreCase).Trim();
        return cleanedValue;
    }

    private static string NormalizeQuestion(string question)
    {
        return Regex.Replace((question ?? string.Empty).Trim(), @"\s+", " ");
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        for (int index = 0; index < candidates.Length; index++)
        {
            if (!string.IsNullOrWhiteSpace(candidates[index]) && value.Contains(candidates[index]))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class ExpenseChatbotIntentClassifier
{
    private static readonly Lazy<ExpenseChatbotIntentModel> Model =
        new Lazy<ExpenseChatbotIntentModel>(TrainModel);

    public ExpenseChatbotIntentResult Classify(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return ExpenseChatbotIntentResult.NotConfident(ExpenseChatbotQueryType.AccountingExpensesTotal);
        }

        try
        {
            ExpenseChatbotIntentModel model = Model.Value;
            PredictionEngine<ExpenseChatbotIntentTrainingData, ExpenseChatbotIntentPrediction> engine =
                model.MlContext.Model.CreatePredictionEngine<ExpenseChatbotIntentTrainingData, ExpenseChatbotIntentPrediction>(model.Transformer);

            ExpenseChatbotIntentPrediction prediction = engine.Predict(
                new ExpenseChatbotIntentTrainingData
                {
                    Text = question
                });

            ExpenseChatbotQueryType queryType;
            if (!ExpenseChatbotService.TryParseQueryType(prediction.PredictedLabel, out queryType))
            {
                return ExpenseChatbotIntentResult.NotConfident(ExpenseChatbotQueryType.AccountingExpensesTotal);
            }

            double confidence = GetConfidence(prediction.Score);
            bool isConfident = confidence >= ExpenseChatbotConfig.GetIntentConfidenceThreshold();
            return new ExpenseChatbotIntentResult(queryType, confidence, isConfident);
        }
        catch
        {
            return ExpenseChatbotIntentResult.NotConfident(ExpenseChatbotQueryType.AccountingExpensesTotal);
        }
    }

    private static ExpenseChatbotIntentModel TrainModel()
    {
        MLContext mlContext = new MLContext(seed: 1);
        IDataView trainingData = mlContext.Data.LoadFromEnumerable(GetTrainingData());

        IEstimator<ITransformer> pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", "Text"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        ITransformer transformer = pipeline.Fit(trainingData);
        return new ExpenseChatbotIntentModel(mlContext, transformer);
    }

    private static IEnumerable<ExpenseChatbotIntentTrainingData> GetTrainingData()
    {
        return new List<ExpenseChatbotIntentTrainingData>
        {
            Sample("المصاريف المتعلقة بالصيانة السيارات 3314", ExpenseChatbotQueryType.AccountRelatedExpenses),
            Sample("المصاريف المتعلقة بحساب 3314", ExpenseChatbotQueryType.AccountRelatedExpenses),
            Sample("مصروفات الباب المحاسبي 3314", ExpenseChatbotQueryType.AccountRelatedExpenses),
            Sample("expenses related to account 3314", ExpenseChatbotQueryType.AccountRelatedExpenses),

            Sample("المجموع الكلي للموجودات الثابتة للكلية لكل السنوات", ExpenseChatbotQueryType.FixedAssetsTotal),
            Sample("المجموع الكلي للموجودات الثابتة للكلية خلال فترة معينة", ExpenseChatbotQueryType.FixedAssetsTotal),
            Sample("total fixed assets for all years", ExpenseChatbotQueryType.FixedAssetsTotal),

            Sample("المجموع الكلي للمباني للكلية لكل الاعوام", ExpenseChatbotQueryType.BuildingsTotal),
            Sample("المجموع الكلي للمباني للكلية خلال فترة معينة", ExpenseChatbotQueryType.BuildingsTotal),
            Sample("total buildings for all years", ExpenseChatbotQueryType.BuildingsTotal),

            Sample("المجموع الكلي للمصروفات لشخص معين", ExpenseChatbotQueryType.AccountingExpensesTotal),
            Sample("المجموع الكلي للمصروفات لتبويب محاسبي معين", ExpenseChatbotQueryType.AccountingExpensesTotal),
            Sample("اجمالي المصروفات للباب المحاسبي 3", ExpenseChatbotQueryType.AccountingExpensesTotal),
            Sample("total expenses for account 3314", ExpenseChatbotQueryType.AccountingExpensesTotal),
            Sample("show me expenses for Ahmed", ExpenseChatbotQueryType.AccountingExpensesTotal),
            Sample("اعرض مصروفات احمد", ExpenseChatbotQueryType.AccountingExpensesTotal)
        };
    }

    private static ExpenseChatbotIntentTrainingData Sample(string text, ExpenseChatbotQueryType queryType)
    {
        return new ExpenseChatbotIntentTrainingData
        {
            Text = text,
            Label = queryType.ToString()
        };
    }

    private static double GetConfidence(float[] scores)
    {
        if (scores == null || scores.Length == 0)
        {
            return 0D;
        }

        return scores.Max();
    }
}

public sealed class ExpenseChatbotIntentResult
{
    public ExpenseChatbotIntentResult(ExpenseChatbotQueryType queryType, double confidence, bool isConfident)
    {
        QueryType = queryType;
        Confidence = confidence;
        IsConfident = isConfident;
    }

    public ExpenseChatbotQueryType QueryType { get; private set; }

    public double Confidence { get; private set; }

    public bool IsConfident { get; private set; }

    public static ExpenseChatbotIntentResult NotConfident(ExpenseChatbotQueryType fallbackQueryType)
    {
        return new ExpenseChatbotIntentResult(fallbackQueryType, 0D, false);
    }
}

public sealed class ExpenseChatbotIntentModel
{
    public ExpenseChatbotIntentModel(MLContext mlContext, ITransformer transformer)
    {
        MlContext = mlContext;
        Transformer = transformer;
    }

    public MLContext MlContext { get; private set; }

    public ITransformer Transformer { get; private set; }
}

public sealed class ExpenseChatbotIntentTrainingData
{
    public string Text { get; set; }

    public string Label { get; set; }
}

public sealed class ExpenseChatbotIntentPrediction
{
    public string PredictedLabel { get; set; }

    public float[] Score { get; set; }
}
