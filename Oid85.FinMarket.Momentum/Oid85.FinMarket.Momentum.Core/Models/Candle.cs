namespace Oid85.FinMarket.Momentum.Core.Models;

public class Candle
{
    /// <summary>
    /// Индекс свечи
    /// </summary>
    public int Index { get; set; } = 0;

    /// <summary>
    /// Цена открытия
    /// </summary>
    public double Open { get; set; } = 0.0;

    /// <summary>
    /// Цена закрытия
    /// </summary>
    public double Close { get; set; } = 0.0;

    /// <summary>
    /// Макс. цена
    /// </summary>
    public double High { get; set; } = 0.0;

    /// <summary>
    /// Мин. цена
    /// </summary>
    public double Low { get; set; } = 0.0;

    /// <summary>
    /// Объем
    /// </summary>
    public long Volume { get; set; } = 0;

    /// <summary>
    /// Дата
    /// </summary>
    public DateOnly Date { get; set; } = DateOnly.MinValue;
}