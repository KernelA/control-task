namespace ControlTask
{
    using System;
    using System.Collections.Generic;

    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.OdeSolvers;

    public class TargetODE
    {
        private static readonly double _a = 0.00007292123518 * Math.Sqrt(3);
        private readonly double _tMax;
        private IReadOnlyList<double> _params;
        private int _sizeOfRes = 251;
        private double[] _timeValues;
        private Vector<double> _x0;

        private Vector<double> OdeFunction(double t, Vector<double> x)
        {
            int u1IndexValue = NSwitch - 1;

            for (int i = 0; i < NSwitch - 1; i++)
            {
                if (t >= _timeValues[i] && t < _timeValues[i + 1])
                {
                    u1IndexValue = i;
                    break;
                }
            }

            var _y = CreateVector.Dense<double>(2);

            _y[0] = _a * x[1] + _params[u1IndexValue];
            _y[1] = -_a * x[0] + _params[u1IndexValue + NSwitch];

            return _y;
        }

        public int NSwitch => _timeValues.Length - 1;

        public int NumSteps => _sizeOfRes - 1;

        public double TMax => _tMax;

        public double X10 => _x0[0];

        public double X20 => _x0[1];

        public TargetODE(double x01, double x02, double Tmax, double[] TimeValues, int NumSteps)
        {
            _x0 = CreateVector.Dense<double>(2);
            _x0[0] = x01;
            _x0[1] = x02;
            _tMax = Tmax;
            _timeValues = TimeValues;
            _sizeOfRes = NumSteps + 1;
        }

        public TargetODE DeepCopy()
        {
            double[] timeCopy = new double[_timeValues.Length];

            _timeValues.CopyTo(timeCopy, 0);
            var copy = new TargetODE(X10, X20, TMax, timeCopy, NumSteps);

            return copy;
        }

        public Vector<double>[] Integrate(IReadOnlyList<double> Control)
        {
            _params = Control;
            return RungeKutta.FourthOrder(_x0, 0, TMax, _sizeOfRes, OdeFunction);
        }
    }
}