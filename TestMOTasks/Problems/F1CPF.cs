namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class F1CPF : BaseFCPF
    {
        private double[] _resSum;

        protected override IReadOnlyList<double> TempSum(IReadOnlyList<double> Point)
        {
            bool isEven = true;
            _resSum[0] = _resSum[1] = 0.0;
            double temp = 0.0, x1;

            for (int i = 1; i < Point.Count; i++)
            {
                x1 = Math.Pow(Point[0], 0.5 * (1.0 + 3 * (i - 1.0) / (base.LowerBounds.Count - 2)));

                if (isEven)
                {
                    temp = Point[i] - x1;
                    _resSum[1] += temp * temp;
                }
                else
                {
                    temp = Point[i] - x1;
                    _resSum[0] += temp * temp;
                }

                isEven = !isEven;
            }

            return _resSum;
        }

        public F1CPF(int Dim) : base(2, "F1CPF", Enumerable.Repeat(0.0, Dim).ToArray(), Enumerable.Repeat(1.0, Dim).ToArray())
        {
            _resSum = new double[2];
        }
    }
}