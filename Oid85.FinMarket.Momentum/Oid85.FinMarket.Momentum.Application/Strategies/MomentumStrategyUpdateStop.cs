using Oid85.FinMarket.Momentum.Application.Models;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyUpdateStop : MomentumStrategy
    {
        private readonly double DrawdownLimitPercent = 15.0;

        public override List<string> GetDescription()
        {
            return [
                "Версия UpdateStop",
                "Пересчет стопов в середине периода",
                $"Период моментума: {Period} дней",
                $"Количество отбираемых тикеров для моментума: {CounTopTickers} шт.",
                $"Дни ребалансировки: {string.Join(", ", RebalanceDays).Trim()} каждого месяца",
                $"Дни пересчета стопов: {string.Join(", ", UpdateStopDays).Trim()} каждого месяца",
                $"Стоп расчитывается как двойной средний размер тела свечи за {Period} дней",
                "При срабатывании стопа закрывать позицию и покупать фонд ликвидности"
                ];
        }

        public override void InitMonitorParameters()
        {
            Period = 10;
            CounTopTickers = 10;
            RebalanceDays = [1, 11, 21];
            UpdateStopDays = [6, 16, 26];
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
                    if (IsUpdateStopDay)
                        UpdateClassicStops();

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
