-- لاگ جزئیات ارسال هشدار تلگرام (نماینده / ادمین)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbAlertSendLogs' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbAlertSendLogs
    (
        tbAsl_ID             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FK_User_ID           INT NULL,
        tbAsl_RecipientName  NVARCHAR(200) NOT NULL,
        tbAsl_ChatId         NVARCHAR(50) NULL,
        tbAsl_AlertType      NVARCHAR(200) NOT NULL,
        tbAsl_Message        NVARCHAR(MAX) NOT NULL,
        tbAsl_SentAt         DATETIME NOT NULL CONSTRAINT DF_tbAlertSendLogs_SentAt DEFAULT (GETDATE()),
        tbAsl_IsSuccess      BIT NOT NULL CONSTRAINT DF_tbAlertSendLogs_IsSuccess DEFAULT (0),
        tbAsl_Error          NVARCHAR(500) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbAlertSendLogs_SentAt')
BEGIN
    CREATE INDEX IX_tbAlertSendLogs_SentAt
        ON dbo.tbAlertSendLogs(tbAsl_SentAt DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbAlertSendLogs_FK_User_ID')
BEGIN
    CREATE INDEX IX_tbAlertSendLogs_FK_User_ID
        ON dbo.tbAlertSendLogs(FK_User_ID);
END
GO
