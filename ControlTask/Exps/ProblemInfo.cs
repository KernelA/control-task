namespace ControlTask.Exps
{
    using System.Collections.Generic;
    using System.IO;

    internal class ProblemInfo
    {
        private ProblemInfo(MOExp Exp, MOFWParam Parameters, string PathToFile, int NumExp)
        {
            (int NSwitches, double TMax, double X10, double X20, Bounds U1Bounds, Bounds U2Bounds, double Lambda1
           , double Lambda2, double Lambda3, double Lambda4, int TotalRun, int NSteps) = Exp;

            Problem = new MOControlTask(NSwitches, U1Bounds.Lower, U1Bounds.Upper, TMax, X10, X20, Lambda1, Lambda2, Lambda3, Lambda4, NSteps);
            this.Parameters = new EOpt.Math.Optimization.FWParams(Parameters.NP, Parameters.Imax, Parameters.M, Parameters.Smin, Parameters.Smax, Parameters.Amax);
            this.TotalRun = TotalRun;
            this.FileName = PathToFile;
            this.BaseDir = Path.GetDirectoryName(this.FileName);
            this.NumExp = NumExp;
            this.NumSteps = NSteps;
        }

        public int NumExp { get; private set; }

        public int NumSteps { get; private set; }

        public EOpt.Math.Optimization.FWParams Parameters { get; private set; }

        public string BaseDir { get; private set; }

        public string FileName { get; private set; }

        public MOControlTask Problem { get; private set; }

        public int TotalRun { get; private set; }

        public static LinkedList<ProblemInfo> CreateTasks(MOControlExperiments Exp, string BaseDir)
        {
            LinkedList<ProblemInfo> tasksInfo = new LinkedList<ProblemInfo>();

            int i = 0;

            foreach (var exp in Exp.MOExperiments)
            {
                int j = 0;
                i++;

                foreach (var par in exp.MOFWParams)
                {
                    string pathToFile = Path.Combine(BaseDir, $"Experiment_{i}", $"part_{++j}.xml");
                    tasksInfo.AddLast(new ProblemInfo(exp, par, pathToFile, i));
                }
                
            }

            return tasksInfo;
        }
    }
}