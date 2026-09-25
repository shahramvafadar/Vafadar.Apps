// LocalizationService changes process-wide cultures, so tests in this assembly must not run in parallel.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
