using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace BedrockLauncher.Handlers
{
    internal static class NativeMsixvcExtractor
    {
        private const string CoreDllName = "launcher_core.dll";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private delegate int ExtractWideDelegate(
            [MarshalAs(UnmanagedType.LPWStr)] string msixvcPath,
            [MarshalAs(UnmanagedType.LPWStr)] string outputDirectory);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        private delegate int ExtractAnsiDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string msixvcPath,
            [MarshalAs(UnmanagedType.LPStr)] string outputDirectory);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        private delegate int ExtractWithPipeAnsiDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string msixvcPath,
            [MarshalAs(UnmanagedType.LPStr)] string outputDirectory,
            [MarshalAs(UnmanagedType.LPStr)] string pipeName);

        public static bool TryExtract(string msixvcPath, string outputDirectory, out string error)
        {
            return TryExtract(msixvcPath, outputDirectory, null, out error);
        }

        public static bool TryExtract(string msixvcPath, string outputDirectory, Action<long, long> progressCallback, out string error)
        {
            error = string.Empty;

            try
            {
                string nativeDirectory = GetNativeDirectory();
                if (!Directory.Exists(nativeDirectory))
                {
                    error = $"Native extractor directory was not found: {nativeDirectory}";
                    return false;
                }

                foreach (string dependency in new[] { "vcruntime140_1.dll", "libHttpClient.dll", "launcher_api.dll" })
                    TryLoadLibrary(Path.Combine(nativeDirectory, dependency));

                Environment.SetEnvironmentVariable("LAUNCHER_CORE_DLL", Path.Combine(nativeDirectory, CoreDllName));
                Environment.SetEnvironmentVariable("LAUNCHER_API_DLL", Path.Combine(nativeDirectory, "launcher_api.dll"));

                SetDllDirectory(nativeDirectory);
                try
                {
                    IntPtr core = LoadLibrary(Path.Combine(nativeDirectory, CoreDllName));
                    if (core == IntPtr.Zero)
                    {
                        error = $"Could not load {CoreDllName}. Win32 error: {Marshal.GetLastWin32Error()}";
                        return false;
                    }

                    Directory.CreateDirectory(outputDirectory);
                    string firstError = string.Empty;

                    IntPtr getWithPipe = GetProcAddress(core, "GetWithPipe");
                    if (getWithPipe != IntPtr.Zero)
                    {
                        if (TryExtractWithPipe(
                            getWithPipe,
                            Path.GetFullPath(msixvcPath),
                            Path.GetFullPath(outputDirectory),
                            progressCallback,
                            out firstError))
                        {
                            error = string.Empty;
                            return true;
                        }

                        Trace.WriteLine($"Native MSIXVC GetWithPipe failed, trying direct exports: {firstError}");
                    }

                    IntPtr getWide = GetProcAddress(core, "GetW");
                    if (getWide != IntPtr.Zero)
                    {
                        var extract = Marshal.GetDelegateForFunctionPointer<ExtractWideDelegate>(getWide);
                        int result = extract(Path.GetFullPath(msixvcPath), Path.GetFullPath(outputDirectory));
                        if (HandleResult(result, out error))
                            return true;

                        Trace.WriteLine($"Native MSIXVC GetW failed: {error}");
                    }

                    IntPtr getAnsi = GetProcAddress(core, "Get");
                    if (getAnsi != IntPtr.Zero)
                    {
                        var extract = Marshal.GetDelegateForFunctionPointer<ExtractAnsiDelegate>(getAnsi);
                        int result = extract(Path.GetFullPath(msixvcPath), Path.GetFullPath(outputDirectory));
                        if (HandleResult(result, out error))
                            return true;

                        if (!string.IsNullOrWhiteSpace(firstError) && !string.Equals(firstError, error, StringComparison.OrdinalIgnoreCase))
                            error = $"{firstError}; {error}";

                        return false;
                    }

                    error = string.IsNullOrWhiteSpace(firstError)
                        ? "Native extractor exports GetWithPipe/GetW/Get were not found."
                        : firstError;
                    return false;
                }
                finally
                {
                    SetDllDirectory(string.Empty);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Trace.WriteLine($"Native MSIXVC extraction failed: {ex}");
                return false;
            }
        }

        private static string GetNativeDirectory()
        {
            string outputNativeDirectory = Path.Combine(AppContext.BaseDirectory, "native", "launchercore");
            if (Directory.Exists(outputNativeDirectory))
                return outputNativeDirectory;

            string projectNativeDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "native", "launchercore");
            if (Directory.Exists(projectNativeDirectory))
                return projectNativeDirectory;

            return outputNativeDirectory;
        }

        private static bool TryExtractWithPipe(
            IntPtr getWithPipe,
            string msixvcPath,
            string outputDirectory,
            Action<long, long> progressCallback,
            out string error)
        {
            string pipeServerName = "bedrock_msixvc_progress_" + Guid.NewGuid().ToString("N");
            string pipeFullName = @"\\.\pipe\" + pipeServerName;

            using NamedPipeServerStream pipe = new NamedPipeServerStream(
                pipeServerName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            Task readerTask = ReadProgressPipeAsync(pipe, progressCallback);
            int result;

            try
            {
                var extract = Marshal.GetDelegateForFunctionPointer<ExtractWithPipeAnsiDelegate>(getWithPipe);
                result = extract(msixvcPath, outputDirectory, pipeFullName);
            }
            finally
            {
                try { pipe.Dispose(); }
                catch { }
            }

            try { readerTask.Wait(TimeSpan.FromSeconds(1)); }
            catch { }

            return HandleResult(result, out error);
        }

        private static async Task ReadProgressPipeAsync(NamedPipeServerStream pipe, Action<long, long> progressCallback)
        {
            try
            {
                await pipe.WaitForConnectionAsync().ConfigureAwait(false);
                using StreamReader reader = new StreamReader(pipe);

                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    TryReportProgress(line, progressCallback);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Native MSIXVC progress pipe failed: {ex}");
            }
        }

        private static void TryReportProgress(string jsonLine, Action<long, long> progressCallback)
        {
            if (progressCallback == null) return;

            try
            {
                using JsonDocument document = JsonDocument.Parse(jsonLine.Replace("\\", "/"));
                JsonElement root = document.RootElement;

                long current = ReadJsonInt64(root, "global_current");
                long total = ReadJsonInt64(root, "global_total");

                if (current <= 0 || total <= 0)
                {
                    current = ReadJsonInt64(root, "current");
                    total = ReadJsonInt64(root, "total");
                }

                if (total > 0)
                    progressCallback(current, total);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to parse native MSIXVC progress: {ex.Message}");
            }
        }

        private static long ReadJsonInt64(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out JsonElement value) && value.TryGetInt64(out long result)
                ? result
                : 0;
        }

        private static void TryLoadLibrary(string path)
        {
            if (!File.Exists(path)) return;

            IntPtr handle = LoadLibrary(path);
            if (handle == IntPtr.Zero)
                Trace.WriteLine($"Could not preload native dependency {Path.GetFileName(path)}: {Marshal.GetLastWin32Error()}");
        }

        private static bool HandleResult(int result, out string error)
        {
            error = result switch
            {
                0 => string.Empty,
                1 => "NH_ERR_EXCEPTION",
                2 => "NH_ERR_INVALID_PARAMS",
                3 => "NH_ERR_KEY_NOT_FOUND",
                4 => "NH_ERR_UNAUTHORIZED_CALLER",
                5 => "NH_ERR_PIPE_OPEN_FAILED",
                6 => "NH_ERR_INPUT_NOT_FOUND",
                7 => "NH_ERR_OUTPUT_DIR_INVALID",
                8 => "NH_ERR_PARSE_FAILED",
                9 => "NH_ERR_EXTRACT_FAILED",
                _ => $"ERR_UNKNOWN_{result}"
            };

            return result == 0;
        }
    }
}
