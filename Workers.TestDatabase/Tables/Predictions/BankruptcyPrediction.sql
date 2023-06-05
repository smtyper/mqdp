CREATE TABLE [Predictions].[BankruptcyPrediction]
(
    [HashId] UNIQUEIDENTIFIER NOT NULL,
    [ParentHashId] UNIQUEIDENTIFIER NOT NULL,
    [Model] NVARCHAR(20) NOT NULL,
    [Score] DECIMAL(18, 2) NOT NULL,
    [Probability] NVARCHAR(6) NOT NULL,
    [ChangeDate] DATETIME2 NOT NULL,
    CONSTRAINT [PK_BankruptcyPrediction] PRIMARY KEY ([HashId]),
    CONSTRAINT [FK_BankruptcyPrediction_Report] FOREIGN KEY ([ParentHashId]) REFERENCES [Rosstat].[Report] ([HashId]),
    INDEX [IX_HashId] ([HashId]),
    INDEX [IX_ParentHashId] ([ParentHashId])
)
