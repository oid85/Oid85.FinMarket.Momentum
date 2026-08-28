namespace Oid85.FinMarket.Algo.Core.Models
{
    /// <summary>
    /// Инструмент
    /// </summary>
    public class Instrument
    {
        /// <summary>
        /// Тикер
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// Наименование
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тип
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Лот
        /// </summary>
        public int? Lot { get; set; } = null;
    }
}
