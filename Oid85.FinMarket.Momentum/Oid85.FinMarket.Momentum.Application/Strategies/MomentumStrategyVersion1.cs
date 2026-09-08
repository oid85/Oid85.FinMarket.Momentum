using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;

namespace Oid85.FinMarket.Momentum.Application.Strategies
{
    public class MomentumStrategyVersion1 : MomentumStrategy
    {
        public override void Execute()
        {
            foreach (var date in Dates)
            {
                CurrentDate = date;

                if (RebalanceDays.Contains(CurrentDate.Day))
                {
                    ProtocolMessages.Clear();

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

                if (CurrentDrawdown >= 15.0)
                    foreach (var ticker in PortfolioWithoutMonTickers)
                        ClosePosition(ticker);

                EquitySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = (TotalSum / 1000.0).RoundTo(2)
                    });

                MoneySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = ((Money + PositionData[KnownTickers.MON].Cost) / 1000.0).RoundTo(2)
                    });
            }
        }
    }
}
