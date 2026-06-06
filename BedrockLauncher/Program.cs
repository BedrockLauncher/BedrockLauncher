using BedrockLauncher.Downloaders;
using BedrockLauncher.Handlers;
using BedrockLauncher.Localization.Language;
using BedrockLauncher.ViewModels;
using JemExtensions;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace BedrockLauncher
{
    public static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static ConsoleWindow _cli;
        public static ConsoleWindow cli => _cli ??= new ConsoleWindow();
        private const string MsixvcExtractorHelperArgument = "--bedrock-msixvc-extract";
        private const string StoreEngagementPackageFamilyName = "Microsoft.Services.Store.Engagement_8wekyb3d8bbwe";
        private const string StoreEngagementMinimumVersion = "10.0.19011.0";
        private const string StoreEngagementAppxUrl = "https://github.com/RaythCo-Creations/downloads/raw/main/Microsoft.Services.Store.Engagement_10.0.19011.0_x64__8wekyb3d8bbwe.Appx";

        [STAThread]
        public static int Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            if (TryRunMsixvcExtractorHelper(args, out int helperExitCode))
                return helperExitCode;

            RuntimeHandler.StartLogging();
            RuntimeHandler.LogStartupInformation();
            RuntimeHandler.ValidateOSArchitecture();
            Trace.WriteLine("Application Starting...");
            var application = new App();
            application.Startup += OnApplicationInitalizing;
            application.InitializeComponent();
            return application.Run();
        }

        private static bool TryRunMsixvcExtractorHelper(string[] args, out int exitCode)
        {
            exitCode = 0;
            if (args == null || args.Length == 0 || !string.Equals(args[0], MsixvcExtractorHelperArgument, StringComparison.OrdinalIgnoreCase))
                return false;

            string errorFilePath = args.Length > 3 ? args[3] : null;

            try
            {
                if (args.Length < 4)
                {
                    WriteMsixvcHelperError(errorFilePath, "Missing MSIXVC helper arguments.");
                    exitCode = 2;
                    return true;
                }

                string packagePath = args[1];
                string outputDirectory = args[2];

                bool extracted = NativeMsixvcExtractor.TryExtract(packagePath, outputDirectory, (current, total) =>
                {
                    Console.WriteLine($"PROGRESS {current} {total}");
                }, out string error);

                if (!extracted)
                {
                    WriteMsixvcHelperError(errorFilePath, error);
                    exitCode = 3;
                    return true;
                }

                exitCode = 0;
                return true;
            }
            catch (Exception ex)
            {
                WriteMsixvcHelperError(errorFilePath, ex.Message);
                exitCode = 4;
                return true;
            }
        }

        private static void WriteMsixvcHelperError(string errorFilePath, string message)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(errorFilePath))
                {
                    string directory = Path.GetDirectoryName(errorFilePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(errorFilePath, message ?? string.Empty);
                }
            }
            catch
            {
            }

            try
            {
                Console.Error.WriteLine(message ?? string.Empty);
            }
            catch
            {
            }
        }

        public static void OnApplicationInitalizing(object sender, StartupEventArgs e)
        {
            Trace.WriteLine("Application Initalization Started!");
            StartupArgsHandler.SetStartupArgs(e.Args);
            StartupArgsHandler.RunPreStartupArgs();
            Trace.WriteLine("Application Initalization Finished!");
        }
        public static async Task OnApplicationLoaded()
        {
            await MainViewModel.Default.ShowWaitingDialog(async () =>
            {
                Trace.WriteLine("Preparing Application...");
                LanguageManager.Init();
                MainDataModel.Default.LoadConfig();
                MainDataModel.Default.Config.EnsurePlayableInstallationSelected(forceSync: true);
                MainDataModel.Default.ProgressBarState.PlayButtonLanguageChanged = !MainDataModel.Default.ProgressBarState.PlayButtonLanguageChanged;
                Trace.WriteLine("Preparing Application: DONE");
                await Task.CompletedTask;
            });

            StartBackgroundStartupRefresh();
        }

        private static void StartBackgroundStartupRefresh()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await MainDataModel.Default.LoadVersions(true);
                    MainDataModel.Default.ProgressBarState.PlayButtonLanguageChanged = !MainDataModel.Default.ProgressBarState.PlayButtonLanguageChanged;

                    await RuntimeHandler.InitalizeBugRockOfTheWeek();

                    if (await MainDataModel.Updater.CheckForUpdatesAsync(true))
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MainViewModel.Default.UpdateButton.ShowUpdateButton();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Background startup refresh failed:");
                    Trace.WriteLine(ex);
                }
            });
        }

        public static async Task OnApplicationRefresh()
        {

            await MainViewModel.Default.ShowWaitingDialog(async () =>
            {
                Trace.WriteLine("Refreshing Application...");
                MainDataModel.Default.LoadConfig();
                await MainDataModel.Default.LoadVersions();
                Trace.WriteLine("Refreshing Application: DONE");
            });
        }

        public static bool CheckForVCRuntime()
        {
            return EnsureVCRuntimeAsync().GetAwaiter().GetResult();
        }

        private static async Task<bool> EnsureVCRuntimeAsync()
        {
            Trace.WriteLine("Checking VC++ Runtime version");
            const string minimumVersionS = "14.14.26405.0";
            string runtimeArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";

            if (IsVCRuntimeInstalled(runtimeArch, minimumVersionS))
            {
                Trace.WriteLine("VC++ Runtime OK");
                return true;
            }

            Trace.WriteLine($"VC++ Runtime {minimumVersionS} or higher not found. Installing {runtimeArch} redistributable.");
            System.Windows.Forms.MessageBox.Show(
                "Microsoft Visual C++ Redistributable is missing. BedrockLauncher will download and install the official Microsoft runtime now.",
                "BedrockLauncher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            try
            {
                string installerPath = await DownloadVCRedistInstaller(runtimeArch);
                int exitCode = RunVCRedistInstaller(installerPath);

                if (exitCode == 0 || exitCode == 3010 || exitCode == 1638 || IsVCRuntimeInstalled(runtimeArch, minimumVersionS))
                {
                    Trace.WriteLine($"VC++ Runtime installer completed with exit code {exitCode}.");
                    return true;
                }

                Trace.WriteLine($"VC++ Runtime installer failed with exit code {exitCode}.");
                System.Windows.Forms.MessageBox.Show(
                    $"The Visual C++ Redistributable installer finished with exit code {exitCode}. Please try again or install it manually from Microsoft.",
                    "BedrockLauncher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"VC++ Runtime automatic installation failed: {ex}");
                System.Windows.Forms.MessageBox.Show(
                    "BedrockLauncher could not install the Visual C++ Redistributable automatically. Please check your internet connection and try again.",
                    "BedrockLauncher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }

        private static bool IsVCRuntimeInstalled(string architecture, string minimumVersionS)
        {
            Version minimumVersion = new Version(minimumVersionS);
            RegistryView[] registryViews = Environment.Is64BitOperatingSystem
                ? new[] { RegistryView.Registry64, RegistryView.Registry32 }
                : new[] { RegistryView.Registry32 };

            foreach (RegistryView registryView in registryViews)
            {
                try
                {
                    using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
                    using RegistryKey key = baseKey.OpenSubKey($@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\{architecture}");
                    if (key == null) continue;

                    object installed = key.GetValue("Installed");
                    if (installed is int installedValue && installedValue != 1) continue;

                    object versionValue = key.GetValue("Version");
                    if (versionValue == null) continue;

                    Version currentVersion = new Version(versionValue.ToString().Replace("v", ""));
                    if (currentVersion.CompareTo(minimumVersion) >= 0) return true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Could not read VC++ Runtime registry state from {registryView}: {ex.Message}");
                }
            }

            return false;
        }

        private static async Task<string> DownloadVCRedistInstaller(string architecture)
        {
            string url = architecture == "x64"
                ? "https://aka.ms/vs/17/release/vc_redist.x64.exe"
                : "https://aka.ms/vs/17/release/vc_redist.x86.exe";
            string installerPath = Path.Combine(Path.GetTempPath(), $"BedrockLauncher-vc_redist.{architecture}.exe");

            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            byte[] installerBytes = await httpClient.GetByteArrayAsync(url);
            if (installerBytes.Length < 1024 * 1024) throw new InvalidDataException("Downloaded VC++ Redistributable installer is too small.");

            File.WriteAllBytes(installerPath, installerBytes);
            return installerPath;
        }

        private static int RunVCRedistInstaller(string installerPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(installerPath, "/install /passive /norestart")
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            using Process process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode;
        }

        public static bool EnsureStoreEngagementPackage()
        {
            return EnsureStoreEngagementPackageAsync().GetAwaiter().GetResult();
        }

        private static async Task<bool> EnsureStoreEngagementPackageAsync()
        {
            Trace.WriteLine("Checking Microsoft Services Store Engagement package");

            if (IsStoreEngagementInstalled())
            {
                Trace.WriteLine("Microsoft Services Store Engagement package OK");
                return true;
            }

            if (!Environment.Is64BitOperatingSystem)
            {
                Trace.WriteLine("Microsoft Services Store Engagement x64 package cannot be installed on a non-x64 OS.");
                System.Windows.Forms.MessageBox.Show(
                    "Microsoft Services Store Engagement is missing, but the bundled dependency package is x64-only. Please install the correct dependency package for this PC.",
                    "BedrockLauncher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            System.Windows.Forms.MessageBox.Show(
                "Microsoft Services Store Engagement is missing. BedrockLauncher will download and install the dependency package now.",
                "BedrockLauncher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            try
            {
                string packagePath = await DownloadStoreEngagementPackage();
                await InstallStoreEngagementPackage(packagePath);

                if (IsStoreEngagementInstalled())
                {
                    Trace.WriteLine("Microsoft Services Store Engagement package installed successfully");
                    return true;
                }

                Trace.WriteLine("Microsoft Services Store Engagement install completed but package was not detected afterwards.");
                System.Windows.Forms.MessageBox.Show(
                    "Microsoft Services Store Engagement could not be detected after installation. Please try running BedrockLauncher again.",
                    "BedrockLauncher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Microsoft Services Store Engagement automatic installation failed: {ex}");
                System.Windows.Forms.MessageBox.Show(
                    "BedrockLauncher could not install Microsoft Services Store Engagement automatically. Please check your internet connection and try again.",
                    "BedrockLauncher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }

        private static bool IsStoreEngagementInstalled()
        {
            Version minimumVersion = new Version(StoreEngagementMinimumVersion);
            PackageManager packageManager = new PackageManager();

            try
            {
                foreach (var package in packageManager.FindPackagesForUser(string.Empty, StoreEngagementPackageFamilyName))
                {
                    var packageVersion = package.Id.Version;
                    Version currentVersion = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
                    if (currentVersion.CompareTo(minimumVersion) >= 0) return true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Could not check Microsoft Services Store Engagement package state: {ex.Message}");
            }

            return false;
        }

        private static async Task<string> DownloadStoreEngagementPackage()
        {
            string packagePath = Path.Combine(Path.GetTempPath(), "Microsoft.Services.Store.Engagement_10.0.19011.0_x64__8wekyb3d8bbwe.Appx");

            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            byte[] packageBytes = await httpClient.GetByteArrayAsync(StoreEngagementAppxUrl);
            if (packageBytes.Length < 32 * 1024) throw new InvalidDataException("Downloaded Store Engagement package is too small.");

            File.WriteAllBytes(packagePath, packageBytes);
            return packagePath;
        }

        private static async Task InstallStoreEngagementPackage(string packagePath)
        {
            PackageManager packageManager = new PackageManager();
            DeploymentResult result = await AwaitDeployment(packageManager.AddPackageAsync(new Uri(packagePath), null, DeploymentOptions.None));

            if (!string.IsNullOrWhiteSpace(result.ErrorText))
            {
                Trace.WriteLine("Microsoft Services Store Engagement deployment message: " + result.ErrorText);
            }
        }

        private static Task<DeploymentResult> AwaitDeployment(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deployment)
        {
            TaskCompletionSource<DeploymentResult> completionSource = new TaskCompletionSource<DeploymentResult>();

            deployment.Completed = (operation, status) =>
            {
                try
                {
                    if (status == AsyncStatus.Completed)
                    {
                        completionSource.TrySetResult(operation.GetResults());
                    }
                    else if (status == AsyncStatus.Canceled)
                    {
                        completionSource.TrySetCanceled();
                    }
                    else
                    {
                        completionSource.TrySetException(operation.ErrorCode ?? new InvalidOperationException("Package deployment failed."));
                    }
                }
                catch (Exception ex)
                {
                    completionSource.TrySetException(ex);
                }
            };

            return completionSource.Task;
        }

        public static bool CheckForWindowsVersion()
        {
            Trace.WriteLine("Checking Windows Version");
            Thread.Sleep(500);
            bool result = false;
            string minimumVersionS = "10.0.19041.0";

            try
            {
                Version currentVersion = Environment.OSVersion.Version;
                Version minimumVersion = new Version(minimumVersionS);
                if (currentVersion.CompareTo(minimumVersion) >= 0) result = true;
            }
            catch (Exception) { }

            if (!result)
            {
                Trace.WriteLine("This application only works on Windows version " + minimumVersionS + " or above!");
                System.Windows.Forms.MessageBox.Show("This application only works on Windows version " + minimumVersionS + " or above!", "Error");
            }
            else
            {
                Trace.WriteLine("Windows Version OK");
            }
                return result;
        }
    }
}
