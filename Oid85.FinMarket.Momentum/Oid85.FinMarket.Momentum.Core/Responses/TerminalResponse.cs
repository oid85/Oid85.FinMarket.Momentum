using Oid85.FinMarket.Momentum.Common.KnownConstants;

namespace Oid85.FinMarket.Momentum.Core.Responses
{
    public class TerminalResponse
    {
        public decimal TotalSum { get; set; }
        public decimal Money { get; set; }
        public decimal TotalDailyPnl { get; set; }
        public List<TerminalRow> Rows { get; set; } = [];
    }

    public class TerminalRow
    {
        public string Ticker { get; set; } = string.Empty;
        public TerminalTargetPosition TargetPosition { get; set; } = new();
        public TerminalLifePosition LifePosition { get; set; } = new();
        public TerminalSyncSizeButton SyncSizeButton { get; set; } = new();
        public TerminalTargetStop TargetStop { get; set; } = new();
        public TerminalLifeStop LifeStop { get; set; } = new();
        public TerminalSyncStopButton SyncStopButton { get; set; } = new();
        public TerminalSyncTickerSizeTask SyncTickerSizeTask { get; set; } = new();
        public TerminalSyncTickerStopTask SyncTickerStopTask { get; set; } = new();
    }

    public class TerminalTargetPosition
    {
        public bool DoShow { get; set; } = false;
        public int Size { get; set; }
        public string ColorFill { get; set; } = KnownColors.White;
    }

    public class TerminalLifePosition
    {
        public bool DoShow { get; set; } = false;
        public int Size { get; set; }
        public string ColorFill { get; set; } = KnownColors.White;
        public decimal Cost { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal DailyPnl { get; set; }
    }

    public class TerminalSyncSizeButton
    {
        public bool DoShow { get; set; } = false;
        public string Title { get; set; } = string.Empty;
        public string ColorFill { get; set; } = KnownColors.White;
        public string Task { get; set; } = string.Empty;
    }

    public class TerminalTargetStop
    {
        public bool DoShow { get; set; } = false;
        public int Size { get; set; }
        public decimal StopPrice { get; set; }
        public string ColorFill { get; set; } = KnownColors.White;
    }

    public class TerminalLifeStop
    {
        public bool DoShow { get; set; } = false;
        public int Size { get; set; }
        public decimal StopPrice { get; set; }
        public string ColorFill { get; set; } = KnownColors.White;
    }

    public class TerminalSyncStopButton
    {
        public bool DoShow { get; set; } = false;
        public string Title { get; set; } = string.Empty;
        public string ColorFill { get; set; } = KnownColors.White;
        public string Task { get; set; } = string.Empty;
    }

    public class TerminalSyncTickerSizeTask
    {
        public bool DoShow { get; set; } = false;
        public string State { get; set; } = string.Empty;
        public string ColorFill { get; set; } = KnownColors.White;
        public string Task { get; set; } = string.Empty;
    }

    public class TerminalSyncTickerStopTask
    {
        public bool DoShow { get; set; } = false;
        public string State { get; set; } = string.Empty;
        public string ColorFill { get; set; } = KnownColors.White;
        public string Task { get; set; } = string.Empty;
    }
}
