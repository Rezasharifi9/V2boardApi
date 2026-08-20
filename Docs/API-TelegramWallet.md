# API موجودی کیف پول ربات تلگرام (Telegram Wallet)

نمایش موجودی کیف پول ربات تلگرام صاحب یک اشتراک در اپلیکیشن — همان عددی که مشتری در ربات با «موجودی کیف پولت» می‌بیند (`tbTelegramUsers.Tel_Wallet`).

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `GetTelegramWallet`
- مدل ورودی: `V2boardApi/Areas/api/Data/ApiModels/GetTelegramWalletModel.cs`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/TelegramWalletViewModel.cs`

> پرداخت از همین کیف پول با [`CheckAgentInvoice`](API-AgentInvoice.md#پرداخت-از-کیف-پول-payfromwallet) و فیلد `PayFromWallet` انجام می‌شود. این اندپوینت فقط **خواندن** موجودی است و چیزی کسر نمی‌کند.

---

## جای این اندپوینت در جریان کار

```
GET /api/v1/Sub/Agent?token={subToken}
                │
                ▼
        agentToken
                │
        ┌───────┴────────────────────────┐
        ▼                                ▼
GET /User/GetAgentPlans      POST /User/GetTelegramWallet
        │                                │
        │                         balance + hasWallet
        │                                │
        └────────────┬───────────────────┘
                     ▼
        POST /User/CreateAgentInvoice
                     │
                     ▼
        POST /User/CheckAgentInvoice
             PayFromWallet: true   ← اگر hasWallet و موجودی کافی باشد
```

کلاینت `GetTelegramWallet` را بعد از وارد کردن اشتراک صدا می‌زند. اگر `hasWallet` برابر `false` باشد دکمه «پرداخت از کیف پول» نباید نمایش داده شود.

---

## اندپوینت

```
POST /User/GetTelegramWallet
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type درخواست | `application/json` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` |

> مسیر بدون پیشوند `api/v1` است — روت Web API یعنی `{controller}/{action}`، دقیقاً مثل [`GetAgentPlans`](API-AgentPlans.md). `POST` انتخاب شده تا توکن ساب در کوئری‌استرینگ و لاگ IIS ثبت نشود.

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | همان `agentToken` که [`Sub/Agent`](API-SubAgent.md) برگردانده. `Bearer <token>` هم پذیرفته می‌شود |
| `Content-Type` | بله | `application/json` |

### بدنه‌ی درخواست

```json
{ "SubscriptionToken": "8f3a1b2c4d5e6f7a8b9c" }
```

| فیلد | نوع | الزامی | توضیح |
|------|------|:------:|-------|
| `SubscriptionToken` | string | بله | توکن لینک ساب مشتری (`tbLinks.tbL_Token`) — همان مقداری که در `https://{SubAddress}/api/v1/client/subscribe?token=` دیده می‌شود |

جست‌وجو روی `tbLinks.tbL_Token` انجام می‌شود و **علاوه بر آن** شرط می‌گذارد که `tbL_Email` به `@{AgentUsername}` ختم شود؛ بنابراین یک نماینده نمی‌تواند موجودی مشتری نماینده‌ی دیگری را ببیند، حتی اگر توکن ساب آن را داشته باشد.

---

## پاسخ موفق — `200 OK`

پاسخ **همیشه** `200` است اگر اشتراک پیدا شود. وصل نبودن حساب تلگرام خطا نیست، یک وضعیت است — کلاینت باید به `hasWallet` نگاه کند.

### اشتراک متصل به ربات تلگرام

```json
{
  "balance": 150000,
  "hasWallet": true,
  "subscriptionName": "ali12",
  "message": "موجودی کیف پول ربات"
}
```

### اشتراک بدون حساب تلگرام

اشتراک‌هایی که از داخل اپلیکیشن (بدون ربات) ساخته شده‌اند معمولاً `FK_TelegramUserID` ندارند:

