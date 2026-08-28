namespace Oid85.FinMarket.Algo.Core.Models;

public class Strategy
{
    public virtual string StrategyDescription { get; set; } = string.Empty;

    public virtual string StrategyName { get; set; } = string.Empty;

    public virtual List<StrategyParameter> StrategyParameters { get; set; } = [];

    public double StartMoney { get; set; }

    public double EndMoney { get; set; }
    
    public string Ticker { get; set; } = string.Empty;   

    public string PortfolioName { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;

    public DateOnly StartDate => Candles.First().Date;
    
    public DateOnly EndDate => Candles.Last().Date;
    
    public Dictionary<string, int> Parameters { get; set; } = [];

    public int StabilizationPeriod { get; set; } = 1;
    
    public Dictionary<string, List<Candle>> CandleData { get; set; } = [];

    public List<Candle> Candles => CandleData[Ticker];

    public List<double> OpenPrices => [.. Candles.Select(x => x.Open)];
    
    public List<double> ClosePrices => [.. Candles.Select(x => x.Close)];
    
    public List<double> HighPrices => [.. Candles.Select(x => x.High)];
    
    public List<double> LowPrices => [.. Candles.Select(x => x.Low)];

    public List<DiagramPoint> DiagramPoints { get; set; } = [];
    
    public bool SignalLong { get; set; }
    
    public bool SignalShort { get; set; }

    public bool FilterLong { get; set; } = true;

    public bool FilterShort { get; set; } = true;
    
    public bool SignalCloseLong { get; set; }
    
    public bool SignalCloseShort { get; set; }

    public List<StopLimit?> StopLimits { get; set; } = [];

    public SortedDictionary<DateOnly, Position> Positions { get; set; } = [];

    public int GetPositionSize(double orderPrice)
    {
        if (EndMoney <= orderPrice)
            return 0;

        return orderPrice == 0.0 ? 0 : Convert.ToInt32(EndMoney / orderPrice);        
    }

    public Position? LastActivePosition {
        get
        {
            if (LastPosition is null)
                return null;
            
            if (LastPosition.IsActive)
                return LastPosition;

            return null;
        }
    }

    public Position? LastPosition => Positions.Count == 0 ? null : Positions.Last().Value;
    
    public int CurrentPosition
    {
        get
        {
            if (LastActivePosition == null)
                return 0;

            if (LastActivePosition.IsLong)
                return Math.Abs(LastActivePosition.Quantity);

            if (LastActivePosition.IsShort)
                return -1 * Math.Abs(LastActivePosition.Quantity);

            return 0;
        }
    }

    public double CurrentPositionCost
    {
        get
        {
            if (LastActivePosition == null)
                return 0.0;

            if (LastActivePosition.IsLong)
                return LastActivePosition.Cost;

            if (LastActivePosition.IsShort)
                return -1 * Math.Abs(LastActivePosition.Cost);

            return 0.0;
        }
    }
    
    public void BuyAtPrice(int quantity, double price, int candleIndex) =>
        AddTrade(
            new()
            {
                CandleIndex = candleIndex,
                Quantity = Math.Abs(quantity),
                Price = price,
                Date = Candles[candleIndex].Date
            });

    public void SellAtPrice(int quantity, double price, int candleIndex) =>
        AddTrade(
            new()
            {
                CandleIndex = candleIndex,
                Quantity = -1 * Math.Abs(quantity),
                Price = price,
                Date = Candles[candleIndex].Date
            });

    private void AddTrade(Trade trade)
    {
        if (trade.Quantity == 0)
            return;                

        if (LastActivePosition is null)
            Positions.Add(
                trade.Date, 
                new()
                {
                    Ticker = Ticker,
                    EntryPrice = trade.Price,
                    EntryDate = trade.Date,
                    EntryCandleIndex = trade.CandleIndex,
                    IsActive = true,
                    IsLong = trade.Quantity > 0,
                    IsShort = trade.Quantity < 0,
                    Quantity = trade.Quantity,
                    Cost = trade.Quantity * trade.Price
                });

        else
        {
            int count = Positions.Count;

            var key = Positions.Last().Key;

            Positions[key].ExitPrice = trade.Price;
            Positions[key].ExitDate = trade.Date;
            Positions[key].ExitCandleIndex = trade.CandleIndex;
            Positions[key].IsActive = false;

            double profit = 0.0;
            
            if (Positions[key].IsLong)
                profit = Math.Abs(Positions[key].Quantity) * (Positions[key].ExitPrice!.Value - Positions[key].EntryPrice);
            
            if (Positions[key].IsShort)
                profit = Math.Abs(Positions[key].Quantity) * (Positions[key].EntryPrice - Positions[key].ExitPrice!.Value);            
            
            Positions[key].NetProfit = profit;
            Positions[key].NetProfitPercent = profit / EndMoney * 100.0;
            
            var totalProfit = Positions.Sum(x => x.Value.NetProfit);
            Positions[key].TotalNetProfit = totalProfit;
            Positions[key].TotalProfitPct = totalProfit / EndMoney * 100.0;
            
            EndMoney += profit;
            
            EqiutyCurve.TryAdd(Positions[key].ExitDate!.Value, Positions[key].TotalNetProfit);
            
            double drawdown;
            
            if (count < 2)
                drawdown = 0.0;

            else
            {
                var maxTotalProfit = Positions.Take(count - 1).Max(x => x.Value.TotalNetProfit);

                drawdown = Positions[key].TotalNetProfit >= maxTotalProfit
                    ? 0.0
                    : maxTotalProfit - Positions[key].TotalNetProfit;
            }
            
            DrawdownCurve.TryAdd(Positions[key].ExitDate!.Value, drawdown);
        }
    }

    public void CloseAtStop(Position position, double stopPrice, int candleIndex)
    {
        if (StopLimits.Count != Candles.Count)
        {
            StopLimits.Clear();
            
            for (int i = 0; i < Candles.Count; i++) 
                StopLimits.Add(null);            
        }

        // Если последняя свеча
		if (candleIndex > StopLimits.Count - 1)
			return;
		
		// Пробуем выйти по стопу
		if (LastActivePosition is not null && StopLimits[candleIndex - 1] is not null)		
		{
			if (position.IsLong && Candles[candleIndex - 1].Close <= StopLimits[candleIndex - 1]!.StopPrice)			
				SellAtPrice(position.Quantity, stopPrice, candleIndex);	
			
			else if (position.IsShort && Candles[candleIndex - 1].Close >= StopLimits[candleIndex - 1]!.StopPrice)			
				BuyAtPrice(position.Quantity, stopPrice, candleIndex);				
		}
		
		// Если не вышли, то переставляем стоп
		if (LastActivePosition is not null)
			StopLimits[candleIndex] = new StopLimit { StopPrice = stopPrice, Quantity = position.Quantity };
    }    
    
    public void CloseAtPrice(Position position, double price, int candleIndex)
    {
        // Отправляем команды, если длинная позиция
        if (position.IsLong)
            SellAtPrice(position.Quantity, price, candleIndex);

        // Отправляем команды, если короткая позиция
        else if (position.IsShort)
            BuyAtPrice(position.Quantity, price, candleIndex);
    }

    public Dictionary<DateOnly, double> EqiutyCurve { get; set; } = [];

    public Dictionary<DateOnly, double> DrawdownCurve  { get; set; } = [];
    
    public double ProfitFactor
    {
        get
        {
            if (Positions.Count == 0) return 0.0;

            double profits = Positions.Where(x => x.Value.NetProfit > 0.0).Sum(x => x.Value.NetProfit);
            double losses = Positions.Where(x => x.Value.NetProfit < 0.0).Sum(x => x.Value.NetProfit);

            if (losses == 0.0) return double.PositiveInfinity;
            
            return profits / Math.Abs(losses);
        }
    }

    public double RecoveryFactor => MaxDrawdown == 0.0 ? double.PositiveInfinity : TotalNetProfit / MaxDrawdown;

    public double TotalNetProfit => Positions.Count == 0 ? 0.0 : Positions.Sum(x => x.Value.NetProfit);

    public double AverageNetProfit => Positions.Count == 0 ? 0.0 : Positions.Select(x => x.Value.NetProfit).Average();

    public double AverageNetProfitPercent => Positions.Count == 0 ? 0.0 : Positions.Select(x => x.Value.NetProfitPercent).Average();

    public double Drawdown  => LastPosition is null ? 0.0 : Positions.Max(x => x.Value.TotalNetProfit) - LastPosition.TotalNetProfit;

    public double MaxDrawdown  => DrawdownCurve.Count == 0 ? 0.0 : Math.Abs(DrawdownCurve.Max(x => x.Value));

    public double MaxDrawdownPercent => EqiutyCurve.Count == 0 ? 0.0 : Math.Abs(MaxDrawdown / EqiutyCurve.Max(x => x.Value) * 100.0);

    public int NumberPositions => Positions.Count;

    public int WinningPositions => Positions.Count == 0 ? 0 : Positions.Count(x => x.Value.NetProfit > 0.0);

    public double WinningTradesPercent => NumberPositions == 0.0 ? 0.0 : Convert.ToDouble(WinningPositions) / Convert.ToDouble(NumberPositions) * 100.0;    
    
    public double TotalReturn => EndMoney > StartMoney ? (EndMoney - StartMoney) / StartMoney * 100.0 : 0.0;
    
    public double AnnualYieldReturn => EndMoney > StartMoney ? TotalReturn / ((EndDate.DayNumber - StartDate.DayNumber) / 365.0): 0.0;
    
    public virtual void Execute()
    {

    }

    public void Init(Dictionary<string, int> parameterSet, double money)
    {
        Parameters = parameterSet;
        StopLimits.Clear();
        Positions.Clear();
        EqiutyCurve.Clear();
        DrawdownCurve.Clear();
        StartMoney = money;
        EndMoney = money;

        DiagramPoints.Clear();
        for (int i = 0; i < Candles.Count; i++)
            DiagramPoints.Add(new()
            {
                Index = Candles[i].Index,
                Date = Candles[i].Date
            });
    }
}