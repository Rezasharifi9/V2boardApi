# API تغییر لینک اشتراک (Reset Subscription Link)

معادل گزینه **تغییر لینک 🔗** در ربات تلگرام: توکن لینک ساب عوض می‌شود، لینک قبلی قطع می‌گردد، و **توکن جدید + لینک کامل جدید** برمی‌گردد تا اپلیکیشن همان‌جا اشتراک را به‌روز کند.

- کنترلر: `V2boardApi/Areas/api/Controllers/SubController.cs` → متد `ResetLink`
- مدل خروجی: `V2boardApi/Areas/api/Data/ApiModels/SubscriptionResetLinkModel.cs`
- مرجع رفتاری در ربات: `BotController` → شاخه‌ی `callback[0] == "ResetLink"`

> اندپوینت هم‌خانواده: [`GET /api/v1/Sub/Info`](API-SubscriptionInfo.md) · [`GET /api/v1/Sub/Usage`](API-SubscriptionUsage.md) · [`GET /api/v1/Sub/Agent`](API-SubAgent.md)

---

## چه کار می‌کند

با داشتن توکن فعلی اشتراک:

1. کاربر `v2_user` با همان توکن پیدا می‌شود
2. یک توکن ۱۶ کاراکتری جدید و یک `uuid` جدید ساخته می‌شود (همان الگوریتم ربات و پنل)
3. ستون‌های `token` و `uuid` در MySQL (`v2_user`) به‌روز می‌شوند — کانکشن‌های فعلی روی UUID قدیمی قطع می‌شوند
4. `tbLinks.tbL_Token` در SQL Server با توکن جدید عوض می‌شود تا کیف پول، فاکتور و تمدید از کار نیفتند
5. لاگ‌های پنل که `SubToken` قدیمی دارند به توکن جدید منتقل می‌شوند
6. کش `Info` / `Agent` / `Usage` برای توکن قدیمی پاک می‌شود

بعد از این فراخوانی، لینک قبلی دیگر کانفیگ برنمی‌گرداند. کلاینت باید مقدار `token` (یا `subscriptionLink`) را ذخیره کند و از آن به بعد همهٔ درخواست‌های ساب را با توکن جدید بزند.

---

## اندپوینت

```
POST /api/v1/Sub/ResetLink?token={token}
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | ندارد؛ خودِ توکن اشتراک نقش کلید دسترسی را دارد |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` |
| کش سمت کلاینت | غیرفعال (`Cache-Control: no-cache`) |
| کش سمت سرور | ندارد — این عملیات نوشتنی است |

> این اندپوینت **پیشوند `api/v1` دارد**، چون `SubController` یک کنترلر MVC است — برخلاف `/User/*` و `/MobileDevice/*` که روی روت Web API یعنی `{controller}/{action}` می‌نشینند.

> `POST` انتخاب شده چون لینک قبلی را باطل می‌کند. `GET` پشتیبانی نمی‌شود تا prefetch مرورگر یا WebView اشتباهاً لینک را عوض نکند.

### پارامترها

| نام | محل | نوع | الزامی | توضیح |
|-----|-----|------|:------:|-------|
| `token` | Query String | string | بله* | همان توکن فعلی لینک ساب: `https://{SubAddress}/api/v1/client/subscribe?token={token}` |

\* اگر `token` در کوئری‌استرینگ نیاید، مقدار هدر `Authorization` خوانده می‌شود. هر دو فرم `Authorization: <token>` و `Authorization: Bearer <token>` پذیرفته می‌شوند. **فرستادن با هدر توصیه می‌شود** — URLها در لاگ IIS و تاریخچه مرورگر ذخیره می‌شوند.

بدنه درخواست لازم نیست.

---

## پاسخ موفق — `200 OK`

```json
{
  "success": true,
  "token": "a1b2c3d4e5f67890",
  "subscriptionLink": "https://sub.example.com/api/v1/client/subscribe?token=a1b2c3d4e5f67890",
  "backupSubscriptionLink": "https://sub2.example.com/api/v1/client/subscribe?token=a1b2c3d4e5f67890"
}
```

### فیلدها

| فیلد | نوع | توضیح |
|------|------|-------|
| `success` | bool | در پاسخ موفق همیشه `true` |
| `token` | string | توکن جدید — از این به بعد همین را نگه دارید و در `Info` / `Usage` / `Agent` بفرستید |
| `subscriptionLink` | string \| null | لینک کامل ساب با توکن جدید (`https://{SubAddress}/api/v1/client/subscribe?token=...`) |
| `backupSubscriptionLink` | string \| null | لینک پشتیبان؛ فقط اگر `tbServers.BackupSubAddr` روی سرور تنظیم شده باشد |

`subscriptionLink` وقتی `null` است که آدرس ساب روی سرور خالی باشد؛ در این حالت باز هم `token` معتبر است و کلاینت می‌تواند لینک را خودش بسازد.

---

