namespace MOTestTasks.Problems
{

    using EOpt.Math.Optimization.MOOpt;

    interface INamedMOProblem : IMOOptProblem
    { 
        string Name { get; }
    }
}
