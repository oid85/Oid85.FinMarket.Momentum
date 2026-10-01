using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Models;
using Oid85.FinMarket.Momentum.Core.Responses;
using Oid85.FinMarket.Momentum.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Momentum.Application.Mapping;

public static class TerminalMapper
{
    public static TerminalResponse ToTerminalResponse(
        MonitorResponse monitorResponse, 
        PortfolioInfoResult portfolioInfoResult, 
        OutboxTaskListResult outboxTaskListResult)
    {
        var response = new TerminalResponse
        {
            TotalSum = portfolioInfoResult.TotalSum,
            Money = portfolioInfoResult.Money,
            TotalDailyPnl = portfolioInfoResult.TotalDailyPnl
        };

        var targetTickers = monitorResponse.CurrentPositions.Select(x => x.Ticker).ToList();
        var lifeTickers = portfolioInfoResult.Positions.Where(x => x.Size != 0).Select(x => x.Ticker).ToList();

        List<string> tickers = [.. targetTickers, .. lifeTickers];
        List<string> distinctTickers = [.. tickers.Distinct()];

        List<string> terminalTickers = [
            ..distinctTickers.Where(x => targetTickers.Contains(x) && x != KnownTickers.FMMM),
            ..distinctTickers.Where(x => targetTickers.Contains(x) && x == KnownTickers.FMMM),
            ..distinctTickers.Where(x => lifeTickers.Contains(x) && !targetTickers.Contains(x))
            ];

        var rows = new List<TerminalRow>();

        foreach (var ticker in terminalTickers)
        {
            var targetPosition = monitorResponse.CurrentPositions.Find(x => x.Ticker == ticker);
            var lifePosition = portfolioInfoResult.Positions.Find(x => x.Ticker == ticker);

            var row = new TerminalRow
            {
                Ticker = ticker,
                TargetPosition = GetTerminalTargetPosition(targetPosition, lifePosition),
                LifePosition = GetTerminalLifePosition(targetPosition, lifePosition),
                SyncSizeButton = GetTerminalSyncSizeButton(targetPosition, lifePosition),

                TargetStop = new TerminalTargetStop
                {
                    DoShow = false,
                    Size = 0,
                    StopPrice = 0,
                    ColorFill = KnownColors.White
                },

                LifeStop = new TerminalLifeStop
                {
                    DoShow = false,
                    Size = 0,
                    StopPrice = 0,
                    ColorFill = KnownColors.White
                },

                SyncStopButton = new TerminalSyncStopButton
                {
                    DoShow = false,
                    Title = string.Empty,
                    Task = "SyncStopTicker",
                    ColorFill = KnownColors.White
                },

                SyncTickerSizeTask = new TerminalSyncTickerSizeTask
                {
                    DoShow = false,
                    State = string.Empty,
                    Task = string.Empty,
                    ColorFill = KnownColors.White
                },

                SyncTickerStopTask = new TerminalSyncTickerStopTask
                {
                    DoShow = false,
                    State = string.Empty,
                    Task = string.Empty,
                    ColorFill = KnownColors.White
                }
            };

            rows.Add(row);
        }

        int number = 1;
        foreach (var row in rows)
            row.Number = number++;

        response.Rows = rows;

        return response;
    }

    private static TerminalTargetPosition GetTerminalTargetPosition(CurrentPosition? targetPosition, PositionDataItem? lifePosition)
    {
        var targetSize = targetPosition?.Size ?? 0;
        var lifeSize = lifePosition?.Size ?? 0;

        string colorFill = KnownColors.White;

        if (targetSize != 0 || lifeSize != 0)
            if (targetSize != lifeSize)
                colorFill = KnownColors.LightYellow;

        return new TerminalTargetPosition
        {
            DoShow = targetPosition is not null,
            Size = targetSize,
            ColorFill = colorFill
        };
    }

    private static TerminalLifePosition GetTerminalLifePosition(CurrentPosition? targetPosition, PositionDataItem? lifePosition)
    {
        var lifeSize = lifePosition?.Size ?? 0;
        var lifeCost = lifePosition?.Cost ?? 0;
        var currentPrice = lifePosition?.CurrentPrice ?? 0;
        var dailyPnl = lifePosition?.DailyPnl ?? 0;

        string colorFill = KnownColors.White;

        if (dailyPnl > 0) colorFill = KnownColors.LightGreen;
        if (dailyPnl < 0) colorFill = KnownColors.LightRed;

        return new TerminalLifePosition
        {
            DoShow = lifePosition is not null,
            Size = lifeSize,
            Cost = lifeCost,
            CurrentPrice = currentPrice,
            DailyPnl = dailyPnl,
            ColorFill = colorFill
        };
    }

    private static TerminalSyncSizeButton GetTerminalSyncSizeButton(CurrentPosition? targetPosition, PositionDataItem? lifePosition)
    {
        var targetSize = targetPosition?.Size ?? 0;
        var lifeSize = lifePosition?.Size ?? 0;

        string title = string.Empty;

        return new TerminalSyncSizeButton
        {
            DoShow = targetSize != lifeSize,
            Title = title,
            Task = "SyncSizeTicker",
            ColorFill = KnownColors.White
        };
    }
}