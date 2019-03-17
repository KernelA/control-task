namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class DTLZ2 : BaseProblem
    {
        private const int k = 10;
        private const double PI_2 = Math.PI / 2;
        private double[] _xm, _res;

        private double G()
        {
            double sum = 0, temp;

            for (int i = 0; i < _xm.Length; i++)
            {
                temp = _xm[i] - 0.5;
                sum += temp * temp;
            }

            return sum;
        }

        public DTLZ2(int CountObjs) : base(CountObjs, $"DTLZ2({CountObjs})", Enumerable.Repeat(0.0, CountObjs + k - 1).ToArray(), Enumerable.Repeat(1.0, CountObjs + k - 1).ToArray())
        {
            _xm = new double[k];
            _res = new double[CountObjs];
        }

        public override double ObjFunction(IReadOnlyList<double> Point, int NumObj) => throw new NotImplementedException();

        public override IEnumerable<double> TargetFunction(IReadOnlyList<double> Point)
        {
            for (int i = 0; i < _xm.Length; i++)
            {
                _xm[i] = Point[CountObjs - 1 + i];
            }

            double g = G();

            int numObj = 0;

            for (int i = CountObjs - 1; i >= 0; i--)
            {
                double product = 1;

                for (int j = 0; j < i; j++)
                {
                    product *= Math.Cos(Point[j] * PI_2);
                }

                if (numObj == 0)
                {
                    product *= Math.Cos(Point[i] * PI_2);
                }
                else if (numObj == 1)
                {
                    product *= Math.Cos(Point[i] * PI_2) * Math.Sin(Point[i + 1] * PI_2);
                }
                else
                {
                    product *= Math.Sin(Point[i] * PI_2);
                }

                _res[numObj] = product * (1 + g);

                numObj++;
            }

            return _res;
        }
    }
}