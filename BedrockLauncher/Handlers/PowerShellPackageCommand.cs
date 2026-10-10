using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BedrockLauncher.Handlers
{
    /// <summary>Runs the PowerShell package cmdlets (Add-/Remove-AppxPackage) shared by the UWP and GDK pipelines.</summary>
    internal static class PowerShellPackageCommand
    {
        internal static string Escape(string value)
        {
            return value?.Replace("'", "''") ?? string.Empty;
        }

        internal static async Task RunAsync(string command, string operationName)
        {
            await RunForOutputAsync(command, operationName);
        }

        /// <summary>Runs the command and returns its standard output. Throws when PowerShell exits with an error.</summary>
        internal static async Task<string> RunForOutputAsync(string command, string operationName)
        {
            string powershellPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe");

            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = powershellPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    ArgumentList =
                    {
                        "-NoProfile",
                        "-NonInteractive",
                        "-ExecutionPolicy",
                        "Bypass",
                        "-Command",
                        command
                    }
                }
            };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start PowerShell for {operationName}.");

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                string details = string.Join(Environment.NewLine, new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));
                throw new InvalidOperationException($"{operationName} failed with exit code {process.ExitCode}. {details}");
            }

            return output;
        }
    }
}
