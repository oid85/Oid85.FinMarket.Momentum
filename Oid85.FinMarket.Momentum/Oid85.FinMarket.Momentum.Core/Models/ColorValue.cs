namespace Oid85.FinMarket.Algo.Core.Models
{
    /// <summary>
    /// Цвет - значение
    /// </summary>
    public class ColorValue<T>
    {
        public string Color { get; set; }
        public T Value { get; set; }
    }
}
