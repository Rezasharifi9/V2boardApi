-- =============================================================================
-- ارتقا به 1.9.0 (1002) — رفع باگ برخی کاربران + بهبود صفحه فاکتورها و تنظیمات
-- پیش‌نیاز: 1.9.0 (1001)
--
-- قالب نسخه‌بندی: MAJOR.MINOR.PATCH (BUILD)
--   1.9.0 = نسخه معنایی (SemVer)   |   (1002) = شماره بیلد یکنواخت افزایشی
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0 (1001)')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0 (1002)')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.9.0 (1002)', '1405/05/03', 192, 1, 1);

    DECLARE @v1002 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0 (1002)');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        -- ── ادمین ──
        (@v1002, N'[Fixed] رفع باگ‌های گزارش‌شده برخی کاربران', N'رفع کرش ربات هنگام حذف پیام منقضی (قدیمی‌تر از ۴۸ ساعت)، رفع خطای دریافت رسید بدون فاکتور، و رفع خطای ورود با فرم خالی به پنل.', 1, 10, 1),
        (@v1002, N'[New] فیلتر پیشرفته فاکتورها', N'افزودن فیلتر کاربر، شماره پیگیری، بازه مبلغ و بازه تاریخ در صفحه فاکتورها برای جست‌وجوی آسان.', 1, 20, 1),
        (@v1002, N'[New] یوزرنیم تلگرام ادمین در تنظیمات', N'امکان تنظیم آیدی پشتیبانی (AdminUsername) ادمین از صفحه تنظیمات پنل.', 1, 30, 1),
        (@v1002, N'[Fixed] ارسال هشدارها و اعلانات تسویه', N'رفع مشکل ارسال نشدن پیام‌های تلگرام تسویه و ثبت نشدن اعلان‌های تسویه در پنل نماینده.', 1, 40, 1),
        -- ── نماینده ──
        (@v1002, N'[New] فیلتر فاکتورها', N'فیلتر آسان فاکتورها بر اساس کاربر، شماره پیگیری، مبلغ و تاریخ.', 2, 10, 1),
        (@v1002, N'[Fixed] هشدارهای تسویه در پنل', N'دریافت درست هشدارهای «قبل از موعد»، «سررسید»، «پس از سررسید» و «قبل از مسدودسازی» در بخش اعلانات پنل.', 2, 20, 1);
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

-- پس از اجرا: App Pool را recycle کنید تا کش نسخه JS به‌روز شود.
