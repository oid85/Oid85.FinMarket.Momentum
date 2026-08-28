using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Application.Mapping;
using Oid85.FinMarket.Algo.Common.Extensions;
using Oid85.FinMarket.Algo.Core.Models;
using Skender.Stock.Indicators;

namespace Oid85.FinMarket.Algo.Application.Factories;

public class IndicatorFactory : IIndicatorFactory
{
    public List<double> Highest(List<double> values, int period) => 
        values.Select((_, i) => i - period < 0 ? 0.0 : values.Skip(i - period + 1).Take(period).Max()).ToList();

    public List<double> Lowest(List<double> values, int period) => 
        values.Select((_, i) => i - period < 0 ? 0.0 : values.Skip(i - period + 1).Take(period).Min()).ToList();

    public List<double> Sma(List<double> values, int period) => 
        values.Select((_, i) => i - period < 0 ? 0.0 : values.Skip(i - period + 1).Take(period).Average()).ToList();

    public List<double> Supertrend(List<Candle> candles, int period, double multiplier) =>
        candles
            .Select(ApplicationMapper.Map)
            .GetSuperTrend(period, multiplier)
            .Select(x => x.SuperTrend)
            .Select(x => x.HasValue ? Convert.ToDouble(x.Value) : 0.0)
            .ToList();

    public List<double> Atr(List<Candle> candles, int period) =>
        candles
            .Select(ApplicationMapper.Map)
            .GetAtr(period)
            .Select(x => x.Atr)
            .Select(x => x.HasValue ? Convert.ToDouble(x.Value) : 0.0)
            .ToList();

    public List<double> Hma(List<Candle> candles, int period) =>
        candles
            .Select(ApplicationMapper.Map)
            .GetHma(period)
            .Select(x => x.Hma)
            .Select(x => x.HasValue ? Convert.ToDouble(x.Value) : 0.0)
            .ToList();
    
    public List<double> Ema(List<Candle> candles, int period) =>
        candles
            .Select(ApplicationMapper.Map)
            .GetEma(period)
            .Select(x => x.Ema)
            .Select(x => x.HasValue ? Convert.ToDouble(x.Value) : 0.0)
            .ToList();
    
    public List<double> Adx(List<Candle> candles, int period) =>
        candles
            .Select(ApplicationMapper.Map)
            .GetAdx(period)
            .Select(x => x.Adx)
            .Select(x => x.HasValue ? Convert.ToDouble(x.Value) : 0.0)
            .ToList();

    public List<double> UltimateSmoother(List<double> values, int period)
    {
        // Ultimate Smoother function based on John Ehlers' formula
        double coeff = Math.Sqrt(2.0);
        double step = 2.0 * Math.PI / period;
        double a1 = Math.Exp(-1.0 * coeff * Math.PI / period);
        double b1 = 2.0 * a1 * Math.Cos(coeff * step / period);
        double c2 = b1;
        double c3 = -1.0 * a1 * a1;
        double c1 = (1 + c2 - c3) / 4.0;
        
        var us = new List<double>();

        for (int i = 0; i < values.Count; i++)
            us.Add(i < 3
                ? values[i]
                : (1 - c1) * values[i] + 
                  (2 * c1 - c2) * values[i - 1] - 
                  (c1 + c3) * values[i - 2] + 
                  c2 * us[i - 1] + 
                  c3 * us[i - 2]);

        return us;
    }

    public (List<double> UpperBand, List<double> LowerBand) BollingerBands(List<Candle> candles, int period, double stdDev)
    {
        var quotes = candles.Select(ApplicationMapper.Map);
        var bollingerBandsResults = quotes.GetBollingerBands(period, stdDev);
        
        var upperBand = new List<double>();
        var lowerBand = new List<double>();

        foreach (var bollingerBandsResult in bollingerBandsResults)
        {
            upperBand.Add(bollingerBandsResult.UpperBand ?? 0.0);
            lowerBand.Add(bollingerBandsResult.LowerBand ?? 0.0);
        }

        return (upperBand, lowerBand);
    }

    public (List<double> UpperBand, List<double> LowerBand) AdaptivePriceChannelAdx(List<Candle> candles, int periodAdx, int periodPc)
    {
        List<double> price = candles.Select(x => (x.High + x.Low + x.Close + x.Close) / 4.0).ToList();
        List<double> adx = Adx(candles, periodAdx);
        List<double> upperBand = new List<double>().InitValues(candles.Count);
        List<double> lowerBand = new List<double>().InitValues(candles.Count);

        int startIndex = new List<int> { periodAdx, periodPc }.Max() + 1;
        
        for (int i = startIndex; i < candles.Count; i++)
        {
	        int n = (int) Math.Floor(periodPc * ((100.0 - adx[i]) / 100.0));

	        double max = price[i];
	        double min = price[i];

	        for (int j = i - n; j < i; j++) if (price[j] > max) max = price[j];
	        for (int j = i - n; j < i; j++) if (price[j] < min) min = price[j];

	        upperBand[i] = max;
	        lowerBand[i] = min;
        }
        
        return (upperBand, lowerBand);
    }

