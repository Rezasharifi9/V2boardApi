-- تعمیر نسخه فعلی changelog — اجرا اگر badge یا صفحه تغییرات نسخه قدیمی نشان می‌دهد
-- فقط یک نسخه (بالاترین SortOrder فعال) باید tbPclv_IsCurrent = 1 داشته باشد

UPDATE dbo.tbPanelChangelogVersions
SET tbPclv_IsCurrent = 0
WHERE tbPclv_IsActive = 1;
GO

DECLARE @latestId INT = (
    SELECT TOP 1 tbPclv_ID
    FROM dbo.tbPanelChangelogVersions
    WHERE tbPclv_IsActive = 1
    ORDER BY tbPclv_SortOrder DESC, tbPclv_ID DESC
);

IF @latestId IS NOT NULL
BEGIN
    UPDATE dbo.tbPanelChangelogVersions
    SET tbPclv_IsCurrent = 1
    WHERE tbPclv_ID = @latestId;
END
GO

-- نمایش وضعیت فعلی (برای بررسی)
SELECT tbPclv_Version, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive
FROM dbo.tbPanelChangelogVersions
ORDER BY tbPclv_SortOrder DESC;
GO
