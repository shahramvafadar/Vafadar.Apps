// Native digit formatting is shared process state; application flows must not race one another's display choices.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
