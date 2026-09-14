using Oid85.FinMarket.Momentum.Application.Models;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyVersion1 : MomentumStrategy
    {
        private readonly double DrawdownLimitPercent = 15.0;

        public override List<string> GetDescription()
        {
            return [
                "Версия 1",
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

        public override void Execute()
        {
            foreach (var date in Dates)
            {
                CurrentDate = date;
                BalanceProcessor.CurrentDate = date;
                UpdatePrices();
                UpdateCandles();

                if (IsRebalance)
                {
                    CloseAllPositions();
                    ClearMessages();
                    SetTopTickers();
                    SetWeights();
                    SetEntryPrices();
                    SetAverageCandleBodies();
                    SetStops();                    
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
