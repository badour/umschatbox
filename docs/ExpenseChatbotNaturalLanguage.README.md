# شرح ملف ExpenseChatbotNaturalLanguage.cs

هذا الملف مسؤول عن فهم سؤال المستخدم الطبيعي وتحويله إلى نوع استعلام SQL مناسب.

بمعنى آخر:

```text
سؤال المستخدم
    ↓
ExpenseChatbotNaturalLanguage.cs
    ↓
نوع السؤال + قيمة البحث
    ↓
SQL Stored Procedure
```

مثال:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

يتحول إلى:

```text
QueryType = AccountRelatedExpenses
SearchValue = 3314
```

ثم يتم استدعاء:

```sql
dbo.Chatbot_GetAccountRelatedExpenses @SearchText = '3314'
```

---

## 1. `ExpenseChatbotNaturalLanguageRequest`

هذا `class` يمثل نتيجة فهم سؤال المستخدم.

يحتوي على:

```csharp
QueryType
SearchValue
OriginalQuestion
UsedNaturalLanguage
Confidence
```

### معنى الخصائص

| الخاصية | المعنى |
|---|---|
| `QueryType` | نوع السؤال الذي فهمه البوت |
| `SearchValue` | القيمة التي سترسل إلى SQL |
| `OriginalQuestion` | سؤال المستخدم بعد التنظيف |
| `UsedNaturalLanguage` | هل الإدخال جملة طبيعية أم قيمة مباشرة |
| `Confidence` | نسبة ثقة ML.NET بالتصنيف |

مثال:

```text
المجموع الكلي للمباني للكلية لكل الاعوام
```

الناتج:

```text
QueryType = BuildingsTotal
SearchValue = all years
```

---

## 2. `ExpenseChatbotNaturalLanguageParser`

هذا أهم `class` في الملف.

وظيفته:

```text
قراءة السؤال
تصنيف السؤال
استخراج قيمة البحث
إرجاع نتيجة جاهزة للخدمة
```

---

## 3. Constructor الافتراضي

```csharp
public ExpenseChatbotNaturalLanguageParser()
```

ينشئ classifier داخلي:

```csharp
new ExpenseChatbotIntentClassifier()
```

---

## 4. Constructor مع classifier

```csharp
public ExpenseChatbotNaturalLanguageParser(
    ExpenseChatbotIntentClassifier classifier)
```

هذا يسمح لك بتمرير classifier خارجي.

يفيد في:

```text
اختبارات الوحدة
تبديل طريقة التصنيف
حقن dependency مخصص
```

---

## 5. `Parse`

```csharp
public ExpenseChatbotNaturalLanguageRequest Parse(
    string question,
    ExpenseChatbotQueryType fallbackQueryType)
```

هذه أهم function في الملف.

### ماذا تفعل؟

1. تنظف السؤال.
2. تحدد هل السؤال جملة طبيعية.
3. تحاول التصنيف بالقواعد المباشرة.
4. إذا لم تنجح القواعد، تستخدم ML.NET.
5. تستخرج قيمة البحث.
6. ترجع `ExpenseChatbotNaturalLanguageRequest`.

مثال:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

النتيجة:

```text
QueryType = AccountRelatedExpenses
SearchValue = 3314
```

---

## 6. `LooksLikeNaturalLanguage`

```csharp
private static bool LooksLikeNaturalLanguage(string question)
```

تحدد هل النص جملة طبيعية أم لا.

مثال:

```text
3314
```

ليس جملة طبيعية.

لكن:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

جملة طبيعية.

تبحث عن كلمات مثل:

```text
مصروفات
مصاريف
حساب
موجودات
مباني
مجموع
```

### كيف تحدثها؟

إذا تريد إضافة كلمة جديدة يفهمها البوت، أضفها إلى القائمة.

مثال:

```csharp
"نفقات",
```

---

## 7. `TryClassifyByRules`

