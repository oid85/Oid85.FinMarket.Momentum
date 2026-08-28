namespace Oid85.FinMarket.Algo.Core.Models;

public class StrategyExecuteResult
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Начало периода
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Конец периода
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Тикер инструмента
    /// </summary>
    public string Ticker { get; set; }

    /// <summary>
    /// Наименование портфеля
    /// </summary>
    public string PortfolioName { get; set; }

    /// <summary>
    /// Наименование процесса
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// Описание стратегии
    /// </summary>
    public string StrategyDescription { get; set; }

    /// <summary>
    /// Наименование стратегии
    /// </summary>
    public string StrategyName { get; set; }

    /// <summary>
    /// Параметры стратегии
    /// </summary>
    public string StrategyParams { get; set; }

    /// <summary>
    /// Параметры стратегии (хэш)
    /// </summary>
    public string StrategyParamsHash { get; set; }

    /// <summary>
    /// Количество сделок
    /// </summary>
    public int NumberPositions { get; set; }

    /// <summary>
    /// Текущая позиция
    /// </summary>
    public int CurrentPosition { get; set; }

    /// <summary>
    /// Текущая позиция (стоимость)
    /// </summary>
    public double CurrentPositionCost { get; set; }

    /// <summary>
    /// Profit Factor
    /// </summary>
    public double ProfitFactor { get; set; }

    /// <summary>
    /// Recovery Factor
    /// </summary>
    public double RecoveryFactor { get; set; }

    /// <summary>
    /// Total Net Profit
    /// </summary>
    public double TotalNetProfit { get; set; }

    /// <summary>
    /// Average Net Profit
    /// </summary>
    public double AverageNetProfit { get; set; }

    /// <summary>
    /// Average Profit Percent
    /// </summary>
    public double AverageNetProfitPercent { get; set; }

    /// <summary>
    /// Drawdown
    /// </summary>
    public double Drawdown { get; set; }

    /// <summary>
    /// Max Drawdown
    /// </summary>
    public double MaxDrawdown { get; set; }

    /// <summary>
    /// MaxDrawdownPercent
    /// </summary>
    public double MaxDrawdownPercent { get; set; }

    /// <summary>
    /// Winning Positions
    /// </summary>
    public int WinningPositions { get; set; }

    /// <summary>
    /// Winning Trades Percent
    /// </summary>
    public double WinningTradesPercent { get; set; }

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
    /// Капитал
    /// </summary>
    public Dictionary<DateOnly, double> EqiutyCurve { get; set; } = [];

    /// <summary>
    /// Просадка
    /// </summary>
    public Dictionary<DateOnly, double> DrawdownCurve { get; set; } = [];

    /// <summary>
    /// График
    /// </summary>
    public List<DiagramPoint> DiagramPoints { get; set; } = [];

    /// <summary>
    /// Позиции
    /// </summary>
    public SortedDictionary<DateOnly, Position> Positions { get; set; } = [];

    /// <summary>
    /// Последняя активная позиция
    /// </summary>
    public Position? LastActivePosition { get; set; } = null;

    /// <summary>
    /// Последняя позиция
    /// </summary>
    public Position? LastPosition { get; set; } = null;
}