using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Interfaces.Factories;

public interface IIndicatorFactory
{
    List<double> Highest(List<double> values, int period);
    List<double> Lowest(List<double> values, int period);
    List<double> Sma(List<double> values, int period);
    List<double> Supertrend(List<Candle> candles, int period, double multiplier);
    List<double> Atr(List<Candle> candles, int period);
    List<double> Hma(List<Candle> candles, int period);
    List<double> Ema(List<Candle> candles, int period);
    List<double> Adx(List<Candle> candles, int period);
    List<double> UltimateSmoother (List<double> values, int period);
    (List<double> UpperBand, List<double> LowerBand) BollingerBands (List<Candle> candles, int period, double stdDev);
    (List<double> UpperBand, List<double> LowerBand) AdaptivePriceChannelAdx (List<Candle> candles, int periodAdx, int periodPc);
    List<double> EhlersNonlinearFilter(List<Candle> candles);
    List<double> AdaptiveParabolic(List<Candle> candles, int period);
    List<double> Nrtr(List<Candle> candles, int period, double multiplier);
    List<double> Hurst(List<Candle> candles, int period);
}