-- =============================================================================
-- ردیابی مشاهده pop-up تغییرات نسخه — یک‌بار برای هر کاربر در هر نسخه
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbPanelChangelogSeen' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbPanelChangelogSeen
    (
        tbPcls_ID       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FK_User_ID      INT NOT NULL,
        tbPcls_Version  VARCHAR(20) NOT NULL,
        tbPcls_SeenAt   DATETIME NOT NULL CONSTRAINT DF_tbPanelChangelogSeen_SeenAt DEFAULT (GETDATE()),
        CONSTRAINT UQ_tbPanelChangelogSeen_User_Version UNIQUE (FK_User_ID, tbPcls_Version)
    );

    CREATE INDEX IX_tbPanelChangelogSeen_FK_User_ID
        ON dbo.tbPanelChangelogSeen(FK_User_ID, tbPcls_Version);
END
GO
