using System.Globalization;
using Oid85.FinMarket.Algo.Application.Interfaces.Services;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Strategies
{
    public class MomentumWeekLong(
        IDataService dataService) 
        : Strategy
    {
        public override string StrategyName { get; set; } = nameof(MomentumWeekLong);

        public override string StrategyDescription { get; set; } = "Momentum. Контроль риска. Только лонг. Раз в неделю";

        public override List<StrategyParameter> StrategyParameters { get; set; } =
            [
                new () { Name = "Period", Def = 10, Min = 10, Max = 100, Step = 10 },
                new () { Name = "Percent", Def = 10, Min = 10, Max = 50, Step = 10 }
            ];

        public override void Execute()
        {
            // Получаем параметры
            int period = Parameters["Period"];
            int percent = Parameters["Percent"];

            // Ограничение на убыток в процентах
            double lossPercentLimit = 2.0;

            for (int i = StabilizationPeriod; i < Candles.Count - 1; i++)
            {                
                int numberWeekPrev = ISOWeek.GetWeekOfYear(Candles[i - 1].Date.ToDateTime(TimeOnly.MinValue));
                int numberWeekCurr = ISOWeek.GetWeekOfYear(Candles[i].Date.ToDateTime(TimeOnly.MinValue));

                // Торгуем только в начале недели
                bool isBalancing = numberWeekCurr > numberWeekPrev;
                
                if (isBalancing)
                {
                    var topTickers = dataService.GetMomentumTopTickers(CandleData, Candles[i].Date, period, percent);

                    bool tickerInTop = topTickers.Contains(Ticker);

                    // Правило входа
                    SignalLong = tickerInTop;

                    // Правило выхода
                    SignalCloseLong = !tickerInTop;

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
                }

                else
                {
                    if (LastActivePosition is not null)
                    {
                        double profit = Math.Abs(LastActivePosition.Quantity) * (Candles[i].Close - LastActivePosition.EntryPrice);

                        // Если имеем убыток
                        if (profit < 0)
                        {
                            double lossPercent = Math.Abs(profit / EndMoney * 100.0);

                            if (lossPercent >= lossPercentLimit)
                                SignalCloseLong = true;

                        }

                        if (SignalCloseLong)
                            SellAtPrice(Math.Abs(LastActivePosition.Quantity), Candles[i].Close, i + 1);
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
