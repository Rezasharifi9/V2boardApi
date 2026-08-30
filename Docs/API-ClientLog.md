# API ثبت خطای اپلیکیشن (Client Log)

کلاینت اندروید خطاها، کرش‌ها و هشدارهایی که می‌خورد را به پنل می‌فرستد تا در همان جدول `NLog` با **تگ ثابت اپلیکیشن** ذخیره شوند. در صفحهٔ **پیکربندی ← لاگ سیستم** با فیلتر Logger برابر `AndroidApp` جدا می‌شوند.

- کنترلر: `V2boardApi/Areas/api/Controllers/MobileDeviceController.cs` → متد `LogError`
- مدل ورودی: `V2boardApi/Areas/api/Data/ApiModels/ClientLogModel.cs`
- جدول: `dbo.NLog` (همان لاگ سیستم)
- صفحه پنل: `App/Settings/SystemLogs` — دکمهٔ **لاگ اپلیکیشن** فیلتر را روی `AndroidApp` می‌گذارد

کلاینت این را بعد از داشتن `agentToken` صدا می‌زند — معمولاً از یک UncaughtExceptionHandler و از catchهای مهم شبکه/VPN.

---

## تگ‌گذاری برای رهگیری

هر رکورد این فیلدها را پر می‌کند:

| ستون NLog | مقدار | کاربرد در پنل |
|-----------|--------|----------------|
| `Logger` | `AndroidApp.{packageName}` مثلاً `AndroidApp.com.safenet.client` | فیلتر Logger: `AndroidApp` همهٔ اپ‌ها، یا نام کامل یک بیلد |
| `controllerName` | `AndroidApp` | فیلتر کنترلر |
| `actionName` | همان `tag` کلاینت، مثل `crash` یا `vpn-core` | فیلتر اکشن / ماژول |
| `userName` | نام کاربری نمایندهٔ صاحب بیلد | فیلتر کاربر |
| `sessionId` | `deviceId` کلاینت | جزئیات لاگ |
| `customData` | JSON مشخصات دستگاه، نسخه، صفحه، extra | جزئیات لاگ و جستجوی جدول |
| `Exception` | استک‌تریس سمت کلاینت | جزئیات لاگ |
| `Message` | `[tag] متن خطا` | لیست و جستجو |
| `Level` | `Fatal` / `Error` / `Warn` / `Info` / `Debug` / `Trace` | فیلتر سطح |

اگر `packageName` نیامده باشد Logger فقط `AndroidApp` است.

---

## اندپوینت

