using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Common.Extensions;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Strategies
{
    public class DonchianBreakoutClassicLong(
        IIndicatorFactory indicatorFactory) 
        : Strategy
    {
        public override string StrategyName { get; set; } = nameof(DonchianBreakoutClassicLong);

        public override string StrategyDescription { get; set; } = "Канал Дончиана. Только лонг";

        public override List<StrategyParameter> StrategyParameters { get; set; } =
            [
                new () { Name = "PeriodEntry", Def = 10, Min = 10, Max = 100, Step = 5 },
                new () { Name = "PeriodExit", Def = 10, Min = 10, Max = 100, Step = 5 }
            ];

        public override void Execute()
        {
            // Получаем параметры
            int periodEntry = Parameters["PeriodEntry"];
            int periodExit = Parameters["PeriodExit"];

            // Фильтр
            var filterEma = indicatorFactory.Ema(Candles, periodEntry);
            
            // Цены для построения канала
            List<double> price = HighPrices.Add(LowPrices)!.Add(ClosePrices)!.Add(ClosePrices)!.DivConst(4.0);

            // Построение каналов
            List<double> highLevel = indicatorFactory.Highest(price, periodEntry);
            List<double> lowLevel = indicatorFactory.Lowest(price, periodExit);

            // Сглаживание
            int smoothPeriod = 5;
            highLevel = indicatorFactory.Sma(highLevel, smoothPeriod);
            lowLevel = indicatorFactory.Sma(lowLevel, smoothPeriod);

            // Сдвиг вправо на одну свечу
            highLevel = highLevel.Shift(1);
            lowLevel = lowLevel.Shift(1);

            // Переменные для обслуживания позиции
            double trailingStop = 0.0;

            for (int i = StabilizationPeriod; i < Candles.Count - 1; i++)
            {
                // Правило входа
                SignalLong = ClosePrices[i] > highLevel[i];
                FilterLong = Candles[i].Close > filterEma[i];
                
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
                    int entryCandleIndex = LastActivePosition.EntryCandleIndex;

                    if (LastActivePosition.IsLong)
                    {
                        double startTrailingStop = lowLevel[entryCandleIndex];
                        double curTrailingStop = lowLevel[i];

                        trailingStop = i == entryCandleIndex ? startTrailingStop : Math.Max(trailingStop, curTrailingStop);
                        
                        if (Candles[i].Close <= trailingStop)
                            SellAtPrice(positionSize, Candles[i].Close, i + 1);
                    }
                }

                // Отрисовка
                DiagramPoints[i].Price = Candles[i].Close;

                if (LastActivePosition is not null && LastActivePosition.IsLong)
                    DiagramPoints[i].LongPositionIndicator = Candles[i].Close;
            }
        }
    }
}
