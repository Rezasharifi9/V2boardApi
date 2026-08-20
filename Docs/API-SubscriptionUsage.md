# API تاریخچه مصرف اشتراک (Subscription Usage)

مصرف روزانهٔ **۳۰ روز گذشته** یک اشتراک با استفاده از **توکن لینک ساب**. همان داده‌ای که پنل در «تاریخچه مصرف» و ربات در «نمایش ریز مصرف» نشان می‌دهد.

- کنترلر: `V2boardApi/Areas/api/Controllers/SubController.cs` → متد `Usage`
- مدل خروجی: `V2boardApi/Areas/api/Data/ApiModels/SubscriptionUsageHistoryModel.cs`

> اندپوینت هم‌خانواده: [`GET /api/v1/Sub/Info`](API-SubscriptionInfo.md) خلاصهٔ لحظه‌ای حجم و انقضا را برمی‌گرداند. نمایندهٔ صاحب اشتراک: [`GET /api/v1/Sub/Agent`](API-SubAgent.md). تغییر لینک اشتراک: [`POST /api/v1/Sub/ResetLink`](API-SubscriptionResetLink.md).

---

## اندپوینت

```
GET /api/v1/Sub/Usage?token={token}
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | ندارد؛ خودِ `token` نقش کلید دسترسی را دارد |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` |
| کش سمت کلاینت | غیرفعال (`Cache-Control: no-cache`) |
| کش سمت سرور | ۶۰ ثانیه در `HttpRuntime.Cache` |

> این اندپوینت **پیشوند `api/v1` دارد**، چون `SubController` یک کنترلر MVC است — برخلاف `/User/*` و `/MobileDevice/*` که روی روت Web API یعنی `{controller}/{action}` می‌نشینند.

### پارامترها

| نام | محل | نوع | الزامی | توضیح |
|-----|-----|------|:------:|-------|
| `token` | Query String | string | بله* | همان توکنی که در لینک اشتراک است: `https://{SubAddress}/api/v1/client/subscribe?token={token}` |

\* اگر `token` در کوئری‌استرینگ نیاید، مقدار هدر `Authorization` خوانده می‌شود. هر دو فرم `Authorization: <token>` و `Authorization: Bearer <token>` پذیرفته می‌شوند. **فرستادن با هدر توصیه می‌شود** — URLها در لاگ IIS و تاریخچه مرورگر ذخیره می‌شوند.

بازه همیشه ثابت است: از **۳۰ روز قبل از امروز (شروع روز)** تا **پایان امروز**. پارامتر تاریخ جداگانه وجود ندارد.

---

## پاسخ موفق — `200 OK`

```json
{
  "success": true,
  "name": "reza",
  "fromDate": "1404/04/25",
  "toDate": "1404/05/24",
  "totalDownloadGb": 8.12,
  "totalUploadGb": 0.41,
  "totalGb": 8.53,
  "items": [
    {
      "date": "1404/05/24",
      "downloadGb": 1.20,
      "uploadGb": 0.05,
      "totalGb": 1.25
    },
    {
      "date": "1404/05/23",
      "downloadGb": 0.80,
      "uploadGb": 0.02,
      "totalGb": 0.82
    }
  ]
}
```

### فیلدها

| فیلد | نوع | توضیح |
|------|------|-------|
| `success` | bool | در پاسخ موفق همیشه `true` |
| `name` | string | نام اشتراک؛ بخش قبل از اولین `$` یا `@` در فیلد `email` جدول `v2_user` |
| `fromDate` | string | شروع بازه به **شمسی** با فرمت `yyyy/MM/dd` |
| `toDate` | string | پایان بازه به **شمسی** (امروز) با فرمت `yyyy/MM/dd` |
| `totalDownloadGb` | number | مجموع دانلود بازه به گیگابایت (گرد شده تا ۲ رقم اعشار) |
| `totalUploadGb` | number | مجموع آپلود بازه به گیگابایت (گرد شده تا ۲ رقم اعشار) |
| `totalGb` | number | مجموع مصرف بازه = دانلود + آپلود، به گیگابایت |
| `items` | array | لیست روزهایی که ترافیک ثبت شده؛ **جدیدترین روز اول** است |

### فیلدهای هر آیتم

| فیلد | نوع | توضیح |
|------|------|-------|
| `date` | string | تاریخ آن روز به شمسی `yyyy/MM/dd` |
| `downloadGb` | number | دانلود آن روز به گیگابایت |
| `uploadGb` | number | آپلود آن روز به گیگابایت |
| `totalGb` | number | مجموع آن روز به گیگابایت |

### قرارداد مقادیر خاص

| حالت | `items` | مجموع‌ها |
|------|---------|----------|
| اشتراک با مصرف در بازه | یک یا چند روز | اعداد مثبت |
| اشتراک بدون هیچ ترافیکی در ۳۰ روز | `[]` | همه `0` |
| چند رکورد در یک روز (چند سرور / چند snapshot) | یک آیتم برای آن روز | مقادیر همان روز با هم جمع شده‌اند |

> روزهایی که هیچ رکوردی در `v2_stat_user` ندارند در `items` نیستند. اگر برای نمودار به ۳۰ نقطهٔ پیوسته نیاز دارید، سمت کلاینت روزهای جاافتاده را با صفر پر کنید.

> حجم‌ها با `1024³` به گیگابایت تبدیل می‌شوند (همان قرارداد پنل)، نه `1000³`.

