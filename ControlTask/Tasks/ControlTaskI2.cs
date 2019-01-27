namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using EOpt.Math;

    using MathNet.Numerics.OdeSolvers;

    class ControlTaskI2 : ControlBaseTask
    {
        private KahanSum _sum;

        public ControlTaskI2(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20) : base(N, LowerBound, UpperBound, TMax, x10, x20)
        {
            _sum = new KahanSum();

            _lowerBounds[_lowerBounds.Length - 2] = 1000;
            _lowerBounds[_lowerBounds.Length - 1] = 1000;

            _upperBounds[_upperBounds.Length - 2] = 25_000;
            _upperBounds[_upperBounds.Length - 1] = 25_000;
        }

        public override double TargetFunction(IReadOnlyList<double> Params)
        {
            double lambda1 = Params[Params.Count - 2], lambda2 = Params[Params.Count - 1];

            var res = _ode.Integrate(_valueofT, Params);

            X1T = res[res.Length - 1][0];
            X2T = res[res.Length - 1][1];

            _sum.SumResest();

            for (int i = 0; i < _nSwitch; i++)
            {
                _sum.Add(Math.Abs(Params[i]) * _step);
                _sum.Add(Math.Abs(Params[_nSwitch + i]) * _step);
            }

            return _sum.Sum + lambda1 * X1T * X1T + lambda2 * X2T * X2T;
        }
    }
}
