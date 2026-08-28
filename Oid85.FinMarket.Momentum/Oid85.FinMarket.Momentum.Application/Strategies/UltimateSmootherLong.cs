using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Strategies
{
    public class UltimateSmootherLong(
        IIndicatorFactory indicatorFactory)
        : Strategy
    {
        public override string StrategyName { get; set; } = nameof(UltimateSmootherLong);

        public override string StrategyDescription { get; set; } = "Supertrend. Только лонг";

        public override List<StrategyParameter> StrategyParameters { get; set; } =
            [
                new () { Name = "Period", Def = 10, Min = 10, Max = 100, Step = 5 }
            ];

        public override void Execute()
        {
            // Получаем параметры
            int period = Parameters["Period"];
            
            // Расчет индикаторов
            var us = indicatorFactory.UltimateSmoother(ClosePrices, period);

            for (int i = StabilizationPeriod; i < Candles.Count - 1; i++)
            {
                // Правило входа
                SignalLong =
                    us[i - 2] > us[i - 3] &&
                    us[i - 1] > us[i - 2] &&
                    us[i] > us[i - 1];

                // Правило выхода
                SignalCloseLong =
                    us[i - 2] < us[i - 3] &&
                    us[i - 1] < us[i - 2] &&
                    us[i] < us[i - 1];
                
                // Задаем цену для заявки
                double orderPrice = Candles[i].Close;

                // Расчет размера позиции
                int positionSize = GetPositionSize(orderPrice);
                
                if (LastActivePosition is null)
                {
                    if (SignalLong && FilterLong)
                        BuyAtPrice(positionSize, orderPrice, i + 1);
                }
                
                else
                {
                    if (SignalCloseLong)
                        SellAtPrice(positionSize, orderPrice, i + 1);
                }

                // Отрисовка
                DiagramPoints[i].Price = Candles[i].Close;

                if (LastActivePosition is not null && LastActivePosition.IsLong)
                    DiagramPoints[i].LongPositionIndicator = Candles[i].Close;
            }
        }
    }
}
