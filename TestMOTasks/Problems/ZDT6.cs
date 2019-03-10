namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EOpt.Math.Optimization.MOOpt;

    internal class ZDT6 : BaseZDT
    {
        private double[] _res;

        private const double PI_6 = 6 * Math.PI;

        protected override double G(IReadOnlyList<double> Point)
        {
            double sum = 0;

            for (int i = 1; i < Point.Count; i++)
            {
                sum += Point[i];
            }

            return 1 + 9 * Math.Pow(sum / (_lowerBounds.Length - 1), 0.25);
        }

        public ZDT6(int dim) : base(dim, "ZDT6")
        {
            _res = new double[2];
        }

        private double F1(IReadOnlyList<double> Point)
        {
            return 1 - Math.Exp(-4 * Point[0]) * Math.Pow(Math.Sin(PI_6 * Point[0]), 6);
        }

        private double F2(IReadOnlyList<double> Point, double F1)
        {
            double temp = G(Point);

            return temp - F1 * F1 / temp;
        }

        public override IEnumerable<double> TargetFunction(IReadOnlyList<double> Point)
        {
            _res[0] = F1(Point);
            _res[1] = F2(Point, _res[0]);

            return _res;
        }

        public override double ObjFunction(IReadOnlyList<double> Point, int NumObj)
        {
            if (NumObj == 0)
            {
                return F1(Point);
            }
            else
            {
                return F2(Point, F1(Point));
            }
        }
    }
}