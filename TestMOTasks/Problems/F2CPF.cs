namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.Linq.Expressions;

    class F2CPF : BaseFCPF
    {
        private double[] _resSum;

        private const double PI_6 = Math.PI * 6;

        public F2CPF(int Dim) : base(2, "F2CPF", Enumerable.Repeat(0.0, Dim).ToArray(), Enumerable.Repeat(1.0, Dim).ToArray())
        {
            _lowerBounds[_lowerBounds.Length - 1] = -1;
            _upperBounds[_upperBounds.Length - 1] = 1;
            _resSum = new double[2];
        }

        protected override IReadOnlyList<double> TempSum(IReadOnlyList<double> Point)
        {
            bool isEven = true;
            _resSum[0] = _resSum[1] = 0.0;

            double temp = 0.0, x1;

            for (int i = 1; i < Point.Count; i++)
            {
                x1 = Math.Sin(PI_6 * Point[0] + i * Math.PI / LowerBounds.Count);

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
    }
}
