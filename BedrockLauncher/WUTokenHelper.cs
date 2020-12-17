using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCLauncher {
    class WUTokenHelper {

        public static string GetWUToken() {
            try {
                string token;
                int status = GetWUToken(out token);
                if (status >= WU_ERRORS_START && status <= WU_ERRORS_END)
                    throw new WUTokenException(status);
                else if (status != 0)
                    Marshal.ThrowExceptionForHR(status);
                return token;
            } catch (SEHException e) {
                Marshal.ThrowExceptionForHR(e.HResult);
                return ""; //ghey
            }
        }

        private const int WU_ERRORS_START = 0x7ffc0200;
        private const int WU_NO_ACCOUNT = 0x7ffc0200;
        private const int WU_ERRORS_END = 0x7ffc0200;

        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetWUToken([MarshalAs(UnmanagedType.LPWStr)] out string token);

        public class WUTokenException : Exception {
            public WUTokenException(int exception) : base(GetExceptionText(exception)) {
                HResult = exception;
            }
            private static String GetExceptionText(int e) {
                switch (e) {
                    case WU_NO_ACCOUNT: return "No account";
                    default: return "Unknown " + e;
                }
            }
        }

    }
}
