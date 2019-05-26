namespace ControlTask.Exps
{
    using System.Collections.Generic;
    using System.IO;

    internal class TaskInfo
    {
        private TaskInfo(MOExp Exp, MOFWParam Parameters, string ExpDir, int NumExp)
        {
            (int NSwitches, double TMax, double X10, double X20, Bounds U1Bounds, Bounds U2Bounds, double Lambda1
           , double Lambda2, double Lambda3, double Lambda4, int TotalRun, int NSteps) = Exp;

            Problem = new MOControlTask(NSwitches, U1Bounds.Lower, U1Bounds.Upper, TMax, X10, X20, Lambda1, Lambda2, Lambda3, Lambda4, NSteps);
            this.Parameters = new EOpt.Math.Optimization.FWParams(Parameters.NP, Parameters.Imax, Parameters.M, Parameters.Smin, Parameters.Smax, Parameters.Amax);
            this.TotalRun = TotalRun;
            PathToDir = ExpDir;
            this.NumExp = NumExp;
            this.NumSteps = NSteps;
        }

        public int NumExp { get; private set; }

        public int NumSteps { get; private set; }

        public EOpt.Math.Optimization.FWParams Parameters { get; private set; }

        public string PathToDir { get; private set; }

        public MOControlTask Problem { get; private set; }

        public int TotalRun { get; private set; }

        public static LinkedList<TaskInfo> CreateTasks(MOControlExperiments Exp, string BaseDir)
        {
            LinkedList<TaskInfo> tasksInfo = new LinkedList<TaskInfo>();

            int i = 0;

            foreach (var exp in Exp.MOExperiments)
            {
                string dir = Path.Combine(BaseDir, $"Experiment_{++i}");

                foreach (var par in exp.MOFWParams)
                {
                    tasksInfo.AddLast(new TaskInfo(exp, par, dir, i));
                }
            }

            return tasksInfo;
        }
    }
}