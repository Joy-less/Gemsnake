using System.Threading.Tasks;

namespace Gemsnake
{
    public class RubyEvaluator : Evaluator
    {
        public RubyEvaluator(string RubyExecutablePath = "RubyEvaluator.exe", int Port = 0) {
            if (Port == 0) Port = GetFreeTcpPort();
            ExecutableProcess = RunProcess(RubyExecutablePath, Port.ToString());
            Task.Run(() => Listen(Port));
        }
    }
}
