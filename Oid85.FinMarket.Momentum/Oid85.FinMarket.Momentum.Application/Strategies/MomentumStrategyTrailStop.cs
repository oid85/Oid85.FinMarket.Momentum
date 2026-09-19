using Oid85.FinMarket.Momentum.Application.Models;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyTrailStop : MomentumStrategy
    {
        private readonly double DrawdownLimitPercent = 15.0;

        public override List<string> GetDescription()
        {
            return [
                "Версия TrailStop",
                "С трейлинг стопом",
                $"Период моментума: {Period} дней",
                $"Количество отбираемых тикеров для моментума: {CounTopTickers} шт.",
                $"Дни ребалансировки: {string.Join(", ", RebalanceDays).Trim()} каждого месяца",
                $"Стоп расчитывается как двойной средний размер тела свечи за {Period} дней",
                $"Стоп пересчитывается на каждой свече",
                $"Если текущая цена отстоит от цены стопа больше, чем на двойной средний размер тела свечи за {Period} дней,",
                $"то новая цена стопа равна текущая цена минус двойной средний размер тела свечи за {Period} дней",
                "При срабатывании стопа закрывать позицию и покупать фонд ликвидности"
                ];
        }

        public override void InitMonitorParameters()
        {
            Period = 10;
            CounTopTickers = 10;
            RebalanceDays = [1, 11, 21];
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
                    SetWeights();
                    SetEntryPrices();
                    SetAverageCandleBodies();
                    SetClassicStops();
                    OpenPositionsByWeights();
                    AddRebalanceMessage();
                }

                else
                {
                    TrailStops();
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
