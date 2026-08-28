using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Common.Extensions;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Strategies
{
    public class VolatilityBreakoutMiddleLong(
        IIndicatorFactory indicatorFactory) 
        : Strategy
    {
        public override string StrategyName { get; set; } = nameof(VolatilityBreakoutMiddleLong);

        public override string StrategyDescription { get; set; } = "Пробой волатильности. Выход по средней линии. Только лонг";

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
            
            // Фильтр
            var filterEma = indicatorFactory.Ema(Candles, period);
            
            // Построение каналов волатильности
            List<double> price = OpenPrices.Add(ClosePrices)!.DivConst(2.0);
            List<double> atr = indicatorFactory.Atr(Candles, period);
            List<double> highLevel = price.Add(atr.MultConst(multiplier))!; // up = price + atr * multiplier;
            List<double> lowLevel = price.Sub(atr.MultConst(multiplier))!;  // up = price - atr * multiplier;

            highLevel = indicatorFactory.Highest(highLevel, period);
            lowLevel = indicatorFactory.Lowest(lowLevel, period);
            
            highLevel = highLevel.Shift(1);
            lowLevel = lowLevel.Shift(1);
            
            // Сглаживание
            int smoothPeriod = 5;
            highLevel = indicatorFactory.Sma(highLevel, smoothPeriod);
            lowLevel = indicatorFactory.Sma(lowLevel, smoothPeriod);
            
            // Средняя линия канала
            List<double> middleLine = highLevel.Add(lowLevel)!.DivConst(2.0);
            
            // Переменные для обслуживания позиции
            double startTrailing = 0.0;   // Стоп, выставляемый при открытии позиции
            double currentTrailing = 0.0; // Величина текущего стопа
            
            for (int i = StabilizationPeriod; i < Candles.Count - 1; i++)
            {
                SignalLong = Candles[i].Close > highLevel[i];
                FilterLong = Candles[i].Close > filterEma[i];
                
                double orderPrice = Candles[i].Close;
                
                // Расчет размера позиции
                int positionSize = GetPositionSize(orderPrice);
                
                if (LastActivePosition is null)
                {
                    if (SignalLong && FilterLong)
                    {
                        startTrailing = middleLine[i];
                        BuyAtPrice(positionSize, orderPrice, i + 1);
                    }
                }
                
                else
                {
                    if (LastActivePosition.IsLong)
                    {
                        int entryBar = LastActivePosition.EntryCandleIndex;
                        currentTrailing = i == entryBar ? startTrailing : Math.Max(currentTrailing, middleLine[i]);
                        
                        if (Candles[i].Close <= currentTrailing)
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
