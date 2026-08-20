# API لیست تعرفه‌های نماینده (Agent Plans)

دریافت لیست تعرفه‌هایی که به یک نماینده تخصیص داده شده، تیک **نمایش در ربات** آن‌ها روشن است و برایشان **مبلغ فروش نماینده** ثبت شده است.

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `GetAgentPlans`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/AgentPlanViewModel.cs`

---

## اندپوینت

```
GET /User/GetAgentPlans
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` (از `[EnableCors]` روی کنترلر) |

> مسیر بدون پیشوند `api/v1` است. `UserController` یک `ApiController` است و روی روت Web API یعنی `{controller}/{action}` می‌نشیند — برخلاف `SubController` که یک کنترلر MVC است و مسیرش `/api/v1/Sub/Info` می‌شود.

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | مقدار ستون `Token` از جدول `tbUsers` برای همان نماینده |

> **این توکن را از کجا بیاوریم؟** اگر کلاینت فقط توکن ساب مشتری را دارد (بیلد عمومی اپلیکیشن)، اول [`GET /api/v1/Sub/Agent`](API-SubAgent.md) را صدا بزنید؛ خروجی آن `agentToken` را می‌دهد و همان مقدار در این هدر می‌نشیند.

هر دو فرم زیر پذیرفته می‌شوند:

```
Authorization: 9f2c1ab7d4e8...
Authorization: Bearer 9f2c1ab7d4e8...
```

اگر مقدار با `Bearer ` (بدون حساسیت به بزرگی و کوچکی حروف) شروع شود، این پیشوند حذف می‌شود؛ در غیر این صورت کل مقدار به‌عنوان توکن در نظر گرفته می‌شود. فاصله‌های ابتدا و انتها `Trim` می‌شوند.

---

## پاسخ موفق — `200 OK`

```json
{
  "result": [
    { "planId": 154, "planVolume": 30.0, "planMonth": 1.0, "planPrice": 85000.0, "deviceLimit": null, "isUnlimited": false },
    { "planId": 158, "planVolume": 60.0, "planMonth": 1.0, "planPrice": 140000.0, "deviceLimit": null, "isUnlimited": false },
    { "planId": 161, "planVolume": 0.0, "planMonth": 3.0, "planPrice": 320000.0, "deviceLimit": 2, "isUnlimited": true }
  ]
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|------|-------|
| `planId` | int | `tbLinkUserAndPlans.Link_PU_ID` | شناسه‌ی **رکورد لینک نماینده به تعرفه** — نه `Plan_ID` جدول `tbPlans`. همان مقداری که ربات در `callback_data` دکمه‌ی خرید می‌فرستد و همان چیزی که در بدنه‌ی `CreateAgentInvoice` فرستاده می‌شود |
| `planVolume` | number | `tbPlans.PlanVolume` | حجم تعرفه به **گیگابایت** |
| `planMonth` | number | `tbPlans.PlanMonth` | مدت زمان تعرفه به **ماه** |
| `planPrice` | number | `tbLinkUserAndPlans.L_SellPrice` | مبلغ فروش نماینده به **تومان**، عدد خام و بدون جداکننده‌ی سه‌رقمی |
| `deviceLimit` | int \| null | `tbPlans.device_limit` | تعداد کاربر مجاز؛ `null` یعنی محدودیتی تعریف نشده است |
| `isUnlimited` | bool | `tbPlans.IsRobotPlan` | `true` یعنی تعرفه نامحدود است و `planVolume` برای آن معنی ندارد |

نام‌گذاری فیلدها عمداً با `AgentInvoiceViewModel` (خروجی `CreateAgentInvoice`) یکسان است تا کلاینت بتواند از یک مدل مشترک استفاده کند.

اگر نماینده هیچ تعرفه‌ی واجد شرایطی نداشته باشد، پاسخ `200` با آرایه‌ی خالی برمی‌گردد:

```json
{ "result": [] }
```

### ساخت نام نمایشی تعرفه

این API نام آماده برنمی‌گرداند و ساخت متن بر عهده‌ی کلاینت است. برای اینکه خروجی با چیزی که مشتری در ربات تلگرام می‌بیند یکسان باشد (`Tools/Keyboards.cs` متد `GetPlansKeyboard`):

| نوع تعرفه | شرط | فرمت | نمونه |
|-----------|-----|------|-------|
| حجمی | `isUnlimited = false` | `{planMonth} ماهه \| {planVolume} گیگ` | `1 ماهه \| 30 گیگ` |
| نامحدود | `isUnlimited = true` | `{planMonth} ماهه \| نامحدود \| {deviceLimit} کاربر` | `3 ماهه \| نامحدود \| 2 کاربر` |

> در تعرفه‌های نامحدود مقدار `planVolume` هرچه در دیتابیس ثبت شده باشد برگردانده می‌شود (معمولاً `0`) و نباید نمایش داده شود؛ برای تشخیص این حالت از `isUnlimited` استفاده کنید.

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است (خروجی استاندارد `IHttpActionResult` در این پروژه):

```json
"کاربری با این توکن یافت نشد"
```

| Status | پیام | علت |
|:------:|------|------|
| `400` | `توکن در هدر Authorization ارسال نشده است` | هدر `Authorization` وجود ندارد یا بعد از حذف `Bearer` خالی است |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `500` | `خطا در دریافت لیست تعرفه ها` | خطای غیرمنتظره؛ جزئیات کامل در NLog ثبت می‌شود |

---

## منطق انتخاب تعرفه‌ها

۱. توکن از هدر خوانده و نرمال‌سازی می‌شود (`GetAgentTokenFromHeader`).
۲. نماینده با `tbUsers.Token == token` پیدا می‌شود.
۳. رکوردهای `tbLinkUserAndPlans` با شرط‌های زیر انتخاب می‌شوند:

```csharp
p.L_FK_U_ID  == User.User_ID   // متعلق به همین نماینده
p.L_ShowInBot == true          // تیک «نمایش در ربات» روشن
p.L_Status    == true          // لینک فعال
p.L_SellPrice != null          // مبلغ فروش نماینده ثبت شده
```

۴. مرتب‌سازی: ابتدا `tbPlans.PlanMonth` صعودی، سپس `tbPlans.PlanVolume` صعودی.

این دقیقاً همان مجموعه شرط‌هایی است که ربات تلگرام برای ساخت کیبورد تعرفه‌ها استفاده می‌کند، بنابراین خروجی این API با آنچه مشتری در ربات می‌بیند یکسان است.

> تعرفه‌هایی که `L_SellPrice` آن‌ها `NULL` است (در پنل با برچسب «ندارد» نمایش داده می‌شوند) عمداً حذف شده‌اند، چون قیمتی برای برگرداندن ندارند.

---

## نمونه فراخوانی

### cURL

```bash
curl -H "Authorization: 9f2c1ab7d4e8" "https://panel.example.com/User/GetAgentPlans"
```

### JavaScript

```javascript
const res = await fetch('/User/GetAgentPlans', {
  headers: { 'Authorization': agentToken }
});
if (res.ok) {
  const { result } = await res.json();
  result.forEach(p => {
    const name = p.isUnlimited
      ? `${p.planMonth} ماهه | نامحدود | ${p.deviceLimit} کاربر`
      : `${p.planMonth} ماهه | ${p.planVolume} گیگ`;
    console.log(p.planId, name, p.planPrice);
  });
}
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);
    var json = await http.GetStringAsync("https://panel.example.com/User/GetAgentPlans");
    var data = JsonConvert.DeserializeObject<JObject>(json);
    var plans = data["result"].ToObject<List<AgentPlanViewModel>>();
}
```

---

## ملاحظات امنیتی

- توکن نماینده نقش کلید دسترسی را دارد و در خروجی برگردانده **نمی‌شود**؛ فقط ورودی است.
- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است. هرکس توکن یک نماینده را داشته باشد می‌تواند قیمت‌های فروش او را ببیند. توکن را در URL، لاگ یا کد سمت کلاینت عمومی قرار ندهید — به همین دلیل هم از کوئری‌استرینگ به هدر `Authorization` منتقل شد (URLها در لاگ IIS و تاریخچه‌ی مرورگر ذخیره می‌شوند، هدرها معمولاً نه).
- مقایسه‌ی توکن در SQL Server انجام می‌شود و به collation دیتابیس وابسته است؛ در حالت پیش‌فرض (`CI`) بزرگی و کوچکی حروف توکن نادیده گرفته می‌شود.
- خروجی هیچ اطلاعات هویتی نماینده (نام، موبایل، کیف پول) یا اطلاعات مشتریان را برنمی‌گرداند.

---

## پیش‌نیاز دیتابیس

برای اینکه lookup توکن سریع بماند، بهتر است روی ستون `Token` جدول `tbUsers` ایندکس وجود داشته باشد:

```sql
CREATE NONCLUSTERED INDEX IX_tbUsers_Token ON tbUsers (Token);
```
