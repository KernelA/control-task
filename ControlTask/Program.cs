namespace ControlTask
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Xml;

    using EOpt.Math.Optimization;
    using EOpt.Math.Optimization.OOOpt;

    using NLog;
    using System.Text;

    enum ProblemType { I1, I2 };

    class Program
    {
        private static Logger _logger1 = LogManager.GetLogger("Main1");

        private static Logger _logger2 = LogManager.GetLogger("Main2");

        private const int MAX_RUN = 5;

        private static readonly int[] SWITCHES = { 5, 8, 10 };

        private static readonly double[] TIMES = { 0.5, 1, 1.5, 2 };

        private const double X10 = 0.5, X20 = 1;

        private const int U_LOWER = -10, U_UPPER = 10;

        private static string _pathToDir = String.Empty;


        private static void BBBCOptimize(ControlBaseTask problem, Logger logger, XmlDocument doc)
        {
            const int numParams = 4;

            object[][] paramteters =
            {
                new object[numParams] {900, 500, 0.4, 0.5 },
                new object[numParams] {300, 400, 0.2, 0.1},
                new object[numParams] {200, 600, 0.5, 0.5 },
                new object[numParams] {300, 400, 0.5, 0.9 },
                new object[numParams] {200, 1000, 0.5, 0.5 }
            };

            BBBCOptimizer opt = new BBBCOptimizer();

            XmlElement bbbcElem = doc.CreateElement("BBBC");
            doc.DocumentElement.AppendChild(bbbcElem);
            Optimize<BBBCOptimizer, BBBCParams>(problem, opt, paramteters, doc, bbbcElem, logger);
        }

        private static void GEMOptimize(ControlBaseTask problem, Logger logger, XmlDocument doc)
        {
            const int numParams = 5;

            object[][] paramteters =
            {
                new object[numParams]  {1, 100, 500, 2 * Math.Sqrt(problem.LowerBounds.Count), 10},
                new object[numParams] {1, 100, 500, 2 * Math.Sqrt(problem.LowerBounds.Count), 150 },
                new object[numParams] {2, 50, 250, Math.Sqrt(problem.LowerBounds.Count /(double)2), 10 },
                new object[numParams] {2, 50, 250, Math.Sqrt(problem.LowerBounds.Count /(double)2), 150 },
                new object[numParams] {2, 100, 250, Math.Sqrt(problem.LowerBounds.Count /(double)2), 200 }
            };

            GEMOptimizer opt = new GEMOptimizer();

            XmlElement gemElem = doc.CreateElement("GEM");
            doc.DocumentElement.AppendChild(gemElem);
            Optimize<GEMOptimizer, GEMParams>(problem, opt, paramteters, doc, gemElem, logger);
        }

        private static void FWOptimize(ControlBaseTask problem, Logger logger, XmlDocument doc)
        {
            const int numParams = 6;

            object[][] paramteters =
            {
                new object[numParams] {100, 300, 20.0, 10, 30, 5.0},
                new object[numParams] {20, 500, 20.0, 10, 50, 10.0},
                new object[numParams] {20, 150, 20.0, 10, 50, 40.0},
                new object[numParams] {30, 130, 20.0, 10, 50, 9.0 },
                new object[numParams] {40, 125, 2.0, 10, 50, 25.0}
            };

            FWOptimizer opt = new FWOptimizer();

            XmlElement fwElem = doc.CreateElement("FW");
            doc.DocumentElement.AppendChild(fwElem);

            Optimize<FWOptimizer, FWParams>(problem, opt, paramteters, doc, fwElem, logger);
        }

        private static TParams CreateParams<TParams>(object[] parameters)
        {
            Type type = typeof(TParams);

            var constructors = type.GetConstructors();

            object obj = null;

            foreach (var constr in constructors)
            {
                var constrParams = constr.GetParameters();

                if (constrParams.Length > 1)
                {
                    int paramsWithDefaultValue = constrParams.Where(param => param.HasDefaultValue).Count();

                    object[] defValuesParams = new object[paramsWithDefaultValue];

                    {
                        int i = 0;

                        foreach (var param in constrParams)
                        {
                            if (param.HasDefaultValue)
                            {
                                defValuesParams[i++] = param.DefaultValue;
                            }
                        }
                    }

                    object[] resParams = new object[constrParams.Length];

                    if (defValuesParams.Length != 0)
                    {
                        defValuesParams.CopyTo(resParams, (constrParams.Length - defValuesParams.Length));

                    }

                    parameters.CopyTo(resParams, 0);
                    obj = constr.Invoke(resParams);
                }
            }

            if (obj == null)
            {
                throw new ArgumentNullException($"Cannot create object {nameof(obj)}");
            }

            return (TParams)obj;
        }


        private static void Optimize<TOpt, TParams>(ControlBaseTask problem, TOpt opt, object[][] parameters, XmlDocument doc, XmlElement optElem, Logger logger)
            where TOpt : IOOOptimizer<TParams>, new()
        {

            Dictionary<string, string> resInfo = new Dictionary<string, string>()
            {
                ["TargetV"] = "",
                ["lambda1"] = "",
                ["lambda2"] = "",
                ["X1T"] = "",
                ["X2T"] = ""
            };

            foreach (var par in Enumerable.Range(0, parameters.Length).Zip(parameters, (num, par) => (Num: num, Parameters: par)))
            {
                logger.Info($"Try to find solution with {par.Num}th configuration of {parameters.Length}");

                var experimentsElem = doc.CreateElement("Experiment");
                optElem.AppendChild(experimentsElem);
                var optParams = doc.CreateElement("OptParams");
                experimentsElem.AppendChild(optParams);

                TParams pars = CreateParams<TParams>(par.Parameters);

                var paramsType = pars.GetType();

                foreach (var prop in paramsType.GetProperties())
                {
                    if (prop.PropertyType == typeof(bool))
                        continue;

                    var paramElem = doc.CreateElement("Param");
                    paramElem.SetAttribute(prop.Name, prop.GetValue(pars).ToString());
                    optParams.AppendChild(paramElem);
                }


                var runsElem = doc.CreateElement("Runs");

                experimentsElem.AppendChild(runsElem);

                for (int i = 0; i < MAX_RUN; i++)
                {
                    logger.Info($"Run {i} of {MAX_RUN}");

                    var runElem = doc.CreateElement("Run");

                    runsElem.AppendChild(runElem);

                    try
                    {
                        opt.Minimize(pars, problem);
                        problem.TargetFunction(opt.Solution.Point);

                        resInfo["TargetV"] = opt.Solution.Objs[0].ToString();
                        resInfo["lambda1"] = opt.Solution.Point[opt.Solution.Point.Count - 2].ToString();
                        resInfo["lambda2"] = opt.Solution.Point[opt.Solution.Point.Count - 1].ToString();
                        resInfo["X1T"] = problem.X1T.ToString();
                        resInfo["X2T"] = problem.X2T.ToString();

                        var resEelem = doc.CreateElement("Res");

                        foreach (var kv in resInfo)
                        {
                            resEelem.SetAttribute(kv.Key, kv.Value);
                        }

                        runElem.AppendChild(resEelem);

                        var controlsElem = doc.CreateElement("Controls");
                        runElem.AppendChild(controlsElem);

                        for (int j = 0; j < 2 * problem.NSwitches; j++)
                        {
                            var controlElem = doc.CreateElement("Control");
                            int uNum = j < problem.NSwitches ? 1 : 2;
                            controlElem.SetAttribute("Num", uNum.ToString());
                            controlElem.SetAttribute("Value", opt.Solution.Point[j].ToString());
                            controlsElem.AppendChild(controlElem);
                        }

                        //logger.Info($"X1T = {resInfo["X1T"]}");
                        //logger.Info($"X2T = {resInfo["X2T"]}");
                        //logger.Info($"lambda1 = {resInfo["lambda1"]}");
                        //logger.Info($"lambda2 = {resInfo["lambda2"]}");
                        //logger.Info($"I1 = {resInfo["TargetV"]}");
                    }
                    catch (Exception exc)
                    {
                        logger.Error(exc, "Error was in optimization process.");
                        logger.Info("Recreate optimization method.");
                        opt = new TOpt();
                        logger.Info($"Skip run {i}.");
                        continue;
                    }

                    //if (bestSol == null)
                    //{
                    //    bestSol = opt.Solution.DeepCopy();
                    //}
                    //else
                    //{
                    //    if (opt.Solution.Objs[0] < bestSol.Objs[0])
                    //    {
                    //        bestSol = opt.Solution.DeepCopy();
                    //    }
                    //}

                    //logger.Info($"I = {bestSol.Objs[bestSol.Objs.Count - 1]}");
                    //problem.TargetFunction(bestSol.Point);
                    //logger.Info("Solution:");
                    //logger.Info(opt.Solution.Point.Select(a => Math.Round(a, 2)).Select(a => a.ToString()).Aggregate((a, b) => $"{a} {b}"));
                    //logger.Info($"(x1(T), x2(T)) = {temp.X1T}; {temp.X2T}");
                }
            }

        }

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if(args.Length != 1)
            {
                Console.WriteLine("You need to specify path to the directory where store results as cli argument.");
            }
            else
            {
                _pathToDir = args[0];

                if(!Directory.Exists(_pathToDir))
                {
                    Console.WriteLine($"'path' does not exist.");
                    return;
                }
            }

            Task task1 = new Task(taskType => SolveTask((ProblemType)taskType), ProblemType.I1);
            Task task2 = new Task(taskType => SolveTask((ProblemType)taskType), ProblemType.I2);

            task1.Start();
            task2.Start();

            Task.WaitAll(task1, task2);
        }


        private static void SolveTask(ProblemType Problem)
        {
            var logger = Problem == ProblemType.I1 ? _logger1 : _logger2;

            string dirPath = Path.Combine(_pathToDir, Problem == ProblemType.I1 ? "Task1" : "Task2");

            if(!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            XmlWriterSettings xmlSettings = new XmlWriterSettings();

            foreach (var n in SWITCHES)
            {
                logger.Info($"Start solving N = {n}");


                for (int i = 0; i < TIMES.Length; i++)
                {
                    double TMax = TIMES[i];

                    XmlDocument doc = new XmlDocument();
                    XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    XmlElement root = doc.CreateElement("Problem");
                    doc.AppendChild(root);
                    doc.InsertBefore(xmlDeclaration, root);

                    {
                        Dictionary<string, string> problemDesc = new Dictionary<string, string>
                        {
                            ["Name"] = Problem.ToString(),
                            ["Tmax"] = TMax.ToString(),
                            ["NSwitches"] = n.ToString(),
                            ["ULower"] = U_LOWER.ToString(),
                            ["UUpper"] = U_UPPER.ToString(),
                            ["X10"] = X10.ToString(),
                            ["X20"] = X20.ToString()
                        };

                        foreach (var name in problemDesc)
                        {
                            root.SetAttribute(name.Key, name.Value);
                        }
                     }


                    logger.Info($"Time T = {TMax}");
                    logger.Info($"Creating problem. Type is {Problem.ToString()}");

                    ControlBaseTask problem = null;

                    switch (Problem)
                    {
                        case ProblemType.I1:
                            {
                                problem = new ControlTaskI1(n, U_LOWER, U_UPPER, TMax, X10, X20);
                                break;
                            }
                        case ProblemType.I2:
                            {
                                problem = new ControlTaskI2(n, U_LOWER, U_UPPER, TMax, X10, X20);
                                break;
                            }
                        default:
                            {
                                throw new ArgumentException("Invalid problem type.", nameof(ProblemType));
                            }
                    }

                    logger.Info($"Start solving with BBBC.");
                    BBBCOptimize(problem, logger, doc);
                    logger.Info($"Start solving with FW.");
                    FWOptimize(problem, logger, doc);
                    logger.Info($"Start solving with GEM.");
                    GEMOptimize(problem, logger, doc);

                    string pathToFile = Path.Combine(_pathToDir, dirPath, $"res_{n}_{i}.xml");

                    using (XmlWriter writer = XmlWriter.Create(pathToFile, xmlSettings))
                    {
                        doc.Save(writer);
                        logger.Info($"Writed res to a file '{pathToFile}'.");
                    }
                }

            }
        }

    }
}
