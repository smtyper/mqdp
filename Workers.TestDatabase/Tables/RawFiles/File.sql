CREATE TABLE [RawFiles].[File]
(
    [FileName] NVARCHAR(100) NOT NULL,
    [DataDate] DATETIME2 NOT NULL,
    CONSTRAINT [PK_File] PRIMARY KEY ([FileName]),
    INDEX [IX_FileName] ([FileName])
)