```json
{
  "balance": 0,
  "hasWallet": false,
  "subscriptionName": "a1b2c3d4",
  "message": "این اشتراک به حساب ربات تلگرام متصل نیست"
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|------|-------|
| `balance` | long | `tbTelegramUsers.Tel_Wallet` | موجودی به **تومان**. اگر حساب متصل نباشد `0` |
| `hasWallet` | bool | وجود `tbLinks.FK_TelegramUserID` متعلق به همین نماینده | **تنها فیلدی که کلاینت باید برای نمایش دکمه‌ی پرداخت از کیف پول به آن نگاه کند** |
| `subscriptionName` | string | `tbLinks.tbL_Email` | نام اشتراک بدون بخش‌های `$` و `@` |
| `message` | string | — | پیام فارسی قابل نمایش |

> `balance` همان واحدی است که ربات نشان می‌دهد (تومان). مبلغ فاکتور در `CreateAgentInvoice.amount` **ریال** است. برای مقایسه‌ی «آیا موجودی کافی است؟» از قیمت تومانی تعرفه استفاده کنید — یعنی `planPrice` بعد از تخفیف، نه `amount` ریالی. خودِ `CheckAgentInvoice` با `PayFromWallet` موجودی را دوباره سمت سرور چک می‌کند.

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است:

```json
"اشتراکی با این توکن برای این نماینده یافت نشد"
```

| Status | پیام | علت |
|:------:|------|------|
| `400` | `توکن اشتراک ارسال نشده است` | بدنه ارسال نشده یا `SubscriptionToken` خالی است |
| `400` | `توکن در هدر Authorization ارسال نشده است` | هدر `Authorization` وجود ندارد یا خالی است |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `404` | `اشتراکی با این توکن برای این نماینده یافت نشد` | توکن در `tbLinks` نیست، یا مال نماینده‌ی دیگری است |
| `500` | `خطا در دریافت موجودی کیف پول` | خطای غیرمنتظره؛ جزئیات در NLog |

---

## منطق تشخیص

۱. توکن نماینده از هدر خوانده می‌شود (`GetAgentTokenFromHeader`).
۲. اشتراک با `tbL_Token` و پسوند `@{AgentUsername}` پیدا می‌شود — همان `FindAgentSubscriptionAsync` که `CreateAgentInvoice` هم استفاده می‌کند.
۳. اگر `tbLinks.FK_TelegramUserID` تهی باشد، یا کاربر تلگرام مال نماینده‌ی دیگری باشد، `hasWallet = false`.
۴. در غیر این صورت `Tel_Wallet` برگردانده می‌شود (`null` یعنی `0`).

این کیف پول **کیف پول نماینده نیست** (`tbUsers.Wallet` بدهی نماینده است). کیف پول مشتری در ربات است.

---

## نمونه فراخوانی

### cURL

```bash
curl -X POST "https://panel.example.com/User/GetTelegramWallet" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"SubscriptionToken\":\"8f3a1b2c4d5e6f7a8b9c\"}"
```

### JavaScript

```javascript
const res = await fetch('/User/GetTelegramWallet', {
  method: 'POST',
  headers: { 'Authorization': agentToken, 'Content-Type': 'application/json' },
  body: JSON.stringify({ SubscriptionToken: subToken })
});

if (res.ok) {
  const w = await res.json();
  if (w.hasWallet) {
    showWalletPayButton(w.balance);
  }
}
```

### Kotlin

```kotlin
val wallet = HttpUtil.postJson<TelegramWallet>(
    url = AgentApi.url("/User/GetTelegramWallet"),
    jsonBody = JsonUtil.toJson(mapOf("SubscriptionToken" to subToken)),
    headers = AgentApi.authHeaders(agentToken)
)

if (wallet.hasWallet) {
    bindWalletBalance(wallet.balance)
} else {
    hideWalletPayButton()
}
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);

    var body = new StringContent(
        JsonConvert.SerializeObject(new { SubscriptionToken = subToken }),
        Encoding.UTF8,
        "application/json");

    var res = await http.PostAsync("https://panel.example.com/User/GetTelegramWallet", body);
    var json = await res.Content.ReadAsStringAsync();

    if (res.IsSuccessStatusCode)
    {
        var wallet = JsonConvert.DeserializeObject<TelegramWalletViewModel>(json);
    }
}
```

---

## ملاحظات امنیتی

- توکن نماینده و توکن ساب هر دو لازم‌اند. بدون توکن نماینده موجودی برنمی‌گردد؛ بدون تعلق اشتراک به همان نماینده هم `404` است.
- خروجی فقط موجودی همان اشتراک را می‌دهد، نه لیست مشتریان یا کیف پول نماینده.
- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است.
- توکن ساب را در کوئری‌استرینگ نفرستید — به همین دلیل این اندپوینت `POST` است.
- این اندپوینت را روی `HTTPS` سرو کنید.
