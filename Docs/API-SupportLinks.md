# API لینک‌های ارتباطی پشتیبانی نماینده

لیست راه‌های ارتباط با پشتیبانی همان نماینده — روبیکا، بله، ایتا، تلگرام و غیره — برای نمایش در اپلیکیشن.

نماینده این لیست را در پنل، تب **تنظیمات ← پشتیبانی** وارد می‌کند.

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `GetSupportLinks`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/SupportLinkViewModel.cs`
- جدول: `dbo.tbSupportLinks` — اسکریپت ساخت: [`Database/AddSupportLinksTable.sql`](../Database/AddSupportLinksTable.sql)

> آیدی تلگرام پشتیبانی در [`GET /User/GetFaq`](API-Faq.md) با فیلد `supportUsername` هم برمی‌گردد. این اندپوینت لینک‌های ثبت‌شده در پنل را جدا می‌دهد تا صفحه «ارتباط با پشتیبانی» ساخته شود. نسخه و لینک دانلود اپلیکیشن را از [`GET /User/GetAppRelease`](API-AppRelease.md) بگیر.

---

## اندپوینت

```
GET /User/GetSupportLinks
```

| مورد | مقدار |
|------|-------|
| متد | `GET` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` (از `[EnableCors]` روی کنترلر) |

> مسیر بدون پیشوند `api/v1` است. `UserController` یک `ApiController` است و روی روت Web API یعنی `{controller}/{action}` می‌نشیند — دقیقاً مثل [`GetFaq`](API-Faq.md) و [`GetAgentPlans`](API-AgentPlans.md).

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
  "items": [
    {
      "id": 1,
      "title": "تلگرام",
      "link": "https://t.me/SafeNetSupport",
      "phone": "09121234567"
    },
    {
      "id": 2,
      "title": "روبیکا",
      "link": "https://rubika.ir/safenet",
      "phone": null
    },
    {
      "id": 3,
      "title": "بله",
      "link": null,
      "phone": "02191000000"
    }
  ]
}
```

اگر نماینده هیچ لینکی ثبت نکرده باشد، `items` آرایه خالی است:

```json
{
  "items": []
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|:----:|-------|
| `items` | array | `tbSupportLinks` | لیست راه‌های ارتباطی؛ ترتیب ثبت |
| `items[].id` | int | `tbSl_ID` | شناسه رکورد |
| `items[].title` | string | `tbSl_Title` | عنوان نمایشی — مثلاً روبیکا، بله، ایتا، تلگرام |
| `items[].link` | string \| null | `tbSl_Link` | لینک پشتیبانی. اگر خالی ثبت شده باشد `null` است |
| `items[].phone` | string \| null | `tbSl_Phone` | شماره تلفن. اگر خالی ثبت شده باشد `null` است |

حداقل یکی از `link` یا `phone` در پنل اجباری است؛ کلاینت باید فیلد تهی را نشان ندهد.

### نمایش در کلاینت

- اگر `link` مقدار دارد، دکمه/ردیف را به همان URL باز کنید.
- اگر `phone` مقدار دارد، دکمه تماس با `tel:{phone}` بسازید.
- هر دو می‌توانند همزمان پر باشند — هر دو کنترل را نشان دهید.
- اگر `items` خالی است، صفحه پشتیبانی را خالی نگذارید؛ پیام «راه‌های ارتباطی ثبت نشده» کافی است. آیدی تلگرام ربات را در صورت نیاز از [`GetFaq`](API-Faq.md) بگیرید.

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
| `500` | `خطا در دریافت لینک های ارتباطی` | خطای غیرمنتظره؛ جزئیات در NLog |

---

## نمونه فراخوانی

### cURL

```bash
curl "https://panel.example.com/User/GetSupportLinks" -H "Authorization: 9f2c1ab7d4e8"
```

### JavaScript

```javascript
const res = await fetch('/User/GetSupportLinks', {
  headers: { 'Authorization': agentToken }
});

if (res.ok) {
  const data = await res.json();
  renderSupport(data.items);
}
```

### Kotlin

```kotlin
val support = HttpUtil.get<SupportLinkListViewModel>(
    url = AgentApi.url("/User/GetSupportLinks"),
    headers = AgentApi.authHeaders(agentToken)
)

bindSupportLinks(support.items)
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);
    var json = await http.GetStringAsync("https://panel.example.com/User/GetSupportLinks");
    var support = JsonConvert.DeserializeObject<SupportLinkListViewModel>(json);
}
```

---

## ملاحظات

- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است — مثل بقیه‌ی `/User/*`.
- خروجی هیچ کانفیگ، توکن ساب یا اطلاعات مشتری برنمی‌گرداند.
- این اندپوینت را روی `HTTPS` سرو کنید.
