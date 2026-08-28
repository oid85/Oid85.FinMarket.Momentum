namespace Oid85.FinMarket.Algo.Core.Responses.ApiClient
{
    public class GetInstrumentListResponse
    {
        public GetInstrumentListResult Result { get; set; }
    }

    public class GetInstrumentListResult
    {
        public List<GetInstrumentListItemResponse> Instruments { get; set; }
    }

    public class GetInstrumentListItemResponse
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
        /// Дата погашения
        /// </summary>
        public DateOnly? MaturityDate { get; set; } = null;

        /// <summary>
        /// Кол-во купонов в год
        /// </summary>
        public int? CouponQuantityPerYear { get; set; } = null;

        /// <summary>
        /// НКД
        /// </summary>
        public double? Nkd { get; set; } = null;

        /// <summary>
        /// Цена
        /// </summary>
        public double? LastPrice { get; set; } = null;

        /// <summary>
        /// Номинал
        /// </summary>
        public double? Nominal { get; set; } = null;

        /// <summary>
        /// Валюта
        /// </summary>
        public string? Currency { get; set; } = null;

        /// <summary>
        /// Лот
        /// </summary>
        public int? Lot { get; set; } = null;

        /// <summary>
        /// Кредитный рейтинг
        /// </summary>
        public string? Rating { get; set; } = null;

        /// <summary>
        /// Признак плавающего купона
        /// </summary>
        public bool? FloatingCouponFlag { get; set; } = null;
    }
}
