namespace Gemsnake
{
    internal class Program
    {
        static void Main() {
            RubyExample();
            PythonExample();
            Console.WriteLine("End of program.");
            Console.ReadLine();
        }
        static void RubyExample() {
            // Setup evaluator
            RubyEvaluator Ruby = new();
            Ruby.WaitForConnection();
            Console.WriteLine("Ruby started");

            // Get the version
            Console.WriteLine(Ruby.Evaluate("RUBY_VERSION").ReturnValue); // 3.2.2
            // Define a method
            Ruby.Evaluate(@"
def add_some_numbers
    a = 3
    b = 5
    puts a + b
end
");
            // Call a method
            Ruby.Evaluate("add_some_numbers"); // 8
            // Return a value
            Console.WriteLine(Ruby.Evaluate("return 2").ReturnValue); // 2
            // Error
            Evaluator.EvaluationResult Result = Ruby.Evaluate("An Error");
            Console.WriteLine(Result.FormatError()); // Error on line 1: uninitialized constant Evaluator::Error
        }
        static void PythonExample() {
            // Setup evaluator
            PythonEvaluator Python = new();
            Python.WaitForConnection();
            Console.WriteLine("Python started");

            // Get the version
            Console.WriteLine(Python.Evaluate(@"
import sys
return sys.version.split(' ', 1)[0]").ReturnValue); // 3.11.3
            // Define a function
            Python.Evaluate(@"
global add_some_numbers
def add_some_numbers():
    a = 3
    b = 5
    print(a + b)
");
            // Call a function
            Python.Evaluate("add_some_numbers()"); // 8
            // Return a value
            Console.WriteLine(Python.Evaluate("return 2").ReturnValue); // 2
            // Error
            Evaluator.EvaluationResult Result = Python.Evaluate("An Error");
            Console.WriteLine(Result.FormatError()); // Error on line 1: invalid syntax (<string>, line 2)
        }
    }
}