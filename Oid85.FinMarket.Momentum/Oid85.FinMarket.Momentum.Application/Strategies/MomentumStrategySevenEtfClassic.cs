using System.Timers;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Application.Models;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategySevenEtfClassic : MomentumStrategy
    {
        private readonly double DrawdownLimitPercent = 15.0;

        public override List<string> GetDescription()
        {
            return [
                "Версия SevenEtfClassic",
                "Классический для ETF",
                $"Период моментума: {Period} дней",
                $"Дни ребалансировки: {string.Join(", ", RebalanceDays).Trim()} каждого месяца",
                $"Стоп расчитывается как двойной средний размер тела свечи за {Period} дней",
                "При срабатывании стопа закрывать позицию и покупать фонд ликвидности"
                ];
        }

        public override void InitMonitorParameters()
        {
            Period = 10;
            RebalanceDays = [1, 11, 21];
        }

        private void SetSevenEtfWeights()
        {
            Tickers.ForEach(ticker => PositionData[ticker].Weight = 0.0);
            PositionData[MON].Weight = 4.0;

            if (TopTickers.Contains(TDIV)) PositionData[TDIV].Weight = 1.0; else PositionData[MON].Weight += 1.0;
            if (TopTickers.Contains(TITR)) PositionData[TITR].Weight = 1.0; else PositionData[MON].Weight += 1.0;
            if (TopTickers.Contains(TMOS)) PositionData[TMOS].Weight = 1.0; else PositionData[MON].Weight += 1.0;
            if (TopTickers.Contains(TRND)) PositionData[TRND].Weight = 1.0; else PositionData[MON].Weight += 1.0;
            if (TopTickers.Contains(TBRU)) PositionData[TBRU].Weight = 2.0; else PositionData[MON].Weight += 2.0;
            if (TopTickers.Contains(TOFZ)) PositionData[TOFZ].Weight = 2.0; else PositionData[MON].Weight += 2.0;
        }

        private void SetSevenEtfTopTickers() => TopTickers = [.. MomentumHelper.GetMomentumTopTickers(CandleData, CurrentDate.AddDays(-1), Period, CandleData.Count), MON];

        public override void Execute()
        {
            foreach (var date in Dates)
            {
                CurrentDate = date;
                TradingProcessor.CurrentDate = date;
                UpdatePrices();
                UpdateCandles();

                if (IsRebalanceDay)
                {
                    CloseAllPositions();
                    ClearMessages();
                    SetSevenEtfTopTickers();
                    SetSevenEtfWeights();
                    SetEntryPrices();
                    SetAverageCandleBodies();
                    SetClassicStops();                    
                    OpenPositionsByWeights();
                    AddRebalanceMessage();
                }

                else
                {                    
                    // CheckStopsWithClosePosition();
                }

                UpdateEquitySeries();
                UpdateMoneySeries();

                if (Math.Abs(GetCurrentDrawdown()) >= DrawdownLimitPercent)
                    foreach (var ticker in PortfolioWithoutMonTickers)
                        ClosePosition(ticker);
            }
        }
    }
}
