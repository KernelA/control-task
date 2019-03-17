namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EOpt.Math.Optimization.OOOpt;

    internal abstract class ControlBaseTask : IOOOptProblem
    {
        protected double[] _lowerBounds, _upperBounds;

        protected int _nSwitch;
        protected TargetODE _ode;
        protected double _Tmax, _step;
        protected double[] _valueofT;

        public IReadOnlyList<double> LowerBounds => _lowerBounds;

        public int NSwitches => _nSwitch;

        public IReadOnlyList<double> UpperBounds => _upperBounds;

        public double X1T { get; protected set; }

        public double X2T { get; protected set; }

        public ControlBaseTask(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20,
            double LambdaLower, double LambdaUpper)
        {
            if (N < 2)
            {
                throw new ArgumentException($"{nameof(N)} must be greater than 1.", nameof(N));
            }

            if (LowerBound >= UpperBound)
            {
                throw new ArgumentException($"{nameof(LowerBound)} must be less than {nameof(UpperBound)}");
            }

            if (TMax <= 0)
            {
                throw new ArgumentException($"{nameof(TMax)} must be greater than 0.");
            }

            if (LambdaLower >= LambdaUpper)
            {
                throw new ArgumentException($"{nameof(LambdaLower)} must be greater than {nameof(LambdaUpper)}.");
            }

            // Диапазон переключения и lambda1, lambda2.
            _lowerBounds = Enumerable.Repeat(LowerBound, N * 2 + 2).ToArray();
            _upperBounds = Enumerable.Repeat(UpperBound, N * 2 + 2).ToArray();

            _lowerBounds[_lowerBounds.Length - 2] = LambdaLower;
            _lowerBounds[_lowerBounds.Length - 1] = LambdaLower;

            _upperBounds[_upperBounds.Length - 2] = LambdaUpper;
            _upperBounds[_upperBounds.Length - 1] = LambdaUpper;

            _nSwitch = N;

            _step = (double)TMax / _nSwitch;

            _Tmax = TMax;
            _valueofT = new double[_nSwitch + 1];

            for (int i = 0; i < _valueofT.Length; i++)
            {
                _valueofT[i] = i * _step;
            }

            _ode = new TargetODE(x10, x20, _Tmax, _valueofT);
        }

        public abstract double TargetFunction(IReadOnlyList<double> Point);
    }
}