namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using EOpt.Math.Optimization.MOOpt;

    abstract class BaseFCPF : BaseProblem
    {
        protected int OddJ1 { get; private set; }

        protected int EvenJ2 { get; private set; }

        private double[] _res;

        public BaseFCPF(int CountObjs, string Name, IReadOnlyCollection<double> LowerBounds, IReadOnlyCollection<double> UpperBounds) : base(CountObjs, Name, LowerBounds, UpperBounds)
        {
            _res = new double[CountObjs];

            _lowerBounds = LowerBounds.ToArray();
            _upperBounds = UpperBounds.ToArray();
            EvenJ2 = CountEvenJ2();
            OddJ1 = CountOddJ1();
            
        }

        private int CountOddJ1()
        {
            return (LowerBounds.Count - 1) / 2; 
        }

        private int CountEvenJ2()
        {
            return LowerBounds.Count / 2;
        }

        protected abstract IReadOnlyList<double> TempSum(IReadOnlyList<double> Point);

        public override double ObjFunction(IReadOnlyList<double> Point, int NumObj)
        {
            IReadOnlyList<double> temp = TempSum(Point);

            if(NumObj == 0)
            {
                return Point[0] + 2.0 / OddJ1 * temp[0];
            }
            else if(NumObj == 1)
            {
                return 1 - Math.Sqrt(Point[0]) + 2.0 / CountEvenJ2() * temp[1];
            }
            else
            {
                throw new ArgumentException("NumObjs not found.", nameof(NumObj));
            }
        }

        public override IEnumerable<double> TargetFunction(IReadOnlyList<double> Point)
        {
            IReadOnlyList<double> temp = TempSum(Point);

            _res[0] =  Point[0] + 2.0 / OddJ1 * temp[0];
            _res[1] = 1 - Math.Sqrt(Point[0]) + 2.0 / EvenJ2 * temp[1];

            return _res;
        }
    }
}
