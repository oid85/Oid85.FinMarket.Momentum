using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyVersion1 : MomentumStrategy
    {
        private readonly double DrawdownLimit = 15.0;

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

                if (CurrentDrawdown >= DrawdownLimit)
                    foreach (var ticker in PortfolioWithoutMonTickers)
                        ClosePosition(ticker);

                UpdateEquitySeries();
                UpdateMoneySeries();
            }
        }
    }
}
