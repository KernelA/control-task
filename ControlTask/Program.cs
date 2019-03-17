namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using CommandLine;

    using EOpt.Math.Optimization;
    using EOpt.Math.Optimization.MOOpt;
    using EOpt.Math.Optimization.OOOpt;

    using NLog;

    internal static class LambdasTask
    {
        private static Dictionary<int, (double, double)>[] _lambdas;

        static LambdasTask()
        {
            _lambdas = new Dictionary<int, (double, double)>[2];

            for (int i = 0; i < _lambdas.Length; i++)
            {
                _lambdas[i] = new Dictionary<int, (double, double)>(3);
            }

            _lambdas[0].Add(8, (11247.6302753864, 11143.217997764));
            _lambdas[0].Add(10, (22621.5610681351, 18165.9891931346));
            _lambdas[0].Add(15, (18420.4788767144, 10203.7952768332));

            _lambdas[1].Add(8, (2555.43922113448, 4311.17267957873));
            _lambdas[1].Add(10, (4207.31058609648, 1248.40435098767));
            _lambdas[1].Add(15, (4096.36454659811, 2773.91241130186));
        }

        public static (double Lambda1, double Lambda2) GetLambdas(ProblemType Problem, int NSwitches)
        {
            int i = 0;

            switch (Problem)
            {
                case ProblemType.I1:
                    {
                        i = 0;
                        break;
                    }
                case ProblemType.I2:
                    {
                        i = 1;
                        break;
                    }
                default:
                    throw new ArgumentException("An invalid type of the task.", nameof(Problem));
            }

            if (!_lambdas[i].ContainsKey(NSwitches))
            {
                throw new ArgumentException($"{NSwitches} not in dictionary.", nameof(NSwitches));
            }

            return _lambdas[i][NSwitches];
        }
    }

    internal class Options
    {
        [Option('r', "max-run", HelpText = "Максимальное число запусков.", Required = true)]
        public int MaxRun { get; set; }

        [Option('o', "output-dir", HelpText = "Путь до директории, где будут сохранены результаты в виде XML файлов.", Required = true)]
        public string OutputDir { get; set; }

        [Option('s', "switches", HelpText = "Число переключений для управления", Required = true)]
        public IEnumerable<int> Switches { get; set; }

        [Option("task-type", HelpText = "Задачи, которые необходимо решить: I12 или MOI (многокритериальная).", Required = true)]
        public ProblemParamType Tasks { get; set; }

        [Option('t', "max-time", HelpText = "Конечное время для отрезка [0; T] на котором ищется решение.", Required = true)]
        public IEnumerable<double> Times { get; set; }

        [Option('b', "bounds", HelpText = "Ограничения на управления.", Min = 2, Max = 2, Required = true)]
        public IEnumerable<double> UBounds { get; set; }

        [Option('i', "init-value", HelpText = "Начальные условия для задачи Коши.", Min = 2, Max = 2, Required = true)]
        public IEnumerable<double> X0 { get; set; }

        [Option('l', "lambda", HelpText = "Ограничения для lambda. Пары чисел - ограничение снизу, ограничение сверху. Если 4 числа, то отдельно ограничения для первой и второй задачи.", Min = 2, Max = 4, Required =true)]
        public IEnumerable<double> Lambda { get; set; }
    }

    internal class Program
    {
        private static readonly Logger _logger = LogManager.GetLogger("Main");

        private static void BBBCOptimize(ControlBaseTask problem, int MaxRun, Logger logger, XmlDocument doc)
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
            Optimize<BBBCOptimizer, BBBCParams>(problem, opt, paramteters, doc, bbbcElem, logger, MaxRun);
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

        private static void FWOptimize(ControlBaseTask problem, int MaxRun, Logger logger, XmlDocument doc)
        {
            const int numParams = 6;

            object[][] paramteters =
            {
                new object[numParams] {100, 300, 20.0, 10, 30, 2.5},
                new object[numParams] {60, 900, 20.0, 10, 50, 1.0},
                new object[numParams] {20, 150, 20.0, 10, 50, 2.0},
                new object[numParams] {30, 130, 20.0, 10, 50, 9.0 },
                new object[numParams] {200, 200, 2.0, 10, 50, 0.9}
            };

            FWOptimizer opt = new FWOptimizer();

            XmlElement fwElem = doc.CreateElement("FW");
            doc.DocumentElement.AppendChild(fwElem);

            Optimize<FWOptimizer, FWParams>(problem, opt, paramteters, doc, fwElem, logger, MaxRun);
        }

        private static void GEMOptimize(ControlBaseTask problem, int MaxRun, Logger logger, XmlDocument doc)
        {
            const int numParams = 5;

            object[][] paramteters =
            {
                new object[numParams]  {1, 100, 500, 2 * Math.Sqrt(problem.LowerBounds.Count), 10},
                new object[numParams] {1, 100, 500, 2 * Math.Sqrt(problem.LowerBounds.Count), 150 },
                new object[numParams] {2, 75, 250, Math.Sqrt(problem.LowerBounds.Count /(double)2), 10 },
                new object[numParams] {2, 50, 900, Math.Sqrt(problem.LowerBounds.Count /(double)2), 150 },
                new object[numParams] {2, 100, 600, Math.Sqrt(problem.LowerBounds.Count /(double)2), 200 }
            };

            GEMOptimizer opt = new GEMOptimizer();

            XmlElement gemElem = doc.CreateElement("GEM");
            doc.DocumentElement.AppendChild(gemElem);
            Optimize<GEMOptimizer, GEMParams>(problem, opt, paramteters, doc, gemElem, logger, MaxRun);
        }

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

            result.MapResult(options => Run(options), _ => 1);
        }

        private static void MOFWOptimize(MOControlTask problem, int MaxRun, Logger logger, XmlWriter XmlWriter)
        {
            const int numParams = 6;

            object[][] parameters =
            {
                new object[numParams] {150, 2000, 5, 5, 20, 0.25},
                new object[numParams] {250, 500, 15.0, 5, 20, 0.9},
                new object[numParams] {250, 500, 10.0, 10, 30, 0.8},
                new object[numParams] {400, 300, 10.0, 5, 20, 1.2},
                new object[numParams] {350, 1000, 5.0, 10, 20, 0.5},
                new object[numParams] {200, 1000, 5.0, 10, 20, 0.6}
            };

            MOFWOptimizer opt = new MOFWOptimizer();

            XmlWriter.WriteStartElement("MOFW");

            Dictionary<string, string> resInfo = new Dictionary<string, string>()
            {
                ["TargetV1"] = "",
                ["TargetV2"] = "",
                ["X1T"] = "",
                ["X2T"] = ""
            };

            foreach (var par in Enumerable.Range(0, parameters.Length).Zip(parameters, (num, par) => (Num: num, Parameters: par)))
            {
                logger.Info($"Try to find solution with {par.Num}th configuration of {parameters.Length}");

                XmlWriter.WriteStartElement("Experiment");
                XmlWriter.WriteStartElement("OptParams");

                FWParams pars = CreateParams<FWParams>(par.Parameters);

                var paramsType = pars.GetType();

                foreach (var prop in paramsType.GetProperties())
                {
                    if (prop.PropertyType == typeof(bool))
                        continue;

                    XmlWriter.WriteStartElement("Param");
                    XmlWriter.WriteAttributeString(prop.Name, prop.GetValue(pars).ToString());
                    XmlWriter.WriteEndElement();
                }

                XmlWriter.WriteEndElement();

                XmlWriter.WriteStartElement("Runs");

                for (int i = 0; i < MaxRun; i++)
                {
                    logger.Info($"Run {i} of {MaxRun}");

                    XmlWriter.WriteStartElement("Run");

                    try
                    {
                        opt.Minimize(pars, problem);

                        XmlWriter.WriteStartElement("Results");

                        foreach (var point in opt.ParetoFront)
                        {
                            problem.TargetFunction(point.Point);
                            resInfo["TargetV1"] = point.Objs[0].ToString();
                            resInfo["TargetV2"] = point.Objs[1].ToString();
                            resInfo["X1T"] = problem.X1T.ToString();
                            resInfo["X2T"] = problem.X2T.ToString();

                            XmlWriter.WriteStartElement("Res");

                            foreach (var kv in resInfo)
                            {
                                XmlWriter.WriteAttributeString(kv.Key, kv.Value);
                            }

                            XmlWriter.WriteEndElement();
                        }

                        XmlWriter.WriteEndElement();

                        XmlWriter.WriteStartElement("Controls");

                        foreach (var point in opt.ParetoFront)
                        {
                            XmlWriter.WriteStartElement("Control");

                            for (int j = 0; j < 2 * problem.NSwitches; j++)
                            {
                                XmlWriter.WriteStartElement("ContV");
                                int uNum = j < problem.NSwitches ? 1 : 2;
                                XmlWriter.WriteAttributeString("Num", uNum.ToString());
                                XmlWriter.WriteAttributeString("Value", point.Point[j].ToString());
                                XmlWriter.WriteEndElement();
                            }

                            XmlWriter.WriteEndElement();
                        }
                        XmlWriter.WriteEndElement();
                    }
                    catch (Exception exc)
                    {
                        logger.Error(exc, "Error was in optimization process.");
                        logger.Info("Recreate optimization method.");
                        opt = new MOFWOptimizer();
                        logger.Info($"Skip run {i}.");
                        continue;
                    }

                    XmlWriter.WriteEndElement();
                }

                XmlWriter.WriteEndElement();
                XmlWriter.WriteEndElement();
            }

            XmlWriter.WriteEndElement();
        }

        private static void Optimize<TOpt, TParams>(ControlBaseTask problem, TOpt opt, object[][] parameters, XmlDocument doc, XmlElement optElem, Logger logger, int MaxRun)
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

                for (int i = 0; i < MaxRun; i++)
                {
                    logger.Info($"Run {i} of {MaxRun}");

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
                    }
                    catch (Exception exc)
                    {
                        logger.Error(exc, "Error was in optimization process.");
                        logger.Info("Recreate optimization method.");
                        opt = new TOpt();
                        logger.Info($"Skip run {i}.");
                        continue;
                    }
                }
            }
        }

        private static int Run(Options options)
        {
            Logger logger = LogManager.GetLogger("Console");

            if (!Directory.Exists(options.OutputDir))
            {
                logger.Error($"'{options.OutputDir}' does not exist.");
                return -1;
            }

            if (options.MaxRun < 1)
            {
                logger.Error($"MaxRun (valus is {options.MaxRun}) is less than 1.");
                return -1;
            }

            var bound = options.UBounds.ToArray();

            if (bound[0] >= bound[1])
            {
                logger.Error("The lower bound is greater or equal than the upper bound.");
                return -1;
            }

            if (options.Times.Where(t => t <= 0).Any())
            {
                logger.Error("An one value of time is less or equal than 0.");
                return -1;
            }

            if (options.Switches.Where(n => n < 1).Any())
            {
                logger.Error("An one value of switches is less than 0.");
                return -1;
            }

            SolveTask(options.Tasks, options);

            return 0;
        }

        private static void SolveMOTaskWithT(int NSwitches, IReadOnlyCollection<double> Times, IReadOnlyList<double> Bounds, IReadOnlyList<double> X0, string PathToDir, int MaxRun)
        {
            int n = NSwitches;

            string dirPath = Path.Combine(PathToDir, "MOTask");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            MappedDiagnosticsContext.Set("id", $"_{Thread.CurrentThread.ManagedThreadId}_");
            MappedDiagnosticsContext.Set("problem", "_mo-problem_");

            foreach (var (i, Tmax) in Enumerable.Range(0, Times.Count).Zip(Times, (i, tmax) => (i, tmax)))
            {
                string pathToFile = Path.Combine(dirPath, $"res_{n}_{i}.xml");

                _logger.Info($"Open a xml file. '{pathToFile}'");

                var (lambda1, lambda2) = LambdasTask.GetLambdas(ProblemType.I1, n);
                var (lambda3, lambda4) = LambdasTask.GetLambdas(ProblemType.I2, n);

                double lowU = Bounds[0], uppU = Bounds[1], x10 = X0[0], x20 = X0[1];

                using (XmlWriter writer = XmlWriter.Create(pathToFile))
                {
                    MOControlTask problem = new MOControlTask(n, lowU, uppU, Tmax, x10, x20, lambda1, lambda2, lambda3, lambda4);

                    _logger.Info($"Start solving N = {n}");

                    writer.WriteStartDocument();
                    writer.WriteStartElement("Problem");

                    {
                        Dictionary<string, string> problemDesc = new Dictionary<string, string>
                        {
                            ["Name"] = "MOProblem",
                            ["Tmax"] = Tmax.ToString(),
                            ["NSwitches"] = n.ToString(),
                            ["ULower"] = lowU.ToString(),
                            ["UUpper"] = uppU.ToString(),
                            ["X10"] = x10.ToString(),
                            ["X20"] = x20.ToString(),
                            ["lambda1"] = lambda1.ToString(),
                            ["lambda2"] = lambda2.ToString(),
                            ["lambda3"] = lambda3.ToString(),
                            ["lambda4"] = lambda4.ToString()
                        };

                        foreach (var name in problemDesc)
                        {
                            writer.WriteAttributeString(name.Key, name.Value);
                        }
                    }

                    _logger.Info($"Time T = {Tmax}");
                    _logger.Info($"Creating problem. Type is MOProblem");
                    _logger.Info($"Start solving with MOFW.");

                    MOFWOptimize(problem, MaxRun, _logger, writer);

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                _logger.Info("Close xml file.");
            }

            MappedDiagnosticsContext.Remove("id");
            MappedDiagnosticsContext.Remove("problem");
        }

        private static void SolveTask(ProblemParamType task, Options options)
        {
            if (task == ProblemParamType.MOI)
            {
                Parallel.ForEach(options.Switches, (n) => SolveMOTaskWithT(n, options.Times.ToArray(), options.UBounds.ToArray(), options.X0.ToArray(), options.OutputDir, options.MaxRun));
            }
            else if (task == ProblemParamType.I12)
            {
                double lambdaL1, lambdaU1, lambdaL2, lambdaU2;

                var temp = options.Lambda.ToArray();

                if (temp.Length == 2)
                {
                    lambdaL1 = lambdaL2 = temp[0];
                    lambdaU1 = lambdaU2 = temp[1];
                }
                else
                {
                    int i = 0;
                    lambdaL1 = temp[i++];
                    lambdaU1 = temp[i++];
                    lambdaL2 = temp[i++];
                    lambdaU2 = temp[i++];
                }

                var task1 = Task.Factory.StartNew(() => SolveTask(ProblemType.I1, options.Switches.ToArray(), options.Times.ToArray(), options.UBounds.ToArray(), options.X0.ToArray(), options.OutputDir, options.MaxRun, lambdaL1, lambdaU1));
                var task2 = Task.Factory.StartNew(() => SolveTask(ProblemType.I2, options.Switches.ToArray(), options.Times.ToArray(), options.UBounds.ToArray(), options.X0.ToArray(), options.OutputDir, options.MaxRun, lambdaL2, lambdaU2));

                Task.WaitAll(task1, task2);
            }
        }

        private static void SolveTask(ProblemType Problem, IReadOnlyCollection<int> NSwitches, IReadOnlyCollection<double> Times, IReadOnlyList<double> Bounds, IReadOnlyList<double> X0, string PathToDir, int MaxRun, double LowerLambda, double UpperLambda)
        {
            MappedDiagnosticsContext.Set("id", $"_{Thread.CurrentThread.ManagedThreadId}_");
            MappedDiagnosticsContext.Set("problem", Problem == ProblemType.I1 ? "_i1_" : "_i2_");

            string dirPath = Path.Combine(PathToDir, Problem == ProblemType.I1 ? "Task1" : "Task2");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            XmlWriterSettings xmlSettings = new XmlWriterSettings();

            double uLower = Bounds[0], uUpper = Bounds[1];

            double X10 = X0[0], X20 = X0[1];

            int i = 0;

            foreach (var n in NSwitches)
            {
                _logger.Info($"Start solving N = {n}");

                foreach (var TMax in Times)
                {
                    XmlDocument doc = new XmlDocument();
                    XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    XmlElement root = doc.CreateElement("Problem");
                    doc.AppendChild(root);
                    doc.InsertBefore(xmlDeclaration, root);

                    {
                        var problemDesc = new Dictionary<string, string>
                        {
                            ["Name"] = Problem.ToString(),
                            ["Tmax"] = TMax.ToString(),
                            ["NSwitches"] = n.ToString(),
                            ["ULower"] = uLower.ToString(),
                            ["UUpper"] = uUpper.ToString(),
                            ["X10"] = X0[0].ToString(),
                            ["X20"] = X0[1].ToString()
                        };

                        foreach (var name in problemDesc)
                        {
                            root.SetAttribute(name.Key, name.Value);
                        }
                    }

                    _logger.Info($"Time T = {TMax}");
                    _logger.Info($"Creating problem. Type is {Problem.ToString()}");

                    ControlBaseTask problem = null;

                    switch (Problem)
                    {
                        case ProblemType.I1:
                            {
                                problem = new ControlTaskI1(n, uLower, uUpper, TMax, X10, X20, LowerLambda, UpperLambda);
                                break;
                            }
                        case ProblemType.I2:
                            {
                                problem = new ControlTaskI2(n, uLower, uUpper, TMax, X10, X20, LowerLambda, UpperLambda);
                                break;
                            }
                        default:
                            {
                                throw new ArgumentException("Invalid problem type.", nameof(ProblemParamType));
                            }
                    }

                    _logger.Info($"Start solving with BBBC.");
                    BBBCOptimize(problem, MaxRun, _logger, doc);
                    _logger.Info($"Start solving with FW.");
                    FWOptimize(problem, MaxRun, _logger, doc);
                    _logger.Info($"Start solving with GEM.");
                    GEMOptimize(problem, MaxRun, _logger, doc);

                    string pathToFile = Path.Combine(dirPath, $"res_{n}_{i++}.xml");

                    using (XmlWriter writer = XmlWriter.Create(pathToFile, xmlSettings))
                    {
                        doc.Save(writer);
                        _logger.Info($"Write res to a file '{pathToFile}'.");
                    }
                }
            }
        }
    }

    internal enum ProblemParamType { I12, MOI };

    internal enum ProblemType { I1, I2 };
}