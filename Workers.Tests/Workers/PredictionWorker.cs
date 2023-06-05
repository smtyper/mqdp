using Microsoft.Extensions.Options;
using Workers.Tests.Databases;
using Workers.Tests.Models.Predictions;
using Workers.Tests.Models.Rosstat;
using Workers.Tests.Workers.Channels;

namespace Workers.Tests.Workers;

internal class PredictionWorker : EternalWorker<PredictionWorkItem, PredictionChannel, PredictionSettings>
{
    private readonly TestDatabase _testDatabase;

    public PredictionWorker(PredictionChannel channel, IOptions<PredictionSettings> options,
        ILogger<PredictionWorker> logger, TestDatabase testDatabase) : base(channel, options, logger) =>
        _testDatabase = testDatabase;

    protected override async Task ProcessWorkItemAsync(PredictionWorkItem workItem, CancellationToken cancellationToken)
    {
        var (hashId, _) = workItem;

        var report = await _testDatabase.GetRosstatReportAsync(hashId, cancellationToken);
        var periodApprovedYear = GetPeriodApprovedYear(report.Period);

        var predictions = GetPredictions(report, periodApprovedYear);
        await _testDatabase.InsertBankruptcyPredictionsAsync(predictions, cancellationToken);
    }

    private static IReadOnlyCollection<BankruptcyPrediction> GetPredictions(Report report, int periodApprovedYear)
    {
        decimal? GetValue(int key, bool forPreviousYear = false) => GetReportValue(report.ValuesDictionary,
            key.ToString(), periodApprovedYear, forPreviousYear);

        decimal? TruncateValue(decimal? value) => value is null ?
            null :
            Math.Truncate(value.Value * 100) / 100;

        BankruptcyPrediction? GenerateAltmanModelPrediction()
        {

            var k1 = (GetValue(1200) - GetValue(1500)) / GetValue(1600);
            var k2 = GetValue(2400) / GetValue(1600);
            var k3 = GetValue(2300) / GetValue(1600);
            var k4 = GetValue(1300) / (GetValue(1400) + GetValue(1500));

            var z = TruncateValue((6.56M * k1) + (3.26M * k2) + (6.72M * k3) + (1.05M * k4));

            var prediction = z is not null ?
                new BankruptcyPrediction
                {
                    ParentHashId = report.HashId!.Value,
                    Model = BankruptcyPredictionModel.Altman,
                    Score = z.Value,
                    Probability = z switch
                    {
                        <= 1.1M => BankruptcyProbability.High,
                        > 1.1M and < 2.6M => BankruptcyProbability.Medium,
                        >= 2.6M => BankruptcyProbability.Low,
                        _ => throw new ScoreOutOfRangeException()
                    }
                }.WithHashId() :
                null;

            return prediction;
        }

        BankruptcyPrediction? GenerateTafflerModelPrediction()
        {
            var k1 = GetValue(2200) / GetValue(1500);
            var k2 = GetValue(1200) / (GetValue(1400) + GetValue(1500));
            var k3 = GetValue(1500) / GetValue(1600);
            var k4 = GetValue(2110) / GetValue(1600);

            var z = TruncateValue((0.53M * k1) + (0.13M * k2) + (0.18M * k3) + (0.16M * k4));

            var prediction = z is not null ?
                new BankruptcyPrediction
                {
                    ParentHashId = report.HashId!.Value,
                    Model = BankruptcyPredictionModel.Taffler,
                    Score = z.Value,
                    Probability = z switch
                    {
                        > 0.3M => BankruptcyProbability.Low,
                        < 0.2M => BankruptcyProbability.High,
                        _ => throw new ScoreOutOfRangeException()
                    }
                }.WithHashId() :
                null;

            return prediction;
        }

        BankruptcyPrediction? GenerateFulmerModelPrediction()
        {
            var k1 = GetValue(1370) / GetValue(1600);
            var k2 = GetValue(2110) / GetValue(1600);
            var k3 = GetValue(2300) / GetValue(1300);
            var k4 = GetValue(1250) / (GetValue(1400) + GetValue(1500));
            var k5 = GetValue(1400) / GetValue(1600);
            var k6 = GetValue(1500) / GetValue(1600);
            var k7 = GetValue(1600);
            var k8 = GetValue(1200) / (GetValue(1400) + GetValue(1500));
            var k9 = GetValue(2300) / GetValue(2330);

            var h = TruncateValue((5.528M * k1) + (0.212M * k2) + (0.073M * k3) + (1.270M * k4) - (0.120M * k5) +
                (2.335M * k6) + (0.575M * k7) + (1.083M * k8) + (0.894M * k9) - 6.075M);
            var prediction = h is not null ?
                new BankruptcyPrediction
                {
                    ParentHashId = report.HashId!.Value,
                    Model = BankruptcyPredictionModel.Fulmer,
                    Score = h.Value,
                    Probability = h switch
                    {
                        < 0 => BankruptcyProbability.High,
                        > 0 => BankruptcyProbability.Low,
                        _ => throw new ScoreOutOfRangeException()
                    }
                }.WithHashId() :
                null;

            return prediction;
        }

        var predictions = new[]
            {
                GenerateAltmanModelPrediction,
                GenerateTafflerModelPrediction,
                GenerateFulmerModelPrediction
            }
            .Select(generateFunction =>
            {
                try
                {
                    return generateFunction();
                }
                catch (Exception exception) when (exception is DivideByZeroException or ScoreOutOfRangeException)
                {
                    return null;
                }
            })
            .Where(prediction => prediction is not null)
            .ToArray();

        return predictions!;
    }

    private static decimal? GetReportValue(IReadOnlyDictionary<string, long> values, string key, int periodApprovedYear,
        bool forPreviousYear)
    {
        var form = key[0];

        if (form is not ('1' or '2' or '4' or '6'))
            throw new ArgumentOutOfRangeException(nameof(key));

        if (forPreviousYear && form is not ('1' or '2'))
            throw new ArgumentException("Value for previous year is not supported for this form",
                nameof(forPreviousYear));

        var code =
            $"{(periodApprovedYear is not 2012 ? MapCodeToLegacy(key, values) : key)}{(forPreviousYear ? 4 : 3)}";
        var value = values.TryGetValue(code, out var result) ?
            result :
            0;

        return value;
    }

    private static string MapCodeToLegacy(string code, IReadOnlyDictionary<string, long> sourceValues) =>
        code switch
        {
            "2110" => "2010",
            "2400" => "2190",
            "1230" => sourceValues.ContainsKey("1230") ? "1230" : "1240",
            "1520" => "1620",
            "1150" => "1120",
            "1600" => "1300",
            _ => code
        };

    private static int GetPeriodApprovedYear(int reportYear) => reportYear switch
    {
        < 2011 => 2003,
        2011 => 2011,
        _ => 2012
    };

    private class ScoreOutOfRangeException : Exception
    {
    }
}

public record PredictionWorkItem(Guid HashId, DateTime ChangeDate) : WorkItem<PredictionWorkItem>
{
    public override bool AreEqualByValue(PredictionWorkItem workItem) => ChangeDate == workItem.ChangeDate;

    public override PredictionWorkItem WithMinimalValue() => this with { ChangeDate = DateTime.MinValue };

    public override string ToString() => $"{HashId}";
}

internal class PredictionSettings : WorkerSettings
{
}
