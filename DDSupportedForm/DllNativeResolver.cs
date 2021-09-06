using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

internal class DllNativeResolver
{
    [DllImport("kernel32")]
    private static extern IntPtr LoadLibrary(string lpFileName);

    const string dllName = "libwebp.dll";

    public DllNativeResolver()
    {
        var selfdir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

         var dllPath = Path.Combine(selfdir, dllName);
        // System.Diagnostics.Trace.WriteLine(dllPath);

        if (!(File.Exists(dllPath) && LoadLibrary(dllPath) != IntPtr.Zero))
        {
            System.Diagnostics.Trace.WriteLine(dllName + " の読み込みに失敗しました");
            throw new DllNotFoundException(dllPath);
        }
    }
}