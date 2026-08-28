using Oid85.FinMarket.Algo.Infrastructure.Database.Entities.Base;

namespace Oid85.FinMarket.Algo.Infrastructure.Database.Entities
{
    /// <summary>
    /// Параметр
    /// </summary>
    public class ParameterEntity : BaseEntity
    {
        /// <summary>
        /// Наименование
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Значение
        /// </summary>
        public string Value { get; set; }
    }
}
