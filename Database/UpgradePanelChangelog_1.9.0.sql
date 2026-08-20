-- =============================================================================
-- ارتقا به 1.9.0 — رفع باگ‌های گزارش‌شده
-- پیش‌نیاز: 1.8.9
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.9')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.9.0', '1405/04/15', 190, 1, 1);

    DECLARE @v190 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.0');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (@v190, N'[Fixed] رفع باگ‌های گزارش‌شده', N'رفع باگ‌های گزارش‌شده توسط کاربران.', 1, 10, 1),
        (@v190, N'[Fixed] رفع باگ‌های گزارش‌شده', N'رفع باگ‌های گزارش‌شده توسط کاربران.', 2, 10, 1);
END
GO

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
