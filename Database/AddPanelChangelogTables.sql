-- =============================================================================
-- Changelog پنل — اجرا روی SQL Server پروژه
-- شامل: ساخت جداول + seed کامل نسخه‌ها (1.6.0 ، 1.7.0 ، 1.8.0 ، 1.8.1)
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbPanelChangelogVersions' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbPanelChangelogVersions
    (
        tbPclv_ID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        tbPclv_Version     VARCHAR(20) NOT NULL,
        tbPclv_ReleaseDate VARCHAR(20) NOT NULL,
        tbPclv_SortOrder   INT NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_SortOrder DEFAULT (0),
        tbPclv_IsCurrent   BIT NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_IsCurrent DEFAULT (0),
        tbPclv_IsActive    BIT NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_IsActive DEFAULT (1),
        tbPclv_CreatedAt   DATETIME NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbPanelChangelogItems' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbPanelChangelogItems
    (
        tbPcli_ID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FK_Version_ID      INT NOT NULL,
        tbPcli_Title       NVARCHAR(200) NOT NULL,
        tbPcli_Description NVARCHAR(1000) NOT NULL,
        tbPcli_Audience    TINYINT NOT NULL, -- 1=Admin, 2=Agent
        tbPcli_SortOrder   INT NOT NULL CONSTRAINT DF_tbPanelChangelogItems_SortOrder DEFAULT (0),
        tbPcli_IsActive    BIT NOT NULL CONSTRAINT DF_tbPanelChangelogItems_IsActive DEFAULT (1),
        CONSTRAINT FK_tbPanelChangelogItems_tbPanelChangelogVersions
            FOREIGN KEY (FK_Version_ID) REFERENCES dbo.tbPanelChangelogVersions(tbPclv_ID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbPanelChangelogItems_FK_Version_ID')
BEGIN
    CREATE INDEX IX_tbPanelChangelogItems_FK_Version_ID
        ON dbo.tbPanelChangelogItems(FK_Version_ID, tbPcli_SortOrder);
END
GO

-- =============================================================================
-- Seed کامل (فقط اگر جدول نسخه‌ها خالی باشد)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions)
BEGIN
    SET IDENTITY_INSERT dbo.tbPanelChangelogVersions ON;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_ID, tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        (1, '1.8.0', '1405/03/30', 180, 1, 1),
        (2, '1.7.0', '1404/03/30', 170, 0, 1),
        (3, '1.6.0', '1404/03/15', 160, 0, 1);

    SET IDENTITY_INSERT dbo.tbPanelChangelogVersions OFF;

    SET IDENTITY_INSERT dbo.tbPanelChangelogItems ON;

    INSERT INTO dbo.tbPanelChangelogItems
        (tbPcli_ID, FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        -- ── 1.8.0 — Admin ──
        ( 1, 1, N'فیلتر پیشرفته لیست اشتراک‌ها', N'فیلتر نماینده، بازه تاریخ، مرتب‌سازی بر اساس حجم باقی‌مانده و کارت‌های خلاصه تعداد و مبلغ فروش.', 1, 10, 1),
        ( 2, 1, N'صفحه تنظیمات یکپارچه پنل', N'منوی تنظیمات واحد شامل آدرس ساب، MySQL، توکن API و تنظیمات ربات اصلی سیستم.', 1, 20, 1),
        ( 3, 1, N'گزارش فروش روزانه تلگرام', N'ارسال خودکار هر شب ساعت 23:59 گزارش فروش (مبلغ، تعداد، فروش نمایندگان، فروش ربات، حجم مصرفی) به ادمین و نمایندگان از ربات اصلی.', 1, 30, 1),
        ( 4, 1, N'تاریخچه و PDF نماینده', N'فیلتر بازه تاریخ در تاریخچه اشتراک‌های نماینده، کارت‌های خلاصه و دانلود گزارش PDF.', 1, 40, 1),
        ( 5, 1, N'صفحه تغییرات نسخه', N'نمایش تاریخچه تغییرات پنل به تفکیک نسخه برای ادمین و نماینده.', 1, 50, 1),
        ( 6, 1, N'کنترل نسخه فایل‌های JS', N'افزودن پارامتر نسخه پنل به فایل‌های JavaScript برای جلوگیری از کش CDN.', 1, 60, 1),

        -- ── 1.8.0 — Agent ──
        ( 7, 1, N'فیلتر لیست اشتراک‌ها', N'فیلتر تاریخ و مرتب‌سازی اشتراک‌ها به همراه نمایش خلاصه فروش در بازه انتخاب‌شده.', 2, 10, 1),
        ( 8, 1, N'تاریخچه نماینده با فیلتر', N'فیلتر بازه تاریخ در تب تاریخچه پروفایل با نمایش تعداد ساخت، تمدید، مبلغ فروش و فاکتورهای پرداخت‌شده.', 2, 20, 1),
        ( 9, 1, N'خروجی PDF تاریخچه', N'دانلود گزارش PDF از تاریخچه اشتراک‌های ثبت‌شده در پروفایل نماینده.', 2, 30, 1),
        (11, 1, N'اصلاح وضعیت سفارش‌ها', N'نمایش صحیح وضعیت «در انتظار فعال‌سازی» و «در انتظار پرداخت» در سفارش‌ها و بسته‌های رزرو.', 2, 50, 1),

        -- ── 1.7.0 — Admin ──
        (12, 2, N'تنظیمات تسویه بدهی نمایندگان', N'تب تسویه در جزئیات نماینده برای فعال‌سازی تسویه هفتگی/ماهانه، هشدار تلگرام و مسدودسازی خودکار در صورت عدم پرداخت.', 1, 10, 1),
        (13, 2, N'مدیریت فاکتورها', N'بخش پرداخت‌ها به فاکتورها تغییر نام داد. ثبت فاکتور با نمایش بدهی نماینده، پر شدن خودکار مبلغ و تاریخ فعلی.', 1, 20, 1),
        (14, 2, N'تائید فاکتور از لیست', N'امکان تائید فاکتورهای در انتظار پرداخت مستقیماً از جدول فاکتورها.', 1, 30, 1),
        (15, 2, N'محدودیت ویرایش اشتراک', N'ویرایش اشتراک و آیکون مداد فقط برای ادمین (نقش ۱) در دسترس است.', 1, 40, 1),

        -- ── 1.7.0 — Agent ──
        (16, 2, N'فاکتورهای من', N'صفحه فاکتورها برای پیگیری و مشاهده وضعیت فاکتورهای ثبت‌شده توسط نماینده.', 2, 10, 1),
        (17, 2, N'تمدید و رزرو بسته اشتراک', N'امکان رزرو بسته جدید از منوی تمدید اشتراک؛ بسته در زمان مناسب به‌صورت خودکار فعال می‌شود.', 2, 20, 1),
        (18, 2, N'تاریخچه مصرف پیشرفته', N'نمایش تاریخچه مصرف به‌صورت جدول با جستجوی بازه تاریخ، نمایش مجموع مصرف و خروجی PDF.', 2, 30, 1),
        (19, 2, N'ادامه ورود به پنل', N'امکان ادامه ورود به پنل تا ۷ روز بدون وارد کردن مجدد نام کاربری و رمز.', 2, 40, 1),

        -- ── 1.6.0 ──
        (20, 3, N'اعلان تلگرام هنگام ثبت فاکتور', N'ارسال پیام تلگرام به نماینده هنگام ثبت فاکتور با کسر از بدهی یا مبلغ بالاتر از بدهی.', 1, 10, 1),
        (21, 3, N'بهبود تجربه لیست اشتراک‌ها', N'بهبود منوی عملیات اشتراک‌ها شامل QR Code، کپی لینک و تغییر نام اشتراک.', 2, 10, 1);

    SET IDENTITY_INSERT dbo.tbPanelChangelogItems OFF;
END
GO

-- =============================================================================
-- ارتقا به 1.8.0 (اگر قبلاً 1.7.0 نصب شده و seed اولیه اجرا شده)
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.7.0')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.0')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.0', '1405/03/30', 180, 1, 1);

    DECLARE @v18 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.0');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v18, N'فیلتر پیشرفته لیست اشتراک‌ها', N'فیلتر نماینده، بازه تاریخ، مرتب‌سازی بر اساس حجم باقی‌مانده و کارت‌های خلاصه تعداد و مبلغ فروش.', 1, 10, 1),
        (@v18, N'صفحه تنظیمات یکپارچه پنل', N'منوی تنظیمات واحد شامل آدرس ساب، MySQL، توکن API و تنظیمات ربات اصلی سیستم.', 1, 20, 1),
        (@v18, N'گزارش فروش روزانه تلگرام', N'ارسال خودکار هر شب ساعت 23:59 گزارش فروش به ادمین و نمایندگان از ربات اصلی.', 1, 30, 1),
        (@v18, N'تاریخچه و PDF نماینده', N'فیلتر بازه تاریخ در تاریخچه اشتراک‌های نماینده، کارت‌های خلاصه و دانلود گزارش PDF.', 1, 40, 1),
        (@v18, N'صفحه تغییرات نسخه', N'نمایش تاریخچه تغییرات پنل به تفکیک نسخه برای ادمین و نماینده.', 1, 50, 1),
        (@v18, N'کنترل نسخه فایل‌های JS', N'افزودن پارامتر نسخه پنل به فایل‌های JavaScript برای جلوگیری از کش CDN.', 1, 60, 1),
        (@v18, N'فیلتر لیست اشتراک‌ها', N'فیلتر تاریخ و مرتب‌سازی اشتراک‌ها به همراه نمایش خلاصه فروش در بازه انتخاب‌شده.', 2, 10, 1),
        (@v18, N'تاریخچه نماینده با فیلتر', N'فیلتر بازه تاریخ در تب تاریخچه پروفایل با نمایش خلاصه فروش و فاکتورها.', 2, 20, 1),
        (@v18, N'خروجی PDF تاریخچه', N'دانلود گزارش PDF از تاریخچه اشتراک‌های ثبت‌شده در پروفایل نماینده.', 2, 30, 1),
        (@v18, N'اصلاح وضعیت سفارش‌ها', N'نمایش صحیح وضعیت «در انتظار فعال‌سازی» و «در انتظار پرداخت» در سفارش‌ها و بسته‌های رزرو.', 2, 50, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.1 — رفع باگ (بسته‌های رزرو)
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.0')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.1')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.1', '1405/04/01', 181, 1, 1);

    DECLARE @v181 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.1');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v181, N'[Fixed] مدیریت بسته‌های رزرو در اشتراک‌ها', N'امکان فعال‌سازی فوری یا حذف بسته رزرو از مودال «بسته‌های اشتراک»؛ در صورت حذف، مبلغ به حساب شما برگردانده می‌شود.', 2, 10, 1),
        (@v181, N'[Fixed] نمایش بسته رزرو در صفحه اشتراک', N'نمایش بسته‌های رزرو شده در صفحه subscribe کاربر (بر اساس توکن اشتراک).', 2, 20, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.2 — رفع باگ گزارش داشبورد
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.1')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.2')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.2', '1405/04/01', 182, 1, 1);

    DECLARE @v182 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.2');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v182, N'[Fixed] اصلاح اعداد گزارش داشبورد', N'یکسان‌سازی محاسبه فروش هفتگی، میانگین روزانه ماه جاری و نمودار فروش؛ رفع بازه تاریخ هفته گذشته و درصد مقایسه.', 1, 10, 1),
        (@v182, N'[Fixed] نمایش فروش ربات در داشبورد', N'اصلاح پارس تاریخ میلادی (yyyy-MM-dd) خروجی GetBotSales تا تب ربات و درصد عملکرد ماه جاری درست نمایش داده شود.', 1, 20, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.3 — رفع نمایش فروش ربات در داشبورد
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.2')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.3')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.3', '1405/04/01', 183, 1, 1);

    DECLARE @v183 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.3');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v183, N'[Fixed] نمایش فروش ربات در داشبورد', N'اصلاح پارس تاریخ میلادی خروجی GetBotSales تا تب «ربات» و درصد فروش ربات در عملکرد ماه جاری درست نمایش داده شود.', 1, 10, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.4 — تسویه بر اساس آخرین پرداخت
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.3')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.4')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.4', '1405/04/01', 184, 1, 1);

    DECLARE @v184 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.4');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v184, N'[Updated] تنظیمات تسویه بدهی نماینده', N'محاسبه موعد تسویه از آخرین فاکتور پرداخت‌شده؛ هشدار قبل از موعد، یادآوری ۶ ساعته پس از سررسید و مسدودسازی خودکار زیرمجموعه پس از مهلت.', 2, 10, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.5 — مدیریت اشتراک مشترکین تلگرام
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.4')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.5')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.5', '1405/04/01', 185, 1, 1);

    DECLARE @v185 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.5');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v185, N'[New] انتقال اشتراک بین مشترکین', N'امکان انتقال اشتراک ربات تلگرام از یک مشترک به مشترک دیگر از پروفایل مشترک.', 2, 10, 1),
        (@v185, N'[New] حذف اشتراک مشترک', N'حذف اشتراک کاربر از بخش اشتراک‌ها در پروفایل مشترک تلگرام.', 2, 20, 1),
        (@v185, N'[New] لغو بسته تمدیدی', N'لغو بسته‌های رزرو (تمدیدی) از تب سفارشات پروفایل مشترک.', 2, 30, 1),
        (@v185, N'[Fixed] تاریخچه رزرو', N'هنگام حذف بسته رزرو، رکورد مربوطه از تاریخچه نماینده نیز حذف می‌شود.', 2, 40, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.6 — هشدار بسته رزرو در ربات
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.5')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.6')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.6', '1405/04/01', 186, 1, 1);

    DECLARE @v186 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.6');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v186, N'[New] هشدار بسته رزرو در ربات', N'هشدار خودکار به کاربر هنگام کمتر از ۲ و ۱ گیگ حجم یا کمتر از ۲ و ۱ روز زمان باقی‌مانده — فقط اگر بسته رزرو نداشته باشد.', 1, 10, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.7 — رفع باگ و بهبود پنل
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.6')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.7')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.7', '1405/04/04', 187, 1, 1);

    DECLARE @v187 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.7');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v187, N'[Fixed] رفع خطای تائید فاکتور نماینده', N'تائید دستی فاکتور نماینده دیگر از مسیر پیامک کارت‌به‌کارت عبور نمی‌کند و بدون خطای JSON انجام می‌شود.', 1, 10, 1),
        (@v187, N'[Updated] مرتب‌سازی فاکتورهای نماینده', N'جدول فاکتورهای نماینده به‌صورت پیش‌فرض از جدیدترین به قدیمی‌ترین نمایش داده می‌شود.', 1, 20, 1),
        (@v187, N'[Updated] مرتب‌سازی سفارشات ربات', N'جدول سفارشات ربات به‌صورت پیش‌فرض از جدیدترین به قدیمی‌ترین مرتب می‌شود.', 1, 30, 1),
        (@v187, N'[New] مرتب‌سازی و مسدودسازی مشترکین ربات', N'لیست مشترکین تلگرام از جدید به قدیم مرتب می‌شود و امکان مسدود/رفع مسدودیت از پنل اضافه شد.', 1, 40, 1),
        (@v187, N'[New] تخصیص تعرفه به نمایندگان هنگام ویرایش', N'در بخش تعرفه‌ها هنگام ویرایش می‌توانید نمایندگان مجاز را انتخاب کنید.', 1, 50, 1),
        (@v187, N'[Fixed] گزارش روزانه فقط کل سیستم', N'گزارش خودکار عملکرد روزانه فقط یک‌بار برای کل سیستم ارسال می‌شود و گزارش تفکیکی نمایندگان حذف شد.', 1, 60, 1);
