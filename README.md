# V2boardApi

API و پنل مدیریت متصل به [V2board](https://github.com/v2board/v2board) برای فروش اشتراک V2Ray. شامل پنل وب (Razor)، ربات تلگرام، مدیریت نمایندگان و درگاه‌های پرداخت.

> پنل React در این مخزن وجود ندارد و احتمالاً پروژه جداگانه‌ای است.

## امکانات

- سلسله‌مراتب نمایندگان (ادمین، نماینده ارشد، نماینده)
- مدیریت اشتراک V2board (ایجاد، تمدید، ریست، مسدودسازی)
- فروش و پشتیبانی از طریق ربات تلگرام
- درگاه‌های پرداخت: زرین‌پال، تتراپی، پلیسیو، هاب‌اسمارت، کارت‌به‌کارت
- تحویل کانفیگ به کلاینت‌های VPN از `/api/v1/client/subscribe`
- کیف پول، فاکتور، لینک پرداخت و داشبورد فروش
- تمدید خودکار و هشدار اتمام حجم/زمان

## معماری

```
پنل Razor + Web API + ربات تلگرام
              ↓
    سرویس‌ها (Auth, Bot, Timer, Payment)
              ↓
    SQL Server (داده اپ)  +  MySQL (پنل V2board)
```

## تکنولوژی‌ها

| مورد | نسخه |
|------|------|
| .NET Framework | 4.8 |
| ASP.NET MVC / Web API | 5.2.9 |
| Entity Framework | 6.4.4 |
| MySqlConnector | 2.5.0 |
| Telegram.Bot | 22.9.0 |
| NLog | 5.3.2 |
| فرانت‌اند | Bootstrap 5, jQuery, قالب Vuexy (RTL) |
| میزبانی | IIS |

## پیش‌نیازها

- Windows + IIS + .NET Framework 4.8
- SQL Server
- دسترسی به MySQL پنل V2board
- توکن ربات تلگرام و (در صورت نیاز) کلید درگاه پرداخت
- Visual Studio 2022 (برای توسعه)

## نصب و اجرا

```bash
git clone <repository-url>
cd V2boardApi
nuget restore V2boardApi.sln
msbuild V2boardApi.sln /p:Configuration=Release
```

1. `Web.config` را تنظیم کنید
2. دیتابیس SQL Server را مطابق `Model.edmx` بسازید
3. یک رکورد `tbServers` با اطلاعات MySQL پنل V2board اضافه کنید
4. کاربر ادمین با `Role = 1` در `tbUsers` بسازید
5. پروژه را در IIS منتشر کنید

**توسعه:** باز کردن `V2boardApi.sln` در VS2022 و F5 (IIS Express)

## تنظیمات (`Web.config`)

| کلید | توضیح |
|------|-------|
| `JwtSecretKey` | کلید امضای JWT — **قبل از استقرار تغییر دهید** |
| `GeminiApiKey` | کلید Gemini (اختیاری، فعلاً استفاده نمی‌شود) |
| `Entities` | اتصال SQL Server برای EF6 |

**احراز هویت پنل:** `App/Admin/Login` — Forms Auth + کوکی JWT و Role

## APIهای اصلی

### Web API — `/{controller}/{action}`

| متد | مسیر | توضیح |
|-----|------|-------|
| POST | `/User/LoginAdmin` | لاگین اپ موبایل |
| GET | `/User/CheckOrder` | تأیید پرداخت کارت‌به‌کارت |
| GET | `/User/VerifyPayZarinPal` | کال‌بک زرین‌پال |
| GET | `/User/VerifyPay` | کال‌بک هاب‌اسمارت |
| POST | `/User/VerifyTetraPay` | وب‌هوک تتراپی |
| POST | `/User/VerifyPlisio` | وب‌هوک پلیسیو |
| POST | `/Bot/Update?botName=` | وب‌هوک ربات تلگرام |
| GET | `/MobileApp/GetSubscriptionInfo` | اطلاعات اشتراک (هدر Authorization) |

### کلاینت VPN — `api/v1/client/{action}?token=`

`subscribe` · `android` · `ios` · `windows` · `linux`

### پنل مدیریت — `App/{controller}/{action}`

نیاز به لاگین و مجوز نقش (`AuthorizeApp`)

## ساختار پروژه

```
V2boardApi/
├── DataLayer/          # مدل EF6 و Repository
├── V2boardApi/
│   ├── Areas/App/      # پنل مدیریت (MVC)
│   ├── Areas/api/      # Web API و ویوهای کلاینت
│   ├── Tools/          # Auth, Bot, Timer, Payment
│   ├── PaymentMethods/
│   └── assets/         # فایل‌های استاتیک
└── V2boardApi.sln
```

## نکات امنیتی

> **هشدار:** کلیدها و رمز دیتابیس در `Web.config` هاردکد شده‌اند — قبل از استقرار حتماً تغییر دهید.

- `debug="false"` در پروداکشن
- HTTPS و کوکی `Secure=true`
- محدود کردن CORS (حذف `*`)
- رمز عبور با SHA256 بدون salt ذخیره می‌شود — نیاز به ارتقا دارد
- برخی کوئری‌های MySQL با الحاق رشته نوشته شده‌اند — ریسک SQL Injection
- کال‌بک‌های پرداخت بدون احراز هویت هستند — نیاز به تأیید امضای درگاه

## استقرار

1. Build در حالت Release
2. Publish روی IIS (.NET CLR v4.0)
3. SSL فعال
4. وب‌هوک تلگرام: `https://your-domain/Bot/Update/?botName={username}`

## عیب‌یابی

| مشکل | راه‌حل |
|------|--------|
| لاگین نمی‌شود | اتصال SQL و وجود کاربر در `tbUsers` |
| ربات پاسخ نمی‌دهد | توکن و وب‌هوک — StartBot از پنل |
| اشتراک خالی | تنظیمات MySQL در `tbServers` |
| پرداخت ثبت نمی‌شود | دسترسی کال‌بک درگاه به سرور |

## بهبودهای پیشنهادی

- انتقال secrets به تنظیمات امن
- تأیید امضای درگاه‌های پرداخت
- پارامتری‌سازی کوئری‌های MySQL
- هش رمز عبور با bcrypt/Argon2
- مستندسازی Swagger و افزودن تست
