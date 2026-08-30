# API اطلاعات اشتراک (Subscription Info)

سرویس سبک و پرسرعت برای گرفتن خلاصه وضعیت یک اشتراک با استفاده از **توکن لینک ساب**.

- کنترلر: `V2boardApi/Areas/api/Controllers/SubController.cs` → متد `Info`
- مدل خروجی: `V2boardApi/Areas/api/Data/ApiModels/SubscriptionTokenInfoModel.cs`

> اندپوینت هم‌خانواده: [`GET /api/v1/Sub/Agent`](API-SubAgent.md) با همان توکن، **نماینده‌ی صاحب اشتراک** و توکن او را برمی‌گرداند. تاریخچه مصرف ۳۰ روز: [`GET /api/v1/Sub/Usage`](API-SubscriptionUsage.md). تغییر لینک اشتراک: [`POST /api/v1/Sub/ResetLink`](API-SubscriptionResetLink.md). موجودی کیف پول ربات تلگرام همان اشتراک: [`POST /User/GetTelegramWallet`](API-TelegramWallet.md). سوالات متداول ربات: [`GET /User/GetFaq`](API-Faq.md). لینک‌های پشتیبانی نماینده: [`GET /User/GetSupportLinks`](API-SupportLinks.md). نسخه اپلیکیشن: [`GET /User/GetAppRelease`](API-AppRelease.md).

---

## اندپوینت

```
GET /api/v1/Sub/Info?token={token}
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | ندارد؛ خودِ `token` نقش کلید دسترسی را دارد |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` |
| کش سمت کلاینت | غیرفعال (`Cache-Control: no-cache`) |

### پارامترها

| نام | محل | نوع | الزامی | توضیح |
|-----|-----|------|:------:|-------|
| `token` | Query String | string | بله | همان توکنی که در لینک اشتراک استفاده می‌شود: `https://{SubAddress}/api/v1/client/subscribe?token={token}` |

---

## پاسخ موفق — `200 OK`

```json
{
  "success": true,
  "name": "reza",
  "totalVolumeGb": 50.0,
  "usedVolumeGb": 12.34,
  "remainingDays": 18,
  "expireDate": "1404/05/22"
}
```

### فیلدها

| فیلد | نوع | توضیح |
|------|------|-------|
| `success` | bool | در پاسخ موفق همیشه `true` |
| `name` | string | نام اشتراک؛ بخش قبل از اولین `$` یا `@` در فیلد `email` جدول `v2_user` |
| `totalVolumeGb` | number | حجم کل اشتراک به گیگابایت (گرد شده تا ۲ رقم اعشار) |
| `usedVolumeGb` | number | حجم مصرف‌شده = آپلود + دانلود، به گیگابایت (گرد شده تا ۲ رقم اعشار) |
| `remainingDays` | int | تعداد روز باقی‌مانده تا انقضا |
| `expireDate` | string \| null | تاریخ انقضا به **شمسی** با فرمت `yyyy/MM/dd` |

### قرارداد مقادیر خاص

| حالت | `remainingDays` | `expireDate` |
|------|:---------------:|--------------|
| اشتراک فعال | عدد مثبت | تاریخ شمسی |
| اشتراک منقضی‌شده | `0` | تاریخ شمسی (تاریخ گذشته) |
| اشتراک بدون تاریخ انقضا (نامحدود) | `-1` | `null` |

> نکته: `remainingDays` بر مبنای «تعداد روزهای کامل از امروز» محاسبه می‌شود (`Utility.CalculateLeftDayes`) و هیچ‌وقت منفی برنمی‌گردد.

> نکته: حجم باقی‌مانده در خروجی نیست و در صورت نیاز از `totalVolumeGb - usedVolumeGb` محاسبه می‌شود. اگر `usedVolumeGb` بزرگ‌تر از `totalVolumeGb` باشد یعنی حجم تمام شده است.

---

## پاسخ‌های خطا

همه خطاها با همین ساختار و با HTTP Status متناظر برمی‌گردند:

```json
{ "success": false, "message": "اشتراک یافت نشد" }
```

| Status | `message` | علت |
|:------:|-----------|------|
| `400` | `پارامتر token ارسال نشده است` | `token` خالی یا ارسال نشده |
| `404` | `اشتراک یافت نشد` | توکن در جدول `v2_user` وجود ندارد |
| `503` | `سرور پیکربندی نشده است` | سرور پیش‌فرض پنل در کش/دیتابیس موجود نیست |
| `500` | `خطای داخلی` | خطای غیرمنتظره (جزئیات در NLog ثبت می‌شود) |

