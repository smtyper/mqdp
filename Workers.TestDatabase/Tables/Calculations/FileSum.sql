CREATE TABLE [Calculations].[FileSum]
(
    [FileName] NVARCHAR(100) NOT NULL,
    [Sum] DECIMAL(18, 2) NOT NULL,
    [ChangeDate] DATETIME2 NOT NULL,
    CONSTRAINT [PK_FileSum] PRIMARY KEY ([FileName]),
    INDEX [IX_FileName] ([FileName])
)
