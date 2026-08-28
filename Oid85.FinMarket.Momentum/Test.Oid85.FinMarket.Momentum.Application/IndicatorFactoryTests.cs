using Oid85.FinMarket.Algo.Application.Factories;
using Oid85.FinMarket.Algo.Application.Interfaces.Factories;

namespace Test.Oid85.FinMarket.Algo.Application;

public class IndicatorFactoryTests
{
    private readonly IIndicatorFactory _indicatorFactory;
    
    public IndicatorFactoryTests()
    {
        _indicatorFactory = new IndicatorFactory();
    }
    
    [Fact]
    public void Highest_return_is_correct()
    {
        // Arrange
        const int period = 3;
        var values = new List<double> {0.1, 0.2, 0.3, 0.2, 0.5, 0.6, 0.5};

        // Act
        var expected = new List<double> { 0.0, 0.0, 0.0, 0.3, 0.5, 0.6, 0.6 };
        var indicatorValues = _indicatorFactory.Highest(values, period);
        
        // Assert
        Assert.True(Math.Abs(indicatorValues[0] - expected[0]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[1] - expected[1]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[2] - expected[2]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[3] - expected[3]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[4] - expected[4]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[5] - expected[5]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[6] - expected[6]) < 0.01);
    }
    
    [Fact]
    public void Lowest_return_is_correct()
    {
        // Arrange
        const int period = 3;
        var values = new List<double> {0.1, 0.2, 0.3, 0.2, 0.5, 0.6, 0.5};

        // Act
        var expected = new List<double> { 0.0, 0.0, 0.0, 0.2, 0.2, 0.2, 0.5 };
        var indicatorValues = _indicatorFactory.Lowest(values, period);
        
        // Assert
        Assert.True(Math.Abs(indicatorValues[0] - expected[0]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[1] - expected[1]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[2] - expected[2]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[3] - expected[3]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[4] - expected[4]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[5] - expected[5]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[6] - expected[6]) < 0.01);
    }  
    
    [Fact]
    public void Sma_return_is_correct()
    {
        // Arrange
        const int period = 3;
        var values = new List<double> {0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7};

        // Act
        var expected = new List<double> { 0.0, 0.0, 0.0, 0.3, 0.4, 0.5, 0.6 };
        var indicatorValues = _indicatorFactory.Sma(values, period);
        
        // Assert
        Assert.True(Math.Abs(indicatorValues[0] - expected[0]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[1] - expected[1]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[2] - expected[2]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[3] - expected[3]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[4] - expected[4]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[5] - expected[5]) < 0.01);
        Assert.True(Math.Abs(indicatorValues[6] - expected[6]) < 0.01);
    }     
}