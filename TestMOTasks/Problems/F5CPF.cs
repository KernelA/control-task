namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class F5CPF : BaseFCPF
    {
        private double[] _resSum;

        protected override IReadOnlyList<double> TempSum(IReadOnlyList<double> Point)
        {
            bool isEven = true;
            _resSum[0] = _resSum[1] = 0.0;
            double temp = 0.0;

            for (int i = 1; i < Point.Count; i++)
            {
                temp = (Point[i] - Math.Pow(Point[0], 0.5 * (1.0 + 3 * (i - 1.0) / (base.LowerBounds.Count - 2))));

                if (isEven)
                {
                    _resSum[1] += temp * temp;
                }
                else
                {
                    _resSum[0] += temp * temp;
                }

                isEven = !isEven;
            }

            return _resSum;
        }

        public F5CPF(int Dim) : base(2, "F5CPF", Enumerable.Repeat(0.0, 1).Concat(Enumerable.Repeat(-1.0, Dim - 1)).ToList(), Enumerable.Repeat(1.0, 1).Concat(Enumerable.Repeat(1.0, Dim - 1)).ToList())
        {
            _resSum = new double[2];
        }
    }
}