```csharp
private static bool TryClassifyByRules(
    string question,
    out ExpenseChatbotQueryType queryType)
```

تصنف السؤال باستخدام قواعد مباشرة قبل ML.NET.

الترتيب الحالي:

```text
BuildingsTotal
FixedAssetsTotal
AccountRelatedExpenses
AccountingExpensesTotal
```

### لماذا الترتيب مهم؟

لأن بعض الأسئلة قد تحتوي كلمات مشتركة.

مثلا سؤال المباني قد يحتوي:

```text
مجموع
كل
حساب
```

لذلك يتم فحص المباني قبل المصروفات العامة.

---

## 8. `ExtractSearchValue`

```csharp
private static string ExtractSearchValue(
    ExpenseChatbotQueryType queryType,
    string question)
```

بعد معرفة نوع السؤال، هذه function تستخرج القيمة التي سترسل إلى SQL.

أمثلة:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

ترجع:

```text
3314
```

```text
المجموع الكلي للمصروفات لشخص احمد
```

ترجع:

```text
احمد
```

---

## 9. `ExtractAccountRelatedExpensesSearchValue`

```csharp
private static string ExtractAccountRelatedExpensesSearchValue(string question)
```

تستخدم لأسئلة:

```text
المصاريف المتعلقة بحساب معين
```

مثال:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

أولا تبحث عن رقم حساب:

```csharp
ExtractAccountCodeToken(question)
```

فتجد:

```text
3314
```

إذا لم تجد رقم، تحاول استخراج اسم الحساب أو الوصف بعد عبارات مثل:

```text
المصاريف المتعلقة
الباب المحاسبي
حساب
```

هذه القيمة سترسل إلى SQL، وهناك يتم البحث كالتالي:

```text
إذا كانت القيمة رقم حساب -> البحث في FromAccountID
إذا كانت القيمة اسم حساب -> البحث في ToAccountName
إذا كانت القيمة وصف أو جزء من تفاصيل -> البحث في DocDetails و DocTitl
```

---

## 10. `ExtractAccountingAnalyticsSearchValue`

```csharp
private static string ExtractAccountingAnalyticsSearchValue(string question)
```

تستخدم للأسئلة العامة مثل:

```text
المجموع الكلي للمصروفات لشخص احمد
```

أو:

```text
المجموع الكلي للمصروفات لتبويب محاسبي 3314
```

تبحث بالترتيب عن:

1. سنة مثل `2023`.
2. آخر 3 سنوات.
3. رقم حساب مثل `3314`.
4. اسم شخص.
5. كل السنوات.

---

## 11. `IsAccountRelatedExpensesQuestion`

```csharp
private static bool IsAccountRelatedExpensesQuestion(string question)
```

تحدد هل السؤال عن مصاريف مرتبطة بحساب معين.

يفضل أن يحتوي السؤال على:

```text
كلمة مصاريف
كلمة حساب / باب / تبويب / صيانة
رقم حساب أو اسم حساب أو وصف مرتبط بالحساب
```

مثال صحيح:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

يرجع:

```text
true
```

---

## 12. `IsFixedAssetsTotalQuestion`

```csharp
private static bool IsFixedAssetsTotalQuestion(string question)
```

تحدد هل السؤال عن مجموع الموجودات الثابتة.

مثال:

```text
المجموع الكلي للموجودات الثابتة للكلية لكل السنوات
```

يرجع:

```text
FixedAssetsTotal
```

لأنه يحتوي:

```text
موجودات
ثابتة
مجموع
```

---

## 13. `IsBuildingsTotalQuestion`

```csharp
private static bool IsBuildingsTotalQuestion(string question)
```

تحدد هل السؤال عن المباني.

مثال:

```text
المجموع الكلي للمباني للكلية لكل الاعوام
```

يرجع:

```text
BuildingsTotal
```

لأنه يحتوي:

```text
مباني
مجموع
```

