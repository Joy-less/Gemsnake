using System.Threading.Tasks;

namespace Gemsnake
{
    public class RubyEvaluator : Evaluator
    {
        public RubyEvaluator(int Port = 29420, string RubyExecutablePath = "RubyEvaluator.exe") {
            ExecutableProcess = RunProcess(RubyExecutablePath, Port.ToString());
            Task.Run(() => Listen(Port));
        }
    }
}
