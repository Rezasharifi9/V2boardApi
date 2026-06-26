-- اصلاح audience: ویژگی‌های ربات و داشبورد فقط برای ادمین (tbPcli_Audience = 1)

UPDATE dbo.tbPanelChangelogItems
SET tbPcli_Audience = 1
WHERE tbPcli_IsActive = 1
  AND tbPcli_Title IN (
        N'[Fixed] اصلاح اعداد گزارش داشبورد',
        N'[Fixed] نمایش فروش ربات در داشبورد',
        N'[New] هشدار بسته رزرو در ربات',
        N'گزارش فروش روزانه تلگرام',
        N'گزارش فروش روزانه در تلگرام'
  );
GO

-- نسخه تکراری گزارش تلگرام مخصوص نماینده غیرفعال شود
UPDATE dbo.tbPanelChangelogItems
SET tbPcli_IsActive = 0
WHERE tbPcli_Audience = 2
  AND tbPcli_Title = N'گزارش فروش روزانه در تلگرام';
GO

SELECT tbPcli_Title, tbPcli_Audience, tbPcli_IsActive, v.tbPclv_Version
FROM dbo.tbPanelChangelogItems i
INNER JOIN dbo.tbPanelChangelogVersions v ON v.tbPclv_ID = i.FK_Version_ID
WHERE tbPcli_Title IN (
        N'[Fixed] اصلاح اعداد گزارش داشبورد',
        N'[Fixed] نمایش فروش ربات در داشبورد',
        N'[New] هشدار بسته رزرو در ربات',
        N'گزارش فروش روزانه تلگرام',
        N'گزارش فروش روزانه در تلگرام'
)
ORDER BY v.tbPclv_SortOrder DESC, i.tbPcli_SortOrder;
GO
