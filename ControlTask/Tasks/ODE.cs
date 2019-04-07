namespace ControlTask
{
    using System;
    using System.Collections.Generic;

    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.OdeSolvers;

    internal class TargetODE
    {
        private const int _sizeOfRes = 251;

        private static readonly double _a = 0.00007292123518 * Math.Sqrt(3);

        private int _nSwitch = 0;
        private IReadOnlyList<double> _params;
        private double[] _timeValues;
        private double _tMax;
        private Vector<double> _x0;

        private Vector<double> OdeFunction(double t, Vector<double> x)
        {
            int u1IndexValue = _nSwitch - 1;

            for (int i = 0; i < _nSwitch - 1; i++)
            {
                if (t >= _timeValues[i] && t < _timeValues[i + 1])
                {
                    u1IndexValue = i;
                    break;
                }
            }

            var _y = CreateVector.Dense<double>(2);

            _y[0] = _a * x[1] + _params[u1IndexValue];
            _y[1] = -_a * x[0] + _params[u1IndexValue + _nSwitch];

            return _y;
        }

        public TargetODE(double x01, double x02, double Tmax, double[] TimeValues)
        {
            _x0 = CreateVector.Dense<double>(2);
            _x0[0] = x01;
            _x0[1] = x02;
            _tMax = Tmax;
            _nSwitch = TimeValues.Length - 1;
            _timeValues = TimeValues;
        }

        public Vector<double>[] Integrate(IReadOnlyList<double> Control)
        {
            _params = Control;
            return RungeKutta.FourthOrder(_x0, 0, _tMax, _sizeOfRes, OdeFunction);
        }
    }
}
