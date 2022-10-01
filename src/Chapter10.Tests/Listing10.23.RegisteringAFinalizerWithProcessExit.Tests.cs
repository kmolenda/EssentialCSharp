using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using AddisonWesley.Michaelis.EssentialCSharp.Shared.Tests;

using System.Text.RegularExpressions;

namespace AddisonWesley.Michaelis.EssentialCSharp.Chapter10.Listing10_23.Tests
{
    [TestClass]
    public class DisposeTests
    {
        public TestContext TestContext { get; set; } = null!; // Auto-initialized by MSTest.

        static string Ps1DirectoryPath { get; } =
            Path.GetFullPath(Path.Join("..", "..", "..", "..", "Chapter10"), Environment.CurrentDirectory);
        static string Ps1Path { get; } = Path.GetFullPath($"{Ps1DirectoryPath}/Listing10.23.RegisteringAFinalizerWithProcessExit.ps1", Environment.CurrentDirectory);

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            string testStage = "create";
            Assert.AreEqual<int>(0, RunPowerShellScript(testStage, out string psOutput),
                $"Script failed with $testStage='{testStage}'. psOutput:\n{psOutput}");
            string projectFilePath =
                Path.Join(Ps1DirectoryPath, "ProcessExitTestProgram", "ProcessExitTestProgram.csproj");
            Assert.IsTrue(File.Exists(projectFilePath),
                $"The expected project file, '{projectFilePath}', was not created.");
        }

        private static int RunPowerShellScript(string testStage, out string psOutput) =>
            RunPowerShellScript(testStage, null, 0, out psOutput);
        private static int RunPowerShellScript(
            string testStage, string? finalizerOrderOption, int traceLevel, out string psOutput) => PowerShellTestUtilities.RunPowerShellScript(
                            Ps1Path, $"-TestStage {testStage} -FinalizerOption {finalizerOrderOption ?? "ignore"} {traceLevel}", out psOutput);

        [ClassCleanup]
        public static void RemoveProcessExitProj()
        {
            Assert.AreEqual<int>(0, RunPowerShellScript(
                "cleanup", out string _));
        }

        [DataTestMethod]
        [DataRow("processExit", FinalizerRegisteredWithProcessExit, DisplayName = "Finalizer Registered With ProcessExit")]
        [DataRow("dispose", DisposeManuallyCalledExpectedOutput, DisplayName = "Dispose called before ProcessExit does finalizer")]
        [DataRow("gc", GCCalled, DisplayName = "Garbage Collected called")]
        public void FinalizerRunsAsPredicted_ConsoleOutputIsInOrder(string finalizerOrderOption, string expectedOutput)
        {
            int traceValue = 0;
            string testStatus = "run";

            TestContext.WriteLine($"Ps1Path = '{Path.GetFullPath(Ps1Path)}'");
            string psOutput;
            int exitCode = RunPowerShellScript(
                testStatus, finalizerOrderOption, traceValue, out psOutput);

            Assert.AreEqual(0, exitCode, $"PowerShell Output: {psOutput}");

            Assert.AreEqual<string>(RemoveWhiteSpace(expectedOutput), RemoveWhiteSpace(psOutput),
                $"Unexpected output from '{Ps1Path} {traceValue} {finalizerOrderOption} {testStatus}:{Environment.NewLine}{psOutput}");
        }

        private static int RunPowerShellScript(string testStage, out string psOutput) =>
            RunPowerShellScript(testStage, null, 0, out psOutput);
        private static int RunPowerShellScript(
            string testStage, string? finalizerOrderOption, int traceLevel, out string psOutput) => PowerShellTestUtilities.RunPowerShellScript(
                            Ps1Path, $"-TestStage {testStage} -FinalizerOption {finalizerOrderOption??"ignore"} {traceLevel}", out psOutput);

        public const string DisposeManuallyCalledExpectedOutput =
            @"Main: Starting...
            DoStuff: Starting...
            SampleUnmanagedResource.ctor: Starting...
            SampleUnmanagedResource.ctor: Creating managed stuff...
            SampleUnmanagedResource.ctor: Creating unmanaged stuff...
            SampleUnmanagedResource.ctor: Exiting...
            Dispose: Starting...
            Dispose: Disposing managed stuff...
            Dispose: Disposing unmanaged stuff...
            Dispose: Exiting...
            DoStuff: Exiting...
            Main: Exiting...";

        public const string FinalizerRegisteredWithProcessExit =
            @"Main: Starting...
            DoStuff: Starting...
            SampleUnmanagedResource.ctor: Starting...
            SampleUnmanagedResource.ctor: Creating managed stuff...
            SampleUnmanagedResource.ctor: Creating unmanaged stuff...
            SampleUnmanagedResource.ctor: Exiting...
            DoStuff: Exiting...
            Main: Exiting...
            ProcessExitHandler: Starting...
            Dispose: Starting...
            Dispose: Disposing managed stuff...
            Dispose: Disposing unmanaged stuff...
            Dispose: Exiting...
            ProcessExitHandler: Exiting...";

        public const string GCCalled =
            @"Main: Starting...
            DoStuff: Starting...
            SampleUnmanagedResource.ctor: Starting...
            SampleUnmanagedResource.ctor: Creating managed stuff...
            SampleUnmanagedResource.ctor: Creating unmanaged stuff...
            SampleUnmanagedResource.ctor: Exiting...
            DoStuff: Exiting...
            Finalize: Starting...
            Dispose: Starting...
            Dispose: Disposing unmanaged stuff...
            Dispose: Exiting...
            Finalize: Exiting...
            Main: Exiting...";

        public static string RemoveWhiteSpace(string str)
        {
            return Regex.Replace(str, @"\s+", String.Empty);
        }

    }
}