---

## 14. `IsAccountingExpensesTotalQuestion`

```csharp
private static bool IsAccountingExpensesTotalQuestion(string question)
```

تحدد هل السؤال عن مجموع المصروفات.

أمثلة:

```text
المجموع الكلي للمصروفات لشخص احمد
المجموع الكلي للمصروفات لتبويب محاسبي 3314
```

تبحث عن:

```text
مصروفات / مصاريف
```

مع:

```text
مجموع / قيمة / حساب / شخص / تبويب
```

---

## 15. `ExtractAccountCodeToken`

```csharp
private static string ExtractAccountCodeToken(string question)
```

تستخرج رقم الحساب من السؤال.

مثال:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

ترجع:

```text
3314
```

تعتمد على regex:

```csharp
\b(?<code>\d{3,10})\b
```

يعني:

```text
رقم من 3 إلى 10 خانات
```

### إذا حساباتك من رقمين

غيرها إلى:

```csharp
\b(?<code>\d{2,10})\b
```

---

## 16. `ExtractLastYearsValue`

```csharp
private static string ExtractLastYearsValue(string question)
```

تتعرف على فترات مثل:

```text
last 3 years
اخر 3 سنوات
السنوات الثلاثة الاخيرة
```

وترجع:

```text
last 3 years
```

---

## 17. `ExtractFromYearValue`

```csharp
private static string ExtractFromYearValue(string question)
```

تتعرف على سنة بداية مثل:

```text
from 2023
من 2023
```

وترجع:

```text
from 2023
```

---

## 18. `ExtractAfterPhrase`

```csharp
private static string ExtractAfterPhrase(
    string question,
    params string[] phrases)
```

هذه function عامة.

تبحث عن النص الموجود بعد عبارة معينة.

مثال:

```text
المجموع الكلي للمصروفات لشخص احمد
```

إذا كانت العبارة:

```text
لشخص
```

ترجع:

```text
احمد
```

---

## 19. `CleanExtractedValue`

```csharp
private static string CleanExtractedValue(string value)
```

تنظف القيمة المستخرجة.

تحذف كلمات زائدة مثل:

```text
الى
ل
السيد
دكتور
محاسبي
```

مثال:

```text
السيد احمد
```

تصبح:

```text
احمد
```

---

## 20. `NormalizeQuestion`

```csharp
private static string NormalizeQuestion(string question)
```

تنظف السؤال من المسافات الزائدة.

مثال:

```text
المجموع   الكلي   للمباني
```

تصبح:

```text
المجموع الكلي للمباني
```

---

## 21. `ContainsAny`

```csharp
private static bool ContainsAny(
    string value,
    params string[] candidates)
```

تفحص هل النص يحتوي أي كلمة من قائمة كلمات.

مثال:

```csharp
ContainsAny(question, "مباني", "المباني")
```

إذا السؤال يحتوي:

```text
مباني
```

يرجع:

```text
true
```

---

## 22. `ExpenseChatbotIntentClassifier`

هذا class خاص بـ ML.NET.

يستخدم إذا القواعد المباشرة لم تكفِ.

يحتوي على:

```text
Classify
TrainModel
GetTrainingData
Sample
GetConfidence
```

---

## 23. `Classify`

```csharp
public ExpenseChatbotIntentResult Classify(string question)
```

يأخذ السؤال ويرجع نوعه المتوقع.

مثال:

```text
total buildings for all years
```

قد يرجع:

```text
BuildingsTotal
```

---

## 24. `TrainModel`

```csharp
private static ExpenseChatbotIntentModel TrainModel()
```

يبني نموذج ML.NET صغير داخل التطبيق.

يعتمد على:

```text
FeaturizeText
SdcaMaximumEntropy
```

أي أنه يتعلم من الأمثلة النصية الموجودة داخل الكود.

---

## 25. `GetTrainingData`

