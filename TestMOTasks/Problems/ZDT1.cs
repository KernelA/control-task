namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EOpt.Math.Optimization.MOOpt;

    internal class ZDT1 : BaseZDT
    {
        private double[] _res;


        public ZDT1(int dim) : base(dim, "ZDT1")
        {
            _res = new double[2];
        }

        private double F1(IReadOnlyList<double> Point) => Point[0];

        private double F2(IReadOnlyList<double> Point)
        {
            double temp = base.G(Point);

            return temp * (1 - Math.Sqrt(Point[0] / temp));
        }

        public override IEnumerable<double> TargetFunction(IReadOnlyList<double> Point)
        {
            _res[0] = F1(Point);
            _res[1] = F2(Point);

            return _res;
        }

        public override double ObjFunction(IReadOnlyList<double> Point, int NumObj)
        {
            if(NumObj == 0)
            {
                return F1(Point);
            }
            else
            {
                return F2(Point);
            }
        }
    }
}