    public List<double> EhlersNonlinearFilter(List<Candle> candles)
    {
        List<double> price = candles.Select(x => (x.High + x.Low + x.Close + x.Close) / 4.0).ToList();
        List<double> coef = new List<double>().InitValues(candles.Count);
        List<double> dcef = new List<double>().InitValues(candles.Count);

        const int coefLookback = 5;
        for (int i = coefLookback; i < candles.Count; i++)
            coef[i] = Math.Pow(price[i] - price[i - 1], 2) +
                      Math.Pow(price[i] - price[i - 2], 2) +
                      Math.Pow(price[i] - price[i - 3], 2) +
                      Math.Pow(price[i] - price[i - 4], 2) +
                      Math.Pow(price[i] - price[i - 5], 2);
        
        double sumCoef = 0.0;
        double sumCoefPrice = 0.0;

        for (int i = coefLookback; i < candles.Count; i++)
        {
            for (int j = 0; j < coefLookback; j++)
            {
                sumCoef += coef[i - j];
                sumCoefPrice += coef[i - j] * price[i - j];
            }

            dcef[i] = sumCoefPrice / sumCoef;
        }
        
        return dcef;
    }

    public List<double> AdaptiveParabolic(List<Candle> candles, int period)
    {
        int oldBar = 0;
        bool lng = false;
        bool shrt = false;
        bool revers = false;

        double tr = 0.0;
        double atr = 0.0;
        double hmax = 0.0;
        double lmin = 0.0;
        double oldAtr = 0.0;
        double af = 0.0;

        var psar = new List<double>().InitValues(candles.Count);

        var highPrices = candles.Select(x => x.High).ToList();
        var lowPrices = candles.Select(x => x.Low).ToList();
        
        for (int i = 0; i < candles.Count; i++)
        {
            if (i < period)
	            psar[i] = 0.0;
            
            else
            {
	            if (i == period)
	            {
	                psar[i] = lowPrices[i];
	                lng = true;
		            hmax = highPrices[i];
	                tr = 0.0;

                    for (int j = i - period; j < i - 1; j++) 
	                    tr += highPrices[j] - lowPrices[j];

                    oldAtr = tr / period;
	                revers = true;
	            }
	            
	            else
	            {
		            if (i != oldBar)
		            {
		                tr = 0.0;

                        for (int j = i - period; j < i - 1; j++) 
	                        tr += highPrices[j] - lowPrices[j];

                        atr = tr / period;
		                af = atr / (oldAtr + atr);
		                af /= 10.0;
		                oldAtr = atr;

			            if (lng)
			            {
				            if (hmax < highPrices[i - 1]) 
					            hmax = highPrices[i - 1];

			                psar[i] = psar[i - 1] + af * (hmax - psar[i - 1]);
			            }

			            if (shrt)
			            {
				            if (lmin > lowPrices[i - 1]) 
					            lmin = lowPrices[i - 1];

			                psar[i] = psar[i - 1] + af * (lmin - psar[i - 1]);
			            }

		                revers = true;
		            }

		            if (lng && lowPrices[i] < psar[i] && revers)
		            {
		                psar[i] = hmax;
		                shrt = true;
		                lng = false;
		                lmin = lowPrices[i];
		                revers = false;
		            }

		            if (shrt && highPrices[i] > psar[i] && revers)
		            {
		                psar[i] = lmin;
		                lng = true;
		                shrt = false;
		                hmax = highPrices[i];
		                revers = false;
		            }
	            }

                oldBar = i;
            }
        }

        return psar;
    }

    public List<double> Nrtr(List<Candle> candles, int period, double multiplier)
    {
	    var nrtr = new List<double>().InitValues(candles.Count);

	    double reverse = 0;
	    int trend = 0;

	    var currentK = candles[0].Close;
	    var highPrice = candles[0].High;
	    var lowPrice = candles[0].Low;

	    for (int i = 0; i < candles.Count; i++)
	    {
		    double price = candles[i].Close;

		    double prevK = currentK;

		    currentK = (prevK + (price - prevK) / period) * multiplier;

		    int newTrend = 0;

		    if (trend >= 0)
		    {
			    if (price > highPrice)
				    highPrice = price;

			    reverse = highPrice - currentK;

			    if (price <= reverse)
			    {
				    newTrend = -1;
				    lowPrice = price;
				    reverse = lowPrice + currentK;
			    }
		    }
		    
		    if (trend <= 0)
		    {
			    if (price < lowPrice)
				    lowPrice = price;

			    reverse = lowPrice + currentK;

			    if (price >= reverse)
			    {
				    newTrend = +1;
				    highPrice = price;
				    reverse = highPrice - currentK;
			    }
		    }

		    if (newTrend != 0)
			    trend = newTrend;

		    nrtr[i] = reverse;
	    }

	    return nrtr;
    }

    public List<double> Hurst(List<Candle> candles, int period)
    {
	    /*
		H = log(R/S) / log(n/2)
		Где: 
		R - размах выборки, то есть разница между максимальным и минимальным значениями временного ряда в данной выборке.
		S - стандартное отклонение выборки.
		n - количество точек в выборке.
	    */
	    
	    var hurst = new List<double>().InitValues(candles.Count);

	    var closePrices = candles.Select(x => x.Close).ToList();
	    
	    for (int i = period; i < candles.Count; i++)
	    {
		    var values = new List<double>();

		    for (int j = i - period; j < i; j++)
			    values.Add(closePrices[j]);

		    double stdDev = values.StdDev(); // Стандартное отклонение
		    double range = values.Range();   // Размах
		    
		    hurst[i] = Math.Log10(range / stdDev) / Math.Log10(period / 2.0);
	    }

	    return hurst;
    }
}