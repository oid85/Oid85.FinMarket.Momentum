namespace Oid85.FinMarket.Momentum.Common.Extensions
{
    /// <summary>
    /// Математические операции
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Сдвиг вправо
        /// </summary>
        public static List<double> Shift(this List<double> values, int shift)
        {
            var result = new List<double>();
            
            for (int i = 0; i < shift; i++)
                result.Add(0.0);

            for (int i = 0; i < values.Count - shift; i++)
                result.Add(values[i]);

            return result;
        }
        
        /// <summary>
        /// Инициализация коллекции нулями
        /// </summary>
        public static List<double> InitValues(this List<double> values, int n)
        {
            for (int i = 0; i < n; i++)
                values.Add(0.0);

            return values;
        }
        
        /// <summary>
        /// Умножение ряда на константу
        /// </summary>
        public static List<double> MultConst(this List<double> values, double constValue) => 
            values.Select((x, i) => x * constValue).ToList();

        /// <summary>
        /// Сложение двух рядов
        /// </summary>
        public static List<double>? Add(this List<double> values, List<double> add) => 
            values.Count != add.Count ? null : values.Select((x, i) => x + add[i]).ToList();

        /// <summary>
        /// Вычитание из одного ряда другого
        /// </summary>
        public static List<double>? Sub(this List<double> values, List<double> sub) => 
            values.Count != sub.Count ? null : values.Select((x, i) => x - sub[i]).ToList();

        /// <summary>
        /// Деление ряда на константу
        /// </summary>
        public static List<double> DivConst(this List<double> values, double constValue) => 
            values.Select((x, i) => x / constValue).ToList();
        
        /// <summary>
        /// Добавление константы к ряду
        /// </summary>
        public static List<double> AddConst(this List<double> values, double constValue) => 
            values.Select((x, i) => x + constValue).ToList();        
        
        /// <summary>
        /// Дисперсия
        /// </summary>
        public static double Variance(this List<double> values)
        {
            double average = values.Average();
            double sum = values.Sum(x => (x - average) * (x - average));
            return sum / (values.Count - 1);
        }
        
        /// <summary>
        /// Стандартное отклонение
        /// </summary>
        public static double StdDev(this List<double> values) => 
            Math.Sqrt(values.Variance());

        /// <summary>
        /// Логарифмирование ряда (натуральным логарифмом)
        /// </summary>
        public static List<double> Log(this List<double> values)
        {
            var result = new List<double>();

            for (int i = 0; i < values.Count; i++)
                if (values[i] > 0)
                    result.Add(Math.Log(values[i]));

                else
                    result.Add(0);
            
            return result;
        }
        
        /// <summary>
        /// Приращения (разность текущего и предыдущего значений)
        /// </summary>
        public static List<double> Increments(this List<double> values)
        {
            var result = new List<double> { 0.0 };

            for (int i = 1; i < values.Count; i++)
                result.Add(values[i] - values[i - 1]);
            
            return result;
        }        
        
        /// <summary>
        /// Размах
        /// </summary>
        public static double Range(this List<double> values) => 
            values.Max() - values.Min();
        
        /// <summary>
        /// Корреляция по формуле Пирсона
        /// </summary>
        public static double Correlation(this List<double> valuesX, List<double> valuesY)
        {
            if (valuesX.Count != valuesY.Count)
                return 0.0;

            double averageX = valuesX.Average();
            double averageY = valuesY.Average();

            double sum = 0.0;
            double sumX = 0.0;
            double sumY = 0.0;

            for (int i = 0; i < valuesX.Count; i++)
            {
                sum += (valuesX[i] - averageX) * (valuesY[i] - averageY);
                sumX += (valuesX[i] - averageX) * (valuesX[i] - averageX);
                sumY += (valuesY[i] - averageY) * (valuesY[i] - averageY);
            }

            return sum / Math.Sqrt(sumX * sumY);
        }    
        
        /// <summary>
        /// Центрирование
        /// </summary>
        public static List<double> Centering(this List<double> values)
        {
            double average = values.Average();
            return values.Select(x => x - average).ToList();
        }         
    }
}