> این اعداد **مصرف روزانهٔ ثبت‌شده در ۳۰ روز** هستند، نه `usedVolumeGb` اندپوینت `Info`. مقدار `Info` حجم مصرف‌شده از ابتدای بستهٔ فعلی است و با جمع `items` لزوماً یکی نیست.

---

## پاسخ‌های خطا

همه خطاها با همین ساختار و با HTTP Status متناظر برمی‌گردند:

```json
{ "success": false, "message": "اشتراک یافت نشد" }
```

| Status | `message` | علت |
|:------:|-----------|------|
| `400` | `پارامتر token ارسال نشده است` | نه `token` در کوئری‌استرینگ آمده و نه هدر `Authorization` |
| `404` | `اشتراک یافت نشد` | توکن در جدول `v2_user` وجود ندارد |
| `503` | `سرور پیکربندی نشده است` | سرور پیش‌فرض پنل در کش/دیتابیس موجود نیست |
| `500` | `خطای داخلی` | خطای غیرمنتظره (جزئیات در NLog ثبت می‌شود) |

---

## نمونه فراخوانی

### cURL — قابل ایمپورت در Postman

```bash
curl --location --request GET "https://panel.example.com/api/v1/Sub/Usage?token=8f3a1b2c4d5e6f7a8b9c" --header "Accept: application/json"
```

### cURL — با هدر (توصیه‌شده، توکن در URL ثبت نمی‌شود)

```bash
curl --location --request GET "https://panel.example.com/api/v1/Sub/Usage" --header "Authorization: 8f3a1b2c4d5e6f7a8b9c" --header "Accept: application/json"
```

### JavaScript

```javascript
const res = await fetch(`/api/v1/Sub/Usage?token=${encodeURIComponent(token)}`);
if (!res.ok) {
  const err = await res.json();
  throw new Error(err.message);
}

const data = await res.json();
console.log(data.name, data.totalGb, data.items.length);
data.items.forEach(day => {
  console.log(day.date, day.totalGb);
});
```

### Kotlin

```kotlin
val usage = HttpUtil.getJson<SubscriptionUsage>(
    AgentApi.url("/api/v1/Sub/Usage?token=$subToken")
)

val points = usage.items.map { it.date to it.totalGb }
```

### C#

```csharp
using (var http = new HttpClient())
{
    var url = "https://panel.example.com/api/v1/Sub/Usage?token=" + Uri.EscapeDataString(token);
    var json = await http.GetStringAsync(url);
    var usage = JsonConvert.DeserializeObject<SubscriptionUsageHistoryModel>(json);
}
```

---

## طراحی و عملکرد

منبع داده همان جدول آمار V2board است که پنل (`SubscriptionsController.BuildUsageHistoryAsync`) و ربات تلگرام از آن می‌خوانند:

```sql
SELECT id,email FROM v2_user WHERE token=@token LIMIT 1
SELECT d,u,updated_at FROM v2_stat_user WHERE user_id=@userId AND updated_at>=@startUnix
```

- توکن و `user_id` به‌صورت پارامتر پاس داده می‌شوند (بدون ریسک SQL Injection).
- دو کوئری پشت‌سرهم روی **یک** اتصال MySQL اجرا می‌شوند؛ اگر اشتراک پیدا نشود، کوئری دوم اصلاً زده نمی‌شود.
- رکوردهای یک روز (چند نود / چند snapshot) در حافظه با هم جمع می‌شوند.
- **بدون Entity Framework** — مثل `Info`، هیچ `DbContext` ساخته نمی‌شود.
- **Session غیرفعال** است (`[SessionState(SessionStateBehavior.Disabled)]`).
- **بدون `LogActionFilter`**.
- اطلاعات سرور از کش (`ServerCacheHelper`) خوانده می‌شود.
- خروجی JSON آماده‌شده به‌مدت **۶۰ ثانیه** در `HttpRuntime.Cache` با کلید توکن نگه داشته می‌شود.

### تغییر مدت کش

مقدار ثابت `UsageCacheSeconds` در `SubController.cs`. مقدار `0` یعنی کش کاملاً غیرفعال:

```csharp
/// <summary>مدت کش پاسخ تاریخچه مصرف (ثانیه). صفر یعنی بدون کش.</summary>
private const int UsageCacheSeconds = 60;
```

آمار `v2_stat_user` معمولاً هر چند دقیقه یک‌بار به‌روز می‌شود؛ کش ۶۰ ثانیه‌ای برای نمودار مصرف کافی است.

### پیش‌نیاز دیتابیس

هیچ تغییر اسکیمایی لازم نیست. برای سرعت:

- روی ستون `token` جدول `v2_user` معمولاً ایندکس یکتا وجود دارد.
- روی `v2_stat_user` ایندکس `(user_id, updated_at)` توصیه می‌شود (در V2board اغلب از قبل هست).

---

## ملاحظات امنیتی

- توکن اشتراک نقش کلید دسترسی را دارد؛ هرکس توکن را داشته باشد تاریخچه مصرف را می‌بیند — دقیقاً مثل خود لینک ساب و [`/api/v1/Sub/Info`](API-SubscriptionInfo.md).
- **توکن را در کوئری‌استرینگ نفرستید مگر مجبور باشید.** حالت هدر `Authorization` برای همین پشتیبانی می‌شود.
- هدر CORS روی `*` تنظیم شده تا از اپ موبایل و مرورگر قابل استفاده باشد.
- خروجی هیچ اطلاعات نماینده، ایمیل کامل، UUID یا کانفیگی را برنمی‌گرداند.
- این اندپوینت را روی `HTTPS` سرو کنید.
