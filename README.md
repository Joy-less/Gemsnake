# Gemsnake

![Image](Resources/GemsnakeMini.png)

Are you coding in C#, but want the flexibility of Ruby or the libraries of Python? Gemsnake lets you have all three from the comfort of C#, without needing to install Python or Ruby.

The best part is it's as easy as:

```csharp
RubyEvaluator Ruby = new();
Ruby.Evaluate("puts('Hello from Ruby!')");

PythonEvaluator Python = new();
Python.Evaluate("print('Hello from Python!')");
```

Gemsnake also lets you return values:

```csharp
string Greeting = Ruby.Evaluate("return 'Hi!'").ReturnValue;
```

You can also run code asynchronously:

```csharp
await Python.EvaluateAsync("print('running in the background')");
```

## Setup for Visual Studio
1. Download `Gemsnake.zip` from the latest release.
2. Extract the zip and move `Evaluator.cs`, `RubyEvaluator.cs` and `PythonEvaluator.cs` into your project files.
3. Move `RubyEvaluator.exe` and `PythonEvaluator.exe` into your binary output folder.

## Setup for Unity
1. Download `Gemsnake.zip` from the latest release.
2. Extract the zip and move `Evaluator.cs`, `RubyEvaluator.cs` and `PythonEvaluator.cs` into your Assets folder.
3. Create a folder called `StreamingAssets` inside in your Assets folder and move `RubyEvaluator.exe` and `PythonEvaluator.exe` into it.
4. Install the Newtonsoft.Json package via the Package Manager (add package by name -> com.unity.nuget.newtonsoft-json). [(Help)](https://github.com/jilleJr/Newtonsoft.Json-for-Unity/wiki/Install-official-via-UPM#installing-the-package-via-upm-window)
5. Comment out (add "// " before) the first line of `Evaluator.cs`, as Unity doesn't support dynamic data types.
6. Create a script that looks like this:
```csharp
using UnityEngine;
using Gemsnake;

public class NewBehaviourScript : MonoBehaviour
{
    RubyEvaluator Ruby;

    void Start() {
        RunCodeInRuby();
    }

    async void RunCodeInRuby() {
        Ruby = new RubyEvaluator(RubyExecutablePath: Application.streamingAssetsPath + "/RubyEvaluator.exe");
        object Result = (await Ruby.EvaluateAsync("return 2 + 3")).ReturnValue;
        Debug.Log(Result.ToString());
    }

    void OnApplicationQuit() {
        Ruby.Stop();
    }
}
```
7. Playtest your game, and after a few seconds, 5 should be printed to the console.

_Notes:_
- _Make sure to use EvaluateAsync rather than Evaluate, as otherwise Unity will freeze._
- _If you don't stop the evaluator on OnApplicationQuit, it will continue running in the background after the playtest ends._

## Using gems/packages
If you want to use gems/packages, or upgrade the language version:
- Download `Gemsnake Build Tools.zip` and run the build scripts.
- Input which gems/packages to include and new executables will be built.

## Objectives
- Gemsnake is designed as a replacement for IronRuby and IronPython, which have been long discontinued.
- It's designed to help you run Ruby & Python code while creating an interface in C#.
- It targets .NET 7.0.

## How it works
- Gemsnake works by creating an executable ahead of time using [Ocran](https://github.com/Midscore-IO/ocran) and [PyInstaller](https://github.com/pyinstaller/pyinstaller) for Ruby and Python respectively.
- These executables can run code from a string using Ruby's `eval()` and Python's `exec()`. As Python's `exec()` does not return a value, the code input is wrapped into a function and called.
- The C# program will start these executables as processes running in the background. They communicate over a localhost TCP connection, and you can specify over which port if you desire.

## Limitations
- As PyInstaller and Ocran create executables, this will only work on Windows. (If you run `Build PythonEvaluator.py` from Linux or macOS, you may be able to get Python to work on those platforms.)
- The bigger the executables are, the longer they take to start (only really an issue if you are using custom gems/packages).
- Two applications running on the same ports on the same computer will conflict with each other.
