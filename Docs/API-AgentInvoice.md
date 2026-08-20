# APIهای فاکتور پرداخت مستقیم نماینده (Agent Invoice)

اندپوینت‌های پرداخت مشتری نماینده از بیرون ربات — معادل **💳 پرداخت مستقیم** و **پرداخت از کیف پول** در ربات تلگرام.

| اندپوینت | کار |
|----------|-----|
| [`POST /User/CreateAgentInvoice`](#اندپوینت) | ساخت فاکتور و برگرداندن مبلغ و شماره کارت |
| [`POST /User/UploadAgentInvoiceReceipt`](#api-آپلود-رسید-فاکتور-upload-agent-invoice-receipt) | آپلود عکس رسید و ارسال آن به ادمین ربات تلگرام |
| [`POST /User/CheckAgentInvoice`](#api-بررسی-وضعیت-فاکتور-check-agent-invoice) | بررسی تائید شدن فاکتور، جزئیات اشتراک بعد از تائید، یا پرداخت از کیف پول |
| [`POST /User/GetTelegramWallet`](API-TelegramWallet.md) | موجودی کیف پول ربات تلگرام صاحب اشتراک |

> هر چهار تا با **توکن نماینده** کار می‌کنند. اگر کلاینت فقط توکن ساب مشتری را دارد، اول [`GET /api/v1/Sub/Agent`](API-SubAgent.md) را صدا بزند تا `agentToken` را بگیرد.

---

# API ساخت فاکتور (Create Agent Invoice)

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `CreateAgentInvoice`
- مدل ورودی: `V2boardApi/Areas/api/Data/ApiModels/CreateAgentInvoiceModel.cs`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/AgentInvoiceViewModel.cs`
- مرجع رفتاری در ربات: `Areas/api/Controllers/BotController.cs` → شاخه‌ی `callback[0] == "ConfirmPay"`

> این API فاکتور می‌سازد. تائید پرداخت از سه راه است: آپلود رسید و تائید ادمین در ربات ([`UploadAgentInvoiceReceipt`](#api-آپلود-رسید-فاکتور-upload-agent-invoice-receipt))، **صفحه‌ی فاکتورهای پنل**، یا [`CheckAgentInvoice`](#پرداخت-از-کیف-پول-payfromwallet) با `PayFromWallet: true`.

---

## اندپوینت

```
POST /User/CreateAgentInvoice
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type درخواست | `application/json` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` (از `[EnableCors]` روی کنترلر) |

> مسیر بدون پیشوند `api/v1` است. `UserController` یک `ApiController` است و روی روت Web API یعنی `{controller}/{action}` می‌نشیند — دقیقاً مثل [`GetAgentPlans`](API-AgentPlans.md).

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | مقدار ستون `Token` از جدول `tbUsers` برای همان نماینده — یا همان `agentToken` که [`Sub/Agent`](API-SubAgent.md) برگردانده |
| `Content-Type` | بله | `application/json` |

هر دو فرم `Authorization: <token>` و `Authorization: Bearer <token>` پذیرفته می‌شوند (منطق مشترک `GetAgentTokenFromHeader`).

### بدنه‌ی درخواست

```json
{
  "PlanId": 154,
  "SubscriptionToken": "",
  "DeviceId": "9774d56d682e549c"
}
```

| فیلد | نوع | الزامی | توضیح |
|------|------|:------:|-------|
| `PlanId` | int | بله | شناسه‌ی **رکورد لینک نماینده به تعرفه** یعنی `tbLinkUserAndPlans.Link_PU_ID` — نه `tbPlans.Plan_ID`. همان `PlanId` که [`GetAgentPlans`](API-AgentPlans.md) برمی‌گرداند |
| `SubscriptionToken` | string | خیر | توکن اشتراکی که باید **تمدید** شود (`tbLinks.tbL_Token`). خالی یا ارسال‌نشده یعنی یک **اشتراک جدید** ساخته شود |
| `DeviceId` | string | خیر | شناسه‌ی دستگاهی که فاکتور را می‌سازد، همان `deviceId` که با [`MobileDevice/Register`](API-MobileDevice.md) ثبت شده. نام قدیمی `AndroidId` هنوز پذیرفته می‌شود. فاکتور و سفارش به رکورد دستگاه در `tbMobileUsers` وصل می‌شوند |

#### رفتار `DeviceId`

| حالت | نتیجه |
|------|-------|
| ارسال نشده یا خالی | فاکتور بدون اتصال به دستگاه ساخته می‌شود |
| دستگاه ثبت‌شده و متعلق به **همین** نماینده | `FK_MobileUser_ID` سفارش و فاکتور پر می‌شود و `tbMu_LastSeenDate` به‌روز می‌شود |
| دستگاه پیدا نشود یا مال نماینده‌ی دیگری باشد | فاکتور **بدون** اتصال ساخته می‌شود و یک `Warn` در NLog ثبت می‌شود — درخواست خطا نمی‌دهد |

#### رفتار `SubscriptionToken`

| مقدار | نتیجه | `tbOrders.OrderType` |
|-------|-------|------|
| `""` / `null` / فقط فاصله | نام اشتراک جدید تولید می‌شود | `جدید` |
| توکن معتبر متعلق به همین نماینده | همان اشتراک برای تمدید انتخاب می‌شود | `تمدید` |
| توکنی که پیدا نشود یا مال نماینده‌ی دیگری باشد | خطای `404` | — |

توکن همان مقداری است که در لینک ساب مشتری دیده می‌شود:

```
https://sub.example.com/api/v1/client/subscribe?token=8f3a1b2c4d5e6f7a8b9c
                                                       └────── همین مقدار ──────┘
```

جست‌وجو روی `tbLinks.tbL_Token` انجام می‌شود و **علاوه بر آن** شرط می‌گذارد که `tbL_Email` به `@{AgentUsername}` ختم شود؛ بنابراین یک نماینده نمی‌تواند برای اشتراک نماینده‌ی دیگری فاکتور بسازد، حتی اگر توکن آن را داشته باشد.

---

## پاسخ موفق — `200 OK`

```json
{
  "trackingCode": "9f3a1b2c#42",
  "amount": 1500347,
  "cardNumber": "6037997512345678",
  "cardHolderName": "رضا شریفی",
  "subscriptionName": "a1b2c3d4",
  "planPrice": 150000,
  "planVolume": 50,
  "planMonth": 1,
  "deviceLimit": 3
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|------|-------|
| `trackingCode` | string | `tbDepositWallet_Log.dw_TaxId` | کد پیگیری فاکتور با فرمت `{guid8}#{User_ID نماینده}` |
| `amount` | long | `tbDepositWallet_Log.dw_Price` | **مبلغ نهایی قابل واریز به ریال** — با تخفیف اعمال‌شده و سه رقم یکتای انتهایی |
| `cardNumber` | string | `tbBankCardNumbers.CardNumber` | شماره کارت **فعال** نماینده |
| `cardHolderName` | string | `tbBankCardNumbers.InTheNameOf` | نام دارنده‌ی کارت |
| `subscriptionName` | string | `tbOrders.AccountName` | نام اشتراک **بدون** بخش‌های `$` و `@` |
| `planPrice` | number | `tbLinkUserAndPlans.L_SellPrice` | مبلغ تعرفه به **تومان** و **بدون تخفیف** |
| `planVolume` | number | `tbPlans.PlanVolume` | حجم تعرفه به گیگابایت |
| `planMonth` | number | `tbPlans.PlanMonth` | مدت زمان تعرفه به ماه |
| `deviceLimit` | int? | `tbPlans.device_limit` | تعداد کاربر مجاز. `null` یعنی محدودیتی تعریف نشده |

> ⚠️ `amount` و `planPrice` عمداً دو چیز متفاوت‌اند: `planPrice` قیمت **تومانی و خام** تعرفه است (برای نمایش در فاکتور) و `amount` مبلغ **ریالی و نهایی** است که مشتری باید دقیقاً واریز کند. اگر تخفیف نماینده فعال باشد نسبت این دو `planPrice × 10` نیست.

### درباره‌ی `subscriptionName`

مقدار برگشتی فقط بخش قبل از اولین `$` یا `@` است (`GetDisplayNameOfAccount`)، ولی نام کامل در `tbOrders.AccountName` ذخیره می‌شود چون مرحله‌ی ساخت/تمدید روی v2board به نام کامل نیاز دارد.

```
ذخیره در دیتابیس :  a1b2c3d4$e5f6g7h8@agentuser
برگشتی در JSON   :  a1b2c3d4
```

این مقدار فقط برای **نمایش** است. برای تمدید همین اشتراک در آینده باید `SubscriptionToken` بفرستید، نه این نام.

> ⚠️ برای اشتراک **جدید**، توکن ساب هنوز وجود ندارد و در خروجی این API برنمی‌گردد. توکن در [مرحله‌ی تائید پرداخت](#تائید-فاکتور) ساخته و در `tbLinks.tbL_Token` ثبت می‌شود، و از آن به بعد `CheckAgentInvoice` لینک اشتراک را برمی‌گرداند.

### درباره‌ی `deviceLimit`

برای تعرفه‌های نامحدود (`tbPlans.IsRobotPlan = true`) حجم معنایی ندارد و ملاک، تعداد کاربر است. این API عدد خام `planVolume` را برمی‌گرداند و منطق نمایشی «نامحدود» را اعمال نمی‌کند — برخلاف [`GetAgentPlans`](API-AgentPlans.md) که رشته‌ی آماده‌ی `PlanName` می‌سازد. تصمیم‌گیری درباره‌ی نمایش با کلاینت است:

```javascript
const title = inv.deviceLimit
  ? `${inv.planMonth} ماهه | نامحدود | ${inv.deviceLimit} کاربر`
  : `${inv.planMonth} ماهه | ${inv.planVolume} گیگ`;
```

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است (خروجی استاندارد `IHttpActionResult` در این پروژه):

```json
"تعرفه ای با این شناسه برای این نماینده یافت نشد"
```

| Status | پیام | علت |
|:------:|------|------|
| `400` | `اطلاعات درخواست ارسال نشده است` | بدنه‌ی JSON ارسال نشده یا قابل پارس نیست |
| `400` | `توکن در هدر Authorization ارسال نشده است` | هدر `Authorization` وجود ندارد یا بعد از حذف `Bearer` خالی است |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `404` | `تعرفه ای با این شناسه برای این نماینده یافت نشد` | `Link_PU_ID` وجود ندارد، مال نماینده‌ی دیگری است، `L_Status = false` است یا `L_SellPrice` آن `NULL` است |
| `404` | `برای این نماینده کارت بانکی فعالی ثبت نشده است` | هیچ رکورد `tbBankCardNumbers` با `Active = 1` برای نماینده وجود ندارد |
| `404` | `اشتراکی با این توکن برای این نماینده یافت نشد` | `SubscriptionToken` پر بود ولی در `tbLinks` رکوردی با آن توکن و پسوند `@{AgentUsername}` پیدا نشد |
| `500` | `ساخت نام اشتراک جدید با خطا مواجه شد` | بعد از ۱۰ تلاش، نام یکتایی تولید نشد (عملاً غیرممکن) |
| `500` | `خطا در ساخت فاکتور` | خطای غیرمنتظره؛ جزئیات کامل در NLog ثبت می‌شود |

> در حالت خطا هیچ رکوردی در دیتابیس ثبت نمی‌شود؛ `SaveChangesAsync` آخرین قدم قبل از ساخت پاسخ است.

---

## منطق محاسبه‌ی مبلغ

```
Price      = tbLinkUserAndPlans.L_SellPrice                    // تومان
Price     -= (int)(Price × Present_Discount)                   // در صورت وجود تخفیف فعال
BasePrice  = Math.Round(Price) × 10                            // ریال
amount     = BasePrice + سه رقم یکتا (۱ تا ۹۹۹)
```

### تخفیف

تخفیف از `tbBotSettings.Present_Discount` **همان نماینده** خوانده می‌شود (اولین رکورد `Agent.tbBotSettings`) — دقیقاً همان مقداری که ربات هم در پرداخت مستقیم اعمال می‌کند. اگر نماینده تنظیمات ربات یا تخفیف نداشته باشد، قیمت دست‌نخورده می‌ماند.

### سه رقم یکتا

برخلاف ربات که یک عدد کاملاً تصادفی (`Random.Next(1, 999)`) اضافه می‌کند، این API یکتایی را **واقعاً بررسی می‌کند** (`BuildUniqueInvoicePriceAsync`):

۱. تمام فاکتورهای `FOR_PAY` که مبلغشان در بازه‌ی `(BasePrice , BasePrice + 999]` است خوانده می‌شوند.
۲. سه رقم انتهایی آن‌ها به‌عنوان «مصرف‌شده» علامت می‌خورد.
۳. از یک نقطه‌ی تصادفی شروع و اولین عدد آزاد بین ۱ تا ۹۹۹ انتخاب می‌شود.

دلیلش این است که تشخیص پرداخت در این سیستم بر اساس **تطبیق مبلغ** انجام می‌شود؛ دو فاکتور باز با مبلغ یکسان یعنی تخصیص اشتباه واریزی.

> اگر هر ۹۹۹ حالت اشغال باشد، به عدد تصادفی اولیه برمی‌گردد. عملاً یعنی ۹۹۹ فاکتور باز هم‌زمان با یک قیمت پایه — که اگر رخ دهد نشانه‌ی مشکل دیگری است.

### نکته درباره‌ی خوانایی مبلغ

چون `BasePrice` حاصل ضرب در ۱۰ است، همیشه به `0` ختم می‌شود. اگر قیمت تومانی تعرفه مضربی از ۱۰۰ باشد (حالت معمول)، مبلغ پایه به `000` ختم می‌شود و سه رقم آخر **دقیقاً** همان شناسه‌ی یکتای فاکتور است — یعنی با نگاه به مبلغ واریزی می‌شود فاکتور را تشخیص داد.

اگر قیمت تعرفه مضرب ۱۰۰ نباشد این خوانایی از بین می‌رود، ولی درستی کار خدشه‌دار نمی‌شود: کوئری بررسی یکتایی کل بازه‌ی `(BasePrice , BasePrice + 999]` را می‌بیند، پس همپوشانی با فاکتورهای دیگری که مبلغ پایه‌ی نزدیک دارند هم تشخیص داده می‌شود.

---

## رکوردهایی که در دیتابیس ساخته می‌شوند

هر فراخوانی موفق یک `tbOrders` و یک `tbDepositWallet_Log` متصل به آن می‌سازد:

### `tbOrders`

| ستون | مقدار |
|------|-------|
| `Order_Guid` | `Guid.NewGuid()` |
| `AccountName` | نام کامل اشتراک (`name$random@AgentUsername`) |
| `OrderDate` | `DateTime.Now` |
| `OrderStatus` | `FOR_PAY` |
| `OrderType` | `جدید` یا `تمدید` |
| `Order_Price` | قیمت تومانی **با** تخفیف |
| `PriceWithOutDiscount` | `L_SellPrice` خام |
| `Traffic` / `Month` | از `tbPlans` |
| `V2_Plan_ID` | `tbPlans.Plan_ID_V2` |
| `FK_Link_Plan_ID` | `Link_PU_ID` |
| `FK_Tel_UserID` | `NULL` ← فاکتور API کاربر تلگرام ندارد |
| `FK_MobileUser_ID` | شناسه‌ی دستگاه، یا `NULL` وقتی `DeviceId` نیامده باشد |

### `tbDepositWallet_Log`

| ستون | مقدار |
|------|-------|
| `dw_Price` | مبلغ ریالی نهایی با سه رقم یکتا |
| `dw_Status` | `FOR_PAY` |
| `dw_TaxId` | کد پیگیری |
| `dw_PayMethod` | `ApiCard` ← نشانگر فاکتور ساخته‌شده از طریق API |
| `FK_PayMethod_ID` | `5` ← روش پرداخت «اپلیکیشن» (`tbpm_Key = 'APP'`) |
| `FK_TelegramUser_ID` | `NULL` |
| `FK_MobileUser_ID` | شناسه‌ی دستگاه، یا `NULL` |

---

## تائید فاکتور

### چرا این فاکتورها وارد تائید خودکار پیامک نمی‌شوند

`FK_PayMethod_ID = 5` **عمدی** است. کوئری تشخیص واریزی در `TransactionHanderService.CheckOrder` روی شرط زیر فیلتر می‌کند:

```csharp
p.dw_Price == pr && p.dw_Status == "FOR_PAY" && p.tbPaymentMethods.tbpm_Key == "CardToCard"
```

کلید این فاکتورها `APP` است، پس هرگز وارد مسیر **پیامک** نمی‌شوند.

دلیل فنی‌اش هم این است که کل مسیر `CheckOrder` به `tbTelegramUsers` وابسته است — هم برای ارسال پیام و هم برای رسیدن به کانکشن‌استرینگ سرور (`item.tbTelegramUsers.tbUsers.tbServers.ConnectionString`). فاکتور اپلیکیشن کاربر تلگرام ندارد؛ اگر وارد آن حلقه می‌شد، یک `NullReferenceException` کل پردازش آن پیامک را `rollback` می‌کرد — **شامل فاکتورهای کاربران دیگری که مبلغ مشابه داشتند**.

### مسیر تائید از روی رسید (ربات ادمین)

اگر مشتری در آخرین مرحله‌ی ویزارد با [`UploadAgentInvoiceReceipt`](#api-آپلود-رسید-فاکتور-upload-agent-invoice-receipt) عکس رسید را بفرستد، سرور همان عکس را به **ادمین ربات تلگرام نماینده** می‌فرستد — معادل ارسال عکس رسید در خود ربات.

ادمین دو مرحله را طی می‌کند، دقیقاً مثل تائید دستی ربات (`accept` → `Faccept`):

۱. روی **✅ تائید** می‌زند (`Aaccept%{dw_ID}`).
۲. از دکمه‌های مبلغ، فاکتور مطابق رسید را انتخاب می‌کند (`AFaccept%{dw_ID}`).
۳. `AppInvoiceService.ConfirmAsync` اشتراک را می‌سازد یا تمدید می‌کند.

مشتری بعد از زدن «پرداخت کرده‌ام» با [`CheckAgentInvoice`](#api-بررسی-وضعیت-فاکتور-check-agent-invoice) نتیجه و جزئیات اشتراک را می‌گیرد. پیام تمدید به تلگرام مشتری نمی‌رود چون فاکتور اپلیکیشن کاربر تلگرام ندارد.

### مسیر تائید از پنل

تائید از **صفحه‌ی فاکتورهای پنل** هم انجام می‌شود. `BotFactorsController.Accept` فاکتورهای `FK_PayMethod_ID = 5` را به `Tools/AppInvoiceService.cs` می‌فرستد که:

۱. مالکیت را بررسی می‌کند — نماینده از پسوند `@AgentUsername` در `tbOrders.AccountName` پیدا می‌شود.
۲. اگر `tbLinks` برای این نام اشتراک وجود نداشته باشد، اشتراک را روی v2board **می‌سازد** (`insert into v2_user`) و رکورد `tbLinks` با توکن جدید ثبت می‌کند.
۳. اگر وجود داشته باشد **تمدید** می‌کند (`update v2_user ...`) — و اگر بسته‌ی فعلی هنوز اعتبار دارد، سفارش را `FOR_RESERVE` می‌گذارد، دقیقاً مثل رفتار ربات.
۴. `dw_Status` را `FINISH` و `OrderStatus` را `FINISH` (یا `FOR_RESERVE`) می‌کند.

### پاک‌سازی خودکار

`TimerService.RemoveExpireFactores` علاوه بر `CardToCard`، کلید `APP` را هم پاک می‌کند، پس فاکتورهای پرداخت‌نشده‌ی اپلیکیشن روی دست نمی‌مانند. تایمر **هشدار** (`AlertDeleteFactoresCard`) همچنان فقط `CardToCard` را می‌بیند، بنابراین برای این فاکتورها پیام تلگرامی فرستاده نمی‌شود.

### نمایش در پنل

- **صفحه‌ی فاکتورها** (`Areas/App/Controllers/BotFactorsController.cs`) این فاکتورها را نشان می‌دهد. چون کاربر تلگرام ندارند، ستون «کاربر» نام دستگاه (`Manufacturer Model`) و در نبود دستگاه ثبت‌شده، نام اشتراک را نمایش می‌دهد. روش پرداخت با برچسب آبی «اپلیکیشن» دیده می‌شود.
- مالکیت از دو راه تشخیص داده می‌شود: `tbMobileUsers.tbUsers.Username` یا پسوند `@Username` در `AccountName`. یعنی فاکتوری که `DeviceId` نداشته هم برای نماینده‌اش دیده می‌شود.
- **کاربران موبایل** (`App/MobileUsers`) دستگاه‌ها را جداگانه نشان می‌دهد — فقط برای ادمین. مستندات: [`API-MobileDevice.md`](API-MobileDevice.md).

> متد `GetFactors` در همین کنترلر شرط `p.FK_TelegramUser_ID != null` دارد و عمداً دست‌نخورده مانده؛ آن خروجی مخصوص کاربران ربات است.

---

## نمونه فراخوانی

### cURL — اشتراک جدید

```bash
curl -X POST "https://panel.example.com/User/CreateAgentInvoice" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"PlanId\":154}"
```

### cURL — تمدید اشتراک موجود

```bash
curl -X POST "https://panel.example.com/User/CreateAgentInvoice" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"PlanId\":154,\"SubscriptionToken\":\"8f3a1b2c4d5e6f7a8b9c\"}"
```

### JavaScript

```javascript
const res = await fetch('/User/CreateAgentInvoice', {
  method: 'POST',
  headers: {
    'Authorization': agentToken,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ PlanId: 154, SubscriptionToken: '' })
});

if (res.ok) {
  const inv = await res.json();
  console.log('کد پیگیری :', inv.trackingCode);
  console.log('مبلغ واریزی (ریال) :', inv.amount.toLocaleString('fa-IR'));
  console.log('کارت :', inv.cardNumber, '-', inv.cardHolderName);
} else {
  console.error(await res.json());
}
```

### C#

```csharp
using (var http = new HttpClient())
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);

    var body = new StringContent(
        JsonConvert.SerializeObject(new { PlanId = 154, SubscriptionToken = "" }),
        Encoding.UTF8,
        "application/json");

    var res = await http.PostAsync("https://panel.example.com/User/CreateAgentInvoice", body);
    var json = await res.Content.ReadAsStringAsync();

    if (res.IsSuccessStatusCode)
    {
        var invoice = JsonConvert.DeserializeObject<AgentInvoiceViewModel>(json);
    }
}
```

---

# API آپلود رسید فاکتور (Upload Agent Invoice Receipt)

آپلود عکس رسید برای فاکتوری که با `CreateAgentInvoice` ساخته شده، و ارسال همان عکس به ادمین ربات تلگرام نماینده.

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `UploadAgentInvoiceReceipt`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/AgentInvoiceReceiptViewModel.cs`
- ارسال تلگرام: `V2boardApi/Tools/AppInvoiceReceiptService.cs`
- تائید ادمین: `BotController` کال‌بک‌های `Aaccept` / `AFaccept` → `AppInvoiceService.ConfirmAsync`

> این همان کاری است که در ربات با فرستادن **عکس** رسید انجام می‌شود: ادمین عکس را می‌بیند، تائید می‌زند، مبلغ فاکتور را انتخاب می‌کند، و اشتراک ساخته یا تمدید می‌شود. فرق این است که عکس از اپ می‌آید، نه از چت تلگرام مشتری.

---

## جای این اندپوینت در ویزارد ساخت فاکتور

```
POST /User/CreateAgentInvoice
            │
            ▼
   مبلغ + شماره کارت  ← مرحله‌ی آخر ویزارد
            │
            ▼
POST /User/UploadAgentInvoiceReceipt   ← عکس رسید
            │
            ▼  عکس برای ادمین ربات
ادمین: ✅ تائید  →  انتخاب مبلغ
            │
            ▼
POST /User/CheckAgentInvoice           ← دکمه‌ی «پرداخت کرده‌ام»
            │
            ▼
isConfirmed + جزئیات اشتراک
```

کلاینت بعد از ساخت فاکتور، فرم آپلود رسید را نشان می‌دهد. بعد از آپلود، دکمه‌ی «پرداخت کرده‌ام» را به `CheckAgentInvoice` وصل کند و تا `isConfirmed == true` poll کند. اگر سفارش **تمدید** باشد پیام `اشتراک شما با موفقیت تمدید شد` و فیلدهای حجم/انقضا را نشان بدهد.

---

## اندپوینت

```
POST /User/UploadAgentInvoiceReceipt
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type درخواست | `multipart/form-data` |
| Content-Type پاسخ | `application/json; charset=utf-8` |
| CORS | `Access-Control-Allow-Origin: *` |

> مسیر بدون پیشوند `api/v1` است — مثل بقیه‌ی `/User/*`. هدر `Content-Type` را خودتان با boundary نسازید؛ کلاینت HTTP آن را می‌گذارد.

### هدرها

| نام | الزامی | توضیح |
|-----|:------:|-------|
| `Authorization` | بله | همان `agentToken`. `Bearer <token>` هم پذیرفته می‌شود |
| `Content-Type` | بله | `multipart/form-data` با boundary |

### فیلدهای فرم

| فیلد | نوع | الزامی | توضیح |
|------|------|:------:|-------|
| `TaxId` | text | بله | همان `trackingCode` خروجی `CreateAgentInvoice`. نام `taxId` هم خوانده می‌شود |
| `Receipt` | file | بله | عکس رسید. نام `receipt` هم خوانده می‌شود. اگر نام فایل مشخص نباشد **اولین فایل** فرم گرفته می‌شود |

### محدودیت فایل

| مورد | مقدار |
|------|-------|
| پسوند مجاز | `.jpg` `.jpeg` `.png` `.webp` |
| سقف حجم | ۴ مگابایت |
| محل ذخیره | `assets/img/AppInvoiceReceipts/` |
| نام ذخیره‌شده | `{guid}{ext}` در ستون `tbDepositWallet_Log.dw_payment_id` |

آپلود مجدد تا وقتی فاکتور `FOR_PAY` است مجاز است: فایل قبلی پاک می‌شود و عکس جدید دوباره برای ادمین فرستاده می‌شود.

---

## پاسخ موفق — `200 OK`

```json
{
  "trackingCode": "9f3a1b2c#42",
  "receiptUploaded": true,
  "sentToAdmin": true,
  "message": "رسید برای ادمین ارسال شد. پس از تائید، اشتراک تمدید می‌شود"
}
```

اگر ربات نماینده یا `AdminBot_ID` تنظیم نشده باشد، فایل **ذخیره می‌شود** ولی تلگرام نمی‌رود:

```json
{
  "trackingCode": "9f3a1b2c#42",
  "receiptUploaded": true,
  "sentToAdmin": false,
  "message": "رسید ذخیره شد ولی ربات یا ادمین تلگرام نماینده تنظیم نشده است"
}
```

در این حالت تائید از **صفحه‌ی فاکتورهای پنل** ممکن است. کلاینت باید `sentToAdmin` را بخواند؛ `receiptUploaded: true` یعنی فایل روی سرور است.

### فیلدها

| فیلد | نوع | توضیح |
|------|------|-------|
| `trackingCode` | string | همان `TaxId` |
| `receiptUploaded` | bool | فایل روی دیسک ذخیره شده |
| `sentToAdmin` | bool | عکس با دکمه‌ی تائید برای ادمین ربات ارسال شده |
| `message` | string | پیام فارسی قابل نمایش |

---

## پاسخ‌های خطا

بدنه‌ی خطا یک رشته‌ی JSON ساده است:

```json
"فایل رسید ارسال نشده است"
```

| Status | پیام | علت |
|:------:|------|------|
| `400` | `توکن در هدر Authorization ارسال نشده است` | هدر خالی است |
| `400` | `کد پیگیری ارسال نشده است` | فیلد `TaxId` در فرم نیست |
| `400` | `فایل رسید ارسال نشده است` | فایل در فرم نیست یا خالی است |
| `400` | `فقط عکس jpg و png و webp تا سقف ۴ مگابایت پذیرفته می‌شود` | پسوند یا حجم خارج از محدوده است |
| `400` | `این فاکتور مربوط به اپلیکیشن نیست` | فاکتور `FK_PayMethod_ID` برابر `APP` نیست |
| `400` | `این فاکتور قبلا تائید شده است` | `dw_Status` دیگر `FOR_PAY` نیست |
| `404` | `کاربری با این توکن یافت نشد` | توکن نماینده نامعتبر است |
| `404` | `فاکتوری با این کد پیگیری یافت نشد` | کد پیگیری نیست، یا مال نماینده‌ی دیگری است |
| `500` | `خطا در آپلود رسید فاکتور` | خطای غیرمنتظره؛ جزئیات در NLog |

---

## رفتار سمت ادمین ربات

پیام ارسالی به `tbBotSettings.AdminBot_ID` یک عکس است با کپشن مشابه رسید ربات:

```
📱 رسید پرداخت اپلیکیشن

👤 اشتراک : ali12
🔖 کد پیگیری : 9f3a1b2c#42
💰 مبلغ : 1,500,347 ریال
📦 نوع : تمدید
📲 دستگاه : Samsung SM-A52
👔 نماینده : agentuser

♨️ موارد فوق مورد تایید است ؟
```

دکمه‌ها:

| مرحله | کال‌بک | کار |
|-------|--------|-----|
| ۱ | `Aaccept%{dw_ID}` | لیست مبلغ فاکتورهای `FOR_PAY` همان دستگاه (یا همان اشتراک اگر دستگاه نباشد) |
| ۲ | `AFaccept%{dw_ID}` | تائید همان فاکتور با `AppInvoiceService.ConfirmAsync` |

این دو پیشوند جدا از `accept` / `Faccept` ربات‌اند تا فاکتور `APP` وارد `CheckOrder` (کارت‌به‌کارت + کاربر تلگرام) نشود.

بعد از تائید موفق، ادمین پیام «✅ تراکنش با موفقیت تایید شد» می‌گیرد. مشتری باید `CheckAgentInvoice` را صدا بزند.

---

## نمونه فراخوانی

### cURL

```bash
curl -X POST "https://panel.example.com/User/UploadAgentInvoiceReceipt" \
  -H "Authorization: 9f2c1ab7d4e8" \
  -F "TaxId=9f3a1b2c#42" \
  -F "Receipt=@/path/to/receipt.jpg;type=image/jpeg"
```

### JavaScript

```javascript
const form = new FormData();
form.append('TaxId', inv.trackingCode);
form.append('Receipt', receiptFile); // File از input یا دوربین

const res = await fetch('/User/UploadAgentInvoiceReceipt', {
  method: 'POST',
  headers: { 'Authorization': agentToken },
  body: form
});

if (res.ok) {
  const r = await res.json();
  if (r.sentToAdmin) {
    startPolling(inv.trackingCode); // CheckAgentInvoice تا isConfirmed
  } else {
    alert(r.message);
  }
}
```

### Kotlin

```kotlin
val body = MultipartBody.Builder().setType(MultipartBody.FORM)
    .addFormDataPart("TaxId", trackingCode)
    .addFormDataPart(
        "Receipt", "receipt.jpg",
        file.asRequestBody("image/jpeg".toMediaType())
    )
    .build()

val req = Request.Builder()
    .url(AgentApi.url("/User/UploadAgentInvoiceReceipt"))
    .header("Authorization", agentToken)
    .post(body)
    .build()
```

### C#

```csharp
using (var http = new HttpClient())
using (var form = new MultipartFormDataContent())
using (var file = new ByteArrayContent(System.IO.File.ReadAllBytes(receiptPath)))
{
    http.DefaultRequestHeaders.Add("Authorization", agentToken);
    form.Add(new StringContent(trackingCode), "TaxId");
    file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
    form.Add(file, "Receipt", "receipt.jpg");

    var res = await http.PostAsync("https://panel.example.com/User/UploadAgentInvoiceReceipt", form);
    var json = await res.Content.ReadAsStringAsync();
}
```

---

# API بررسی وضعیت فاکتور (Check Agent Invoice)

بررسی اینکه فاکتور ساخته‌شده با `CreateAgentInvoice` تائید شده است یا خیر. بعد از تائید، جزئیات اشتراک (حجم، روز باقی‌مانده، تاریخ انقضا) هم برمی‌گردد تا اپ صفحه‌ی «اشتراک شما با موفقیت تمدید شد» را نشان بدهد.

- کنترلر: `V2boardApi/Areas/api/Controllers/UserController.cs` → متد `CheckAgentInvoice`
- مدل ورودی: `V2boardApi/Areas/api/Data/ApiModels/CheckAgentInvoiceModel.cs`
- مدل خروجی: `V2boardApi/Areas/api/Data/ViewModels/AgentInvoiceStatusViewModel.cs`

```
POST /User/CheckAgentInvoice
```

| مورد | مقدار |
|------|-------|
| متد | `POST` |
| احراز هویت | توکن نماینده در هدر `Authorization` |
| Content-Type درخواست | `application/json` |

> `POST` انتخاب شده تا کد پیگیری در کوئری‌استرینگ و لاگ IIS ثبت نشود — همان دلیلی که در `CreateAgentInvoice` هم رعایت شده.

### بدنه‌ی درخواست

```json
{ "TaxId": "9f3a1b2c#42", "PayFromWallet": false }
```

| فیلد | نوع | الزامی | توضیح |
|------|------|:------:|-------|
| `TaxId` | string | بله | همان `trackingCode` که `CreateAgentInvoice` برگردانده است (`tbDepositWallet_Log.dw_TaxId`) |
| `PayFromWallet` | bool | خیر | `true` یعنی مشتری پرداخت از کیف پول ربات را زده است. ارسال‌نشده یا `false` یعنی فقط وضعیت فاکتور خوانده شود — رفتار قبلی. جزئیات: [پرداخت از کیف پول](#پرداخت-از-کیف-پول-payfromwallet) |

---

## پاسخ موفق — `200 OK`

پاسخ **همیشه** `200` است اگر فاکتور پیدا شود؛ تائید نشدن پرداخت خطا نیست، یک وضعیت است.

### فاکتور تائید نشده

```json
{
  "trackingCode": "9f3a1b2c#42",
  "isConfirmed": false,
  "status": "FOR_PAY",
  "orderType": "جدید",
  "subscriptionName": "a1b2c3d4",
  "amount": 1500347,
  "subscriptionLink": null,
  "backupSubscriptionLink": null,
  "hasReceipt": false,
  "totalVolumeGb": null,
  "usedVolumeGb": null,
  "remainingDays": null,
  "expireDate": null,
  "message": "هنوز فاکتور شما تمدید نشده است. این فرایند ممکن است ۵ تا ۱۵ دقیقه طول بکشد. در صورت عدم تائید، رسید خودتان را ارسال کنید"
}
```

اگر رسید آپلود شده ولی ادمین هنوز تائید نکرده، همان پیام برمی‌گردد و فقط `hasReceipt` برابر `true` است:

```json
{
  "trackingCode": "9f3a1b2c#42",
  "isConfirmed": false,
  "status": "FOR_PAY",
  "orderType": "تمدید",
  "subscriptionName": "ali12",
  "amount": 1500347,
  "subscriptionLink": null,
  "backupSubscriptionLink": null,
  "hasReceipt": true,
  "totalVolumeGb": null,
  "usedVolumeGb": null,
  "remainingDays": null,
  "expireDate": null,
  "message": "هنوز فاکتور شما تمدید نشده است. این فرایند ممکن است ۵ تا ۱۵ دقیقه طول بکشد. در صورت عدم تائید، رسید خودتان را ارسال کنید"
}
```

کلاینت روی دکمه‌ی «پرداخت کرده‌ام» اگر `isConfirmed` برابر `false` بود همین `message` را نشان بدهد. بعد از تائید ادمین دوباره همین اندپوینت را صدا بزند تا صفحه موفقیت بیاید.

### تائید شده — سفارش **تمدید**

```json
{
  "trackingCode": "9f3a1b2c#42",
  "isConfirmed": true,
  "status": "FINISH",
  "orderType": "تمدید",
  "subscriptionName": "ali12",
  "amount": 1500347,
  "subscriptionLink": null,
  "backupSubscriptionLink": null,
  "hasReceipt": true,
  "totalVolumeGb": 50.0,
  "usedVolumeGb": 0.0,
  "remainingDays": 30,
  "expireDate": "1404/06/19",
  "message": "اشتراک شما با موفقیت تمدید شد"
}
```

لینک برنمی‌گردد چون مشتری از قبل لینک اشتراکش را دارد و تمدید آن را عوض نمی‌کند. جزئیات حجم و انقضا همان فیلدهای [`Sub/Info`](API-SubscriptionInfo.md) هستند تا اپ بتواند صفحه‌ی موفقیت را مثل تائید دستی ربات نشان بدهد.

اگر بسته‌ی فعلی هنوز اعتبار داشته و سفارش رزرو شده باشد، `message` برابر است با `پرداخت تائید شد و بسته به صورت رزرو ثبت شد`.

### تائید شده — سفارش **جدید**

```json
{
  "trackingCode": "9f3a1b2c#42",
  "isConfirmed": true,
  "status": "FINISH",
  "orderType": "جدید",
  "subscriptionName": "a1b2c3d4",
  "amount": 1500347,
  "subscriptionLink": "https://sub.example.com/api/v1/client/subscribe?token=8f3a1b2c4d5e6f7a8b9c",
  "backupSubscriptionLink": "https://sub2.example.com/api/v1/client/subscribe?token=8f3a1b2c4d5e6f7a8b9c",
  "hasReceipt": true,
  "totalVolumeGb": 50.0,
  "usedVolumeGb": 0.0,
  "remainingDays": 30,
  "expireDate": "1404/06/19",
  "message": "پرداخت تائید شد و اشتراک ساخته شده است"
}
```

### فیلدها

| فیلد | نوع | منبع | توضیح |
|------|------|------|-------|
| `trackingCode` | string | ورودی | همان `TaxId` |
| `isConfirmed` | bool | `dw_Status == "FINISH"` | **تنها فیلدی که کلاینت باید برای تصمیم‌گیری به آن نگاه کند** |
| `status` | string | `tbDepositWallet_Log.dw_Status` | وضعیت خام: `FOR_PAY` / `FINISH` / `FOR_RESERVE` |
| `orderType` | string | `tbOrders.OrderType` | `جدید` یا `تمدید` |
| `subscriptionName` | string | `tbOrders.AccountName` | بدون بخش‌های `$` و `@` |
| `amount` | long | `tbDepositWallet_Log.dw_Price` | مبلغ ریالی فاکتور |
| `subscriptionLink` | string? | `tbLinks.tbL_Token` + `tbServers.SubAddress` | فقط در سفارش **جدید تائید شده** |
| `backupSubscriptionLink` | string? | `tbServers.BackupSubAddr` | اگر آدرس پشتیبان روی سرور تعریف شده باشد |
| `hasReceipt` | bool | پسوند تصویر در `dw_payment_id` | `true` یعنی مشتری عکس رسید آپلود کرده |
| `totalVolumeGb` | number? | `v2_user.transfer_enable` | فقط بعد از تائید. حجم کل به گیگابایت |
| `usedVolumeGb` | number? | `v2_user.u + d` | فقط بعد از تائید. حجم مصرف‌شده به گیگابایت |
| `remainingDays` | int? | `v2_user.expired_at` | فقط بعد از تائید. `0` منقضی، `-1` نامحدود |
| `expireDate` | string? | `v2_user.expired_at` | فقط بعد از تائید. شمسی `yyyy/MM/dd` |
| `message` | string | — | پیام فارسی قابل نمایش مستقیم به مشتری |

### ساخت لینک اشتراک

```
https://{tbServers.SubAddress}/api/v1/client/subscribe?token={tbLinks.tbL_Token}
```

سرور اول از `tbLinks.tbServers` خوانده می‌شود و اگر تهی بود، به `Agent.tbServers` برمی‌گردد. این دقیقاً همان فرمتی است که ربات تلگرام هم به مشتری می‌دهد.

### حالت‌های میانی

اگر پرداخت تائید شده ولی هنوز اشتراک ساخته نشده، پاسخ همچنان `200` با `isConfirmed: true` است ولی `subscriptionLink` تهی می‌ماند:

| `message` | یعنی |
|-----------|------|
| `پرداخت تائید شد ولی اشتراک هنوز ساخته نشده است` | رکوردی در `tbLinks` برای این نام اشتراک نیست یا توکن ندارد |
| `پرداخت تائید شد ولی آدرس لینک اشتراک روی سرور تنظیم نشده است` | `tbServers.SubAddress` تهی است |

کلاینت باید در این دو حالت دوباره تلاش (poll) کند یا خطا را به پشتیبانی گزارش دهد.

---

## پرداخت از کیف پول (`PayFromWallet`)

معادل دکمه‌ی **💳 پرداخت از کیف پول** در ربات (`BotController` شاخه‌ی `AccpetWallet`)، ولی از داخل اپلیکیشن و روی همان فاکتوری که `CreateAgentInvoice` ساخته است.

موجودی از [`GetTelegramWallet`](API-TelegramWallet.md) خوانده می‌شود. خودِ کسر و تمدید اینجا انجام می‌شود.

### شرط لازم

فاکتور باید برای **تمدید اشتراکی باشد که در ربات ساخته شده** — یعنی رکورد `tbLinks` آن `FK_TelegramUserID` داشته باشد. اشتراک تازه‌ی ساخته‌شده از اپلیکیشن معمولاً کاربر تلگرام ندارد و این حالت کار نمی‌کند.

### جریان

```
CreateAgentInvoice          → فاکتور FOR_PAY
GetTelegramWallet           → نمایش موجودی در اپ
کاربر «پرداخت از کیف پول»
CheckAgentInvoice
  PayFromWallet: true       → اگر موجودی کافی بود: کسر + تمدید + FINISH
```

اگر `PayFromWallet` نیاید یا `false` باشد، رفتار قبلی است: فقط وضعیت خوانده می‌شود و تائید از پنل می‌ماند.

اگر فاکتور از قبل `FINISH` باشد، `PayFromWallet` نادیده گرفته می‌شود و همان وضعیت تائیدشده برمی‌گردد — دوبار کسر نمی‌شود.

### چه کارهایی سمت سرور انجام می‌شود

همه داخل یک تراکنش در `AppInvoiceService.ConfirmFromWalletAsync`:

۱. مالکیت فاکتور و تعلق اشتراک به همین نماینده بررسی می‌شود.
۲. کاربر تلگرام از `tbLinks.FK_TelegramUserID` خوانده می‌شود.
۳. `Tel_Wallet` با `tbOrders.Order_Price` (تومان، با تخفیف) مقایسه می‌شود — **نه** با `amount` ریالی که سه رقم یکتا دارد.
۴. اگر کافی نبود فاکتور دست‌نخورده می‌ماند و پاسخ `200` با `isConfirmed: false` برمی‌گردد.
۵. بدهی نماینده مثل ربات افزایش می‌یابد (`tbUsers.Wallet`) و سقف اعتبار (`Limit`) چک می‌شود.
۶. مبلغ از `Tel_Wallet` کسر می‌شود.
۷. اشتراک تمدید می‌شود، یا اگر بسته‌ی فعلی هنوز اعتبار دارد سفارش `FOR_RESERVE` می‌شود — همان رفتار ربات و تائید پنل.
۸. `dw_Status = FINISH` و `dw_PayMethod = ApiWallet`.

> `FK_PayMethod_ID` همچنان `5` (`APP`) می‌ماند، پس این فاکتور وارد مسیر تائید پیامک نمی‌شود.

### پاسخ‌ها وقتی `PayFromWallet: true` است

موفقیت همان شکل پاسخ تائیدشده‌ی معمولی است (`isConfirmed: true`). شکست پرداخت از کیف پول هم `200` است، نه خطا؛ کلاینت باید `isConfirmed` و `message` را بخواند:

| `isConfirmed` | `message` | معنی |
|:-------------:|-----------|------|
| `false` | `موجودی کیف پول کافی نیست` | `Tel_Wallet` کمتر از قیمت تومانی سفارش است. فاکتور `FOR_PAY` مانده و می‌شود کارت‌به‌کارت کرد |
| `false` | `این اشتراک به حساب ربات تلگرام متصل نیست` | `FK_TelegramUserID` تهی است — دکمه‌ی کیف پول نباید برای این اشتراک نشان داده می‌شد |
| `false` | `امکان پرداخت از کیف پول در حال حاضر وجود ندارد لطفا با پشتیبانی ارتباط بگیرید` | سقف اعتبار نماینده پر شده یا قیمت تمام‌شده‌ی نماینده تعریف نشده |
| `true` | `پرداخت تائید شد و اشتراک تمدید شده است` | تمدید (یا رزرو) انجام شد و مبلغ کسر شد |

بعد از موفقیت، کلاینت می‌تواند [`GetTelegramWallet`](API-TelegramWallet.md) را دوباره صدا بزند تا موجودی به‌روز را نشان دهد.

### نمونه — پرداخت از کیف پول

```bash
curl -X POST "https://panel.example.com/User/CheckAgentInvoice" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"TaxId\":\"9f3a1b2c#42\",\"PayFromWallet\":true}"
```

```javascript
const st = await fetch('/User/CheckAgentInvoice', {
  method: 'POST',
  headers: { 'Authorization': agentToken, 'Content-Type': 'application/json' },
  body: JSON.stringify({ TaxId: inv.trackingCode, PayFromWallet: true })
}).then(r => r.json());

if (st.isConfirmed) {
  console.log(st.message);
} else {
  alert(st.message); // موجودی کافی نیست / حساب تلگرام متصل نیست / ...
}
```

---

## پاسخ‌های خطا

| Status | پیام | علت |
|:------:|------|------|
| `400` | `کد پیگیری ارسال نشده است` | بدنه ارسال نشده یا `TaxId` خالی است |
| `400` | `توکن در هدر Authorization ارسال نشده است` | هدر `Authorization` وجود ندارد یا خالی است |
| `404` | `کاربری با این توکن یافت نشد` | هیچ رکوردی در `tbUsers` با این `Token` نیست |
| `404` | `فاکتوری با این کد پیگیری یافت نشد` | کد پیگیری وجود ندارد، سفارش متصل ندارد، **یا مال نماینده‌ی دیگری است** |
| `500` | `خطا در بررسی وضعیت فاکتور` | خطای غیرمنتظره؛ جزئیات در NLog |

> فاکتور نماینده‌ی دیگر عمداً همان `404` را می‌گیرد، نه `403`. این‌طور نمی‌شود از روی پاسخ فهمید که یک کد پیگیری وجود دارد ولی مال کس دیگری است.

---

## نمونه فراخوانی

### cURL

```bash
curl -X POST "https://panel.example.com/User/CheckAgentInvoice" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"TaxId\":\"9f3a1b2c#42\"}"
```

بدون `PayFromWallet` فقط وضعیت خوانده می‌شود. برای کسر از کیف پول همان درخواست را با `"PayFromWallet": true` بفرستید — بخش [پرداخت از کیف پول](#پرداخت-از-کیف-پول-payfromwallet).

```bash
curl -X POST "https://panel.example.com/User/CheckAgentInvoice" -H "Authorization: 9f2c1ab7d4e8" -H "Content-Type: application/json" -d "{\"TaxId\":\"9f3a1b2c#42\",\"PayFromWallet\":true}"
```

### JavaScript — با poll

```javascript
async function waitForPayment(trackingCode, agentToken) {
  for (let i = 0; i < 60; i++) {
    const res = await fetch('/User/CheckAgentInvoice', {
      method: 'POST',
      headers: { 'Authorization': agentToken, 'Content-Type': 'application/json' },
      body: JSON.stringify({ TaxId: trackingCode })
    });

    if (!res.ok) throw new Error(await res.json());

    const st = await res.json();
    if (st.isConfirmed) return st;

    await new Promise(r => setTimeout(r, 10000));
  }
  return null;
}

const st = await waitForPayment(inv.trackingCode, agentToken);
if (!st) {
  console.log('هنوز تائید نشده');
} else if (st.orderType === 'تمدید') {
  // دکمه‌ی «پرداخت کرده‌ام» — معادل پیام تائید دستی ربات
  console.log(st.message); // اشتراک شما با موفقیت تمدید شد
  console.log(st.totalVolumeGb, st.usedVolumeGb, st.remainingDays, st.expireDate);
} else if (st.orderType === 'جدید') {
  console.log('لینک اشتراک :', st.subscriptionLink);
  console.log(st.totalVolumeGb, st.remainingDays, st.expireDate);
}
```

---

## ملاحظات امنیتی

- توکن نماینده نقش کلید دسترسی را دارد و در خروجی برگردانده **نمی‌شود**؛ فقط ورودی است.
- **`PayFromWallet` فقط کیف پول همان اشتراک را می‌زند:** کاربر تلگرام از `tbLinks.FK_TelegramUserID` همان سفارشی که فاکتور به آن وصل است خوانده می‌شود، و آن کاربر باید `FK_User_ID` همین نماینده را داشته باشد. موجودی نماینده‌ی دیگر یا اشتراک بدون حساب تلگرام کسر نمی‌شود.
- اگر فاکتور از قبل `FINISH` باشد `PayFromWallet` نادیده گرفته می‌شود تا مبلغ دو بار کسر نشود.
- کد پیگیری با `Guid` هشت‌کاراکتری ساخته می‌شود و حدس زدنش عملی نیست، ولی به آن به‌عنوان تنها لایه‌ی امنیتی تکیه نکنید — به همین دلیل هدر `Authorization` هم لازم است.
- این اندپوینت فیلتر `[Authorize]` ندارد و اعتبارسنجی فقط بر پایه‌ی تطبیق `tbUsers.Token` است. هرکس توکن یک نماینده را داشته باشد می‌تواند به نام او فاکتور بسازد.
- **مالکیت در هر دو سمت بررسی می‌شود:** تعرفه باید `L_FK_U_ID == Agent.User_ID` باشد و اشتراک باید نامی ختم‌شده به `@{AgentUsername}` داشته باشد. یک نماینده نمی‌تواند با تعرفه یا اشتراک نماینده‌ی دیگری فاکتور بسازد — **حتی اگر توکن ساب آن اشتراک را در اختیار داشته باشد.**
- توکن ساب مشتری یک شناسه‌ی حساس است (هرکس آن را داشته باشد به کانفیگ‌های اشتراک دسترسی دارد). چون در بدنه‌ی `POST` فرستاده می‌شود نه در کوئری‌استرینگ، در لاگ IIS ثبت نمی‌شود.
- شماره کارت و نام دارنده‌ی کارت نماینده در خروجی برمی‌گردند — همان اطلاعاتی که ربات هم به مشتری نشان می‌دهد. این اندپوینت را روی `HTTPS` سرو کنید.
- هیچ محدودیت نرخی (rate limit) روی این اندپوینت وجود ندارد. هر فراخوانی یک رکورد `FOR_PAY` می‌سازد؛ این رکوردها با `TimerService.RemoveExpireFactores` پس از مهلت مقرر پاک می‌شوند، ولی یک کلاینت خراب یا مهاجم با توکن معتبر همچنان می‌تواند در بازه‌ی آن مهلت جدول را پر کند. اگر در معرض اینترنت قرار می‌گیرد، محدودیت نرخ در سطح IIS یا reverse proxy اضافه کنید.
- توکن را در URL، لاگ یا کد سمت کلاینت عمومی قرار ندهید.
- فایل رسید فقط از نماینده‌ی صاحب فاکتور پذیرفته می‌شود و فقط عکس با سقف ۴ مگابایت. نام ذخیره‌شده یک `Guid` است، نه نام اصلی فایل کاربر.
- کال‌بک تلگرام `Aaccept` / `AFaccept` فقط از `AdminBot_ID` همان ربات پذیرفته می‌شود و فاکتور باید به همان نماینده (`@{botName}`) تعلق داشته باشد.

---

## پیش‌نیاز دیتابیس

هیچ تغییر اسکیمایی لازم نیست. برای سرعت، ایندکس‌های زیر مفیدند:

```sql
CREATE NONCLUSTERED INDEX IX_tbUsers_Token ON tbUsers (Token);
CREATE NONCLUSTERED INDEX IX_tbLinks_Token ON tbLinks (tbL_Token) INCLUDE (tbL_Email);
CREATE NONCLUSTERED INDEX IX_tbLinks_Email ON tbLinks (tbL_Email);
CREATE NONCLUSTERED INDEX IX_tbDepositWallet_Log_Status_Price ON tbDepositWallet_Log (dw_Status, dw_Price);
```

- ایندکس دوم برای پیدا کردن اشتراک در حالت تمدید است. شرط `tbL_Email LIKE '%@agent'` به‌خاطر `%` ابتدایی از ایندکس استفاده نمی‌کند، ولی چون فیلتر روی توکن قبلاً به یک رکورد رسیده، هزینه‌ای ندارد.
- ایندکس سوم برای بررسی تکراری‌نبودن نام اشتراک تولیدشده (`tbL_Email == Name`) است.
- ایندکس چهارم مخصوص کوئری بررسی یکتایی سه رقم انتهایی است که در هر فراخوانی اجرا می‌شود.
