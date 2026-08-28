namespace Oid85.FinMarket.Algo.Core.Models;

public class Candle
{
    /// <summary>
    /// Индекс свечи
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Цена открытия
    /// </summary>
    public double Open { get; set; }

    /// <summary>
    /// Цена закрытия
    /// </summary>
    public double Close { get; set; }

    /// <summary>
    /// Макс. цена
    /// </summary>
    public double High { get; set; }

    /// <summary>
    /// Мин. цена
    /// </summary>
    public double Low { get; set; }

    /// <summary>
    /// Объем
    /// </summary>
    public long Volume { get; set; }

    /// <summary>
    /// Дата
    /// </summary>
    public DateOnly Date { get; set; }
}