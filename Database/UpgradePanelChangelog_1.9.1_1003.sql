-- =============================================================================
-- ارتقا به 1.9.1 (1003) — کاربران موبایل و فاکتورهای اپلیکیشن
-- پیش‌نیاز: 1.9.0 (1002)
--
-- ⚠️ قبل از این اسکریپت، Database/AddMobileUsersTable.sql را اجرا کنید.
--
-- قالب نسخه‌بندی: MAJOR.MINOR.PATCH (BUILD)
--   1.9.1 = نسخه معنایی (SemVer)   |   (1003) = شماره بیلد یکنواخت افزایشی
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbMobileUsers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    RAISERROR (N'جدول tbMobileUsers وجود ندارد. ابتدا Database/AddMobileUsersTable.sql را اجرا کنید.', 16, 1);
END
GO

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0 (1002)')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.1 (1003)')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.9.1 (1003)', '1405/05/14', 193, 1, 1);

    DECLARE @v1003 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.1 (1003)');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        -- ── ادمین ──
        (@v1003, N'[New] بخش کاربران موبایل', N'لیست کامل گوشی‌هایی که اپلیکیشن روی آن‌ها نصب شده، همراه با نماینده هر دستگاه، نسخه برنامه، وضعیت نوتیفیکیشن و تعداد فاکتورها. فعلا فقط برای ادمین.', 1, 10, 1),
        (@v1003, N'[New] روش پرداخت «اپلیکیشن»', N'روش پرداخت APP با شناسه ۵ به سیستم اضافه شد. فاکتورهای ساخته‌شده از داخل برنامه با همین روش ثبت می‌شوند و در صفحه فاکتورها قابل تفکیک هستند.', 1, 20, 1),
        (@v1003, N'[New] نمایش فاکتورهای موبایل در پنل', N'فاکتورهای ساخته‌شده از داخل اپلیکیشن — که کاربر تلگرام ندارند — حالا در صفحه فاکتورها دیده و از همان‌جا تائید می‌شوند.', 1, 30, 1),
        (@v1003, N'[Changed] تائید فاکتور اپلیکیشن بدون ربات', N'تائید این فاکتورها اشتراک را می‌سازد یا تمدید می‌کند ولی هیچ پیامی به ربات تاییدیه‌ها فرستاده نمی‌شود.', 1, 40, 1),
        (@v1003, N'[New] ثبت خودکار دستگاه در اولین اجرا', N'اپلیکیشن در اولین اجرا مشخصات گوشی و توکن نوتیفیکیشن را به پنل می‌فرستد و دستگاه زیرمجموعه نماینده همان بیلد ثبت می‌شود.', 1, 50, 1),
        (@v1003, N'[Fixed] پاک‌سازی فاکتورهای منقضی اپلیکیشن', N'فاکتورهای پرداخت‌نشده اپلیکیشن هم مثل کارت‌به‌کارت پس از مهلت مقرر پاک می‌شوند.', 1, 60, 1),
        -- ── نماینده ──
        (@v1003, N'[New] فاکتورهای اپلیکیشن در صفحه فاکتورها', N'خریدهایی که مشتریان از داخل برنامه انجام می‌دهند در صفحه فاکتورها با روش پرداخت «اپلیکیشن» نمایش داده می‌شوند و از همان‌جا قابل تائید هستند.', 2, 10, 1),
        (@v1003, N'[Changed] تائید بدون پیام ربات', N'تائید فاکتور اپلیکیشن اشتراک را می‌سازد یا تمدید می‌کند؛ مشتری نتیجه را داخل خود برنامه می‌بیند و پیامی از ربات دریافت نمی‌کند.', 2, 20, 1);
END
GO

-- تعمیر tbPclv_IsCurrent — فقط آخرین نسخه فعال باید current باشد
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

-- پس از اجرا: App Pool را recycle کنید تا کش نسخه و کش JS به‌روز شود.
