# Gemsnake

![Image](Resources/Gemsnake.png)

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

## Objectives
- Gemsnake is designed as a replacement for IronRuby and IronPython, which have been long discontinued.
- It's designed to help you run Ruby & Python code while creating an interface in C#.

## How it works
- Gemsnake works by creating an executable ahead of time using [Ocran](https://github.com/Midscore-IO/ocran) and [PyInstaller](https://github.com/pyinstaller/pyinstaller) for Ruby and Python respectively.
- If you want to install gems/packages, or upgrade the language version, just run one of the Build scripts and input which gems/packages to include, and they will build new executables.
- These executables can run code from a string using Ruby's eval() and Python's exec(). As Python's exec() does not return a value, the code input is wrapped into a function and called.
- The C# program will start these executables as processes running in the background. They communicate over a localhost TCP connection, and you can specify over which port if you desire.