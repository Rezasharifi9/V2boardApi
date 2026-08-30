# API سوالات متداول ربات (FAQ)

همان پرسش‌وپاسخ دکمه **«❓ سؤالات رایج»** ربات تلگرام، برای نمایش در اپلیکیشن.

متن از یک منبع مشترک می‌آید (`FaqViewModel`) تا ربات و API از هم جدا نشوند.

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `GetFaq`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/FaqViewModel.cs`
- منبع ربات: `BotController` شاخه `mess == "❓ سؤالات رایج"`

> آیدی پشتیبانی در [`GET /api/v1/Sub/Agent`](API-SubAgent.md) هم با فیلد `supportUsername` برمی‌گردد. این اندپوینت همان مقدار را کنار خود سوالات می‌گذارد تا صفحه FAQ با یک فراخوانی کامل شود. لینک‌های روبیکا/بله/ایتا/تلگرام ثبت‌شده در پنل را از [`GET /User/GetSupportLinks`](API-SupportLinks.md) بگیر. نسخه و لینک دانلود اپلیکیشن را از [`GET /User/GetAppRelease`](API-AppRelease.md) بگیر.

---

## اندپوینت

```
GET /User/GetFaq
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` (از `[EnableCors]` روی کنترلر) |

> مسیر بدون پیشوند `api/v1` است. `UserController` یک `ApiController` است و روی روت Web API یعنی `{controller}/{action}` می‌نشیند — دقیقاً مثل [`GetAgentPlans`](API-AgentPlans.md).

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
  "title": "❓ سؤالات رایج در خصوص سرویس ها ❓",
  "items": [
    {
      "question": "🔹 آیا اشتراک من ثابت است و می‌توانم آی‌پی را تغییر دهم؟",
      "answer": "بله، اشتراک ها به صورت ثابت (استاتیک) ارائه می‌شود."
    },
    {
      "question": "🔹 آیا می‌توانم با چند دستگاه به یک اشتراک متصل شوم؟",
      "answer": "بله، اشتراک ما به شما اجازه می‌دهد که بدون محدودیت کاربری، به چندین دستگاه به طور همزمان متصل شوید."
    },
    {
      "question": "🔹 آیا می‌توانم موقعیت سرورم را تغییر دهم؟",
      "answer": "بله، شما می‌توانید به راحتی از طریق لیست سرورهای موجود در اشتراک ، سرور مورد نظر خود را انتخاب کنید"
    },
    {
      "question": "🔹 آیا حجم باقی مانده یا زمان باقی مانده به دوره بعد انتقال می یابد؟",
      "answer": "خیر، حجم یا زمان باقی مانده شما به دوره بعد انتقال نمی یابد و باید در دوره خریداری شده مصرف شود !!"
    },
    {
      "question": "🔹 آیا قبل از اتمام زمان یا حجم , بسته جدید تمدید کنم بسته قبلی از بین میرود ؟",
      "answer": "خیر، اگر حجم یا زمان داشته باشید بسته جدید رزرو خواهد شد و بعد از پایان بسته فعلی جایگزین خواهد شد !!"
    }
  ],
  "footer": "💬 اگر سوالی داشتید که پاسخ آن را نیافتید با پشتیبانی در ارتباط باشید.",
  "supportUsername": "SafeNetSupport"
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|:----:|-------|
| `title` | string | متن ربات | عنوان صفحه FAQ |
| `items` | array | متن ربات | لیست پرسش و پاسخ؛ ترتیب همان ترتیب ربات است |
| `items[].question` | string | متن ربات | سوال، با همان پیشوند `🔹` |
| `items[].answer` | string | متن ربات | پاسخ؛ تگ HTML ندارد — برخلاف پیام تلگرام که سوال را `<b>` می‌کند |
| `footer` | string | متن ربات | جمله پایانی قبل از آیدی پشتیبانی |
| `supportUsername` | string \| null | `tbBotSettings.AdminUsername` | آیدی پشتیبانی نماینده، **بدون** `@` |

اگر نماینده تنظیمات ربات نداشته باشد یا `AdminUsername` خالی باشد، `supportUsername` برابر `null` است. در این حالت کلاینت نباید لینک تلگرام پشتیبانی بسازد — همان رفتاری که برای [`Sub/Agent`](API-SubAgent.md) توصیه شده.

`@` ابتدای مقدار حذف می‌شود تا کلاینت خودش تصمیم بگیرد چطور نمایش دهد یا لینک `https://t.me/{username}` بسازد.

### نمایش در کلاینت

ربات بعد از `footer` این خط را هم می‌فرستد:

```
🆔 @{supportUsername}
```

این API آن خط را جدا برمی‌گرداند تا اپ بتواند دکمه/لینک پشتیبانی بسازد، نه متن ثابت. اگر `supportUsername` تهی است آن ردیف را نشان ندهید.

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
| `500` | `خطا در دریافت سوالات متداول` | خطای غیرمنتظره؛ جزئیات در NLog |

---

## نمونه فراخوانی

### cURL

```bash
curl "https://panel.example.com/User/GetFaq" -H "Authorization: 9f2c1ab7d4e8"
```

### JavaScript

```javascript
const res = await fetch('/User/GetFaq', {
  headers: { 'Authorization': agentToken }
});

if (res.ok) {
  const faq = await res.json();
  renderFaq(faq.title, faq.items, faq.footer);
  if (faq.supportUsername) {
    showSupportLink('https://t.me/' + faq.supportUsername);
  }
}
```

### Kotlin

```kotlin
val faq = HttpUtil.get<FaqViewModel>(
    url = AgentApi.url("/User/GetFaq"),
    headers = AgentApi.authHeaders(agentToken)
)

bindFaq(faq.title, faq.items, faq.footer)
if (!faq.supportUsername.isNullOrBlank()) {
    bindSupport("https://t.me/${faq.supportUsername}")
}
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);
    var json = await http.GetStringAsync("https://panel.example.com/User/GetFaq");
    var faq = JsonConvert.DeserializeObject<FaqViewModel>(json);
}
```

---

## ملاحظات

- سوالات در دیتابیس نیستند؛ متن ثابت است و با تغییر `FaqViewModel.DefaultItems` هم در ربات و هم در این API عوض می‌شود.
- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است — مثل بقیه‌ی `/User/*`.
- خروجی هیچ کانفیگ، توکن ساب یا اطلاعات مشتری برنمی‌گرداند.
- این اندپوینت را روی `HTTPS` سرو کنید.
