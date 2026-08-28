using Oid85.FinMarket.Algo.Application.Helpers;
using Oid85.FinMarket.Algo.Core.Models;

namespace Test.Oid85.FinMarket.Algo.Application;

public class MonitorHelperTests
{
    [Fact]
    public void Map_Positions_return_is_correct()
    {
        // Arrange
        List<DateOnly> dates =
                [
                    DateOnly.Parse("2026.07.01"),
                    DateOnly.Parse("2026.07.02"),
                    DateOnly.Parse("2026.07.03"),
                    DateOnly.Parse("2026.07.04"),
                    DateOnly.Parse("2026.07.05"),
                    DateOnly.Parse("2026.07.06"),
                    DateOnly.Parse("2026.07.07"),
                    DateOnly.Parse("2026.07.08"),
                    DateOnly.Parse("2026.07.09"),
                    DateOnly.Parse("2026.07.10")
                ];

        SortedDictionary<DateOnly, Position> positions = new()
        {
            { DateOnly.Parse("2026.07.02"), new() { IsActive = false, IsLong = true, IsShort = false, EntryDate = DateOnly.Parse("2026.07.02"), ExitDate = DateOnly.Parse("2026.07.04") } },
            { DateOnly.Parse("2026.07.06"), new() { IsActive = false, IsLong = false, IsShort = true, EntryDate = DateOnly.Parse("2026.07.06"), ExitDate = DateOnly.Parse("2026.07.07") } },
            { DateOnly.Parse("2026.07.09"), new() { IsActive = true, IsLong = true, IsShort = false, EntryDate = DateOnly.Parse("2026.07.09"), ExitDate = null } }
        };

        // Act
        List<DateValue<int>> expected =
            [
                new () { Date = DateOnly.Parse("2026.07.01"), Value = 0 },
                new () { Date = DateOnly.Parse("2026.07.02"), Value = 1 },
                new () { Date = DateOnly.Parse("2026.07.03"), Value = 1 },
                new () { Date = DateOnly.Parse("2026.07.04"), Value = 1 },
                new () { Date = DateOnly.Parse("2026.07.05"), Value = 0 },
                new () { Date = DateOnly.Parse("2026.07.06"), Value = -1 },
                new () { Date = DateOnly.Parse("2026.07.07"), Value = -1 },
                new () { Date = DateOnly.Parse("2026.07.08"), Value = 0 },
                new () { Date = DateOnly.Parse("2026.07.09"), Value = 1 },
                new () { Date = DateOnly.Parse("2026.07.10"), Value = 1 }
            ];

        var sut = MonitorHelper.Map(positions, dates);

        //Assert
        Assert.Equivalent(sut, expected);
    }

    [Fact]
    public void Merge_return_is_correct()
    {
        // Arrange
        List<DateOnly> dates =
                [
                    DateOnly.Parse("2026.07.01"),
                    DateOnly.Parse("2026.07.02"),
                    DateOnly.Parse("2026.07.03")
                ];

        List<List<DateValue<int>>> data =
            [
                [
                    new () { Date = DateOnly.Parse("2026.07.01"), Value = 0 },
                    new () { Date = DateOnly.Parse("2026.07.02"), Value = 1 },
                    new () { Date = DateOnly.Parse("2026.07.03"), Value = 1 }
                ],
                [
                    new () { Date = DateOnly.Parse("2026.07.01"), Value = 0 },
                    new () { Date = DateOnly.Parse("2026.07.02"), Value = 1 },
                    new () { Date = DateOnly.Parse("2026.07.03"), Value = 0 }
                ],
                [
                    new () { Date = DateOnly.Parse("2026.07.01"), Value = 1 },
                    new () { Date = DateOnly.Parse("2026.07.02"), Value = 1 },
                    new () { Date = DateOnly.Parse("2026.07.03"), Value = 0 }
                ]
            ];

        // Act
        List<DateValue<int>> expected =
            [
                new () { Date = DateOnly.Parse("2026.07.01"), Value = 1 },
                new () { Date = DateOnly.Parse("2026.07.02"), Value = 3 },
                new () { Date = DateOnly.Parse("2026.07.03"), Value = 1 }
            ];

        var sut = MonitorHelper.Merge(data, dates);

        //Assert
        Assert.Equivalent(sut, expected);
    }
}