using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Responses;
using Oid85.FinMarket.Momentum.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Momentum.Application.Mapping;

public static class TerminalMapper
{
    public static TerminalResponse ToTerminalResponse(MonitorResponse monitorResponse, PortfolioInfoResult portfolioInfoResult)
    {
        var response = new TerminalResponse
        {
            TotalSum = portfolioInfoResult.TotalSum,
            Money = portfolioInfoResult.Money,
            TotalDailyPnl = portfolioInfoResult.TotalDailyPnl
        };

        var targetTickers = monitorResponse.CurrentPositions.Select(x => x.Ticker).ToList();
        var lifeTickers = portfolioInfoResult.Positions.Select(x => x.Ticker).ToList();

        List<string> tickers = [.. targetTickers, .. lifeTickers];
        List<string> distinctTickers = [.. tickers.Distinct()];

        List<string> terminalTickers = [
            ..distinctTickers.Where(x => targetTickers.Contains(x) && x != KnownTickers.FMMM),
            ..distinctTickers.Where(x => targetTickers.Contains(x) && x == KnownTickers.FMMM),
            ..distinctTickers.Where(x => lifeTickers.Contains(x))
            ];

        var rows = new List<TerminalRow>();

        foreach (var ticker in terminalTickers)
        {
            var targetPosition = monitorResponse.CurrentPositions.Find(x => x.Ticker == ticker);
            var lifePosition = portfolioInfoResult.Positions.Find(x => x.Ticker == ticker);

            var row = new TerminalRow
            {
                Ticker = ticker,

                TargetPosition = new TerminalTargetPosition
                {
                    DoShow = targetPosition is not null,
                    Size = 0,
                    ColorFill = KnownColors.White
                },

                LifePosition = new TerminalLifePosition
                {
                    DoShow = lifePosition is not null,
                    Size = 0,
                    ColorFill = KnownColors.White,
                    Cost = 0,
                    CurrentPrice = 0,
                    DailyPnl = 0
                },

                SyncSizeButton = new TerminalSyncSizeButton
                {
                    DoShow = true,
                    Title = string.Empty,
                    ColorFill = KnownColors.White,
                    Task = string.Empty
                },

                TargetStop = new TerminalTargetStop
                {
                    DoShow = true,
                    Size = 0,
                    ColorFill = KnownColors.White,
                    StopPrice = 0
                },

                LifeStop = new TerminalLifeStop
                {
                    DoShow = true,
                    Size = 0,
                    ColorFill = KnownColors.White,
                    StopPrice = 0
                },

                SyncStopButton = new TerminalSyncStopButton
                {
                    DoShow = true,
                    Title = string.Empty,
                    ColorFill = KnownColors.White,
                    Task = string.Empty
                },

                SyncTickerSizeTask = new TerminalSyncTickerSizeTask
                {
                    DoShow = true,
                    State = string.Empty,
                    ColorFill = KnownColors.White,
                    Task = string.Empty
                },

                SyncTickerStopTask = new TerminalSyncTickerStopTask
                {
                    DoShow = true,
                    State = string.Empty,
                    ColorFill = KnownColors.White,
                    Task = string.Empty
                }
            };

            rows.Add(row);
        }

        response.Rows = rows;

        return response;
    }
}