namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.Linq.Expressions;

    using MathNet.Numerics;
    using MathNet.Numerics.OdeSolvers;
    using MathNet.Numerics.LinearAlgebra;

    using EOpt.Math.Optimization.OOOpt;
    using EOpt.Math.Optimization;

    abstract class ControlBaseTask : IOOOptProblem
    {
        private double[] _lowerBounds, _upperBounds;

        protected double[] _valueofT;

        protected IReadOnlyList<double> _params;

        protected const int _sizeOfRes = 100;

        private static readonly double _a = 0.00007292123518 * Math.Sqrt(3);

        protected int _nSwitch;

        protected Vector<double> _x0, _y;

        protected double _Tmax;

        public double[] LowerBounds => _lowerBounds;

        public double[] UpperBounds => _upperBounds;

        IReadOnlyList<double> IConstrOptProblem<double, double>.LowerBounds => throw new NotImplementedException();

        IReadOnlyList<double> IConstrOptProblem<double, double>.UpperBounds => throw new NotImplementedException();

        public ControlBaseTask(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20)
        {
            if(N < 2)
            {
                throw new ArgumentException($"{nameof(N)} must be greater than 1.", nameof(N));
            }

            if(LowerBound >= UpperBound)
            {
                throw new ArgumentException($"{nameof(LowerBound)} must be less than {nameof(UpperBound)}");
            }

            if(TMax <= 0)
            {
                throw new ArgumentException($"{nameof(TMax)} must be greater than 0.");
            }

            // Диапазон переключения и lambda1, lambda2.
            _lowerBounds = Enumerable.Repeat(LowerBound, N * 2 + 2).ToArray();
            _upperBounds = Enumerable.Repeat(UpperBound, N * 2 + 2).ToArray();

            _lowerBounds[_lowerBounds.Length - 2] = 1E-6;
            _lowerBounds[_lowerBounds.Length - 1] = 1E-6;

            _upperBounds[_upperBounds.Length - 2] = 100;
            _upperBounds[_upperBounds.Length - 1] = 100;

            _x0 = CreateVector.Dense<double>(2);
            _x0[0] = x10;
            _x0[1] = x20;

            _y = CreateVector.Dense<double>(2);
            _nSwitch = N;
            double step = (double)TMax / N;
            _Tmax = TMax;
            _valueofT = new double[N];

            for (int i = 0; i < N; i++)
            {
                _valueofT[i] = i * step;
            }
        }

        protected Vector<double> OdeFunction(double t, Vector<double> x)
        {
            int u1IndexValue = 0;

            for (int i = 0; i < _nSwitch - 1; i++)
            {
                if(t >= _valueofT[i] && t < _valueofT[i + 1])
                {
                    u1IndexValue = i;
                    break;
                }
            }

            _y[0] = _a * x[1] + _params[u1IndexValue];
            _y[1] = -_a * x[0] + _params[2 * u1IndexValue];

            return _y;
        }

        public abstract double TargetFunction(IReadOnlyList<double> Point);
    }


}
