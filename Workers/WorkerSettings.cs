using System;
using System.ComponentModel.DataAnnotations;

namespace Workers;

public abstract class WorkerSettings
{
    private const string TimeSpanMaxValueString = "10675199.02:48:05.4775807";

    [Required]
    public bool IsEnabled { get; internal set; } = true;

    [Required]
    [Range(1, int.MaxValue)]
    public int ConcurrentTaskCount { get; set; } = 1;

    [Required]
    [Range(typeof(TimeSpan), "0", TimeSpanMaxValueString)]
    public TimeSpan IdleTime { get; set; } = TimeSpan.FromHours(12);

    [Required]
    public string WorkItemFetchGroup { get; set; } = "Default";
}
