# API نسخه اپلیکیشن (App Release)

نسخهٔ منتشرشدهٔ اپلیکیشن، لینک دانلود، متن تغییرات و پرچم نصب اجباری — برای نمایش دیالوگ به‌روزرسانی در کلاینت اندروید.

ادمین این مقادیر را در پنل، منوی **پیکربندی ← نسخه اپلیکیشن** وارد می‌کند. یک رکورد برای کل پنل است، نه به‌ازای هر نماینده.

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `GetAppRelease`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/AppReleaseViewModel.cs`
- جدول: `dbo.tbAppRelease` — اسکریپت ساخت: [`Database/AddAppRelease.sql`](../Database/AddAppRelease.sql)
- صفحه پنل: `App/Settings/AppRelease`

کلاینت این را بعد از داشتن `agentToken` صدا می‌زند — معمولاً در شروع برنامه، کنار [`MobileDevice/Register`](API-MobileDevice.md).

---

## اندپوینت

```
GET /User/GetAppRelease
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` (از `[EnableCors]` روی کنترلر) |

> مسیر بدون پیشوند `api/v1` است. `UserController` یک `ApiController` است و روی روت Web API یعنی `{controller}/{action}` می‌نشیند — دقیقاً مثل [`GetFaq`](API-Faq.md) و [`GetSupportLinks`](API-SupportLinks.md).

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | همان `agentToken` که [`Sub/Agent`](API-SubAgent.md) برگردانده. `Bearer <token>` هم پذیرفته می‌شود |

هر دو فرم زیر پذیرفته می‌شوند:

```
Authorization: 9f2c1ab7d4e8...
Authorization: Bearer 9f2c1ab7d4e8...
```

---

## پاسخ موفق — `200 OK`

```json
{
  "version": "2.1.8",
  "versionCode": 218,
  "downloadUrl": "https://cdn.example.com/app-2.1.8.apk",
  "changelog": [
    "رفع باگ اتصال",
    "بهبود پایداری",
    "نمایش مصرف روزانه در صفحه اشتراک"
  ],
  "forceInstall": true
}
```

اگر هنوز در پنل چیزی ذخیره نشده باشد، فیلدهای خالی با `changelog` آرایه خالی و `forceInstall: false` برمی‌گردند — این هم `200` است، نه خطا:

```json
{
  "version": null,
  "versionCode": null,
  "downloadUrl": null,
  "changelog": [],
  "forceInstall": false
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|:----:|-------|
| `version` | string \| null | `tbAr_Version` | نسخهٔ نمایشی (`versionName`) مثل `2.1.8` |
| `versionCode` | int \| null | `tbAr_VersionCode` | شماره بیلد اندروید برای مقایسه با نسخهٔ نصب‌شده |
| `downloadUrl` | string \| null | `tbAr_DownloadUrl` | لینک دانلود APK / صفحهٔ نصب |
| `changelog` | string[] | `tbAr_Changelog` | لیست تغییرات همین نسخه؛ هر عنصر یک مورد جدا. اگر موردی ثبت نشده باشد آرایه خالی است |
| `forceInstall` | bool | `tbAr_ForceInstall` | اگر `true` باشد کاربر نمی‌تواند دیالوگ آپدیت را ببندد |

---

## منطق پیشنهادی کلاینت اندروید

1. بعد از داشتن `agentToken` این اندپوینت را صدا بزنید.
2. اگر `versionCode` مقدار دارد و `versionCode` محلی کوچک‌تر است، دیالوگ به‌روزرسانی را نشان دهید.
3. اگر `versionCode` خالی است ولی `version` پر است، با `versionName` محلی مقایسه کنید.
4. اگر `forceInstall` برابر `true` است، دیالوگ را غیرقابل‌بستن کنید و بقیهٔ برنامه را قفل کنید.
5. دکمهٔ دانلود را به `downloadUrl` وصل کنید. اگر `downloadUrl` خالی است، دکمه را نشان ندهید.
6. `changelog` را به‌صورت لیست نشان دهید (هر عنصر یک ردیف). اگر آرایه خالی است، بخش تغییرات را پنهان کنید.
7. اگر همهٔ فیلدهای نسخه/لینک خالی‌اند، هیچ دیالوگی نشان ندهید.

نمونهٔ Kotlin:

```kotlin
val release = HttpUtil.get<AppReleaseViewModel>(
    url = AgentApi.url("/User/GetAppRelease"),
    headers = AgentApi.authHeaders(agentToken)
)

val localCode = BuildConfig.VERSION_CODE
val remoteCode = release.versionCode
val needsUpdate = when {
    remoteCode != null -> localCode < remoteCode
    !release.version.isNullOrBlank() -> BuildConfig.VERSION_NAME != release.version
    else -> false
}

if (needsUpdate) {
    showUpdateDialog(
        changelog = release.changelog,
        downloadUrl = release.downloadUrl,
        force = release.forceInstall
    )
}
```

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است:

```json
"توکن در هدر Authorization ارسال نشده است"
```

| Status | پیام | علت |
|:------:|------|------|
| `400` | `توکن در هدر Authorization ارسال نشده است` | هدر `Authorization` وجود ندارد یا خالی است |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `500` | `خطا در دریافت نسخه اپلیکیشن` | خطای غیرمنتظره؛ جزئیات در NLog |

---

## نمونه فراخوانی

### cURL

```bash
curl "https://panel.example.com/User/GetAppRelease" -H "Authorization: 9f2c1ab7d4e8"
```

### JavaScript

```javascript
const res = await fetch('/User/GetAppRelease', {
  headers: { 'Authorization': agentToken }
});

if (res.ok) {
  const data = await res.json();
  maybeShowUpdate(data);
}
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);
    var json = await http.GetStringAsync("https://panel.example.com/User/GetAppRelease");
    var release = JsonConvert.DeserializeObject<AppReleaseViewModel>(json);
}
```

---

## ملاحظات

- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است — مثل بقیه‌ی `/User/*`.
- خروجی هیچ کانفیگ، توکن ساب یا اطلاعات مشتری برنمی‌گرداند.
- لینک دانلود را روی `HTTPS` بگذارید.
- `versionCode` را با همان عددی که در بیلد اندروید (`versionCode` / `VERSION_CODE`) است پر کنید؛ مقایسهٔ رشته‌ای `version` فقط وقتی معنا دارد که `versionCode` خالی باشد.
- `changelog` همیشه آرایه است. در پنل هر تغییر یک ردیف جدا ذخیره می‌شود.
