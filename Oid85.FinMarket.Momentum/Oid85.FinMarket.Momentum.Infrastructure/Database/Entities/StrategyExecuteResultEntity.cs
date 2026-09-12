using Oid85.FinMarket.Momentum.Infrastructure.Database.Entities.Base;

namespace Oid85.FinMarket.Momentum.Infrastructure.Database.Entities;

public class StrategyExecuteResultEntity : BaseEntity
{
    /// <summary>
    /// Начало периода
    /// </summary>
    public DateOnly StartDate { get; set; }
    
    /// <summary>
    /// Конец периода
    /// </summary>
    public DateOnly EndDate { get; set; }
    
    /// <summary>
    /// Тикеры
    /// </summary>
    public string Tickers { get; set; }
    
    /// <summary>
    /// Наименование стратегии
    /// </summary>
    public string StrategyName { get; set; }
    
    /// <summary>
    /// Параметры стратегии
    /// </summary>
    public string StrategyParams { get; set; }    
    
    /// <summary>
    /// Recovery Factor
    /// </summary>
    public double RecoveryFactor { get; set; }
    
    /// <summary>
    /// Net Profit
    /// </summary>
    public double NetProfit { get; set; }      
    
    /// <summary>
    /// MaxDrawdownPercent
    /// </summary>
    public double MaxDrawdownPercent { get; set; }
    
    /// <summary>
    /// Start Money
    /// </summary>
    public double StartMoney { get; set; }
    
    /// <summary>
    /// End Money
    /// </summary>
    public double EndMoney { get; set; }
    
    /// <summary>
    /// Доходность всего, %
    /// </summary>
    public double TotalReturn { get; set; }
    
    /// <summary>
    /// Доходность годовая, %;
    /// </summary>
    public double AnnualYieldReturn { get; set; }

    /// <summary>
    /// Сообщение
    /// </summary>
    public string ResultMessage { get; set; }
	
    /// <summary>
    /// Эквити
    /// </summary>
    public string EquityCurve { get; set; }	
}