namespace ControlTask
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    using EOpt.Math.Optimization.MOOpt;

    using NLog;

    class Program
    {
        private static Logger _logger = LogManager.GetLogger("Main");

        static void Main(string[] args)
        {
            

            BBBCParams parameters = new BBBCParams(300, 600, 0.4, 0.7);

            BBBCOptimizer opt = new BBBCOptimizer();

            opt.InitializeParameters(parameters);

            const int max = 5;

            const int Tstart = 2, Tend = 10;

            const int Tstep = 2;

            for (int t = Tstart; t <= Tend; t += Tstep)
            {
                ControlBaseTask task = new ControlTaskI2(5, -10, 10, t, 0.1, 0.2);

                for (int i = 0; i < max; i++)
                {
                    _logger.Info($"Run: {i} of {max}");
                    _logger.Info($"Time = {t}");
                    opt.Minimize(new GeneralParams(task.TargetFunction, task.LowerBounds, task.UpperBounds));
                    _logger.Info($"I = {opt.Solution[opt.Solution.Dimension - 1]}");
                    _logger.Info("Solution:");
                    _logger.Info(opt.Solution.Coordinates.Select(a => Math.Round(a, 2)).Select(a => a.ToString()).Aggregate((a, b) => $"{a} {b}"));
                }
            }


        }
    }
}
