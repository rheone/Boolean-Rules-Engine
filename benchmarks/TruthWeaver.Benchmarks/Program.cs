using System.Reflection;
using BenchmarkDotNet.Running;

// Always pass --inProcess when running this suite: BenchmarkDotNet's default toolchain generates and
// builds a throwaway project per benchmark case, validating its target runtime against BenchmarkDotNet's
// own RuntimeMoniker table first - a table that does not yet recognize this repo's net11.0 preview
// target framework (global.json) and throws NotImplementedException before a single benchmark runs.
// --inProcess (InProcessEmitToolchain) runs every case in this process instead, skipping that
// generated-project/SDK-resolution step entirely. The tradeoff (no process isolation between cases) is
// acceptable here: every benchmark in this suite is a short, allocation-light, single-threaded call with
// no static state that would leak between cases.
_ = await BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).RunAsync(args);
