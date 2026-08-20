# API ثبت دستگاه کلاینت (Android / iOS / Windows)

ثبت هر دستگاهی که اپلیکیشن روی آن نصب شده — گوشی، تبلت یا دسکتاپ — و تخصیص آن به نماینده‌ی صاحب همان بیلد. سرور پلتفرم را محدود نمی‌کند؛ فقط یک شناسه‌ی یکتا می‌خواهد.

| اندپوینت | کار |
|----------|-----|
| [`POST /MobileDevice/Register`](#اندپوینت-ثبت-دستگاه) | ثبت یا به‌روزرسانی یک دستگاه |
| [`POST /MobileDevice/UpdateToken`](#api-بهروزرسانی-توکن-نوتیفیکیشن) | فقط به‌روزرسانی توکن Push |

- کنترلر: `V2boardApi/Areas/api/Controllers/MobileDeviceController.cs`
- مدل ورودی: `V2boardApi/Areas/api/Data/ApiModels/RegisterMobileDeviceModel.cs`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/MobileDeviceViewModel.cs`
- جدول: `dbo.tbMobileUsers` — اسکریپت ساخت: [`Database/AddMobileUsersTable.sql`](../Database/AddMobileUsersTable.sql)
- سمت کلاینت اندروید: `V2rayNG/app/src/main/java/com/v2ray/ang/handler/DeviceRegistrationManager.kt`

کلید اجباری JSON **`deviceId`** است (مستقل از سیستم‌عامل). نام‌های قدیمی `androidId` و `androidVersion` هنوز خوانده می‌شوند تا کلاینت اندروید فعلی نشکند؛ اگر هر دو بیایند `deviceId` / `osVersion` اولویت دارند.

> در **پاسخ** فیلد `deviceId` یک `int` است و شناسه‌ی رکورد در دیتابیس (`tbMu_ID`) است، نه همان رشته‌ی درخواست. در Flutter برای درخواست و پاسخ دو مدل جدا بسازید.

---

## چرا این جدول لازم است

فاکتورهایی که از داخل اپلیکیشن ساخته می‌شوند کاربر تلگرام ندارند. بدون `tbMobileUsers` هیچ راهی نبود که بفهمیم یک فاکتور را کدام دستگاه ساخته، و ارسال Push Notification هم ممکن نبود. هر رکورد این جدول یک **نصب** است، نه یک شخص.

```
tbUsers (نماینده)
   │ 1
   │
   │ *
tbMobileUsers (دستگاه) ──1──*── tbOrders
                       └─1──*── tbDepositWallet_Log
```

---

## شناسه‌ی یکتا به‌ازای هر پلتفرم

فیلد اجباری `deviceId` کلید یکتایی نصب است (ستون `tbMu_AndroidId`، حداکثر ۶۴ کاراکتر). مقدار را از منبع پایدار همان سیستم‌عامل بگیرید — در Flutter معمولاً `device_info_plus`:

| پلتفرم | مقدار پیشنهادی برای `deviceId` | پایدار می‌ماند تا… |
|--------|-------------------------------|---------------------|
| **Android** | `AndroidDeviceInfo.id` (`Settings.Secure.ANDROID_ID`) | نصب مجدد همین اپ روی همین کاربر دستگاه (از اندروید ۸ به بعد به‌ازای ترکیب اپ+کاربر+دستگاه فرق می‌کند) |
| **iOS** | `IosDeviceInfo.identifierForVendor` | حذف همه‌ی اپ‌های همان Vendor از دستگاه |
| **Windows** | `WindowsDeviceInfo.deviceId` یا یک GUID که اپ در اولین اجرا بسازد و در local storage نگه دارد | پاک شدن داده‌ی محلی اپ |

> روی iOS از `identifierForVendor` استفاده کنید، نه IDFA — IDFA نیاز به ATT دارد و برای تشخیص نصب تکراری لازم نیست.
> روی Windows ترجیح با شناسه‌ی محدود به خود اپ است، نه شناسه‌ی سراسری ماشین.

برای اینکه پنل بفهمد دستگاه کدام سیستم‌عامل است، فیلد `device` را با یکی از این مقادیر پر کنید: `android`، `ios`، `windows`. مقادیر دیگر هم پذیرفته می‌شوند و ذخیره می‌شوند.

---

# اندپوینت ثبت دستگاه

```
POST /MobileDevice/Register
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type درخواست | `application/json` |
| CORS | `Access-Control-Allow-Origin: *` |

> مسیر بدون پیشوند `api/v1` است — روت Web API یعنی `{controller}/{action}`، دقیقاً مثل [`CreateAgentInvoice`](API-AgentInvoice.md).

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | مقدار ستون `Token` از `tbUsers` برای همان نماینده. `Bearer <token>` هم پذیرفته می‌شود |
| `Content-Type` | بله | `application/json` |

اگر کلاینتی نتواند هدر بفرستد، فیلد `AgentToken` در بدنه هم خوانده می‌شود. هدر اولویت دارد.

### بدنه‌ی درخواست — Android

```json
{
  "deviceId": "9774d56d682e549c",
  "firebaseToken": "fMEP0vJqS0...",
  "manufacturer": "Samsung",
  "model": "SM-A546E",
  "device": "android",
  "product": "a54xx",
  "hardware": "qcom",
  "osVersion": "15",
  "sdk": 35,
  "appVersion": "2.1.8",
  "versionCode": 218,
  "packageName": "com.safenet.client",
  "language": "fa",
  "country": "IR",
  "timezone": "Asia/Tehran",
  "screenWidth": 1080,
  "screenHeight": 2400,
  "density": 420,
  "notificationEnabled": true,
  "rooted": false
}
```

### بدنه‌ی درخواست — iOS

```json
{
  "deviceId": "E621E1F8-C36C-495A-93FC-0C247A3E6E5F",
  "firebaseToken": "c3RhcGxlLXRva2Vu...",
  "manufacturer": "Apple",
  "model": "iPhone 16 Pro",
  "device": "ios",
  "product": "iPhone17,1",
  "hardware": "arm64",
  "osVersion": "18.5",
  "sdk": 18,
  "appVersion": "2.1.8",
  "versionCode": 218,
  "packageName": "com.safenet.client",
  "language": "fa",
  "country": "IR",
  "timezone": "Asia/Tehran",
  "screenWidth": 1206,
  "screenHeight": 2622,
  "density": 460,
  "notificationEnabled": true,
  "rooted": false
}
```

### بدنه‌ی درخواست — Windows

```json
{
  "deviceId": "8f3a1b2c-4d5e-6f7a-8b9c-0d1e2f3a4b5c",
  "firebaseToken": null,
  "manufacturer": "Dell",
  "model": "XPS 15 9530",
  "device": "windows",
  "product": "WindowsDesktop",
  "hardware": "x64",
  "osVersion": "10.0.26100",
  "sdk": 26100,
  "appVersion": "2.1.8",
  "versionCode": 218,
  "packageName": "com.safenet.client",
  "language": "fa",
  "country": "IR",
  "timezone": "Asia/Tehran",
  "screenWidth": 1920,
  "screenHeight": 1080,
  "density": 96,
  "notificationEnabled": true,
  "rooted": false
}
```

| فیلد | نوع | الزامی | ستون | معنی مشترک | Android | iOS | Windows |
|------|------|:------:|------|-------------|---------|-----|---------|
| `deviceId` | string(64) | **بله** | `tbMu_AndroidId` | شناسه‌ی یکتای نصب | `AndroidDeviceInfo.id` | `identifierForVendor` | `WindowsDeviceInfo.deviceId` یا GUID محلی |
| `androidId` | string(64) | خیر | همان | نام قدیمی `deviceId` — اگر `deviceId` خالی باشد خوانده می‌شود | | | |
| `firebaseToken` | string(500) | خیر | `tbMu_FirebaseToken` | توکن Push | FCM | FCM یا APNs | FCM یا WNS — اگر ندارید `null` |
| `manufacturer` | string(100) | خیر | `tbMu_Manufacturer` | سازنده | `Build.MANUFACTURER` | `Apple` | برند از WMI |
| `model` | string(100) | خیر | `tbMu_Model` | مدل نمایشی | `Build.MODEL` | marketing name | مدل لپ‌تاپ / PC |
| `device` | string(100) | خیر | `tbMu_Device` | **پلتفرم** — `android` / `ios` / `windows` | `android` | `ios` | `windows` |
| `product` | string(100) | خیر | `tbMu_Product` | کد محصول داخلی | `Build.PRODUCT` | مثل `iPhone17,1` | SKU یا `WindowsDesktop` |
| `hardware` | string(100) | خیر | `tbMu_Hardware` | معماری / چیپ | `Build.HARDWARE` | `arm64` | `x64` / `arm64` |
| `osVersion` | string(20) | خیر | `tbMu_AndroidVersion` | نسخه‌ی سیستم‌عامل | `Build.VERSION.RELEASE` | `UIDevice.systemVersion` | `10.0.26100` |
| `androidVersion` | string(20) | خیر | همان | نام قدیمی `osVersion` — اگر `osVersion` خالی باشد خوانده می‌شود | | | |
| `sdk` | int | خیر | `tbMu_Sdk` | عدد نسخه‌ی API / بیلد | `SDK_INT` | major iOS | عدد بیلد ویندوز |
| `appVersion` | string(30) | خیر | `tbMu_AppVersion` | نسخه‌ی نمایشی اپ | `versionName` | `CFBundleShortVersionString` | package version |
| `versionCode` | int | خیر | `tbMu_VersionCode` | نسخه‌ی عددی اپ | `versionCode` | `CFBundleVersion` | نسخهٔ عددی بیلد |
| `packageName` | string(150) | خیر | `tbMu_PackageName` | شناسه‌ی بسته | applicationId | Bundle Identifier | Package Family Name |
| `language` | string(10) | خیر | `tbMu_Language` | زبان دستگاه | | | |
| `country` | string(10) | خیر | `tbMu_Country` | کشور دستگاه | | | |
| `timezone` | string(60) | خیر | `tbMu_Timezone` | شناسه IANA — برای ارسال نوتیفیکیشن در ساعت مناسب | | | |
| `screenWidth` | int | خیر | `tbMu_ScreenWidth` | پیکسل عرض | | | |
| `screenHeight` | int | خیر | `tbMu_ScreenHeight` | پیکسل ارتفاع | | | |
| `density` | int | خیر | `tbMu_Density` | تراکم نمایشگر | `densityDpi` | scale × ۱۶۰ | DPI ویندوز |
| `notificationEnabled` | bool | خیر | `tbMu_NotificationEnabled` | اجازه‌ی نمایش نوتیفیکیشن | | | |
| `rooted` | bool | خیر | `tbMu_Rooted` | تشخیص روت / جیلبریک — **صرفاً اطلاعاتی**. روی Windows معمولاً `false` | | | |
| `AgentToken` | string | خیر | — | جایگزین هدر `Authorization` | | | |

> رشته‌های بلندتر از طول ستون بریده می‌شوند تا ورودی نامعتبر کلاینت باعث خطای ذخیره نشود. فیلدهای ناشناخته نادیده گرفته می‌شوند. سرور مقدار `deviceId` را اعتبارسنجی پلتفرمی نمی‌کند — هر رشته‌ی غیرخالی تا ۶۴ کاراکتر قبول است.

علاوه بر این‌ها، سرور خودش این‌ها را پر می‌کند:

| ستون | مقدار |
|------|-------|
| `FK_User_ID` | نماینده‌ی صاحب توکن |
| `tbMu_RegisterDate` | زمان اولین ثبت |
| `tbMu_LastSeenDate` | زمان هر فراخوانی |
| `tbMu_LastIp` | `X-Forwarded-For` و در نبودش `UserHostAddress` |
| `tbMu_IsActive` | `1` در زمان ساخت |

---

## پاسخ موفق — `200 OK`

```json
{
  "deviceId": 128,
  "agentUsername": "darkbaz",
  "businessName": "دارک‌باز",
  "isExisting": false,
  "message": "دستگاه با موفقیت ثبت شد"
}
```

| فیلد | نوع | توضیح |
|------|------|-------|
| `deviceId` | int | شناسه‌ی رکورد `tbMobileUsers.tbMu_ID` — **با `deviceId` رشته‌ای درخواست فرق دارد** |
| `agentUsername` | string | نماینده‌ای که دستگاه زیرمجموعه‌ی اوست |
| `businessName` | string | `tbUsers.BussinesTitle` برای نمایش در برنامه |
| `isExisting` | bool | `true` یعنی این `deviceId` (رشته‌ی درخواست) قبلاً ثبت شده بود و فقط به‌روز شد |
| `message` | string | پیام فارسی قابل نمایش |

---

## Idempotent بودن

کلاینت در صورت خطا دوباره تلاش می‌کند، پس این اندپوینت **عمداً** idempotent است:

- `deviceId` ایندکس یکتا دارد (`UX_tbMobileUsers_AndroidId`) — در همه‌ی پلتفرم‌ها همین فیلد کلید است
- فراخوانی دوباره با همان `deviceId` رکورد موجود را **به‌روز** می‌کند، نه اینکه رکورد دوم بسازد
- نصب مجدد برنامه هم به همان رکورد می‌خورد و تاریخچه‌ی فاکتورهای دستگاه حفظ می‌شود، به‌شرطی که شناسه‌ی دستگاه عوض نشده باشد (جدول بالا)

### نصب با بیلد نماینده‌ی دیگر

اگر دستگاهی که قبلاً زیرمجموعه‌ی نماینده‌ی الف بود با بیلد نماینده‌ی ب ثبت شود، `FK_User_ID` به ب تغییر می‌کند و انتقال در NLog ثبت می‌شود. منطق این است که **توکن بیلد نصب‌شده** ملاک است، نه سابقه. فاکتورهای قبلی دستگاه دست‌نخورده باقی می‌مانند و همچنان به نماینده‌ی الف تعلق دارند، چون مالکیت فاکتور از `tbOrders.AccountName` می‌آید نه از `FK_User_ID` دستگاه.

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است.

| Status | پیام | علت |
|:------:|------|------|
| `400` | `اطلاعات دستگاه ارسال نشده است` | بدنه‌ی JSON ارسال نشده یا قابل پارس نیست |
| `400` | `شناسه دستگاه (DeviceId) ارسال نشده است` | هم `deviceId` و هم `androidId` خالی‌اند |
| `400` | `توکن نماینده در هدر Authorization ارسال نشده است` | نه هدر و نه `AgentToken` |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `500` | `خطا در ثبت دستگاه` | خطای غیرمنتظره؛ جزئیات در NLog |

---

# API به‌روزرسانی توکن نوتیفیکیشن

```
POST /MobileDevice/UpdateToken
```

وقتی سرویس Push توکن جدید می‌دهد (پاک کردن داده‌ی برنامه، بازیابی بکاپ، نصب مجدد) نیازی به فرستادن دوباره‌ی کل مشخصات نیست. برای هر پلتفرم همان `deviceId` ثبت‌شده را بفرستید.

```json
{
  "deviceId": "9774d56d682e549c",
  "firebaseToken": "cNEW0vJqS0...",
  "notificationEnabled": true
}
```

خروجی همان `MobileDeviceViewModel` است. اگر دستگاه هنوز ثبت نشده باشد `404` برمی‌گردد و کلاینت باید `Register` را صدا بزند.

---

## اتصال به فاکتورها

`CreateAgentInvoice` یک فیلد اختیاری `deviceId` هم می‌گیرد (`androidId` هنوز به‌عنوان نام قدیمی پذیرفته می‌شود). مقدارش باید **همان شناسه‌ای** باشد که با `Register` فرستاده شده. اگر ارسال شود و دستگاه **متعلق به همان نماینده** باشد:

- `tbOrders.FK_MobileUser_ID` و `tbDepositWallet_Log.FK_MobileUser_ID` پر می‌شوند
- `tbMu_LastSeenDate` دستگاه به‌روز می‌شود

اگر دستگاه پیدا نشود یا مال نماینده‌ی دیگری باشد، فاکتور **بدون** اتصال ساخته می‌شود و یک هشدار در NLog ثبت می‌شود — ساخت فاکتور به خاطر این شکست نمی‌خورد.

جزئیات بیشتر: [`API-AgentInvoice.md`](API-AgentInvoice.md)

---

## سمت اپلیکیشن

### Flutter

در اولین اجرای بعد از گرفتن `agentToken`، `deviceId` پایدار پلتفرم را بخوانید و `Register` را صدا بزنید. نمونه با `device_info_plus`:

```dart
import 'dart:io';
import 'package:device_info_plus/device_info_plus.dart';

Future<Map<String, dynamic>> collectDevicePayload() async {
  final plugin = DeviceInfoPlugin();
  String deviceId = '';
  String manufacturer = '';
  String model = '';
  String osVersion = '';
  String device = 'unknown';

  if (Platform.isAndroid) {
    final info = await plugin.androidInfo;
    deviceId = info.id;
    manufacturer = info.manufacturer;
    model = info.model;
    osVersion = info.version.release;
    device = 'android';
  } else if (Platform.isIOS) {
    final info = await plugin.iosInfo;
    deviceId = info.identifierForVendor ?? '';
    manufacturer = 'Apple';
    model = info.utsname.machine;
    osVersion = info.systemVersion;
    device = 'ios';
  } else if (Platform.isWindows) {
    final info = await plugin.windowsInfo;
    deviceId = info.deviceId;
    manufacturer = info.registeredOwner;
    model = info.computerName;
    osVersion = '${info.majorVersion}.${info.minorVersion}.${info.buildNumber}';
    device = 'windows';
  }

  return {
    'deviceId': deviceId,
    'manufacturer': manufacturer,
    'model': model,
    'device': device,
    'osVersion': osVersion,
    'appVersion': '2.1.8',
    'timezone': 'Asia/Tehran',
    'notificationEnabled': true,
  };
}
```

ثبت را idempotent نگه دارید: همان `deviceId` را در اجراهای بعدی دوباره بفرستید. بعد از آپدیت نسخه، `Register` را تکرار کنید تا `appVersion` پنل به‌روز بماند.

### Android (V2rayNG)

`DeviceRegistrationManager.sync()` از `MainActivity.onCreate()` صدا زده می‌شود و هنوز `androidId` می‌فرستد — سرور آن را به‌عنوان نام قدیمی `deviceId` می‌خواند.

1. اگر بیلد `AGENT_API_TOKEN` نداشته باشد کاری نمی‌کند. در بیلد عمومی این مقدار وجود ندارد و باید بعد از وارد کردن اشتراک، از [`GET /api/v1/Sub/Agent`](API-SubAgent.md) گرفته و ذخیره شود — تا آن لحظه `sync()` صبر می‌کند.
2. اگر `versionCode` فعلی قبلاً ثبت شده باشد، برمی‌گردد. یعنی هر بار اجرا فراخوانی نمی‌شود، ولی بعد از **آپدیت برنامه** دوباره ثبت می‌کند تا ستون `appVersion` پنل کهنه نماند.
3. در غیر این صورت یک `OneTimeWorkRequest` یکتا با `NetworkType.CONNECTED` و backoff نمایی (شروع از ۳۰ ثانیه) صف می‌شود.
4. Worker در صورت شکست `Result.retry()` برمی‌گرداند. چون WorkManager درخواست را روی دیسک نگه می‌دارد، گوشی‌ای که آفلاین نصب شده هم اولین باری که اینترنت بگیرد ثبت می‌شود.

بعد از پاسخ مثبت به مجوز نوتیفیکیشن هم یک بار دیگر `sync()` صدا زده می‌شود تا پنل بداند اجازه داده شده است.

#### Firebase

این بیلد SDK فایربیس ندارد، پس `firebaseToken` تهی فرستاده می‌شود و پنل دستگاه را «غیرقابل ارسال Push» ثبت می‌کند. برای فعال کردن:

1. وابستگی `firebase-messaging` و فایل `google-services.json` را اضافه کنید.
2. در `FirebaseMessagingService.onNewToken` این را صدا بزنید:

```kotlin
DeviceRegistrationManager.onFirebaseTokenChanged(context, token)
```

این متد توکن را در MMKV با کلید `DeviceInfoCollector.PREF_FIREBASE_TOKEN` ذخیره و ثبت را دوباره صف می‌کند. هیچ تغییر دیگری لازم نیست.

---

## نمونه فراخوانی

### cURL — Android

```bash
curl -X POST "https://panel.example.com/MobileDevice/Register" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"deviceId\":\"9774d56d682e549c\",\"manufacturer\":\"Samsung\",\"model\":\"SM-A546E\",\"device\":\"android\",\"osVersion\":\"15\",\"sdk\":35,\"appVersion\":\"2.1.8\",\"versionCode\":218,\"timezone\":\"Asia/Tehran\",\"notificationEnabled\":true}"
```

### cURL — iOS

```bash
curl -X POST "https://panel.example.com/MobileDevice/Register" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"deviceId\":\"E621E1F8-C36C-495A-93FC-0C247A3E6E5F\",\"manufacturer\":\"Apple\",\"model\":\"iPhone 16 Pro\",\"device\":\"ios\",\"osVersion\":\"18.5\",\"packageName\":\"com.safenet.client\",\"appVersion\":\"2.1.8\",\"versionCode\":218,\"timezone\":\"Asia/Tehran\",\"notificationEnabled\":true}"
```

### cURL — Windows

```bash
curl -X POST "https://panel.example.com/MobileDevice/Register" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"deviceId\":\"8f3a1b2c-4d5e-6f7a-8b9c-0d1e2f3a4b5c\",\"manufacturer\":\"Dell\",\"model\":\"XPS 15 9530\",\"device\":\"windows\",\"osVersion\":\"10.0.26100\",\"packageName\":\"com.safenet.client\",\"appVersion\":\"2.1.8\",\"versionCode\":218,\"timezone\":\"Asia/Tehran\",\"notificationEnabled\":true}"
```

### Dart (Flutter)

```dart
final payload = await collectDevicePayload();
final response = await http.post(
  Uri.parse('$baseUrl/MobileDevice/Register'),
  headers: {
    'Authorization': agentToken,
    'Content-Type': 'application/json',
  },
  body: jsonEncode(payload),
);
```

---

## بخش پنل

**کاربران موبایل** (`App/MobileUsers`) — فعلاً فقط برای ادمین (`Role = 1`). دستگاه‌های iOS و Windows هم در همین فهرست دیده می‌شوند.

| ستون | منبع |
|------|------|
| دستگاه | `tbMu_Manufacturer` + `tbMu_Model` |
| نماینده | `tbUsers.Username` |
| نسخه برنامه | `tbMu_AppVersion` |
| اندروید | `tbMu_AndroidVersion` — در عمل نسخه‌ی سیستم‌عامل است (اندروید / iOS / Windows) |
| آخرین بازدید | `tbMu_LastSeenDate` |
| نوتیفیکیشن | «آماده» یعنی هم توکن Push دارد هم اجازه‌ی نمایش |
| فاکتورها | تعداد رکوردهای `tbDepositWallet_Log` متصل |
| وضعیت | `tbMu_IsActive` |

با کلیک روی نام دستگاه، مودال جزئیات کامل باز می‌شود (مشخصات سخت‌افزار، محلی‌سازی، نمایشگر، آی‌پی، مجموع پرداختی). فیلد `device` در جزئیات نشان می‌دهد پلتفرم `android`، `ios` یا `windows` بوده.

دکمه‌ی فعال/غیرفعال `tbMu_IsActive` را عوض می‌کند. دستگاه غیرفعال باید از فهرست گیرندگان Push کنار گذاشته شود ولی رکورد و تاریخچه‌اش پاک نمی‌شود.

---

## ملاحظات امنیتی

- روی **Android** ۸ به بعد `ANDROID_ID` به‌ازای هر ترکیب اپ و کاربر و دستگاه متفاوت است. روی **iOS** مقدار `identifierForVendor` با حذف همه‌ی اپ‌های Vendor عوض می‌شود. روی **Windows** پایداری شناسه به این بستگی دارد که GUID را کجا ذخیره کنید. در هر سه حالت هدف تشخیص نصب تکراری **همین برنامه** است، نه ردیابی بین‌اپی.
- توکن نماینده کلید دسترسی است و در خروجی برنمی‌گردد. هرکس توکن یک نماینده را داشته باشد می‌تواند دستگاه جعلی ثبت کند؛ همان محدودیتی که بقیه‌ی اندپوینت‌های `/User/*` هم دارند.
- روی این کنترلر عمداً `[LogActionFilter]` گذاشته **نشده** تا توکن Push و شناسه‌ی دستگاه در جدول لاگ ذخیره نشود.
- هیچ محدودیت نرخی وجود ندارد. چون ثبت idempotent است، تکرار درخواست رکورد اضافه نمی‌سازد؛ ولی برای `deviceId`های ساختگی جدول رشد می‌کند. اگر در معرض اینترنت است، محدودیت نرخ در سطح IIS یا reverse proxy اضافه کنید.
- `rooted` را مبنای هیچ تصمیم امنیتی قرار ندهید؛ روی اندروید با Magisk و روی iOS با جیلبریک دور زده می‌شود.
- این اندپوینت را روی `HTTPS` سرو کنید.

---

## پیش‌نیاز دیتابیس

اجرای [`Database/AddMobileUsersTable.sql`](../Database/AddMobileUsersTable.sql) که این‌ها را می‌سازد:

- جدول `tbMobileUsers` + کلید خارجی به `tbUsers`
- ایندکس یکتای `UX_tbMobileUsers_AndroidId` (کلید یکتا برای همه‌ی پلتفرم‌ها؛ ستون دیتابیس هنوز `tbMu_AndroidId` است)
- ستون‌های `FK_MobileUser_ID` روی `tbOrders` و `tbDepositWallet_Log` + کلیدهای خارجی
- رکورد `tbPaymentMethods` با `tbpm_ID = 5` و `tbpm_Key = 'APP'`

سپس [`Database/UpgradePanelChangelog_1.9.1_1003.sql`](../Database/UpgradePanelChangelog_1.9.1_1003.sql) برای ثبت نسخه در تغییرات پنل.

> بعد از اجرا App Pool را recycle کنید. تغییر اسکیما برای `deviceId` لازم نیست؛ API به همان ستون موجود می‌نویسد.
