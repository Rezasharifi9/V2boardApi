-- =============================================================================
-- ارتقا به 1.9.3 (1005) — نسخه و لینک دانلود اپلیکیشن
-- پیش‌نیاز: 1.9.2 (1004)
--
-- قالب نسخه‌بندی: MAJOR.MINOR.PATCH (BUILD)
--   1.9.3 = نسخه معنایی (SemVer)   |   (1005) = شماره بیلد یکنواخت افزایشی
--
-- این اسکریپت را فقط یک‌بار اجرا کنید. جدول tbAppRelease را هم بسازید:
--   Database/AddAppRelease.sql
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.2 (1004)')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.3 (1005)')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.9.3 (1005)', '1405/06/07', 195, 1, 1);

    DECLARE @v1005 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.3 (1005)');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v1005, N'[Added] پیکربندی نسخه اپلیکیشن', N'در منوی پیکربندی، صفحهٔ نسخه اپلیکیشن برای ورود لینک دانلود، نسخه، تغییرات و نصب اجباری اضافه شد.', 1, 10, 1),
        (@v1005, N'[Added] API نسخه اپلیکیشن برای کلاینت', N'اندپوینت GET /User/GetAppRelease نسخه، لینک دانلود، متن تغییرات و پرچم نصب اجباری را به اپلیکیشن می‌دهد.', 1, 20, 1);
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
