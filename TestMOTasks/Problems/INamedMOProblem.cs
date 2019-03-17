namespace MOTestTasks.Problems
{
    using EOpt.Math.Optimization.MOOpt;

    internal interface INamedMOProblem : IMOOptProblem
    {
        string Name { get; }
    }
}