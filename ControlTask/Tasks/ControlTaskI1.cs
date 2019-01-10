namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using MathNet.Numerics.OdeSolvers;

    class ControlTaskI1 : ControlBaseTask
    {
        public ControlTaskI1(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20)
            : base(N, LowerBound, UpperBound, TMax, x10, x20)
        {


        }
        public override double TargetFunction(IReadOnlyList<double> Params)
        {
            double lambda1 = Params[Params.Count - 2], lambda2 = Params[Params.Count - 1];

            var res = _ode.Integrate(_valueofT, Params);

            double x1T = res[res.Length - 1][0], x2T = res[res.Length - 1][1];

            X1T = x1T;
            X2T = x2T;

            return lambda1 * x1T * x1T + lambda2 * x2T * x2T;
        }
    }
}
