namespace Oid85.FinMarket.Algo.Core.Models;

public class Position
{
    public string Ticker { get; set; } = string.Empty;
    public double EntryPrice { get; set; }
    public double? ExitPrice { get; set; } = null;
    public DateOnly EntryDate { get; set; }
    public DateOnly? ExitDate { get; set; } = null;
    public int EntryCandleIndex { get; set; }
    public int? ExitCandleIndex { get; set; } = null;
    public bool IsActive { get; set; }
    public bool IsLong { get; set; }
    public bool IsShort { get; set; }
    public int Quantity { get; set; }
    public double Cost { get; set; }
    public double NetProfit { get; set; }
    public double NetProfitPercent { get; set; }
    public double TotalNetProfit { get; set; }
    public double TotalProfitPct { get; set; }
}