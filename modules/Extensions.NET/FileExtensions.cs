using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace JemExtensions
{
    public static class FileExtensions
    {

        public static string GetAvaliableFileName(string fileName, string directory, string format = "_{0}")
        {
            int i = 0;

            string newFileName = fileName;

            if (!File.Exists(Path.Combine(directory, newFileName))) return newFileName;
            else while (File.Exists(Path.Combine(directory, newFileName)))
                {
                    i++;
                    newFileName = Path.GetFileNameWithoutExtension(fileName) + string.Format(format, i) + "." + Path.GetExtension(fileName);
                }

            return string.Empty;
        }

        public static void CopyTo(this DirectoryInfo source, DirectoryInfo target)
        {
            if (!target.Exists)
                target.Create();

            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);

            foreach (var subdir in source.GetDirectories())
                subdir.CopyTo(target.CreateSubdirectory(subdir.Name));
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 0x1000)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            long totalRead = 0;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                totalRead += bytesRead;
                Thread.Sleep(10);
                progress.Report(totalRead);
            }
        }



        public static bool IsFileInUse(string fileFullPath, bool throwIfNotExists)
        {
            if (File.Exists(fileFullPath))
            {
                try
                {
                    //if this does not throw exception then the file is not use by another program
                    using (FileStream fileStream = File.OpenWrite(fileFullPath))
                    {
                        if (fileStream == null)
                            return true;
                    }
                    return false;
                }
                catch
                {
                    return true;
                }
            }
            else if (!throwIfNotExists)
            {
                return true;
            }
            else
            {
                throw new FileNotFoundException("Specified path is not exists", fileFullPath);
            }
        }

        #region File Locking

        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public FILETIME ProcessStartTime;
        }

        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
                                              UInt32 nFiles,
                                              string[] rgsFilenames,
                                              UInt32 nApplications,
                                              [In] RM_UNIQUE_PROCESS[] rgApplications,
                                              UInt32 nServices,
                                              string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle,
                                    out uint pnProcInfoNeeded,
                                    ref uint pnProcInfo,
                                    [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
                                    ref uint lpdwRebootReasons);

        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
        /// 
        /// </remarks>
        static public List<Process> WhoIsLocking(string path)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            List<Process> processes = new List<Process>();

            int res = RmStartSession(out handle, 0, key);
            if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0,
                     pnProcInfo = 0,
                     lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = new string[] { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException) { }
                        }
                    }
                    else throw new Exception("Could not list processes locking resource.");
                }
                else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }

        static public List<Process> TryWhoIsLocking(string path)
        {
            try
            {
                return WhoIsLocking(path);
            }
            catch
            {
                return new List<Process>();
            }
        }

        #endregion
    }

    public static class FileSystemInfoExtensions
    {
        public static void Rename(this FileSystemInfo item, string newName)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            FileInfo fileInfo = item as FileInfo;
            if (fileInfo != null)
            {
                fileInfo.Rename(newName);
                return;
            }

            DirectoryInfo directoryInfo = item as DirectoryInfo;
            if (directoryInfo != null)
            {
                directoryInfo.Rename(newName);
                return;
            }

            throw new ArgumentException("Item", "Unexpected subclass of FileSystemInfo " + item.GetType());
        }

        public static void Rename(this FileInfo file, string newName)
        {
            // Validate arguments.
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (newName == null)
            {
                throw new ArgumentNullException("newName");
            }
            else if (newName.Length == 0)
            {
                throw new ArgumentException("The name is empty.", "newName");
            }
            else if (newName.IndexOf(Path.DirectorySeparatorChar) >= 0
                || newName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                throw new ArgumentException("The name contains path separators. The file would be moved.", "newName");
            }

            // Rename file.
            string newPath = Path.Combine(file.DirectoryName, newName);
            file.MoveTo(newPath);
        }

        public static void Rename(this DirectoryInfo directory, string newName)
        {
            // Validate arguments.
            if (directory == null)
            {
                throw new ArgumentNullException("directory");
            }
            else if (newName == null)
            {
                throw new ArgumentNullException("newName");
            }
            else if (newName.Length == 0)
            {
                throw new ArgumentException("The name is empty.", "newName");
            }
            else if (newName.IndexOf(Path.DirectorySeparatorChar) >= 0
                || newName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                throw new ArgumentException("The name contains path separators. The directory would be moved.", "newName");
            }

            // Rename directory.
            string newPath = Path.Combine(directory.Parent.FullName, newName);
            directory.MoveTo(newPath);
        }
    }
}
