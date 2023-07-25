using System.Threading.Tasks;

namespace Gemsnake
{
    public class PythonEvaluator : Evaluator
    {
        public PythonEvaluator(string PythonExecutablePath = "PythonEvaluator.exe", int Port = 0) {
            if (Port == 0) Port = GetFreeTcpPort();
            ExecutableProcess = RunProcess(PythonExecutablePath, Port.ToString());
            Task.Run(() => Listen(Port));
        }
    }
}
