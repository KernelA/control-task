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

        private double[] _lowerBounds, _upperBounds, _targetValues;

        private TargetODE _ode;

        private Vector<double>[] _odeSolution;

        private KahanSum _sum;

        protected double _step;

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

            for (int i = 0; i < NSwitches; i++)
            {
                _sum.Add(Params[i] * Params[i] * _step);
                _sum.Add(Params[i + NSwitches] * Params[i + NSwitches] * _step);
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

            for (int i = 0; i < NSwitches; i++)
            {
                _sum.Add((Math.Abs(Params[i]) + Math.Abs(Params[NSwitches + i])) * _step);
            }

            _sum.Add(_lambda3 * X1T * X1T);
            _sum.Add(_lambda4 * X2T * X2T);

            return _sum.Sum;
        }

        public int CountObjs => OBJ_COUNT;

        public double Lmabda1 => _lambda1;

        public double Lmabda2 => _lambda2;

        public double Lmabda3 => _lambda3;

        public double Lmabda4 => _lambda4;

        public IReadOnlyList<double> LowerBounds => _lowerBounds;

        public int NSwitches => _ode.NSwitch;

        public double TMax => _ode.TMax;

        public IReadOnlyList<double> UpperBounds => _upperBounds;

        public double X10 => _ode.X10;

        public double X1T { get; protected set; }

        public double X20 => _ode.X20;

        public double X2T { get; protected set; }

        public MOControlTask(int N, double LowerBound, double UpperBound, double TMax, double x10, double x20, double lambda1, double lambda2, double lambda3, double lambda4, int NumSteps)
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

            if (lambda1 <= 0)
            {
                throw new ArgumentException($"{nameof(lambda1)} must be greater than 0.");
            }

            if (lambda2 <= 0)
            {
                throw new ArgumentException($"{nameof(lambda2)} must be greater than 0.");
            }

            if (lambda3 <= 0)
            {
                throw new ArgumentException($"{nameof(lambda3)} must be greater than 0.");
            }

            if (lambda4 <= 0)
            {
                throw new ArgumentException($"{nameof(lambda4)} must be greater than 0.");
            }

            _lowerBounds = Enumerable.Repeat(LowerBound, N * 2).ToArray();
            _upperBounds = Enumerable.Repeat(UpperBound, N * 2).ToArray();

            _lambda1 = lambda1;
            _lambda2 = lambda2;
            _lambda3 = lambda3;
            _lambda4 = lambda4;

            double step = (double)TMax / N;

            double[] valueofT = new double[N + 1];

            for (int i = 0; i < valueofT.Length; i++)
            {
                valueofT[i] = i * _step;
            }

            _ode = new TargetODE(x10, x20, TMax, valueofT, NumSteps);
            _targetValues = new double[OBJ_COUNT];

            _sum = new KahanSum();
        }

        public MOControlTask DeepCopy()
        {
            var copy = new MOControlTask(NSwitches, _lowerBounds[0], _upperBounds[0], TMax, _ode.X10, _ode.X20, _lambda1, _lambda2, _lambda3, _lambda4, _ode.NumSteps);

            return copy;
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