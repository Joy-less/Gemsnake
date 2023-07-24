namespace Gemsnake
{
    internal class PythonEvaluator : Evaluator
    {
        public PythonEvaluator(int Port = 29421, string PythonExecutablePath = "PythonEvaluator.exe") {
            ExecutableProcess = RunProcess(PythonExecutablePath, Port.ToString());
            Task.Run(() => Listen(Port));
        }
    }
}
