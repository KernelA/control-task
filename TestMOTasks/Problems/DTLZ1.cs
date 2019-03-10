namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class DTLZ1 : BaseProblem
    {
        private const double PI_20 = Math.PI * 20;

        private const int k = 5;

        private double[] _xm, _res;

        public DTLZ1(int CountObjs) : base(CountObjs, $"DTLZ1({CountObjs})", Enumerable.Repeat(0.0, CountObjs + k - 1).ToArray(), Enumerable.Repeat(1.0, CountObjs + k - 1).ToArray())
        {
            _xm = new double[k];
            _res = new double[CountObjs];
        }

        private double G()
        {
            double sum = 0, temp;

            for (int i = 0; i < _xm.Length; i++)
            {
                temp  = _xm[i] - 0.5;
                sum += temp * temp - Math.Cos(PI_20 * temp);
            }

            return 100 * (k + sum);
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
                    product *= Point[j];
                }

                if(numObj != 0)
                {
                    product *= (1 - Point[i]);
                }

                _res[numObj] = 0.5 * product * (1 + g);

                numObj++;
            }

            return _res;
        }
    }
}
