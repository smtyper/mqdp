CREATE TABLE [States].[Parsing]
(
    [FileName] NVARCHAR(100) NOT NULL,
    [DataDate] DATETIME2 NOT NULL,
    [IsInProcessing] BIT NOT NULL,
    CONSTRAINT [PK_Parsing] PRIMARY KEY ([FileName]),
    INDEX [IX_Key] ([FileName]),
    INDEX [IX_Value] ([DataDate])
)