```csharp
private static IEnumerable<ExpenseChatbotIntentTrainingData> GetTrainingData()
```

هذه قائمة الأمثلة التي يتعلم منها ML.NET.

مثال:

```csharp
Sample("المصاريف المتعلقة بالصيانة السيارات 3314",
       ExpenseChatbotQueryType.AccountRelatedExpenses)
```

### كيف تحدثها؟

إذا تريد أن يفهم صيغة جديدة، أضف مثال هنا.

مثال:

```csharp
Sample("كم مصاريف حساب الصيانة 3314",
       ExpenseChatbotQueryType.AccountRelatedExpenses)
```

---

## 26. `Sample`

```csharp
private static ExpenseChatbotIntentTrainingData Sample(
    string text,
    ExpenseChatbotQueryType queryType)
```

اختصار لإنشاء مثال تدريبي.

---

## 27. `GetConfidence`

```csharp
private static double GetConfidence(float[] scores)
```

يرجع أعلى نسبة ثقة من ML.NET.

إذا الثقة أقل من القيمة الموجودة في Web.config:

```xml
<add key="ExpenseChatbot.IntentConfidenceThreshold" value="0.35" />
```

لا يعتمد التصنيف.

---

## 28. `ExpenseChatbotIntentResult`

يمثل نتيجة ML.NET.

يحتوي على:

```text
QueryType
Confidence
IsConfident
```

---

## 29. `NotConfident`

```csharp
public static ExpenseChatbotIntentResult NotConfident(...)
```

يرجع نتيجة افتراضية إذا ML.NET فشل أو لم يكن واثقا.

في هذا الإصدار الافتراضي هو:

```text
AccountingExpensesTotal
```

---

## 30. `ExpenseChatbotIntentModel`

يحفظ:

```text
MLContext
Transformer
```

أي نموذج ML.NET المدرب.

---

## 31. `ExpenseChatbotIntentTrainingData`

يمثل سطر تدريب:

```text
Text
Label
```

مثال:

```text
Text = "المجموع الكلي للمباني للكلية لكل الاعوام"
Label = BuildingsTotal
```

---

## 32. `ExpenseChatbotIntentPrediction`

يمثل نتيجة التنبؤ من ML.NET:

```text
PredictedLabel
Score
```

---

# كيف تضيف سؤال جديد؟

مثال: تريد دعم سؤال:

```text
كم مجموع الاثاث للكلية
```

إذا الأثاث له حساب يبدأ بـ:

```text
113
```

تحتاج إلى:

## 1. إضافة نوع جديد في enum

```csharp
FurnitureTotal
```

## 2. إضافته في `ExpenseChatbotQueryDefinition`

باسم procedure جديد:

```text
dbo.Chatbot_GetFurnitureTotal
```

## 3. إضافة rule جديد

```csharp
IsFurnitureTotalQuestion(...)
```

## 4. إضافة أمثلة تدريب

```csharp
Sample("كم مجموع الاثاث للكلية",
       ExpenseChatbotQueryType.FurnitureTotal)
```

## 5. إنشاء SQL procedure

```sql
WHERE FromAccountID LIKE N'113%'
```

---

# كيف تضيف كلمات عربية جديدة؟

مثال: تريد أن يفهم كلمة:

```text
نفقات
```

أضفها في:

```text
LooksLikeNaturalLanguage
```

وأيضا في function المناسبة مثل:

```text
IsAccountingExpensesTotalQuestion
```

---

# الخلاصة

هذا الملف مسؤول عن:

```text
فهم السؤال
تصنيف نوع السؤال
استخراج القيمة التي ستذهب للـ SQL
تدريب ML.NET على أمثلة الأسئلة
```

أما الحساب الحقيقي فيتم داخل SQL procedures.

يعني:

```text
ExpenseChatbotNaturalLanguage.cs
يفهم السؤال

SQL Procedure
تحسب النتيجة
```
