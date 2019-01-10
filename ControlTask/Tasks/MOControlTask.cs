namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.Linq.Expressions;

    using EOpt.Math.Optimization.MOOpt;

    using MathNet.Numerics.OdeSolvers;
    using MathNet.Numerics.LinearAlgebra;

    class MOControlTask : IMOOptProblem
    {
        private const int OBJ_COUNT = 2;

        private double[] _lowerBounds, _upperBounds, _targetValues, _valueofT;

        private TargetODE _ode;

        public int CountObjs => OBJ_COUNT;

        private bool _isOdeSolved = false;

        private int _nSwitch;

        private Vector<double>[] _odeSolution;

        protected double _Tmax, _step;

        public IReadOnlyList<double> LowerBounds => _lowerBounds;

        public IReadOnlyList<double> UpperBounds => _upperBounds;

        public double X1T { get; protected set; }

        public double X2T { get; protected set; }

        public int NSwitches => _nSwitch;


        public MOControlTask(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20)
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


            _lowerBounds = Enumerable.Repeat(LowerBound, N * 2 + 4).ToArray();
            _upperBounds = Enumerable.Repeat(UpperBound, N * 2 + 4).ToArray();

            _lowerBounds[_lowerBounds.Length - 4] = 10;
            _lowerBounds[_lowerBounds.Length - 3] = 10;

            _upperBounds[_upperBounds.Length - 4] = 5000;
            _upperBounds[_upperBounds.Length - 3] = 5000;

            _lowerBounds[_lowerBounds.Length - 2] = 500;
            _lowerBounds[_lowerBounds.Length - 1] = 500;

            _upperBounds[_upperBounds.Length - 2] = 50_000;
            _upperBounds[_upperBounds.Length - 1] = 50_000;

            _nSwitch = N;

            _step = (double)TMax / _nSwitch;

            _Tmax = TMax;
            _valueofT = new double[_nSwitch + 1];

            for (int i = 0; i < _valueofT.Length; i++)
            {
                _valueofT[i] = i * _step;
            }

            _ode = new TargetODE(x10, x20, _Tmax);

            _targetValues = new double[OBJ_COUNT];
        }

        private double I1(IReadOnlyList<double> Params)
        {
            if(!_isOdeSolved)
            {
                _odeSolution = _ode.Integrate(_valueofT, Params);
                X1T = _odeSolution[_odeSolution.Length - 1][0];
                X2T = _odeSolution[_odeSolution.Length - 1][1];
                _isOdeSolved = true;
            }
            
            return Params[Params.Count - 4] * X1T * X1T + Params[Params.Count - 3] * X2T * X2T;
        }

        private double I2(IReadOnlyList<double> Params)
        {
            if (!_isOdeSolved)
            {
                _odeSolution = _ode.Integrate(_valueofT, Params);
                X1T = _odeSolution[_odeSolution.Length - 1][0];
                X2T = _odeSolution[_odeSolution.Length - 1][1];
                _isOdeSolved = true;
            }

            double integralValue = 0.0;

            for (int i = 0; i < _nSwitch; i++)
            {
                integralValue += (Math.Abs(Params[i]) + Math.Abs(Params[_nSwitch + i])) * _step;
            }

            return integralValue + Params[Params.Count - 2] * X1T * X1T + Params[Params.Count - 1] * X2T * X2T;
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
