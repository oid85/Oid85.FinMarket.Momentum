using System.Timers;
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
                $"Количество отбираемых тикеров для моментума: {CounTopTickers} шт.",
                $"Дни ребалансировки: {string.Join(", ", RebalanceDays).Trim()} каждого месяца",
                $"Стоп расчитывается как двойной средний размер тела свечи за {Period} дней",
                "При срабатывании стопа закрывать позицию и покупать фонд ликвидности"
                ];
        }

        public override void InitMonitorParameters()
        {
            Period = 10;
            CounTopTickers = 10;
            RebalanceDays = [1, 11, 21];
        }

        private void SetSevenEtfWeights()
        {
            PositionData[MON].Weight = 8.0;

            SetWeightForTicker("TDIV", 1.0);
            SetWeightForTicker("TITR", 1.0);
            SetWeightForTicker("TMOS", 1.0);
            SetWeightForTicker("TRND", 1.0);
            SetWeightForTicker("TBRU", 4.0);
            SetWeightForTicker("TOFZ", 4.0);
        }

        private void SetWeightForTicker(string ticker, double defaultWeight)
        {
            if (TopTickers.Contains(ticker))
                PositionData[ticker].Weight = defaultWeight;

            else
            {
                PositionData[ticker].Weight = 0.0;
                PositionData[MON].Weight += defaultWeight;
            }
        }

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
                    SetTopTickers();
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
