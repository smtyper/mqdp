CREATE TABLE [Rosstat].[Report]
(
    [HashId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(1000) NULL,
    [Okpo] NVARCHAR(8) NULL,
    [Okopf] NVARCHAR(5) NULL,
    [Okfs] NVARCHAR(2) NULL,
    [Okved] NVARCHAR(8) NULL,
    [Inn] NVARCHAR(100) NULL,
    [ChangeDate] DATETIME2(7) NOT NULL,
    [Type] NVARCHAR(100) NULL,
    [Period] INT NOT NULL,
    [DataDate] DATE NOT NULL,
    [Values] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_Report] PRIMARY KEY ([HashId]),
    INDEX [IX_HashId] ([HashId]),
    INDEX [IX_ChangeDate] ([ChangeDate])
)