---

## نمونه فراخوانی

### cURL

```bash
curl "https://panel.example.com/api/v1/Sub/Info?token=ab12cd34ef56"
```

### JavaScript

```javascript
const res = await fetch(`/api/v1/Sub/Info?token=${encodeURIComponent(token)}`);
if (res.ok) {
  const data = await res.json();
  console.log(data.name, data.usedVolumeGb, data.remainingDays, data.expireDate);
}
```

### C#

```csharp
using (var http = new HttpClient())
{
    var url = "https://panel.example.com/api/v1/Sub/Info?token=" + Uri.EscapeDataString(token);
    var json = await http.GetStringAsync(url);
    var info = JsonConvert.DeserializeObject<SubscriptionTokenInfoModel>(json);
}
```

---

## طراحی و عملکرد

این اندپوینت برای فراخوانی پرتکرار طراحی شده و عمداً از مسیر سنگین بقیه کنترلرها جدا نگه داشته شده است:

- **بدون Entity Framework و بدون Repository** — کنترلر هیچ `Entities` یا `Repository<T>` نمی‌سازد (برخلاف `ClientController` و `MobileAppController` که در سازنده یک DbContext و چند ریپازیتوری می‌سازند). فقط یک کوئری مستقیم MySQL اجرا می‌شود.
- **یک کوئری، حداقل ستون، پارامتری:**
  ```sql
  SELECT email,u,d,transfer_enable,expired_at FROM v2_user WHERE token=@token LIMIT 1
  ```
  توکن به‌صورت پارامتر پاس داده می‌شود (بدون ریسک SQL Injection).
- **خواندن با ایندکس عددی ستون** به‌جای `GetOrdinal`، و جداسازی نام با `IndexOfAny` به‌جای دو بار `Split` (بدون تخصیص آرایه اضافه).
- **Session غیرفعال** — با `[SessionState(SessionStateBehavior.Disabled)]` قفل Session در ASP.NET حذف می‌شود؛ روی درخواست‌های همزمان تأثیر مستقیم دارد.
- **بدون `LogActionFilter`** — لاگ کامل هر درخواست برای این حجم فراخوانی گران است، بنابراین این فیلتر روی این کنترلر گذاشته نشده. خطاها همچنان در NLog ثبت می‌شوند.
- **اطلاعات سرور از کش** (`ServerCacheHelper`) خوانده می‌شود، نه از دیتابیس.
- **کش کوتاه‌مدت پاسخ** — خروجی JSON آماده‌شده به‌مدت **۱۰ ثانیه** در `HttpRuntime.Cache` با کلید توکن نگه داشته می‌شود؛ در این بازه نه به MySQL درخواستی می‌رود و نه دوباره سریالایز انجام می‌شود.

### تغییر مدت کش

مقدار ثابت `CacheSeconds` در `SubController.cs` را تغییر بده. مقدار `0` یعنی کش کاملاً غیرفعال و داده همیشه لحظه‌ای:

```csharp
/// <summary>مدت کش پاسخ (ثانیه). صفر یعنی بدون کش.</summary>
private const int CacheSeconds = 10;
```

### پیش‌نیاز دیتابیس

روی ستون `token` در جدول `v2_user` باید ایندکس وجود داشته باشد (در V2board معمولاً به‌صورت unique index موجود است). کل سرعت این سرویس به همین یک lookup وابسته است.

---

## ملاحظات امنیتی

- توکن اشتراک نقش کلید دسترسی را دارد؛ هرکس توکن را داشته باشد می‌تواند این اطلاعات را ببیند — دقیقاً مثل خود لینک ساب. توکن را در لاگ‌های عمومی یا URL های اشتراکی قرار ندهید.
- هدر CORS روی `*` تنظیم شده تا از اپ موبایل و مرورگر قابل استفاده باشد؛ اگر می‌خواهید محدود شود، در متد `JsonResponse` مقدار `Access-Control-Allow-Origin` را به دامنه مورد نظر تغییر دهید.
- خروجی هیچ اطلاعات نماینده، ایمیل کامل، UUID یا کانفیگی را برنمی‌گرداند.
