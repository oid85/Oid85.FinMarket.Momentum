namespace Oid85.FinMarket.Algo.Core.Responses.ApiClient
{
    public class GetCandleListResponse
    {
        public GetCandleListResult Result { get; set; } = new();
    }

    public class GetCandleListResult
    {
        public List<CandleListItem> Candles { get; set; } = [];
    }

    public class CandleListItem
    {
        /// <summary>
        /// Дата
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Открытие
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// Закрытие
        /// </summary>
        public double Close { get; set; }

        /// <summary>
        /// Минимум
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// Максимум
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// Объем
        /// </summary>
        public long Volume { get; set; }
    }
}
