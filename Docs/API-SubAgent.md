# API تشخیص نماینده از روی توکن اشتراک (Subscription Agent)

پیدا کردن نماینده‌ی صاحب یک اشتراک با استفاده از **توکن لینک ساب مشتری**، و برگرداندن **توکن نماینده** تا کلاینت بتواند با آن [تعرفه‌ها](API-AgentPlans.md) را بگیرد و [فاکتور/سفارش](API-AgentInvoice.md) بسازد.

- کنترلر: `V2boardApi/Areas/api/Controllers/SubController.cs` → متد `Agent`
- مدل خروجی: `V2boardApi/Areas/api/Data/ApiModels/SubscriptionAgentModel.cs`

> اندپوینت هم‌خانواده: [`GET /api/v1/Sub/Info`](API-SubscriptionInfo.md) · [`GET /api/v1/Sub/Usage`](API-SubscriptionUsage.md) · [`POST /api/v1/Sub/ResetLink`](API-SubscriptionResetLink.md) · سوالات متداول ربات: [`GET /User/GetFaq`](API-Faq.md) · لینک‌های پشتیبانی: [`GET /User/GetSupportLinks`](API-SupportLinks.md) · نسخه اپلیکیشن: [`GET /User/GetAppRelease`](API-AppRelease.md) · ثبت خطای اپ: [`POST /MobileDevice/LogError`](API-ClientLog.md)

---

## چرا این اندپوینت اضافه شد

تا پیش از این، توکن نماینده هنگام **بیلد گرفتن از اپلیکیشن** داخل آن هاردکد می‌شد (`AGENT_API_TOKEN`) و هر نماینده بیلد اختصاصی خودش را داشت. یعنی:

- برای هر نماینده باید یک بیلد جدا ساخته و منتشر می‌شد
- عوض شدن توکن نماینده در پنل، همه‌ی نصب‌های آن بیلد را از کار می‌انداخت
- یک بیلد عمومی که هر مشتری بتواند اشتراکش را در آن وارد کند ممکن نبود

حالا زنجیره برعکس شده است: مشتری فقط لینک/توکن اشتراک خودش را دارد، و **خود اشتراک می‌گوید مال کدام نماینده است**. نام اشتراک در جدول `v2_user` همیشه با ساختار زیر ساخته می‌شود:

```
a1b2c3d4$e5f6g7h8@agentuser
└─ نام ─┘ └─ رندوم ─┘ └نماینده┘
```

بخش بعد از `@` دقیقاً `tbUsers.Username` نماینده است. این قرارداد جای دیگری هم مبنای مالکیت است (`AppInvoiceService`، `CheckAgentInvoice`، `FindAgentSubscriptionAsync`)، پس چیز جدیدی اختراع نشده — فقط از سمت خواندن هم در دسترس قرار گرفته.

---

## جای این اندپوینت در جریان کار

```
       ┌──────────────────────────────────────────────┐
       │  مشتری توکن ساب خودش را دارد                  │
       └───────────────────┬──────────────────────────┘
                           │
                           ▼
        GET /api/v1/Sub/Agent?token={subToken}      ← این مستند
                           │
                           ▼
        agentToken + businessTitle + sellEnabled
                           │
        ┌──────────────────┴───────────────────┐
        ▼                                      ▼
        GET /User/GetAgentPlans              POST /User/CreateAgentInvoice
Authorization: {agentToken}          Authorization: {agentToken}
        │                                      │
        │                                      ▼
        │                            POST /User/UploadAgentInvoiceReceipt
        │                            (عکس رسید → ادمین ربات)
        │                                      │
        │                                      ▼
        │                            POST /User/CheckAgentInvoice
        │                            (اختیاری: PayFromWallet)
        ▼                                      ▼
   لیست تعرفه‌ها                        جزئیات اشتراک ساخته/تمدیدشده

POST /User/GetTelegramWallet  ← موجودی کیف پول ربات برای دکمه‌ی پرداخت از کیف پول
GET  /User/GetFaq             ← سوالات متداول همان ربات
GET  /User/GetSupportLinks    ← لینک‌های ارتباطی پشتیبانی نماینده
GET  /User/GetAppRelease      ← نسخه، لینک دانلود و نصب اجباری اپلیکیشن
POST /MobileDevice/LogError   ← ثبت خطای اپ در NLog با تگ AndroidApp
```

کلاینت این را **یک بار** بعد از وارد کردن اشتراک صدا می‌زند و `agentToken` را نگه می‌دارد؛ لازم نیست قبل از هر فراخوانی تکرار شود. موجودی کیف پول را از [`GetTelegramWallet`](API-TelegramWallet.md) بگیر. سوالات متداول را از [`GetFaq`](API-Faq.md). لینک‌های پشتیبانی را از [`GetSupportLinks`](API-SupportLinks.md). نسخه اپلیکیشن و لینک دانلود را از [`GetAppRelease`](API-AppRelease.md). خطاهای اپ را با [`LogError`](API-ClientLog.md) بفرست.

