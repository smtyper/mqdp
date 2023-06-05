﻿namespace Workers.Tests.Models.States;

internal record ParsingState
{
    public required string RegistryId { get; init; }

    public required string FileName { get; init; }

    public DateTime ChangeDate { get; init; }

    public bool IsInProcessing { get; init; }
}
