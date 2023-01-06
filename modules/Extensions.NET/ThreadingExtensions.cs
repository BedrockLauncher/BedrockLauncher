using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace JemExtensions
{
    public static class ThreadingExtensions
    {
        [SupportedOSPlatform("Windows")]
        public static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
        [SupportedOSPlatform("Windows")]
        public static Task StartSTATask(Task task)
        {
            var tcs = new TaskCompletionSource<int>();
            var thread = new Thread(() =>
            {
                try
                {
                    task.Start();
                    task.Wait();
                    tcs.SetResult(1);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
        [SupportedOSPlatform("Windows")]
        public static Task StartSTATask(Action action)
        {
            var tcs = new TaskCompletionSource<int>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(1);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
