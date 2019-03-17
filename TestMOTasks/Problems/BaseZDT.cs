namespace MOTestTasks.Problems
{
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class BaseZDT : BaseProblem
    {
        protected virtual double G(IReadOnlyList<double> Point)
        {
            double sum = 0;

            for (int i = 1; i < Point.Count; i++)
            {
                sum += Point[i];
            }

            return 1 + 9 / (_lowerBounds.Length - 1) * sum;
        }

        public BaseZDT(int Dim, string Name) : base(2, Name, Enumerable.Repeat(0.0, Dim).ToArray(), Enumerable.Repeat(1.0, Dim).ToArray())
        {
        }
    }
}