```
POST /MobileDevice/LogError
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type درخواست | `application/json` |
| CORS | `Access-Control-Allow-Origin: *` |

> مسیر بدون پیشوند `api/v1` است — روت Web API یعنی `{controller}/{action}`، دقیقاً مثل [`MobileDevice/Register`](API-MobileDevice.md).

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | همان `agentToken` که [`Sub/Agent`](API-SubAgent.md) برگردانده. `Bearer <token>` هم پذیرفته می‌شود |
| `Content-Type` | بله | `application/json` |

اگر کلاینتی نتواند هدر بفرستد، فیلد `agentToken` در بدنه هم خوانده می‌شود. هدر اولویت دارد.

---

## بدنه‌ی درخواست

حداقل یکی از `message` یا `exception` باید پر باشد.

```json
{
  "level": "Error",
  "tag": "vpn-core",
  "message": "شروع VPN ناموفق بود",
  "exception": "java.lang.IllegalStateException: tun fd is null\n\tat com.v2ray.ang.core.CoreVpnService.start(...)",
  "deviceId": "9774d56d682e549c",
  "packageName": "com.safenet.client",
  "appVersion": "2.1.8",
  "versionCode": 218,
  "manufacturer": "Samsung",
  "model": "SM-A546E",
  "device": "android",
  "osVersion": "15",
  "sdk": 35,
  "screen": "MainActivity",
  "extra": {
    "server": "de-1",
    "mode": "vpn"
  }
}
```

### فیلدها

| فیلد | نوع | الزامی | توضیح |
|------|------|:------:|-------|
| `level` | string | خیر | `Fatal`، `Error`، `Warn`، `Info`، `Debug`، `Trace`. مقدار `crash` به `Fatal` نگاشت می‌شود. پیش‌فرض `Error` |
| `tag` | string | خیر | تگ ماژول/صفحه برای فیلتر اکشن. حروف، عدد، `.` `_` `-` `/`. پیش‌فرض `app` |
| `message` | string | یکی از این دو | متن کوتاه خطا. در لیست به صورت `[tag] متن` دیده می‌شود |
| `exception` | string | یکی از این دو | استک‌تریس. حداکثر حدود ۳۲ هزار کاراکتر نگه داشته می‌شود |
| `deviceId` | string | خیر | همان شناسهٔ نصب [`Register`](API-MobileDevice.md) |
| `packageName` | string | خیر | نام بسته؛ بخشی از Logger می‌شود تا بیلدهای وایت‌لیبل جدا شوند |
| `appVersion` | string | خیر | `versionName` |
| `versionCode` | int | خیر | شماره بیلد |
| `manufacturer` | string | خیر | سازنده دستگاه |
| `model` | string | خیر | مدل دستگاه |
| `device` | string | خیر | پلتفرم: `android` / `ios` / `windows` |
| `osVersion` | string | خیر | نسخه سیستم‌عامل |
| `sdk` | int | خیر | سطح API |
| `screen` | string | خیر | اکتیویتی / صفحه / کلاس محل خطا |
| `extra` | object | خیر | دادهٔ آزاد JSON |
| `agentToken` | string | خیر | فقط اگر هدر Authorization فرستاده نشود |

---

## پاسخ موفق — `200 OK`

```json
{
  "ok": true,
  "logger": "AndroidApp.com.safenet.client"
}
```

| فیلد | توضیح |
|------|--------|
| `ok` | همیشه `true` وقتی رکورد ذخیره شده |
| `logger` | همان مقداری که در ستون Logger نوشته شده — برای اطمینان از تگ |

این اندپوینت را **موفق** حساب کنید و دوباره نفرستید. ارسال تکراری رکورد تکراری می‌سازد.

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است:

```json
"توکن نماینده در هدر Authorization ارسال نشده است"
```

| Status | پیام | علت |
|:------:|------|------|
| `400` | `اطلاعات لاگ ارسال نشده است` | بدنه خالی یا JSON نامعتبر |
| `400` | `متن پیام یا متن خطا ارسال نشده است` | هم `message` و هم `exception` خالی‌اند |
| `400` | `توکن نماینده در هدر Authorization ارسال نشده است` | هدر و فیلد بدنه هر دو خالی‌اند |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `500` | `خطا در ثبت لاگ` | خطای غیرمنتظره سمت سرور |

---

## منطق پیشنهادی کلاینت اندروید

1. بعد از داشتن `agentToken` یک helper بسازید که خطاها را **غیرهمزمان** و **بدون مسدود کردن UI** بفرستد.
2. برای کرش: `Thread.setDefaultUncaughtExceptionHandler` با `level=crash` و `tag=crash`.
3. برای خطاهای عملیاتی (اتصال VPN، HTTP، پرداخت) همان helper را با تگ همان ماژول صدا بزنید.
4. **شکست همین اندپوینت را دوباره به LogError نفرستید** — حلقهٔ بی‌نهایت می‌سازد.
5. اگر شبکه قطع است، در یک صف محلی نگه دارید و بعداً با backoff بفرستید؛ اجباری نیست.

نمونهٔ Kotlin:

```kotlin
fun sendClientLog(
    agentToken: String,
    tag: String,
    message: String,
    throwable: Throwable? = null,
    level: String = if (throwable == null) "Error" else "Error",
    screen: String? = null
) {
    thread(name = "client-log") {
        try {
            val body = JSONObject()
                .put("level", level)
                .put("tag", tag)
                .put("message", message)
                .put("exception", throwable?.stackTraceToString())
                .put("deviceId", Settings.Secure.getString(contentResolver, Settings.Secure.ANDROID_ID))
                .put("packageName", BuildConfig.APPLICATION_ID)
                .put("appVersion", BuildConfig.VERSION_NAME)
                .put("versionCode", BuildConfig.VERSION_CODE)
                .put("manufacturer", Build.MANUFACTURER)
                .put("model", Build.MODEL)
                .put("device", "android")
                .put("osVersion", Build.VERSION.RELEASE)
                .put("sdk", Build.VERSION.SDK_INT)
                .put("screen", screen)

            val req = Request.Builder()
                .url(AgentApi.url("/MobileDevice/LogError"))
                .header("Authorization", agentToken)
                .post(body.toString().toRequestBody("application/json".toMediaType()))
                .build()

            okHttpClient.newCall(req).execute().use { /* ignore body on failure */ }
        } catch (_: Exception) {
            // این شکست را دوباره لاگ نکنید
        }
    }
}

Thread.setDefaultUncaughtExceptionHandler { _, e ->
    sendClientLog(agentToken, tag = "crash", message = e.message ?: "uncaught", throwable = e, level = "crash")
    previousHandler?.uncaughtException(Thread.currentThread(), e)
}
```

---

## نمونه فراخوانی

### cURL

```bash
curl -X POST "https://panel.example.com/MobileDevice/LogError" \
  -H "Authorization: 9f2c1ab7d4e8" \
  -H "Content-Type: application/json" \
  -d "{\"level\":\"Error\",\"tag\":\"vpn-core\",\"message\":\"VPN start failed\",\"packageName\":\"com.safenet.client\",\"deviceId\":\"9774d56d682e549c\"}"
```

### رهگیری در پنل

1. **پیکربندی ← لاگ سیستم**
2. دکمهٔ **لاگ اپلیکیشن** یا فیلتر Logger = `AndroidApp`
3. برای یک بیلد خاص: Logger = `AndroidApp.com.safenet.client`
4. برای یک ماژول: اکشن = `vpn-core` یا `crash`
5. آیکون چشم، `customData` و `Exception` را نشان می‌دهد

---

## ملاحظات

- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است — مثل بقیه‌ی `/MobileDevice/*`.
- بدنهٔ درخواست به‌عنوان `requestData` ذخیره **نمی‌شود** تا استک‌تریس دوبار نوشته نشود.
- ارسال تکراری همان خطا، رکورد تکراری می‌سازد؛ کلاینت باید بعد از `200` دوباره نفرستد.
- `Level` در دیتابیس حداکثر ۵ کاراکتر است؛ مقادیر غیرمجاز به `Error` تبدیل می‌شوند.
- حجم `message` / `exception` / `customData` سمت سرور کوتاه می‌شود تا یک کلاینت جدول را پر نکند.
