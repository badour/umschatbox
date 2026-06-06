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
            ExpenseChatbotIntentResult intent = _classifier.Classify(normalizedQuestion);
            confidence = intent.Confidence;

            if (intent.IsConfident)
            {
                queryType = intent.QueryType;
            }
            else
            {
                ExpenseChatbotQueryType keywordQueryType;
                if (TryClassifyByKeywords(normalizedQuestion, out keywordQueryType))
                {
                    queryType = keywordQueryType;
                }
            }
        }

        string searchValue = ExtractSearchValue(queryType, normalizedQuestion);

        if (string.IsNullOrWhiteSpace(searchValue) && !looksNaturalLanguage)
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
                "show",
                "find",
                "get",
                "list",
                "what",
                "where",
                "which",
                "give",
                "display",
                "search",
                "expense",
                "expenses",
                "invoice",
                "file",
                "link",
                "person",
                "employee",
                "user",
                "for",
                "by",
                "ابحث",
                "اعرض",
                "اظهر",
                "اريد",
                "هات",
                "فين",
                "اين",
                "ملف",
                "ملفات",
                "رابط",
                "روابط",
                "مرفق",
                "مرفقات",
                "مستند",
                "مستندات",
                "وثيقة",
                "وثائق",
                "مصروف",
                "مصروفات",
                "فاتورة",
                "فواتير",
                "شخص",
                "موظف",
                "مستخدم",
                "رقم",
                "تاريخ",
                "تكلفة",
                "مبلغ",
                "ملاحظات");
    }

    private static bool TryClassifyByKeywords(string question, out ExpenseChatbotQueryType queryType)
    {
        string normalized = question.ToLowerInvariant();

        if (ContainsAny(
            normalized,
            "file",
            "files",
            "link",
            "links",
            "attachment",
            "attachments",
            "receipt",
            "receipts",
            "document",
            "documents",
            "file notes",
            "notes",
            "total cost",
            "cost",
            "doc number",
            "document number",
            "doc no",
            "ملف",
            "ملفات",
            "رابط",
            "روابط",
            "مرفق",
            "مرفقات",
            "مستند",
            "مستندات",
            "وثيقة",
            "وثائق",
            "ايصال",
            "إيصال",
            "ملاحظات",
            "بيان",
            "تكلفة",
            "التكلفة",
            "مبلغ",
            "المبلغ",
            "اجمالي",
            "إجمالي",
            "تاريخ",
            "رقم المستند",
            "رقم السند",
            "رقم الملف",
            "رقم الوثيقة"))
        {
            queryType = ExpenseChatbotQueryType.FileLinks;
            return true;
        }

        if (ContainsAny(
            normalized,
            "expense code",
            "expense codes",
            "invoice code",
            "invoice number",
            "invoice no",
            "invoice",
            "كود المصروف",
            "اكواد المصروف",
            "أكواد المصروف",
            "كود الفاتورة",
            "رقم الفاتورة",
            "فاتورة"))
        {
            queryType = ExpenseChatbotQueryType.InvoiceExpenseCode;
            return true;
        }

        if (ContainsAny(
            normalized,
            "person",
            "employee",
            "user",
            "staff",
            "worker",
            "expenses for",
            "expense for",
            "spent by",
            "شخص",
            "موظف",
            "مستخدم",
            "عامل",
            "مصروفات ل",
            "مصروفات عن",
            "مصروفات الموظف",
            "مصروفات المستخدم"))
        {
            queryType = ExpenseChatbotQueryType.PersonExpenses;
            return true;
        }

        queryType = ExpenseChatbotQueryType.FileLinks;
        return false;
    }

    private static string ExtractSearchValue(ExpenseChatbotQueryType queryType, string question)
    {
        switch (queryType)
        {
            case ExpenseChatbotQueryType.FileLinks:
                return ExtractFileSearchValue(question);

            case ExpenseChatbotQueryType.InvoiceExpenseCode:
                return ExtractInvoiceSearchValue(question);

            case ExpenseChatbotQueryType.PersonExpenses:
                return ExtractPersonSearchValue(question);

            default:
                return question;
        }
    }

    private static string ExtractFileSearchValue(string question)
    {
        string value = ExtractAfterPhrase(
            question,
            "file links for",
            "file link for",
            "links for",
            "link for",
            "files for",
            "file for",
            "attachments for",
            "attachment for",
            "receipts for",
            "receipt for",
            "documents for",
            "document for",
            "for invoice",
            "for expense",
            "file notes",
            "notes",
            "total cost",
            "cost",
            "amount",
            "date",
            "doc number",
            "document number",
            "doc no",
            "document no",
            "file number",
            "file no",
            "for",
            "روابط الملفات عن",
            "روابط الملف عن",
            "رابط الملف عن",
            "رابط ملف عن",
            "ملفات عن",
            "ملف عن",
            "مرفقات عن",
            "مرفق عن",
            "مستندات عن",
            "مستند عن",
            "وثائق عن",
            "وثيقة عن",
            "ملاحظات الملف",
            "ملاحظات",
            "بيان الملف",
            "بيان",
            "التكلفة الكلية",
            "اجمالي التكلفة",
            "إجمالي التكلفة",
            "بالإجمالي",
            "التكلفة",
            "بالتكلفة",
            "تكلفة",
            "المبلغ",
            "بالمبلغ",
            "بمبلغ",
            "مبلغ",
            "التاريخ",
            "بالتاريخ",
            "بتاريخ",
            "تاريخ",
            "رقم المستند",
            "برقم المستند",
            "رقم السند",
            "برقم السند",
            "رقم الوثيقة",
            "برقم الوثيقة",
            "رقم الملف",
            "برقم الملف",
            "برقم",
            "رقم");

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string dateValue = ExtractDateLikeToken(question);
        if (!string.IsNullOrWhiteSpace(dateValue))
        {
            return dateValue;
        }

        return ExtractInvoiceLikeToken(question);
    }

    private static string ExtractInvoiceSearchValue(string question)
    {
        string value = ExtractAfterPhrase(
            question,
            "expense code for invoice",
            "expense codes for invoice",
            "code for invoice",
            "codes for invoice",
            "invoice number",
            "invoice no",
            "invoice",
            "for",
            "كود المصروف للفاتورة",
            "كود المصروف لفاتورة",
            "كود فاتورة",
            "رقم الفاتورة",
            "فاتورة",
            "للفاتورة",
            "لفواتير");

        return string.IsNullOrWhiteSpace(value) ? ExtractInvoiceLikeToken(question) : value;
    }

    private static string ExtractPersonSearchValue(string question)
    {
        string value = ExtractAfterPhrase(
            question,
            "expenses for person",
            "expenses for employee",
            "expenses for user",
            "expense for person",
            "expense for employee",
            "expense for user",
            "expenses related to",
            "expense related to",
            "related to",
            "expenses for",
            "expense for",
            "spent by",
            "created by",
            "submitted by",
            "for",
            "by",
            "person",
            "employee",
            "user",
            "مصروفات لشخص",
            "مصروفات لموظف",
            "مصروفات لمستخدم",
            "مصروفات عن شخص",
            "مصروفات عن موظف",
            "مصروفات عن مستخدم",
            "مصروفات ل",
            "مصروفات عن",
            "مصروف ل",
            "مصروف عن",
            "صرف بواسطة",
            "تم الصرف بواسطة",
            "بواسطة",
            "للموظف",
            "للمستخدم",
            "لشخص",
            "لموظف",
            "لمستخدم",
            "شخص",
            "موظف",
            "مستخدم");

        return value;
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

    private static string ExtractInvoiceLikeToken(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        Match invoiceMatch = Regex.Match(
            question,
            @"\b(?:INV|INVOICE|BILL|EXP)[\s:#-]*[A-Za-z0-9-]+\b",
            RegexOptions.IgnoreCase);

        if (invoiceMatch.Success)
        {
            return CleanExtractedValue(invoiceMatch.Value);
        }

        Match numberMatch = Regex.Match(question, @"\b\d{3,}\b");
        return numberMatch.Success ? numberMatch.Value : string.Empty;
    }

    private static string ExtractDateLikeToken(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        Match dateMatch = Regex.Match(
            question,
            @"\b\d{1,4}[-/]\d{1,2}[-/]\d{1,4}\b",
            RegexOptions.IgnoreCase);

        return dateMatch.Success ? dateMatch.Value : string.Empty;
    }

    private static string CleanExtractedValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string cleanedValue = Regex.Replace(value, @"[?.!,;]+$", string.Empty).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"^(named|called|number|no\.?|id|is)\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"^(invoice\s+number|invoice\s+no\.?|invoice|expense\s+code|employee|person|user|staff|member|file\s+notes|notes|total\s+cost|cost|amount|date|doc\s+number|document\s+number|doc\s+no\.?)\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"^(رقم\s+الفاتورة|فاتورة|كود\s+المصروف|موظف|شخص|مستخدم|ملاحظات\s+الملف|ملاحظات|بيان\s+الملف|بيان|التكلفة\s+الكلية|اجمالي\s+التكلفة|إجمالي\s+التكلفة|بالإجمالي|التكلفة|بالتكلفة|تكلفة|المبلغ|بالمبلغ|بمبلغ|مبلغ|التاريخ|بالتاريخ|بتاريخ|تاريخ|رقم\s+المستند|برقم\s+المستند|رقم\s+السند|برقم\s+السند|رقم\s+الوثيقة|برقم\s+الوثيقة|رقم\s+الملف|برقم\s+الملف|برقم|رقم)\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"\s+(please|pls)$", string.Empty, RegexOptions.IgnoreCase).Trim();
        cleanedValue = Regex.Replace(cleanedValue, @"\s+(من فضلك|لو سمحت|رجاء)$", string.Empty, RegexOptions.IgnoreCase).Trim();
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
            if (value.Contains(candidates[index]))
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
            return ExpenseChatbotIntentResult.NotConfident(ExpenseChatbotQueryType.FileLinks);
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
                return ExpenseChatbotIntentResult.NotConfident(ExpenseChatbotQueryType.FileLinks);
            }

            double confidence = GetConfidence(prediction.Score);
            bool isConfident = confidence >= ExpenseChatbotConfig.GetIntentConfidenceThreshold();
            return new ExpenseChatbotIntentResult(queryType, confidence, isConfident);
        }
        catch
        {
            return ExpenseChatbotIntentResult.NotConfident(ExpenseChatbotQueryType.FileLinks);
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
            Sample("show file links for invoice INV-10045", ExpenseChatbotQueryType.FileLinks),
            Sample("find attachment for this expense", ExpenseChatbotQueryType.FileLinks),
            Sample("where is the receipt for expense code TRAVEL", ExpenseChatbotQueryType.FileLinks),
            Sample("get documents for invoice 12345", ExpenseChatbotQueryType.FileLinks),
            Sample("open file link for receipt.pdf", ExpenseChatbotQueryType.FileLinks),
            Sample("search expense files", ExpenseChatbotQueryType.FileLinks),
            Sample("show me uploaded file for this invoice", ExpenseChatbotQueryType.FileLinks),
            Sample("find supporting document link", ExpenseChatbotQueryType.FileLinks),
            Sample("find file by notes maintenance", ExpenseChatbotQueryType.FileLinks),
            Sample("search file using total cost 1500", ExpenseChatbotQueryType.FileLinks),
            Sample("get document by date 2024-05-10", ExpenseChatbotQueryType.FileLinks),
            Sample("show file for doc number 12345", ExpenseChatbotQueryType.FileLinks),
            Sample("ابحث عن رابط الملف", ExpenseChatbotQueryType.FileLinks),
            Sample("اعرض ملف رقم المستند 12345", ExpenseChatbotQueryType.FileLinks),
            Sample("هات مرفق بتاريخ 2024-05-10", ExpenseChatbotQueryType.FileLinks),
            Sample("ابحث في ملاحظات الملف صيانة", ExpenseChatbotQueryType.FileLinks),
            Sample("اعرض الملفات بالتكلفة 1500", ExpenseChatbotQueryType.FileLinks),
            Sample("فين رابط المستند", ExpenseChatbotQueryType.FileLinks),

            Sample("what is the expense code for invoice INV-10045", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("show invoice expense code", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("find code for invoice number 12345", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("get expense codes for this invoice", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("which expense code belongs to invoice", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("invoice details by expense code", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("lookup invoice expense details", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("show specific invoice details", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("ما هو كود المصروف للفاتورة INV-10045", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("اعرض كود الفاتورة", ExpenseChatbotQueryType.InvoiceExpenseCode),
            Sample("ابحث عن رقم الفاتورة 12345", ExpenseChatbotQueryType.InvoiceExpenseCode),

            Sample("show me expenses for Ahmed", ExpenseChatbotQueryType.PersonExpenses),
            Sample("find expenses related to Sarah", ExpenseChatbotQueryType.PersonExpenses),
            Sample("list expenses for employee Ali", ExpenseChatbotQueryType.PersonExpenses),
            Sample("get expenses submitted by user Mary", ExpenseChatbotQueryType.PersonExpenses),
            Sample("show person expense history", ExpenseChatbotQueryType.PersonExpenses),
            Sample("what did this employee spend", ExpenseChatbotQueryType.PersonExpenses),
            Sample("search expenses by person name", ExpenseChatbotQueryType.PersonExpenses),
            Sample("display expenses for staff member", ExpenseChatbotQueryType.PersonExpenses),
            Sample("اعرض مصروفات احمد", ExpenseChatbotQueryType.PersonExpenses),
            Sample("ابحث عن مصروفات الموظف علي", ExpenseChatbotQueryType.PersonExpenses),
            Sample("هات مصروفات المستخدم سارة", ExpenseChatbotQueryType.PersonExpenses)
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
