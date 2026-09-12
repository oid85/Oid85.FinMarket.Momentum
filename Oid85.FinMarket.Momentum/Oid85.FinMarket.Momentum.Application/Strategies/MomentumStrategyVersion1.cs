using Oid85.FinMarket.Momentum.Application.Models;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyVersion1 : MomentumStrategy
    {
        private readonly double DrawdownLimitPercent = 15.0;

        public override int Period => 15;
        public override int CounTopTickers => 8;
        public override List<int> RebalanceDays => [1, 11, 21];

        public override List<string> GetDescription()
        {
            return [
                "Версия 1",
                $"Период моментума: {Period} дней",
                $"Количество отбираемых тикеров для моментума: {CounTopTickers} шт.",
                $"Дни ребалансировки: {string.Join(", ", RebalanceDays).Trim()} каждого месяца",
                $"Стоп-лосс расчитывается как двойной средний размер тела свечи за {Period} дней",
                "При срабатывании стопа закрывать позицию и покупать фонд ликвидности"
                ];
        }

        public override void Execute()
        {
            foreach (var date in Dates)
            {
                CurrentDate = date;

                if (IsRebalance)
                {
                    ClearMessages();
                    SetTopTickers();
                    SetWeights();
                    UpdateCandles();
                    SetStops();
                    SetSizes();
                    UpdateCosts();
                    UpdateMoney();
                    UpdateTotalSum();
                    AddRebalanceMessage();
                }

                else
                {
                    UpdateCandles();
                    UpdateCosts();
                    CheckStopsWithClosePosition();
                    UpdateTotalSum();
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
