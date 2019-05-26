namespace ControlTask
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;

    using CommandLine;

    using EOpt.Math.Optimization;
    using EOpt.Math.Optimization.MOOpt;
    using EOpt.Math.Optimization.OOOpt;

    using Exps;

    using NLog;

    using BBBCParams = EOpt.Math.Optimization.BBBCParams;
    using FWParams = EOpt.Math.Optimization.FWParams;
    using GEMParams = EOpt.Math.Optimization.GEMParams;

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

            _lambdas[0].Add(8, (6020.49142775647, 13228.3055394025));
            _lambdas[0].Add(10, (21240.5168329197, 5288.83298590383));
            _lambdas[0].Add(15, (7372.92658580968, 5518.60746104122));

            _lambdas[1].Add(8, (7398.21074455893, 23228.1859177857));
            _lambdas[1].Add(10, (15014.9665908026, 12347.2391412813));
            _lambdas[1].Add(15, (17727.3778533999, 46942.3996244755));
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
        [Option('c', "config", HelpText = "XML файл с настройками экспериментов.", Required = true)]
        public string Config { get; set; }

        [Option('o', "output-dir", HelpText = "Путь до директории, где будут сохранены результаты в виде XML файлов.", Required = true)]
        public string OutputDir { get; set; }
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

        private static void CollectMOResults(IReadOnlyCollection<TaskInfo> Tasks)
        {
            _logger.Info("Collecting of results");

            foreach (var taskInfo in Tasks)
            {
                string pathToFile = Path.Combine(taskInfo.PathToDir, Path.GetDirectoryName(taskInfo.PathToDir) + ".xml");

                if (!File.Exists(pathToFile))
                {
                    _logger.Info($"Collecting from '{taskInfo.PathToDir}'");

                    using (XmlWriter writer = XmlWriter.Create(pathToFile + ".xml"))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Problem");

                        {
                            var problemDesc = new Dictionary<string, string>
                            {
                                ["Name"] = "MOProblem",
                                ["Tmax"] = taskInfo.Problem.TMax.ToString(),
                                ["NSwitches"] = taskInfo.Problem.NSwitches.ToString(),
                                ["ULower"] = taskInfo.Problem.LowerBounds[0].ToString(),
                                ["UUpper"] = taskInfo.Problem.UpperBounds[0].ToString(),
                                ["X10"] = taskInfo.Problem.X10.ToString(),
                                ["X20"] = taskInfo.Problem.X20.ToString(),
                                ["NumOdeSteps"] = taskInfo.NumSteps.ToString(),
                                ["lambda1"] = taskInfo.Problem.Lmabda1.ToString(),
                                ["lambda2"] = taskInfo.Problem.Lmabda2.ToString(),
                                ["lambda3"] = taskInfo.Problem.Lmabda3.ToString(),
                                ["lambda4"] = taskInfo.Problem.Lmabda4.ToString()
                            };

                            foreach (var name in problemDesc)
                            {
                                writer.WriteAttributeString(name.Key, name.Value);
                            }
                        }

                        writer.WriteStartElement("MOFW");

                        foreach (var file in Directory.EnumerateFiles(taskInfo.PathToDir, "part*.xml"))
                        {
                            _logger.Info($"Collecting from '{file}' to '{pathToFile}'");
                            using (XmlReader reader = XmlReader.Create(file))
                            {
                                writer.WriteNode(reader, false);
                            }

                            File.Delete(file);
                            _logger.Info($"A temporary file deleted: '{file}'");
                        }

                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }
            }
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

        /// <summary>
        /// </summary>
        /// <param name="Path">  </param>
        /// <returns>  </returns>
        /// <exception cref="XmlSchemaException">  </exception>
        private static XmlSchema LoadXsdSchema(string Path)
        {
            XmlSchema schema = null;

            using (FileStream schemaStream = new FileStream(Path, FileMode.Open))
            {
                schema = XmlSchema.Read(schemaStream, null);
            }

            return schema;
        }

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //var a = new MOExp()
            //{
            //    Lambda1 = 10,
            //    Lambda2 = 20,
            //    NSteps = 10,
            //    TMax = 1.5,
            //    NSwitches = 20,
            //    TotalRun = 5,
            //    X10 = 0.6
            //};
            //a.U1Bounds = new Bounds(0, 10);
            //a.U2Bounds = new Bounds(0, 10);
            //a.MOFWParams = new ControlTask.Exps.MOFWParam[] { new ControlTask.Exps.MOFWParam() { Amax = 10, M = 10, Smax = 10, Imax = 10, Smin = 2, NP = 3 } };
            //MOControlExperiments exps = new MOControlExperiments(new MOExp[] { a });

            ////exps.Save("oo.xml");

            //var res = MOControlExperiments.Load("oo.xml");
            ////res.Save("o1.xml");

            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

            result.MapResult(options => Run(options), _ => 1);
        }

        private static void MOFWOptimize(MOControlTask problem, EOpt.Math.Optimization.FWParams Parameters, int MaxRun, XmlWriter XmlWriter)
        {
            MOFWOptimizer opt = new MOFWOptimizer();

            Dictionary<string, string> resInfo = new Dictionary<string, string>()
            {
                ["TargetV1"] = "",
                ["TargetV2"] = "",
                ["X1T"] = "",
                ["X2T"] = ""
            };

            _logger.Info($"Try to find solution.");

            XmlWriter.WriteStartElement("Experiment");
            XmlWriter.WriteStartElement("OptParams");

            var paramsType = Parameters.GetType();

            foreach (var prop in paramsType.GetProperties())
            {
                if (prop.PropertyType == typeof(bool))
                    continue;

                XmlWriter.WriteStartElement("Param");
                XmlWriter.WriteAttributeString(prop.Name, prop.GetValue(Parameters).ToString());
                XmlWriter.WriteEndElement();
            }

            XmlWriter.WriteEndElement();

            XmlWriter.WriteStartElement("Runs");

            for (int i = 0; i < MaxRun; i++)
            {
                _logger.Info($"Run {i} of {MaxRun}");

                XmlWriter.WriteStartElement("Run");

                try
                {
                    opt.Minimize(Parameters, problem);

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
                    _logger.Error(exc, "Error was in optimization process.");
                    _logger.Warn("Recreate optimization method.");
                    opt = new MOFWOptimizer();
                    _logger.Warn($"Skip run {i}.");
                    continue;
                }

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
            if (!Directory.Exists(options.OutputDir))
            {
                _logger.Error($"'{options.OutputDir}' does not exist.");
                return -1;
            }

            if (!File.Exists(options.Config))
            {
                _logger.Error($"'{options.Config}' does not exist.");
                return -1;
            }

            XmlSchema schema = null;

            try
            {
                schema = LoadXsdSchema(Path.Combine("ConfigXSD", "ConfigSchema.xsd"));
            }
            catch (XmlSchemaException exc)
            {
                _logger.Error(exc);
                return -1;
            }

            MOControlExperiments exps = null;

            try
            {
                exps = MOControlExperiments.Load(options.Config, schema);
            }
            catch (InvalidOperationException exc)
            {
                _logger.Error(exc);
                return -1;
            }

            if (exps != null)
            {
                RunMOExps(exps, options.OutputDir);
            }

            return 0;
        }

        private static void RunMOExp(TaskInfo Exp, ParallelLoopState State)
        {
            MappedDiagnosticsContext.Set("id", $"_{Thread.CurrentThread.ManagedThreadId}_");
            MappedDiagnosticsContext.Set("num", Exp.NumExp);

            XmlWriterSettings xmlSettings = new XmlWriterSettings()
            {
                OmitXmlDeclaration = true
            };

            _logger.Info($"Start solving experiment. Number is {Exp.NumExp}");

            string pathToFile = Path.Combine(Exp.PathToDir, $"part_{Exp.NumExp}.xml");

            using (XmlWriter writer = XmlWriter.Create(pathToFile, xmlSettings))
            {
                _logger.Info($"Start solving with MOFW.");
                MOFWOptimize(Exp.Problem, Exp.Parameters, Exp.TotalRun, writer);
            }

            _logger.Info($"Finished a part {pathToFile}");
        }

        private static void RunMOExps(MOControlExperiments Exps, string PathToDir)
        {
            var exps = TaskInfo.CreateTasks(Exps, PathToDir);

            foreach (var exp in exps)
            {
                if (!Directory.Exists(exp.PathToDir))
                {
                    Directory.CreateDirectory(exp.PathToDir);
                }
            }

            Parallel.ForEach(exps, RunMOExp);

            CollectMOResults(exps);
        }


        //private static void SolveTask(ProblemType Problem, IReadOnlyCollection<int> NSwitches, IReadOnlyCollection<double> Times, IReadOnlyList<double> Bounds, IReadOnlyList<double> X0, string PathToDir, int MaxRun, double LowerLambda, double UpperLambda)
        //{
        //    MappedDiagnosticsContext.Set("id", $"_{Thread.CurrentThread.ManagedThreadId}_");
        //    MappedDiagnosticsContext.Set("problem", Problem == ProblemType.I1 ? "_i1_" : "_i2_");

        // string dirPath = Path.Combine(PathToDir, Problem == ProblemType.I1 ? "Task1" : "Task2");

        // if (!Directory.Exists(dirPath)) { Directory.CreateDirectory(dirPath); }

        // XmlWriterSettings xmlSettings = new XmlWriterSettings();

        // double uLower = Bounds[0], uUpper = Bounds[1];

        // double X10 = X0[0], X20 = X0[1];

        // int i = 0;

        // foreach (var n in NSwitches) { _logger.Info($"Start solving N = {n}");

        // foreach (var TMax in Times) { XmlDocument doc = new XmlDocument(); XmlDeclaration
        // xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null); XmlElement root =
        // doc.CreateElement("Problem"); doc.AppendChild(root); doc.InsertBefore(xmlDeclaration, root);

        // { var problemDesc = new Dictionary<string, string> { ["Name"] = Problem.ToString(),
        // ["Tmax"] = TMax.ToString(), ["NSwitches"] = n.ToString(), ["ULower"] = uLower.ToString(),
        // ["UUpper"] = uUpper.ToString(), ["X10"] = X0[0].ToString(), ["X20"] = X0[1].ToString() };

        // foreach (var name in problemDesc) { root.SetAttribute(name.Key, name.Value); } }

        // _logger.Info($"Time T = {TMax}"); _logger.Info($"Creating problem. Type is {Problem.ToString()}");

        // ControlBaseTask problem = null;

        // switch (Problem) { case ProblemType.I1: { problem = new ControlTaskI1(n, uLower, uUpper,
        // TMax, X10, X20, LowerLambda, UpperLambda); break; } case ProblemType.I2: { problem = new
        // ControlTaskI2(n, uLower, uUpper, TMax, X10, X20, LowerLambda, UpperLambda); break; }
        // default: { throw new ArgumentException("Invalid problem type.", nameof(ProblemParamType));
        // } }

        // _logger.Info($"Start solving with BBBC."); BBBCOptimize(problem, MaxRun, _logger, doc);
        // _logger.Info($"Start solving with FW."); FWOptimize(problem, MaxRun, _logger, doc);
        // _logger.Info($"Start solving with GEM."); GEMOptimize(problem, MaxRun, _logger, doc);

        // string pathToFile = Path.Combine(dirPath, $"res_{n}_{i++}.xml");

        //            using (XmlWriter writer = XmlWriter.Create(pathToFile, xmlSettings))
        //            {
        //                doc.Save(writer);
        //                _logger.Info($"Write res to a file '{pathToFile}'.");
        //            }
        //        }
        //    }
        //}
    }

    internal enum ProblemParamType { I12, MOI };

    internal enum ProblemType { I1, I2 };
}