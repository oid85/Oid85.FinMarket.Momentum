using Oid85.FinMarket.Momentum.Core.Models;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class TradingProcessor
    {
        public DateOnly CurrentDate { get; set; } = DateOnly.MinValue;
        public double StartMoneySum { get; set; } = 0.0;
        public Dictionary<string, List<Candle>> CandleData { get; set; } = [];
        public Dictionary<string, Instrument> InstrumentData { get; set; } = [];
        public List<string> Tickers { get; set; } = [];
        public Dictionary<string, Balance> BalanceData { get; set; } = [];
        public double TotalSum => BalanceData.Sum(x => x.Value.Cost);

        public void Reset()
        {
            BalanceData = Tickers.ToDictionary(k => k, v => new Balance());
            BalanceData.TryAdd(MON, new Balance());
            BalanceData.TryAdd(RUB, new Balance { Price = 1.0, Size = StartMoneySum });
        }

        /// <summary>
        /// Закрыть позиции по всем активам
        /// </summary>
        public void CloseAllPositions()
        {
            foreach (var (ticker, _) in BalanceData.Where(x => x.Key != RUB).ToDictionary())
            {
                // Переводим деньги от продажи актива в рубли
                BalanceData[RUB].Size += BalanceData[ticker].Cost;

                // Обнуляем позицию по активу
                BalanceData[ticker].Size = 0.0;
            }
        }

        /// <summary>
        /// Закрыть позицию по тикеру
        /// </summary>
        public void ClosePosition(string ticker, bool openPositionMon = true)
        {
            // Начисляем деньги от продажи актива
            BalanceData[RUB].Size += BalanceData[ticker].Cost;

            // Обнуляем позицию по активу
            BalanceData[ticker].Size = 0.0;

            // Закупаем на остатки фонд ликвидности
            if (openPositionMon)
                OpenPositionMon();
        }

        /// <summary>
        /// Открыть позиции в портфеле по заданным весам активов
        /// </summary>        
        public void OpenPositionsByWeights(Dictionary<string, double> WeightData, bool openPositionMon = true)
        {
            double weightSum = WeightData.Sum(x => x.Value);

            foreach (var (ticker, _) in WeightData.Where(x => x.Key != MON).Where(x => x.Value > 0.0).ToDictionary())
            {
                double share = WeightData[ticker] / weightSum;
                OpenPositionByShare(ticker, share);
            }

            // Закупаем на остатки фонд ликвидности
            if (openPositionMon)
                OpenPositionMon();
        }
        
        /// <summary>
        /// Открыть позицию по тикеру на долю портфеля
        /// </summary>
        public void OpenPositionByShare(string ticker, double share)
        {
            // Расчет позиции
            double cost = TotalSum * share;
            double price = BalanceData[ticker].Price;
            int lot = InstrumentData[ticker].Lot ?? 1;
            double size = Math.Truncate(cost / price / lot) * lot;
            double realCost = size * price;
            BalanceData[ticker].Size += size;

            // Списываем деньги
            BalanceData[RUB].Size -= realCost;
        }

        /// <summary>
        /// Закупить на остаток фонд ликвидности
        /// </summary>       
        public void OpenPositionMon()
        {
            // Закупаем на остатки фонд ликвидности
            double cost = BalanceData[RUB].Cost;
            double price = BalanceData[MON].Price;
            double size = Math.Truncate(cost / price);
            double realCost = size * price;
            BalanceData[MON].Size += size;

            // Списываем деньги
            BalanceData[RUB].Size -= realCost;
        }

        /// <summary>
        /// Обновить цены активов
        /// </summary>
        public void UpdatePrices()
        {
            foreach (var (ticker, _) in BalanceData.Where(x => x.Key != RUB).ToDictionary())
                BalanceData[ticker].Price = CandleData[ticker].FindLast(x => x.Date <= CurrentDate.AddDays(-1))?.Close ?? 0.0;
        }
    }
}
