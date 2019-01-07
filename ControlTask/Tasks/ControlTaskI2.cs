namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using MathNet.Numerics.OdeSolvers;

    class ControlTaskI2 : ControlBaseTask
    {
        public ControlTaskI2(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20) : base(N, LowerBound, UpperBound, TMax, x10, x20)
        {
            _lowerBounds[_lowerBounds.Length - 2] = 500;
            _lowerBounds[_lowerBounds.Length - 1] = 500;

            _upperBounds[_upperBounds.Length - 2] = 50_000;
            _upperBounds[_upperBounds.Length - 1] = 50_000;

        }

        public override double TargetFunction(IReadOnlyList<double> Point)
        {
            _params = Point;

            double lambda1 = _params[_params.Count - 2], lambda2 = _params[_params.Count - 1];

            var res = RungeKutta.FourthOrder(_x0, 0, _Tmax, _sizeOfRes, OdeFunction);

            double x1T = res[res.Length - 1][0], x2T = res[res.Length - 1][1];

            X1T = x1T;
            X2T = x2T;

            double integralValue = 0.0;

            for (int i = 0; i < _nSwitch; i++)
            {
                integralValue += (Math.Abs(_params[i]) + Math.Abs(_params[_nSwitch + i])) * _step;
            }

            return integralValue + lambda1 * x1T * x1T + lambda2 * x2T * x2T;
        }
    }
}
