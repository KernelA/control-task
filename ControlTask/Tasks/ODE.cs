namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.OdeSolvers;

    class TargetODE
    {
        private const int _sizeOfRes = 91;

        private static readonly double _a = 0.00007292123518 * Math.Sqrt(3);

        private Vector<double> _x0;

        private double _Tmax;

        private double[] _timesValue;

        private IReadOnlyList<double> _params;

        private int _nSwitch = 0;

        public TargetODE(double x01, double x02, double Tmax)
        {
            _x0 = CreateVector.Dense<double>(2);
            _x0[0] = x01;
            _x0[1] = x02;
            _Tmax = Tmax;
        }

        public Vector<double>[] Integrate(double[] TimesValue, IReadOnlyList<double> Control)
        {
            _nSwitch = TimesValue.Length - 1;
            _timesValue = TimesValue;
            _params = Control;

            return RungeKutta.FourthOrder(_x0, 0, _Tmax, _sizeOfRes, OdeFunction);
        }

        private Vector<double> OdeFunction(double t, Vector<double> x)
        {
            int u1IndexValue = _nSwitch - 1;

            for (int i = 0; i < _nSwitch - 1; i++)
            {
                if (t >= _timesValue[i] && t < _timesValue[i + 1])
                {
                    u1IndexValue = i;
                    break;
                }
            }

            var _y = CreateVector.Dense<double>(2);

            _y[0] = _a * x[1] + _params[u1IndexValue];
            _y[1] = -_a * x[0] + _params[_nSwitch + u1IndexValue];

            return _y;
        }
    }
}
