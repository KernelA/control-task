namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EOpt.Math;
    using EOpt.Math.Optimization.MOOpt;

    using MathNet.Numerics.LinearAlgebra;

    internal class MOControlTask : IMOOptProblem
    {
        private const int OBJ_COUNT = 2;

        private bool _isOdeSolved = false;
        private double _lambda1, _lambda2, _lambda3, _lambda4;
        private double[] _lowerBounds, _upperBounds, _targetValues, _valueofT;
        private int _nSwitch;
        private TargetODE _ode;

        private Vector<double>[] _odeSolution;

        private KahanSum _sum;

        protected double _Tmax, _step;

        private double I1(IReadOnlyList<double> Params)
        {
            if (!_isOdeSolved)
            {
                _odeSolution = _ode.Integrate(Params);
                X1T = _odeSolution[_odeSolution.Length - 1][0];
                X2T = _odeSolution[_odeSolution.Length - 1][1];
                _isOdeSolved = true;
            }

            _sum.SumResest();

            for (int i = 0; i < _nSwitch; i++)
            {
                _sum.Add(Params[i] * Params[i] * _step);
                _sum.Add(Params[i + _nSwitch] * Params[i + _nSwitch] * _step);
            }

            _sum.Add(_lambda1 * X1T * X1T);
            _sum.Add(_lambda2 * X2T * X2T);

            return _sum.Sum;
        }

        private double I2(IReadOnlyList<double> Params)
        {
            if (!_isOdeSolved)
            {
                _odeSolution = _ode.Integrate(Params);
                X1T = _odeSolution[_odeSolution.Length - 1][0];
                X2T = _odeSolution[_odeSolution.Length - 1][1];
                _isOdeSolved = true;
            }

            _sum.SumResest();

            for (int i = 0; i < _nSwitch; i++)
            {
                _sum.Add((Math.Abs(Params[i]) + Math.Abs(Params[_nSwitch + i])) * _step);
            }

            _sum.Add(_lambda3 * X1T * X1T);
            _sum.Add(_lambda4 * X2T * X2T);

            return _sum.Sum;
        }

        public int CountObjs => OBJ_COUNT;

        public IReadOnlyList<double> LowerBounds => _lowerBounds;

        public int NSwitches => _nSwitch;

        public IReadOnlyList<double> UpperBounds => _upperBounds;

        public double X1T { get; protected set; }

        public double X2T { get; protected set; }

        public MOControlTask(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20, double lambda1, double lambda2, double lambda3, double lambad4)
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

            _lowerBounds = Enumerable.Repeat(LowerBound, N * 2).ToArray();
            _upperBounds = Enumerable.Repeat(UpperBound, N * 2).ToArray();

            _lambda1 = lambda1;
            _lambda2 = lambda2;
            _lambda3 = lambda3;
            _lambda4 = lambad4;

            _nSwitch = N;

            _step = (double)TMax / _nSwitch;

            _Tmax = TMax;
            _valueofT = new double[_nSwitch + 1];

            for (int i = 0; i < _valueofT.Length; i++)
            {
                _valueofT[i] = i * _step;
            }

            _ode = new TargetODE(x10, x20, _Tmax, _valueofT);
            _targetValues = new double[OBJ_COUNT];

            _sum = new KahanSum();
        }

        public double ObjFunction(IReadOnlyList<double> Point, int NumObj)
        {
            _isOdeSolved = false;

            switch (NumObj)
            {
                case 0:
                    {
                        return I1(Point);
                    }
                case 1:
                    {
                        return I2(Point);
                    }
                default:
                    throw new ArgumentException($"NumObj is {NumObj} and it is invalid value.", nameof(NumObj));
            }
        }

        public IEnumerable<double> TargetFunction(IReadOnlyList<double> Point)
        {
            _isOdeSolved = false;

            _targetValues[0] = I1(Point);
            _targetValues[1] = I2(Point);

            return _targetValues;
        }
    }
}