## پاسخ‌های خطا

همه خطاها با همین ساختار و با HTTP Status متناظر برمی‌گردند:

```json
{ "success": false, "message": "اشتراک یافت نشد" }
```

| Status | `message` | علت |
|:------:|-----------|------|
| `400` | `پارامتر token ارسال نشده است` | `token` خالی یا ارسال نشده |
| `404` | `اشتراک یافت نشد` | توکن فعلی در جدول `v2_user` وجود ندارد (یا قبلاً عوض شده) |
| `503` | `سرور پیکربندی نشده است` | سرور پیش‌فرض پنل در کش/دیتابیس موجود نیست |
| `500` | `خطای داخلی` | خطای غیرمنتظره (جزئیات در NLog ثبت می‌شود) |

اگر توکن را دو بار پشت‌سرهم عوض کنید، بار دوم با توکن **قدیمی** پاسخ `404` می‌گیرید — باید توکنی که پاسخ قبلی برگردانده را بفرستید.

---

## نمونه فراخوانی

### cURL — با هدر (توصیه‌شده)

```bash
curl --location --request POST "https://panel.example.com/api/v1/Sub/ResetLink" \
  --header "Authorization: 8f3a1b2c4d5e6f7a8b9c" \
  --header "Accept: application/json"
```

### cURL — توکن در کوئری

```bash
curl --location --request POST "https://panel.example.com/api/v1/Sub/ResetLink?token=8f3a1b2c4d5e6f7a8b9c" \
  --header "Accept: application/json"
```

### JavaScript

```javascript
const res = await fetch(`/api/v1/Sub/ResetLink?token=${encodeURIComponent(subToken)}`, {
  method: 'POST'
});
if (!res.ok) {
  const err = await res.json();
  throw new Error(err.message);
}

const data = await res.json();
// لینک قبلی از این لحظه قطع است
subToken = data.token;
const newLink = data.subscriptionLink;
```

### Kotlin

```kotlin
val result = HttpUtil.postJson<SubscriptionResetLink>(
    AgentApi.url("/api/v1/Sub/ResetLink?token=$subToken")
)

subToken = result.token
val newLink = result.subscriptionLink
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
    var res = await http.PostAsync("https://panel.example.com/api/v1/Sub/ResetLink", null);
    var json = await res.Content.ReadAsStringAsync();
    var data = JsonConvert.DeserializeObject<SubscriptionResetLinkModel>(json);
}
```

---

## طراحی و عملکرد

همان مسیر ربات (`ResetLink`) و پنل (`Subscriptions/Reset`) است، با این تفاوت که ورودی **توکن ساب** است نه ایمیل یا `user_id`:

```sql
SELECT email FROM v2_user WHERE token=@token LIMIT 1
update v2_user set token=@newToken,uuid=@Guid where token=@oldToken
```

- توکن به‌صورت پارامتر پاس داده می‌شود (بدون ریسک SQL Injection).
- ساخت توکن جدید دقیقاً مثل ربات است: سه قطعه از `Guid.NewGuid()` به هم چسبیده (۱۶ کاراکتر هگز).
- `uuid` هم عوض می‌شود تا کلاینت‌های VPN که با UUID قدیمی وصل‌اند قطع شوند — همان هشدار ربات: «بعد از تائید تمام افراد متصل قطع می‌شوند».
- `tbLinks` و `tbLogs` روی SQL Server با `Entities` داخل خود متد به‌روز می‌شوند (`DbContext` در سازندهٔ کنترلر ساخته نمی‌شود).
- کش پاسخ‌های خواندنی برای توکن قدیمی حذف می‌شود تا `Info` / `Agent` / `Usage` بلافاصله `404` بدهند، نه دادهٔ ۱۰ یا ۶۰ ثانیه قبل.

### پیش‌نیاز دیتابیس

هیچ تغییر اسکیمایی لازم نیست. روی ستون `token` در `v2_user` باید ایندکس باشد (در V2board معمولاً unique است).

---

## ملاحظات امنیتی و محصول

- هرکس توکن فعلی را داشته باشد می‌تواند لینک را عوض کند — دقیقاً مثل خود لینک ساب و دکمهٔ ربات. توکن را در لاگ‌های عمومی قرار ندهید.
- این عملیات **برگشت‌پذیر نیست**. لینک قبلی برای همیشه بی‌اعتبار می‌شود. در اپ، قبل از فراخوانی همان تأیید ربات را نشان دهید: افراد متصل قطع می‌شوند و لینک جدید بلافاصله برمی‌گردد.
- بعد از موفقیت، مقدار ذخیره‌شدهٔ ساب در اپ را با `token` یا `subscriptionLink` جدید جایگزین کنید؛ وگرنه تمدید، کیف پول و تاریخچه مصرف با توکن مرده `404` می‌گیرند.
- خروجی هیچ اطلاعات نماینده، ایمیل کامل یا UUID را برنمی‌گرداند.
