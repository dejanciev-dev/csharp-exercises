using BenchmarkDotNet.Running;

// Lets you pick benchmarks interactively, or pass --filter / --job on the command line. See the README.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
