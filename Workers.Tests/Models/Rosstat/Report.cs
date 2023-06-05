using System.Text.Json;
using CsvHelper.Configuration;
using Reputation.Data.Processing.Models;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Workers.Tests.Models.Rosstat;

public class Report : HashableObject
{
    public string? Name { get; init; }

    public string? Okpo { get; init; }

    public string? Okopf { get; init; }

    public string? Okfs { get; init; }

    public string? Okved { get; init; }

    public string? Inn { get; init; }

    public ReportType? Type { get; init; }

    public int Period { get; init; }

    public DateTime DataDate { get; init; }

    public IReadOnlyDictionary<string, long> ValuesDictionary { get; set; } = null!;

    [ExcludedFromHash]
    public string Values { get; set; } = null!;

    [ExcludedFromHash]
    public DateTime ChangeDate { get; set; }
}

public sealed class ReportMap : ClassMap<Report>
{
    public ReportMap()
    {
        Map(report => report.Name).Index(0);
        Map(report => report.Okpo).Index(1);
        Map(report => report.Okopf).Index(2);
        Map(report => report.Okfs).Index(3);
        Map(report => report.Okved).Index(4);
        Map(report => report.Inn).Index(5);
        Map(report => report.Type).Convert(args => args.Row[7] switch
        {
            "0" => ReportType.FullNonProfit,
            "1" => ReportType.Simplified,
            "2" => ReportType.Full,
            _ => throw new ArgumentOutOfRangeException()
        });
        Map(report => report.ValuesDictionary).Convert(args =>
        {
            var measureCode = args.Row[6];
            var measure = measureCode switch
            {
                "383" => 1,
                "384" => 1000,
                "385" => 1000000,
                "386" => 1000000000,
                "387" => 1000000000000,
                _ => throw new ArgumentOutOfRangeException(nameof(measureCode))
            };

            var values = new Dictionary<string, long>
            {
                { "11103", measure * long.Parse(args.Row[8]) },
                { "11104", measure * long.Parse(args.Row[9]) },
                { "11203", measure * long.Parse(args.Row[10]) },
                { "11204", measure * long.Parse(args.Row[11]) },
                { "11303", measure * long.Parse(args.Row[12]) },
                { "11304", measure * long.Parse(args.Row[13]) },
                { "11403", measure * long.Parse(args.Row[14]) },
                { "11404", measure * long.Parse(args.Row[15]) },
                { "11503", measure * long.Parse(args.Row[16]) },
                { "11504", measure * long.Parse(args.Row[17]) },
                { "11603", measure * long.Parse(args.Row[18]) },
                { "11604", measure * long.Parse(args.Row[19]) },
                { "11703", measure * long.Parse(args.Row[20]) },
                { "11704", measure * long.Parse(args.Row[21]) },
                { "11803", measure * long.Parse(args.Row[22]) },
                { "11804", measure * long.Parse(args.Row[23]) },
                { "11903", measure * long.Parse(args.Row[24]) },
                { "11904", measure * long.Parse(args.Row[25]) },
                { "11003", measure * long.Parse(args.Row[26]) },
                { "11004", measure * long.Parse(args.Row[27]) },
                { "12103", measure * long.Parse(args.Row[28]) },
                { "12104", measure * long.Parse(args.Row[29]) },
                { "12203", measure * long.Parse(args.Row[30]) },
                { "12204", measure * long.Parse(args.Row[31]) },
                { "12303", measure * long.Parse(args.Row[32]) },
                { "12304", measure * long.Parse(args.Row[33]) },
                { "12403", measure * long.Parse(args.Row[34]) },
                { "12404", measure * long.Parse(args.Row[35]) },
                { "12503", measure * long.Parse(args.Row[36]) },
                { "12504", measure * long.Parse(args.Row[37]) },
                { "12603", measure * long.Parse(args.Row[38]) },
                { "12604", measure * long.Parse(args.Row[39]) },
                { "12003", measure * long.Parse(args.Row[40]) },
                { "12004", measure * long.Parse(args.Row[41]) },
                { "16003", measure * long.Parse(args.Row[42]) },
                { "16004", measure * long.Parse(args.Row[43]) },
                { "13103", measure * long.Parse(args.Row[44]) },
                { "13104", measure * long.Parse(args.Row[45]) },
                { "13203", measure * long.Parse(args.Row[46]) },
                { "13204", measure * long.Parse(args.Row[47]) },
                { "13403", measure * long.Parse(args.Row[48]) },
                { "13404", measure * long.Parse(args.Row[49]) },
                { "13503", measure * long.Parse(args.Row[50]) },
                { "13504", measure * long.Parse(args.Row[51]) },
                { "13603", measure * long.Parse(args.Row[52]) },
                { "13604", measure * long.Parse(args.Row[53]) },
                { "13703", measure * long.Parse(args.Row[54]) },
                { "13704", measure * long.Parse(args.Row[55]) },
                { "13003", measure * long.Parse(args.Row[56]) },
                { "13004", measure * long.Parse(args.Row[57]) },
                { "14103", measure * long.Parse(args.Row[58]) },
                { "14104", measure * long.Parse(args.Row[59]) },
                { "14203", measure * long.Parse(args.Row[60]) },
                { "14204", measure * long.Parse(args.Row[61]) },
                { "14303", measure * long.Parse(args.Row[62]) },
                { "14304", measure * long.Parse(args.Row[63]) },
                { "14503", measure * long.Parse(args.Row[64]) },
                { "14504", measure * long.Parse(args.Row[65]) },
                { "14003", measure * long.Parse(args.Row[66]) },
                { "14004", measure * long.Parse(args.Row[67]) },
                { "15103", measure * long.Parse(args.Row[68]) },
                { "15104", measure * long.Parse(args.Row[69]) },
                { "15203", measure * long.Parse(args.Row[70]) },
                { "15204", measure * long.Parse(args.Row[71]) },
                { "15303", measure * long.Parse(args.Row[72]) },
                { "15304", measure * long.Parse(args.Row[73]) },
                { "15403", measure * long.Parse(args.Row[74]) },
                { "15404", measure * long.Parse(args.Row[75]) },
                { "15503", measure * long.Parse(args.Row[76]) },
                { "15504", measure * long.Parse(args.Row[77]) },
                { "15003", measure * long.Parse(args.Row[78]) },
                { "15004", measure * long.Parse(args.Row[79]) },
                { "17003", measure * long.Parse(args.Row[80]) },
                { "17004", measure * long.Parse(args.Row[81]) },
                { "21103", measure * long.Parse(args.Row[82]) },
                { "21104", measure * long.Parse(args.Row[83]) },
                { "21203", measure * long.Parse(args.Row[84]) },
                { "21204", measure * long.Parse(args.Row[85]) },
                { "21003", measure * long.Parse(args.Row[86]) },
                { "21004", measure * long.Parse(args.Row[87]) },
                { "22103", measure * long.Parse(args.Row[88]) },
                { "22104", measure * long.Parse(args.Row[89]) },
                { "22203", measure * long.Parse(args.Row[90]) },
                { "22204", measure * long.Parse(args.Row[91]) },
                { "22003", measure * long.Parse(args.Row[92]) },
                { "22004", measure * long.Parse(args.Row[93]) },
                { "23103", measure * long.Parse(args.Row[94]) },
                { "23104", measure * long.Parse(args.Row[95]) },
                { "23203", measure * long.Parse(args.Row[96]) },
                { "23204", measure * long.Parse(args.Row[97]) },
                { "23303", measure * long.Parse(args.Row[98]) },
                { "23304", measure * long.Parse(args.Row[99]) },
                { "23403", measure * long.Parse(args.Row[100]) },
                { "23404", measure * long.Parse(args.Row[101]) },
                { "23503", measure * long.Parse(args.Row[102]) },
                { "23504", measure * long.Parse(args.Row[103]) },
                { "23003", measure * long.Parse(args.Row[104]) },
                { "23004", measure * long.Parse(args.Row[105]) },
                { "24103", measure * long.Parse(args.Row[106]) },
                { "24104", measure * long.Parse(args.Row[107]) },
                { "24213", measure * long.Parse(args.Row[108]) },
                { "24214", measure * long.Parse(args.Row[109]) },
                { "24303", measure * long.Parse(args.Row[110]) },
                { "24304", measure * long.Parse(args.Row[111]) },
                { "24503", measure * long.Parse(args.Row[112]) },
                { "24504", measure * long.Parse(args.Row[113]) },
                { "24603", measure * long.Parse(args.Row[114]) },
                { "24604", measure * long.Parse(args.Row[115]) },
                { "24003", measure * long.Parse(args.Row[116]) },
                { "24004", measure * long.Parse(args.Row[117]) },
                { "25103", measure * long.Parse(args.Row[118]) },
                { "25104", measure * long.Parse(args.Row[119]) },
                { "25203", measure * long.Parse(args.Row[120]) },
                { "25204", measure * long.Parse(args.Row[121]) },
                { "25003", measure * long.Parse(args.Row[122]) },
                { "25004", measure * long.Parse(args.Row[123]) },
                { "32003", measure * long.Parse(args.Row[124]) },
                { "32004", measure * long.Parse(args.Row[125]) },
                { "32005", measure * long.Parse(args.Row[126]) },
                { "32006", measure * long.Parse(args.Row[127]) },
                { "32007", measure * long.Parse(args.Row[128]) },
                { "32008", measure * long.Parse(args.Row[129]) },
                { "33103", measure * long.Parse(args.Row[130]) },
                { "33104", measure * long.Parse(args.Row[131]) },
                { "33105", measure * long.Parse(args.Row[132]) },
                { "33106", measure * long.Parse(args.Row[133]) },
                { "33107", measure * long.Parse(args.Row[134]) },
                { "33108", measure * long.Parse(args.Row[135]) },
                { "33117", measure * long.Parse(args.Row[136]) },
                { "33118", measure * long.Parse(args.Row[137]) },
                { "33125", measure * long.Parse(args.Row[138]) },
                { "33127", measure * long.Parse(args.Row[139]) },
                { "33128", measure * long.Parse(args.Row[140]) },
                { "33135", measure * long.Parse(args.Row[141]) },
                { "33137", measure * long.Parse(args.Row[142]) },
                { "33138", measure * long.Parse(args.Row[143]) },
                { "33143", measure * long.Parse(args.Row[144]) },
                { "33144", measure * long.Parse(args.Row[145]) },
                { "33145", measure * long.Parse(args.Row[146]) },
                { "33148", measure * long.Parse(args.Row[147]) },
                { "33153", measure * long.Parse(args.Row[148]) },
                { "33154", measure * long.Parse(args.Row[149]) },
                { "33155", measure * long.Parse(args.Row[150]) },
                { "33157", measure * long.Parse(args.Row[151]) },
                { "33163", measure * long.Parse(args.Row[152]) },
                { "33164", measure * long.Parse(args.Row[153]) },
                { "33165", measure * long.Parse(args.Row[154]) },
                { "33166", measure * long.Parse(args.Row[155]) },
                { "33167", measure * long.Parse(args.Row[156]) },
                { "33168", measure * long.Parse(args.Row[157]) },
                { "33203", measure * long.Parse(args.Row[158]) },
                { "33204", measure * long.Parse(args.Row[159]) },
                { "33205", measure * long.Parse(args.Row[160]) },
                { "33206", measure * long.Parse(args.Row[161]) },
                { "33207", measure * long.Parse(args.Row[162]) },
                { "33208", measure * long.Parse(args.Row[163]) },
                { "33217", measure * long.Parse(args.Row[164]) },
                { "33218", measure * long.Parse(args.Row[165]) },
                { "33225", measure * long.Parse(args.Row[166]) },
                { "33227", measure * long.Parse(args.Row[167]) },
                { "33228", measure * long.Parse(args.Row[168]) },
                { "33235", measure * long.Parse(args.Row[169]) },
                { "33237", measure * long.Parse(args.Row[170]) },
                { "33238", measure * long.Parse(args.Row[171]) },
                { "33243", measure * long.Parse(args.Row[172]) },
                { "33244", measure * long.Parse(args.Row[173]) },
                { "33245", measure * long.Parse(args.Row[174]) },
                { "33247", measure * long.Parse(args.Row[175]) },
                { "33248", measure * long.Parse(args.Row[176]) },
                { "33253", measure * long.Parse(args.Row[177]) },
                { "33254", measure * long.Parse(args.Row[178]) },
                { "33255", measure * long.Parse(args.Row[179]) },
                { "33257", measure * long.Parse(args.Row[180]) },
                { "33258", measure * long.Parse(args.Row[181]) },
                { "33263", measure * long.Parse(args.Row[182]) },
                { "33264", measure * long.Parse(args.Row[183]) },
                { "33265", measure * long.Parse(args.Row[184]) },
                { "33266", measure * long.Parse(args.Row[185]) },
                { "33267", measure * long.Parse(args.Row[186]) },
                { "33268", measure * long.Parse(args.Row[187]) },
                { "33277", measure * long.Parse(args.Row[188]) },
                { "33278", measure * long.Parse(args.Row[189]) },
                { "33305", measure * long.Parse(args.Row[190]) },
                { "33306", measure * long.Parse(args.Row[191]) },
                { "33307", measure * long.Parse(args.Row[192]) },
                { "33406", measure * long.Parse(args.Row[193]) },
                { "33407", measure * long.Parse(args.Row[194]) },
                { "33003", measure * long.Parse(args.Row[195]) },
                { "33004", measure * long.Parse(args.Row[196]) },
                { "33005", measure * long.Parse(args.Row[197]) },
                { "33006", measure * long.Parse(args.Row[198]) },
                { "33007", measure * long.Parse(args.Row[199]) },
                { "33008", measure * long.Parse(args.Row[200]) },
                { "36003", measure * long.Parse(args.Row[201]) },
                { "36004", measure * long.Parse(args.Row[202]) },
                { "41103", measure * long.Parse(args.Row[203]) },
                { "41113", measure * long.Parse(args.Row[204]) },
                { "41123", measure * long.Parse(args.Row[205]) },
                { "41133", measure * long.Parse(args.Row[206]) },
                { "41193", measure * long.Parse(args.Row[207]) },
                { "41203", measure * long.Parse(args.Row[208]) },
                { "41213", measure * long.Parse(args.Row[209]) },
                { "41223", measure * long.Parse(args.Row[210]) },
                { "41233", measure * long.Parse(args.Row[211]) },
                { "41243", measure * long.Parse(args.Row[212]) },
                { "41293", measure * long.Parse(args.Row[213]) },
                { "41003", measure * long.Parse(args.Row[214]) },
                { "42103", measure * long.Parse(args.Row[215]) },
                { "42113", measure * long.Parse(args.Row[216]) },
                { "42123", measure * long.Parse(args.Row[217]) },
                { "42133", measure * long.Parse(args.Row[218]) },
                { "42143", measure * long.Parse(args.Row[219]) },
                { "42193", measure * long.Parse(args.Row[220]) },
                { "42203", measure * long.Parse(args.Row[221]) },
                { "42213", measure * long.Parse(args.Row[222]) },
                { "42223", measure * long.Parse(args.Row[223]) },
                { "42233", measure * long.Parse(args.Row[224]) },
                { "42243", measure * long.Parse(args.Row[225]) },
                { "42293", measure * long.Parse(args.Row[226]) },
                { "42003", measure * long.Parse(args.Row[227]) },
                { "43103", measure * long.Parse(args.Row[228]) },
                { "43113", measure * long.Parse(args.Row[229]) },
                { "43123", measure * long.Parse(args.Row[230]) },
                { "43133", measure * long.Parse(args.Row[231]) },
                { "43143", measure * long.Parse(args.Row[232]) },
                { "43193", measure * long.Parse(args.Row[233]) },
                { "43203", measure * long.Parse(args.Row[234]) },
                { "43213", measure * long.Parse(args.Row[235]) },
                { "43223", measure * long.Parse(args.Row[236]) },
                { "43233", measure * long.Parse(args.Row[237]) },
                { "43293", measure * long.Parse(args.Row[238]) },
                { "43003", measure * long.Parse(args.Row[239]) },
                { "44003", measure * long.Parse(args.Row[240]) },
                { "44903", measure * long.Parse(args.Row[241]) },
                { "61003", measure * long.Parse(args.Row[242]) },
                { "62103", measure * long.Parse(args.Row[243]) },
                { "62153", measure * long.Parse(args.Row[244]) },
                { "62203", measure * long.Parse(args.Row[245]) },
                { "62303", measure * long.Parse(args.Row[246]) },
                { "62403", measure * long.Parse(args.Row[247]) },
                { "62503", measure * long.Parse(args.Row[248]) },
                { "62003", measure * long.Parse(args.Row[249]) },
                { "63103", measure * long.Parse(args.Row[250]) },
                { "63113", measure * long.Parse(args.Row[251]) },
                { "63123", measure * long.Parse(args.Row[252]) },
                { "63133", measure * long.Parse(args.Row[253]) },
                { "63203", measure * long.Parse(args.Row[254]) },
                { "63213", measure * long.Parse(args.Row[255]) },
                { "63223", measure * long.Parse(args.Row[256]) },
                { "63233", measure * long.Parse(args.Row[257]) },
                { "63243", measure * long.Parse(args.Row[258]) },
                { "63253", measure * long.Parse(args.Row[259]) },
                { "63263", measure * long.Parse(args.Row[260]) },
                { "63303", measure * long.Parse(args.Row[261]) },
                { "63503", measure * long.Parse(args.Row[262]) },
                { "63003", measure * long.Parse(args.Row[263]) },
                { "64003", measure * long.Parse(args.Row[264]) }
            };

            return values;
        });
    }
}
