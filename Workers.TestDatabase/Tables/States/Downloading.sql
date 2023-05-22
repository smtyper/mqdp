CREATE TABLE [States].[Downloading]
(
    [FileName] NVARCHAR(100) NOT NULL,
    [DataDate] DATETIME2 NOT NULL,
    CONSTRAINT [PK_Downloading] PRIMARY KEY ([FileName]),
    INDEX [IX_Key] ([FileName]),
    INDEX [IX_Value] ([DataDate])
)
