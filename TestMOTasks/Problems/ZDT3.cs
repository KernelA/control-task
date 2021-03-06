﻿namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;

    internal class ZDT3 : BaseZDT
    {
        private const double PI_10 = 10 * Math.PI;
        private double[] _res;

        private double F1(IReadOnlyList<double> Point) => Point[0];

        private double F2(IReadOnlyList<double> Point)
        {
            double temp = G(Point);

            return temp * (1 - Math.Sqrt(Point[0] / temp) - Point[0] / temp * Math.Sin(PI_10 * Point[0]));
        }

        public ZDT3(int dim) : base(dim, "ZDT3")
        {
            _res = new double[2];
        }

        public override double ObjFunction(IReadOnlyList<double> Point, int NumObj)
        {
            if (NumObj == 0)
            {
                return F1(Point);
            }
            else
            {
                return F2(Point);
            }
        }

        public override IEnumerable<double> TargetFunction(IReadOnlyList<double> Point)
        {
            _res[0] = F1(Point);
            _res[1] = F2(Point);

            return _res;
        }
    }
}