END
GO

-- =============================================================================
-- ارتقا به 1.8.8 — pop-up تغییرات نسخه
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.7')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.8')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.8', '1405/04/04', 188, 1, 1);

    DECLARE @v188 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.8');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v188, N'[New] pop-up تغییرات نسخه', N'پس از هر به‌روزرسانی پنل، لیست تغییرات نسخه جدید یک‌بار برای ادمین یا نماینده نمایش داده می‌شود.', 1, 10, 1),
        (@v188, N'[New] pop-up تغییرات نسخه', N'پس از هر به‌روزرسانی پنل، لیست تغییرات نسخه جدید یک‌بار برای ادمین یا نماینده نمایش داده می‌شود.', 2, 10, 1);
END
GO

-- =============================================================================
-- تعمیر tbPclv_IsCurrent — فقط آخرین نسخه فعال باید current باشد
-- =============================================================================
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_IsActive = 1)
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsActive = 1;

    UPDATE v SET tbPclv_IsCurrent = 1
    FROM dbo.tbPanelChangelogVersions v
    INNER JOIN (
        SELECT TOP 1 tbPclv_ID AS Id
        FROM dbo.tbPanelChangelogVersions
        WHERE tbPclv_IsActive = 1
        ORDER BY tbPclv_SortOrder DESC, tbPclv_ID DESC
    ) latest ON v.tbPclv_ID = latest.Id;
END
GO

-- پس از اجرا: App Pool را recycle کنید تا کش نسخه JS به‌روز شود.
