using Oid85.FinMarket.Momentum.Application.Models;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyVersion1 : MomentumStrategy
    {
        private readonly double DrawdownLimitPercent = 15.0;

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

                if (CurrentDrawdown >= DrawdownLimitPercent)
                    foreach (var ticker in PortfolioWithoutMonTickers)
                        ClosePosition(ticker);
            }
        }
    }
}
