namespace MOTestTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using CommandLine;
    using CommandLine.Text;

    using EOpt.Math.Optimization;
    using EOpt.Math.Optimization.MOOpt;

    using MathNet.Numerics.Distributions;

    using NLog;

    using Problems;

    internal class ContGen : EOpt.Math.Random.IContUniformGen
    {
        private MathNet.Numerics.Distributions.ContinuousUniform _uniform;

        public ContGen()
        {
            _uniform = new MathNet.Numerics.Distributions.ContinuousUniform();
        }

        public double URandVal(double LowBound, double UpperBound)
        {
            return (UpperBound - LowBound) * _uniform.Sample() + LowBound;
        }
    }

    internal class NormalGen : EOpt.Math.Random.INormalGen
    {
        private Normal _normal;

        public NormalGen()
        {
            _normal = new Normal();
        }

        public double NRandVal(double Mean, double StdDev)
        {
            return Mean + _normal.Sample() * StdDev;
        }
    }

    internal class Options
    {
        private static readonly Example[] _examples = new Example[1]
        {
            new Example("Пример запуска", new Options { TestProblems = Enum.GetValues(typeof(MOTestTasks.TestProblems)).Cast<TestProblems>(), OutputDir = ".../dir", MaxRun = 10, DimDec = 30})
        };

        [Usage]
        public static IEnumerable<Example> Examples => _examples;

        [Option('d', "dim-dec", HelpText = "Размерность пространства решений. Для ZDT6 всегда используется значение 10, если значение параметра больше 10. Для DTLZ1 и DTLZ2 этот параметр не имеет значения так как размерность пространства решения рассчитывается на основе числа критериев.", Default = 30)]
        public int DimDec { get; set; }

        [Option('r', "max-run", HelpText = "Максимальное число запусков.", Required = true)]
        public int MaxRun { get; set; }

        [Option('o', "output-dir", HelpText = "Путь до директории, где будут сохранены результаты в виде XML файлов.", Required = true)]
        public string OutputDir { get; set; }

        [Value(0, MetaName = "Test problems.", HelpText = "Названия тестовых проблем для запуска. Значения перечисляются через пробел.", Min = 1, Max = 8)]
        public IEnumerable<TestProblems> TestProblems { get; set; }
    }

    internal class Program
    {
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

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

            result.MapResult(options => Run(options), _ => 1);
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

            if (options.DimDec < 2)
            {
                logger.Error($"DimDec (valus is {options.DimDec}) is less than 2.");
                return -1;
            }

            if (options.TestProblems.Distinct().Count() != options.TestProblems.Count())
            {
                logger.Error($"TestProblems have a duplicate values.");
                return -1;
            }

            SolveProblems(options);

            return 0;
        }

        private static void SolveFW(INamedMOProblem Problem, string PathToOutDir, int MaxRun)
        {
            MappedDiagnosticsContext.Set("thread_id", $"_{Thread.CurrentThread.ManagedThreadId}_");

            Logger logger = LogManager.GetLogger("Main");

            const int numParams = 6;

            var a  = new FWParams()

            object[][] parameters =
            {
                new object[numParams] {300, 500, 5, 5, 20, 0.1},
                new object[numParams] {300, 500, 5, 5, 20, 0.2},
                new object[numParams] {300, 500, 5, 5, 20, 0.5},
                new object[numParams] {300, 500, 5, 5, 20, 1.9},
                new object[numParams] {300, 500, 5, 5, 20, 1.2}
            };

            string pathToXml = Path.Combine(PathToOutDir, $"{Problem.Name}_res.xml");

            logger.Info($"Open a xml file. '{pathToXml}'");

            var normalGen = new NormalGen();
            var contGen = new ContGen();

            MOFWOptimizer opt = new MOFWOptimizer(contGen, normalGen);

            using (XmlWriter writer = XmlWriter.Create(pathToXml))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Problem");

                Dictionary<string, string> problemDesc = new Dictionary<string, string>
                {
                    ["Name"] = Problem.Name,
                    ["DimDec"] = Problem.LowerBounds.Count.ToString(),
                    ["DimObj"] = Problem.CountObjs.ToString(),
                };

                foreach (var name in problemDesc)
                {
                    writer.WriteAttributeString(name.Key, name.Value);
                }

                writer.WriteStartElement("Bounds");

                foreach (var bound in Problem.LowerBounds.Zip(Problem.UpperBounds, (low, upp) => (Lower: low, Upper: upp)))
                {
                    writer.WriteStartElement("Bound");
                    writer.WriteAttributeString("LowerB", bound.Lower.ToString());
                    writer.WriteAttributeString("UpperB", bound.Upper.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                logger.Info("Start solving with MOFW.");

                foreach (var par in Enumerable.Range(0, parameters.Length).Zip(parameters, (num, par) => (Num: num, Parameters: par)))
                {
                    logger.Info($"Try to find solution with {par.Num}th configuration of {parameters.Length}");

                    writer.WriteStartElement("Experiment");
                    writer.WriteStartElement("OptParams");

                    FWParams pars = CreateParams<FWParams>(par.Parameters);

                    var paramsType = pars.GetType();

                    foreach (var prop in paramsType.GetProperties())
                    {
                        if (prop.PropertyType == typeof(bool))
                        {
                            continue;
                        }

                        writer.WriteStartElement("Param");
                        writer.WriteAttributeString(prop.Name, prop.GetValue(pars).ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                    writer.WriteStartElement("Runs");

                    for (int i = 0; i < MaxRun; i++)
                    {
                        logger.Info($"Run {i} of {MaxRun}");

                        writer.WriteStartElement("Run");

                        try
                        {
                            opt.Minimize(pars, Problem);

                            writer.WriteStartElement("Decisions");

                            foreach (var agent in opt.ParetoFront)
                            {
                                writer.WriteStartElement("Point");

                                for (int coordIndex = 0; coordIndex < agent.Point.Count; coordIndex++)
                                {
                                    writer.WriteAttributeString($"x{coordIndex + 1}", agent.Point[coordIndex].ToString());
                                }

                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();

                            writer.WriteStartElement("Targets");

                            foreach (var agent in opt.ParetoFront)
                            {
                                writer.WriteStartElement("Target");

                                for (int coordIndex = 0; coordIndex < agent.Objs.Count; coordIndex++)
                                {
                                    writer.WriteAttributeString($"F{coordIndex + 1}", agent.Objs[coordIndex].ToString());
                                }

                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();
                        }
                        catch (Exception exc)
                        {
                            logger.Error(exc, "Error was in optimization process.");
                            logger.Info("Recreate optimization method.");
                            opt = new MOFWOptimizer();
                            logger.Info($"Skip run {i}.");
                        }

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }
            logger.Info("Close a xml file.");

            MappedDiagnosticsContext.Remove("thread_id");
        }

        private static void SolveProblems(Options options)
        {
            int dimDec = options.DimDec;

            Dictionary<TestProblems, INamedMOProblem> problems = new Dictionary<TestProblems, INamedMOProblem>()
            {
                [TestProblems.ZDT1] = new ZDT1(dimDec),
                [TestProblems.ZDT2] = new ZDT2(dimDec),
                [TestProblems.ZDT3] = new ZDT3(dimDec),
                [TestProblems.ZDT6] = new ZDT6(Math.Min(10, dimDec)),
                [TestProblems.F1CPF] = new F1CPF(dimDec),
                [TestProblems.F2CPF] = new F2CPF(dimDec),
                [TestProblems.F5CPF] = new F5CPF(dimDec),
                [TestProblems.DTLZ1] = new DTLZ1(3),
                [TestProblems.DTLZ2] = new DTLZ2(3)
            };

            Parallel.ForEach(options.TestProblems, problem => SolveFW(problems[problem], options.OutputDir, options.MaxRun));
        }
    }

    internal enum TestProblems
    { ZDT1, ZDT2, ZDT3, ZDT6, F1CPF, F2CPF, F5CPF, DTLZ1, DTLZ2 };
}