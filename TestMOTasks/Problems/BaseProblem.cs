namespace MOTestTasks.Problems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class BaseProblem : INamedMOProblem
    {
        protected double[] _lowerBounds, _upperBounds;

        public int CountObjs { get; private set; }

        public IReadOnlyList<double> LowerBounds => _lowerBounds;

        public string Name { get; private set; }

        public IReadOnlyList<double> UpperBounds => _upperBounds;

        public BaseProblem(int CountObjs, string Name, IReadOnlyCollection<double> LowerBounds, IReadOnlyCollection<double> UpperBounds)
        {
            if (LowerBounds.Count != UpperBounds.Count)
            {
                throw new ArgumentException("Not equal size of bounds.");
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentNullException(nameof(Name));
            }

            if (CountObjs < 2)
            {
                throw new ArgumentException("CountObjs < 2", nameof(CountObjs));
            }

            this.CountObjs = CountObjs;
            this.Name = Name;

            _lowerBounds = LowerBounds.ToArray();
            _upperBounds = UpperBounds.ToArray();

            for (int i = 0; i < _lowerBounds.Length; i++)
            {
                if (_lowerBounds[i] >= _upperBounds[i])
                {
                    throw new ArgumentException($"The lower bound is greater or equal upper bound at position {i}.");
                }
            }
        }

        public abstract double ObjFunction(IReadOnlyList<double> Point, int NumObj);

        public abstract IEnumerable<double> TargetFunction(IReadOnlyList<double> Point);
    }
}