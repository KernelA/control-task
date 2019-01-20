namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using EOpt.Math.Optimization;
    using EOpt.Math.Optimization.MOOpt;
    using EOpt.Math.Optimization.OOOpt;

    using NLog;

    internal enum ProblemType { I1, I2 };

    internal class Program
    {
        private const int MAX_RUN = 10;

        private const int U_LOWER = -10, U_UPPER = 10;

        private const double X10 = 0.5, X20 = 1;

        private static readonly int[] SWITCHES = { 8, 10 };

        private static readonly double[] TIMES = { 1, 1.5, 2, 4};

        private static readonly Logger _logger = LogManager.GetLogger("Main");

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

        private static void FWOptimize(ControlBaseTask problem, Logger logger, XmlDocument doc)
        {
            const int numParams = 6;

            object[][] paramteters =
            {
                new object[numParams] {100, 300, 20.0, 10, 30, 2.5},
                new object[numParams] {20, 500, 20.0, 10, 50, 1.0},
                new object[numParams] {20, 150, 20.0, 10, 50, 2.0},
                new object[numParams] {30, 130, 20.0, 10, 50, 9.0 },
                new object[numParams] {40, 125, 2.0, 10, 50, 0.9}
            };

            FWOptimizer opt = new FWOptimizer();

            XmlElement fwElem = doc.CreateElement("FW");
            doc.DocumentElement.AppendChild(fwElem);

            Optimize<FWOptimizer, FWParams>(problem, opt, paramteters, doc, fwElem, logger);
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

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (args.Length != 1)
            {
                Console.WriteLine("You need to specify path to the directory where store results as cli argument.");
            }
            else
            {
                _pathToDir = args[0];

                if (!Directory.Exists(_pathToDir))
                {
                    Console.WriteLine($"'{_pathToDir}' does not exist.");
                    return;
                }
            }

            Task task1 = new Task(taskType => SolveTask((ProblemType)taskType), ProblemType.I1);
            Task task2 = new Task(taskType => SolveTask((ProblemType)taskType), ProblemType.I2);

            task1.Start();
            task2.Start();

            Task.WaitAll(task1, task2);

            //SolveMOTask();
        }

        private static void MOFWOptimize(MOControlTask problem, Logger logger, XmlWriter XmlWriter)
        {
            const int numParams = 6;

            object[][] parameters =
            {
                new object[numParams] {250, 600, 5, 5, 20, 2.0},
                new object[numParams] {250, 260, 15.0, 5, 20, 4.0},
                new object[numParams] {250, 300, 10.0, 10, 30, 3.0},
                new object[numParams] {250, 400, 10.0, 5, 20, 2.0 },
                new object[numParams] {300, 900, 5.0, 10, 20, 1.5}
            };


            MOFWOptimizer opt = new MOFWOptimizer();

            XmlWriter.WriteStartElement("MOFW");
            

            Dictionary<string, string> resInfo = new Dictionary<string, string>()
            {
                ["TargetV1"] = "",
                ["TargetV2"] = "",
                ["lambda1"] = "",
                ["lambda2"] = "",
                ["lambda3"] = "",
                ["lambda4"] = "",
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

                for (int i = 0; i < MAX_RUN; i++)
                {
                    logger.Info($"Run {i} of {MAX_RUN}");

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
                            resInfo["lambda1"] = point.Point[point.Point.Count - 4].ToString();
                            resInfo["lambda2"] = point.Point[point.Point.Count - 3].ToString();
                            resInfo["lambda3"] = point.Point[point.Point.Count - 2].ToString();
                            resInfo["lambda4"] = point.Point[point.Point.Count - 1].ToString();
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

                        foreach(var point in opt.ParetoFront)
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
            }

            XmlWriter.WriteEndElement();
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

        private static void SolveMOTask()
        {
            ParallelOptions options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 2
             };

            Parallel.ForEach(Enumerable.Range(0, TIMES.Length).Zip(TIMES, (i, t) => (Tmax: t, Num: i)), options, SolveMOTaskWithT);
        }

        private static void SolveMOTaskWithT((double Tmax, int i) param)
        {
            string dirPath = Path.Combine(_pathToDir, "MOTask");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            MappedDiagnosticsContext.Set("id", $"_{Thread.CurrentThread.ManagedThreadId}_");
            MappedDiagnosticsContext.Set("problem", "_mo-problem_");

            foreach (var n in SWITCHES)
            {
                string pathToFile = Path.Combine(_pathToDir, dirPath, $"res_{n}_{param.i}.xml");

                _logger.Info("Open a xml file.");

                using (XmlWriter writer = XmlWriter.Create(pathToFile))
                {
                    MOControlTask problem = new MOControlTask(n, U_LOWER, U_UPPER, param.Tmax, X10, X20);

                    _logger.Info($"Start solving N = {n}");

                    writer.WriteStartDocument();
                    writer.WriteStartElement("Problem");

                    {
                        Dictionary<string, string> problemDesc = new Dictionary<string, string>
                        {
                            ["Name"] = "MOProblem",
                            ["Tmax"] = param.Tmax.ToString(),
                            ["NSwitches"] = n.ToString(),
                            ["ULower"] = U_LOWER.ToString(),
                            ["UUpper"] = U_UPPER.ToString(),
                            ["X10"] = X10.ToString(),
                            ["X20"] = X20.ToString()
                        };

                        foreach (var name in problemDesc)
                        {
                            writer.WriteAttributeString(name.Key, name.Value);
                        }
                    }

                    _logger.Info($"Time T = {param.Tmax}");
                    _logger.Info($"Creating problem. Type is MOProblem");
                    _logger.Info($"Start solving with MOFW.");

                    MOFWOptimize(problem, _logger, writer);

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                _logger.Info("Close xml file.");
            }

            MappedDiagnosticsContext.Remove("id");
            MappedDiagnosticsContext.Remove("problem");

        }

        private static void SolveTask(ProblemType Problem)
        {
            MappedDiagnosticsContext.Set("id", $"_{Thread.CurrentThread.ManagedThreadId}_");
            MappedDiagnosticsContext.Set("problem", Problem == ProblemType.I1 ? "_i1_" : "_i2_");

            string dirPath = Path.Combine(_pathToDir, Problem == ProblemType.I1 ? "Task1" : "Task2");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            XmlWriterSettings xmlSettings = new XmlWriterSettings();

            foreach (var n in SWITCHES)
            {
                _logger.Info($"Start solving N = {n}");

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

                    _logger.Info($"Time T = {TMax}");
                    _logger.Info($"Creating problem. Type is {Problem.ToString()}");

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

                    _logger.Info($"Start solving with BBBC.");
                    BBBCOptimize(problem, _logger, doc);
                    _logger.Info($"Start solving with FW.");
                    FWOptimize(problem, _logger, doc);
                    _logger.Info($"Start solving with GEM.");
                    GEMOptimize(problem, _logger, doc);

                    string pathToFile = Path.Combine(_pathToDir, dirPath, $"res_{n}_{i}.xml");

                    using (XmlWriter writer = XmlWriter.Create(pathToFile, xmlSettings))
                    {
                        doc.Save(writer);
                        _logger.Info($"Write res to a file '{pathToFile}'.");
                    }
                }
            }
        }
    }

    
}