---

## اندپوینت

```
GET /api/v1/Sub/Agent?token={token}
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | ندارد؛ خودِ توکن اشتراک نقش کلید دسترسی را دارد |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` |
| کش سمت کلاینت | غیرفعال (`Cache-Control: no-cache`) |
| کش سمت سرور | ۶۰ ثانیه در `HttpRuntime.Cache` |

> این اندپوینت **پیشوند `api/v1` دارد**، چون `SubController` یک کنترلر MVC است — برخلاف `/User/*` و `/MobileDevice/*` که روی روت Web API یعنی `{controller}/{action}` می‌نشینند.

### پارامترها

| نام | محل | نوع | الزامی | توضیح |
|-----|-----|------|:------:|-------|
| `token` | Query String | string | بله* | همان توکنی که در لینک اشتراک است: `https://{SubAddress}/api/v1/client/subscribe?token={token}` |

\* اگر `token` در کوئری‌استرینگ نیاید، مقدار هدر `Authorization` خوانده می‌شود. هر دو فرم `Authorization: <token>` و `Authorization: Bearer <token>` پذیرفته می‌شوند. **فرستادن با هدر توصیه می‌شود** — بخش [ملاحظات امنیتی](#ملاحظات-امنیتی).

---

## پاسخ موفق — `200 OK`

```json
{
  "success": true,
  "subscriptionName": "a1b2c3d4",
  "agentUsername": "agentuser",
  "businessTitle": "سیف‌نت",
  "agentToken": "9f2c1ab7d4e8...",
  "botUsername": "SafeNetVpnBot",
  "supportUsername": "SafeNetSupport",
  "sellEnabled": true
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|------|-------|
| `success` | bool | — | در پاسخ موفق همیشه `true` |
| `subscriptionName` | string | `v2_user.email` | نام اشتراک؛ بخش قبل از اولین `$` یا `@` |
| `agentUsername` | string | `tbUsers.Username` | بخش بعد از `@` در نام اشتراک |
| `businessTitle` | string \| null | `tbUsers.BussinesTitle` | نام تجاری برای نمایش در برنامه. اگر نماینده پرش نکرده باشد `null` است — کلاینت باید به `agentUsername` برگردد |
| `agentToken` | string | `tbUsers.Token` | **مقداری که در هدر `Authorization` اندپوینت‌های `/User/*` و `/MobileDevice/*` فرستاده می‌شود** |
| `botUsername` | string \| null | `tbBotSettings.Bot_ID` | آیدی ربات تلگرام نماینده، **بدون** `@` |
| `supportUsername` | string \| null | `tbBotSettings.AdminUsername` | آیدی پشتیبانی نماینده، **بدون** `@` |
| `sellEnabled` | bool | `!tbBotSettings.IsNotActiveSell` | `false` یعنی فروش موقتاً متوقف است و دکمه‌ی خرید نباید نمایش داده شود |

`botUsername` و `supportUsername` از اولین رکورد `tbBotSettings` نماینده خوانده می‌شوند. `@` ابتدای مقدار حذف می‌شود تا کلاینت خودش تصمیم بگیرد چطور نمایش دهد یا لینک `https://t.me/{username}` بسازد.

### درباره‌ی `sellEnabled`

همان سوئیچی است که در ربات تلگرام پیام «فروش در حال حاضر به‌صورت موقت متوقف شده است» را نشان می‌دهد (`BotController` شاخه‌ی `BotSettings.IsNotActiveSell`). اگر نماینده اصلاً تنظیمات ربات نداشته باشد، `true` برمی‌گردد.

> این فیلد فقط برای **نمایش** است. `CreateAgentInvoice` روی آن فیلتر نمی‌کند و اگر کلاینت نادیده‌اش بگیرد فاکتور ساخته می‌شود.

---

## پاسخ‌های خطا

همه‌ی خطاها با همین ساختار و با HTTP Status متناظر برمی‌گردند:

```json
{ "success": false, "message": "اشتراک یافت نشد" }
```

| Status | `message` | علت |
|:------:|-----------|------|
| `400` | `پارامتر token ارسال نشده است` | نه `token` در کوئری‌استرینگ آمده و نه هدر `Authorization` |
| `403` | `نماینده این اشتراک غیرفعال است` | `tbUsers.Status = 0` |
| `404` | `اشتراک یافت نشد` | توکن در جدول `v2_user` وجود ندارد |
| `404` | `نماینده این اشتراک یافت نشد` | نام اشتراک `@` ندارد، یا نام کاربری بعد از `@` در `tbUsers` نیست |
| `503` | `سرور پیکربندی نشده است` | سرور پیش‌فرض پنل در کش/دیتابیس موجود نیست |
| `503` | `برای نماینده این اشتراک توکن ثبت نشده است` | `tbUsers.Token` تهی است — بدون آن کلاینت نمی‌تواند ادامه دهد |
| `500` | `خطای داخلی` | خطای غیرمنتظره (جزئیات در NLog) |

> `Status = NULL` غیرفعال حساب **نمی‌شود**؛ فقط `0` صریح مانع می‌شود. رکوردهای قدیمی زیادی `Status` تهی دارند و مسدود کردنشان یعنی از کار افتادن اشتراک‌های سالم.

اشتراک‌هایی که با `@` ساخته نشده‌اند (نام‌های دستیِ قدیمی) `404` می‌گیرند و در NLog با پیام «نام اشتراک ... بخش نماینده ندارد» ثبت می‌شوند.

---

## منطق تشخیص

۱. توکن از کوئری‌استرینگ یا هدر `Authorization` خوانده و `Trim` می‌شود.
۲. اگر پاسخ در کش ۶۰ ثانیه‌ای موجود باشد، همان برگردانده می‌شود.
۳. یک کوئری پارامتری روی MySQL:

```sql
SELECT email FROM v2_user WHERE token=@token LIMIT 1
```

۴. `email.Split('@')` — عنصر دوم نام کاربری نماینده است. `email.IndexOfAny(['$','@'])` هم نام نمایشی اشتراک را جدا می‌کند.
۵. یک کوئری روی SQL Server:

```csharp
db.tbUsers.AsNoTracking().Where(p => p.Username == agentUsername)
```

فقط ستون‌های لازم `Select` می‌شوند (`Username`, `BussinesTitle`, `Token`, `Status` و سه فیلد از `tbBotSettings`) — کل موجودیت لود نمی‌شود.

۶. خروجی سریالایز و برای ۶۰ ثانیه کش می‌شود.

### درباره‌ی معماری

`SubController` عمداً بدون Entity Framework نوشته شده بود (بخش [طراحی و عملکرد](API-SubscriptionInfo.md#طراحی-و-عملکرد) را ببینید). این متد **تنها استثنای آن قاعده** است، چون اطلاعات نماینده فقط در SQL Server وجود دارد:

- `DbContext` در **سازنده‌ی کنترلر ساخته نمی‌شود** — داخل خود متد با `using` ساخته و بسته می‌شود. یعنی `/api/v1/Sub/Info` که مسیر پرترافیک است هیچ هزینه‌ای بابت آن نمی‌دهد.
- کوئری `AsNoTracking` و با پروجکشن است، پس change tracker درگیر نمی‌شود.
- کش ۶۰ ثانیه‌ای (در برابر ۱۰ ثانیه‌ی `Info`) چون اطلاعات نماینده تقریباً هیچ‌وقت عوض نمی‌شود.

### تغییر مدت کش

مقدار ثابت `AgentCacheSeconds` در `SubController.cs`. مقدار `0` یعنی کش غیرفعال:

```csharp
/// <summary>مدت کش پاسخ تشخیص نماینده (ثانیه). صفر یعنی بدون کش.</summary>
private const int AgentCacheSeconds = 60;
```

> اگر توکن نماینده را در پنل عوض کردید، تا ۶۰ ثانیه ممکن است توکن قدیمی برگردانده شود. برای اعمال فوری، App Pool را recycle کنید.

---

## نمونه فراخوانی

### cURL — قابل ایمپورت در Postman

```bash
curl --location --request GET "https://panel.example.com/api/v1/Sub/Agent?token=8f3a1b2c4d5e6f7a8b9c" --header "Accept: application/json"
```

### cURL — با هدر (توصیه‌شده، توکن در URL ثبت نمی‌شود)

```bash
curl --location --request GET "https://panel.example.com/api/v1/Sub/Agent" --header "Authorization: 8f3a1b2c4d5e6f7a8b9c" --header "Accept: application/json"
```

### cURL — زنجیره‌ی کامل: نماینده ← تعرفه‌ها ← فاکتور

```bash
curl -s "https://panel.example.com/api/v1/Sub/Agent?token=8f3a1b2c4d5e6f7a8b9c"
```

```bash
curl -s --location --request GET "https://panel.example.com/User/GetAgentPlans" --header "Authorization: 9f2c1ab7d4e8"
```

```bash
curl -s --location --request POST "https://panel.example.com/User/CreateAgentInvoice" --header "Authorization: 9f2c1ab7d4e8" --header "Content-Type: application/json" --data "{\"PlanId\":154,\"SubscriptionToken\":\"8f3a1b2c4d5e6f7a8b9c\",\"DeviceId\":\"9774d56d682e549c\"}"
```

> در قدم دوم و سوم مقدار `9f2c1ab7d4e8` همان `agentToken` است که قدم اول برگردانده. در قدم سوم `SubscriptionToken` را بفرستید تا **تمدید** شود؛ خالی بگذارید تا اشتراک جدید ساخته شود.

### JavaScript

```javascript
const res = await fetch(`/api/v1/Sub/Agent?token=${encodeURIComponent(subToken)}`);
if (!res.ok) {
  const err = await res.json();
  throw new Error(err.message);
}

const agent = await res.json();
// از این به بعد همان توکنی که تا دیروز در بیلد هاردکد بود
const agentToken = agent.agentToken;

document.title = agent.businessTitle || agent.agentUsername;

const plans = await fetch('/User/GetAgentPlans', {
  headers: { 'Authorization': agentToken }
}).then(r => r.json());
```

### Kotlin

```kotlin
val agent = HttpUtil.getJson<SubscriptionAgent>(
    AgentApi.url("/api/v1/Sub/Agent?token=$subToken")
)

if (!agent.sellEnabled) hideBuyButton()

AgentApi.saveToken(agent.agentToken)      // به‌جای BuildConfig.AGENT_API_TOKEN
AgentApi.saveBusinessTitle(agent.businessTitle ?: agent.agentUsername)
```

### C#

```csharp
using (var http = new HttpClient())
{
    var url = "https://panel.example.com/api/v1/Sub/Agent?token=" + Uri.EscapeDataString(subToken);
    var json = await http.GetStringAsync(url);
    var agent = JsonConvert.DeserializeObject<SubscriptionAgentModel>(json);
}
```

---

## ملاحظات امنیتی

> ⚠️ **مهم‌ترین نکته‌ی این اندپوینت:** خروجی آن یک **اعتبارنامه** است، نه صرفاً اطلاعات نمایشی. `agentToken` تمام کاری را می‌کند که در `/User/*` تعریف شده — دیدن همه‌ی قیمت‌های فروش نماینده و ساخت فاکتور برای **هر** اشتراک آن نماینده. یعنی هرکس **یک** توکن ساب معتبر از یک نماینده داشته باشد، عملاً به سطح دسترسی خود نماینده در این APIها می‌رسد.

- **این ارتقای سطح دسترسی نسبت به وضعیت قبل است.** پیش از این توکن نماینده فقط داخل بیلد اپلیکیشن بود؛ حالا با هر توکن ساب قابل دریافت است. اگر این تبادل مطلوب نیست، جایگزین درست این است که `GetAgentPlans` و `CreateAgentInvoice` خودشان **توکن ساب** را بپذیرند و نماینده را سمت سرور استخراج کنند، و `agentToken` هرگز از سرور خارج نشود. آن تغییر، دو اندپوینت `/User/*` را هم لمس می‌کند و در این نسخه انجام نشده است.
- **توکن را در کوئری‌استرینگ نفرستید مگر مجبور باشید.** URLها در لاگ IIS، تاریخچه‌ی مرورگر، هدر `Referer` و لاگ reverse proxy ذخیره می‌شوند. حالت هدر `Authorization` برای همین پشتیبانی می‌شود. (حالت کوئری‌استرینگ برای سازگاری با `/api/v1/Sub/Info` و سهولت تست نگه داشته شده است.)
- `Cache-Control: no-cache` روی پاسخ ست می‌شود تا پراکسی‌های میانی توکن نماینده را نگه ندارند. کش ۶۰ ثانیه‌ای فقط **در حافظه‌ی همان سرور** است.
- روی این کنترلر `[LogActionFilter]` گذاشته **نشده**، پس بدنه‌ی پاسخ (شامل `agentToken`) در جدول لاگ ذخیره نمی‌شود.
- هیچ محدودیت نرخی وجود ندارد. این اندپوینت رکوردی نمی‌سازد، ولی می‌شود با آن توکن‌های ساب را brute-force کرد. اگر در معرض اینترنت است، محدودیت نرخ در سطح IIS یا reverse proxy اضافه کنید.
- خروجی هیچ اطلاعات مالی (کیف پول، بدهی، شماره کارت) یا اطلاعات مشتریان دیگر را برنمی‌گرداند. شماره کارت فقط از `CreateAgentInvoice` و در لحظه‌ی ساخت فاکتور برمی‌گردد.
- این اندپوینت را روی `HTTPS` سرو کنید.

---

## پیش‌نیاز دیتابیس

هیچ تغییر اسکیمایی لازم نیست. برای سرعت:

```sql
-- SQL Server
CREATE NONCLUSTERED INDEX IX_tbUsers_Username ON tbUsers (Username)
    INCLUDE (BussinesTitle, Token, Status);
```

روی ستون `token` جدول `v2_user` در MySQL معمولاً از قبل ایندکس یکتا وجود دارد.
