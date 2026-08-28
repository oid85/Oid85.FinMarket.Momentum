using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Strategies
{
    public class SupertrendLong(
        IIndicatorFactory indicatorFactory) 
        : Strategy
    {
        public override string StrategyName { get; set; } = nameof(SupertrendLong);

        public override string StrategyDescription { get; set; } = "Supertrend. Только лонг";

        public override List<StrategyParameter> StrategyParameters { get; set; } =
            [
                new () { Name = "Period", Def = 10, Min = 10, Max = 100, Step = 5 },
                new () { Name = "Multiplier", Def = 25, Min = 20, Max = 30, Step = 5 }
            ];

        public override void Execute()
        {
            // Получаем параметры
            int period = Parameters["Period"];
            double multiplier = Parameters["Multiplier"] / 10.0;
            
            // Расчет индикаторов
            List<double> supertrend = indicatorFactory.Supertrend(Candles, period, multiplier);

            for (int i = StabilizationPeriod; i < Candles.Count - 1; i++)
            {
                // Правило входа
                SignalLong = Candles[i].Close > supertrend[i];
                
                // Правило выхода
                SignalCloseLong = Candles[i].Close < supertrend[i];
                
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
