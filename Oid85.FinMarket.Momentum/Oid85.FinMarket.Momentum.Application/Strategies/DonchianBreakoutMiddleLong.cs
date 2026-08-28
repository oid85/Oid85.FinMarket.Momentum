using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Common.Extensions;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Strategies
{
    public class DonchianBreakoutMiddleLong(
        IIndicatorFactory indicatorFactory) 
        : Strategy
    {
        public override string StrategyName { get; set; } = nameof(DonchianBreakoutMiddleLong);

        public override string StrategyDescription { get; set; } = "Канал Дончиана. Выход по средней линии. Только лонг";

        public override List<StrategyParameter> StrategyParameters { get; set; } =
            [
                new () { Name = "Period", Def = 10, Min = 10, Max = 100, Step = 5 }
            ];

        public override void Execute()
        {
            // Получаем параметры
            int period = Parameters["Period"];

            // Фильтр
            var filterEma = indicatorFactory.Ema(Candles, period);
            
            // Цены для построения канала
            List<double> priceForChannelHigh = HighPrices.Add(LowPrices)!.Add(ClosePrices)!.Add(ClosePrices)!.DivConst(4.0);
            List<double> priceForChannelLow = HighPrices.Add(LowPrices)!.Add(ClosePrices)!.Add(ClosePrices)!.DivConst(4.0);

            // Построение каналов
            List<double> highLevel = indicatorFactory.Highest(priceForChannelHigh, period);
            List<double> lowLevel = indicatorFactory.Lowest(priceForChannelLow, period);

            // Сглаживание
            int smoothPeriod = 5;
            highLevel = indicatorFactory.Sma(highLevel, smoothPeriod);
            lowLevel = indicatorFactory.Sma(lowLevel, smoothPeriod);

            // Сдвиг вправо на одну свечу
            highLevel = highLevel.Shift(1);
            lowLevel = lowLevel.Shift(1);

            // Средняя линия канала
            List<double> middleLine = highLevel.Add(lowLevel)!.DivConst(2.0);
            
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
                        double startTrailingStop = middleLine[entryCandleIndex];
                        double curTrailingStop = middleLine[i];

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
