﻿namespace ControlTask
{
    using System.Collections.Generic;

    using EOpt.Math;

    internal class ControlTaskI1 : ControlBaseTask
    {
        private KahanSum _sum;

        public ControlTaskI1(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20, double LambdaLower, double LambdaUpper)
            : base(N, LowerBound, UpperBound, TMax, x10, x20, LambdaLower, LambdaUpper)
        {
            _sum = new KahanSum();
        }

        public override double TargetFunction(IReadOnlyList<double> Params)
        {
            double lambda1 = Params[Params.Count - 2], lambda2 = Params[Params.Count - 1];

            var res = _ode.Integrate(Params);

            X1T = res[res.Length - 1][0];
            X2T = res[res.Length - 1][1];

            _sum.SumResest();

            for (int i = 0; i < _nSwitch; i++)
            {
                _sum.Add(Params[i] * Params[i] * _step);
                _sum.Add(Params[i + _nSwitch] * Params[i + _nSwitch] * _step);
            }

            return _sum.Sum + lambda1 * X1T * X1T + lambda2 * X2T * X2T;
        }
    }
}