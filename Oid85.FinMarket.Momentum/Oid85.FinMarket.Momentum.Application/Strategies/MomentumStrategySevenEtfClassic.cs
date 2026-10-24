using System.Timers;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Application.Models;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategySevenEtfClassic : Strategy
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
            const double MonWeight = 8.0;
            const double BondWeight = 4.0;
            const double ShareWeight = 1.0;

            Tickers.ForEach(ticker => PositionData[ticker].Weight = 0.0);
            PositionData[MON].Weight = MonWeight;

            if (TopTickers.Contains(TDIV)) PositionData[TDIV].Weight = ShareWeight; else PositionData[MON].Weight += ShareWeight;
            if (TopTickers.Contains(TITR)) PositionData[TITR].Weight = ShareWeight; else PositionData[MON].Weight += ShareWeight;
            if (TopTickers.Contains(TMOS)) PositionData[TMOS].Weight = ShareWeight; else PositionData[MON].Weight += ShareWeight;
            if (TopTickers.Contains(TRND)) PositionData[TRND].Weight = ShareWeight; else PositionData[MON].Weight += ShareWeight;
            if (TopTickers.Contains(TBRU)) PositionData[TBRU].Weight = BondWeight; else PositionData[MON].Weight += BondWeight;
            if (TopTickers.Contains(TOFZ)) PositionData[TOFZ].Weight = BondWeight; else PositionData[MON].Weight += BondWeight;
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
                    CheckStopsWithClosePosition();
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
