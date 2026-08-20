-- =============================================================================
-- ارتقا به 1.9.0 (1001) — تاییدیه مسدودسازی نماینده در ربات + هشدارهای تسویه در اعلانات پنل
-- پیش‌نیاز: 1.9.0
--
-- قالب نسخه‌بندی: MAJOR.MINOR.PATCH (BUILD)
--   1.9.0 = نسخه معنایی (SemVer)   |   (1001) = شماره بیلد یکنواخت افزایشی
-- پیش‌نیاز اسکریپت‌های اسکیمای این نسخه:
--   Database/AddSettlementBlockApprovalColumns.sql
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0 (1001)')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.9.0 (1001)', '1405/04/24', 191, 1, 1);

    DECLARE @v1001 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0 (1001)');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v1001, N'[New] تاییدیه مسدودسازی نماینده در ربات', N'هنگام رسیدن موعد مسدودسازی، به‌جای مسدودسازی خودکار، پیام تاییدیه با مشخصات نماینده (نام، نام کاربری، مقدار بدهی، تاریخ آخرین فاکتور پرداخت‌شده و مدت بدون پرداخت) به همراه دکمه تایید/رد به ربات ادمین ارسال می‌شود. مسدودسازی فقط پس از تایید ادمین انجام می‌گیرد و در صورت عدم تصمیم یا رد، هر ۲ روز یک‌بار یادآوری می‌شود.', 1, 10, 1),
        (@v1001, N'[New] هشدارهای تسویه در اعلانات پنل', N'برای هر نماینده هشدار «قبل از موعد تسویه» و «قبل از مسدودسازی» علاوه بر تلگرام، در بخش اعلانات پنل نیز ثبت می‌شود.', 1, 20, 1),
        (@v1001, N'[Fixed] ارسال پیام‌های اطلاع‌رسانی بدون parseMode', N'رفع مشکل ارسال پیام‌های هشدار تلگرام که بدون تعیین حالت parseMode ارسال می‌شدند.', 1, 30, 1),
        (@v1001, N'[New] هشدار تسویه در پنل', N'دریافت هشدار قبل از موعد تسویه و قبل از مسدودسازی اشتراک‌ها در بخش اعلانات پنل.', 2, 10, 1);
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
