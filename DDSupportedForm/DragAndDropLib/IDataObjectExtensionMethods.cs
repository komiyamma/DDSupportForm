using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;

/// <summary>Helper methods for getting FileContents from DragDrop data.</summary>
/// <see cref="!:https://www.codeproject.com/Articles/28209/Outlook-Drag-and-Drop-in-C"/>
internal static class IDataObjectExtensionMethods
{
    /// <summary>Gets the array of FileNames from the FileGroupDescriptors format.</summary>
    public static string[] GetFileContentNames(this System.Windows.Forms.IDataObject data)
    {
        var names = new string[data.GetFileContentCount()];

        if (names.Length != 0)
        {
            var bytes = data.GetFileGroupDescriptor().ToArray();
            IntPtr fgdPtr = IntPtr.Zero;
            try
            {
                fgdPtr = Marshal.AllocHGlobal(bytes.Length);

                int offset = Marshal.SizeOf(typeof(UInt32));
                int size = Marshal.SizeOf(typeof(FILEDESCRIPTORW));

                for (int i = 0; i < names.Length; i++)
                {
                    var fd = (FILEDESCRIPTORW)Marshal.PtrToStructure(fgdPtr + offset + (i * size), typeof(FILEDESCRIPTORW));
                    names[i] = fd.cFileName;
                }

            }
            finally
            {
                if (fgdPtr != IntPtr.Zero) Marshal.FreeHGlobal(fgdPtr);

            }
        }

        return names;
    }

    /// <summary>Gets the number of files available in the FileGroupDescriptor format.</summary>
    public static int GetFileContentCount(this System.Windows.Forms.IDataObject data)
    {
        // File count is stored as an UInt32 in the FileGroupDescriptor format
        MemoryStream ms = data.GetFileGroupDescriptor();
        if (ms == null) return 0;

        using (var reader = new BinaryReader(ms))
        {
            return (int)reader.ReadUInt32(); // Assume this won't overflow!
        }
    }

    /// <summary>Gets the file content for the specified FileDescriptor index.</summary>
    /// <param name="index">The index of the file content to retrieve.</param>
    public static MemoryStream GetFileContent(this System.Windows.Forms.IDataObject data, int index)
    {
        // As this is indexed, "FileContent" is most likely null, so the COM IDataObject needs to be used
        var comData = (System.Runtime.InteropServices.ComTypes.IDataObject)data;

        var formatetc = new FORMATETC()
        {
            cfFormat = (short)DataFormats.GetFormat("FileContents").Id,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = index,
            ptd = IntPtr.Zero,
            tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL
        };

        var medium = new STGMEDIUM();
        comData.GetData(ref formatetc, out medium);

        switch (medium.tymed)
        {
            case TYMED.TYMED_HGLOBAL:
                return data.GetFileContentFromHGlobal(medium);

            case TYMED.TYMED_ISTREAM:
                return data.GetFileContentFromIStream(medium);

            default:
                throw new InvalidOperationException($"Cannot get FileContent for {medium.tymed} TYMED.");

        }
    }

    private static MemoryStream GetFileContentFromHGlobal(this System.Windows.Forms.IDataObject data, STGMEDIUM medium)
    {
        var innerDataField = data.GetType().GetField("innerData", BindingFlags.NonPublic | BindingFlags.Instance);
        var oldData = (System.Windows.Forms.IDataObject)innerDataField.GetValue(data);


        var getDataFromHGLOBLALMethod = oldData.GetType().GetMethod("GetDataFromHGLOBLAL", BindingFlags.NonPublic | BindingFlags.Instance);

        return (MemoryStream)getDataFromHGLOBLALMethod.Invoke(oldData, new object[] { "FileContents", medium.unionmember });
    }

    private static MemoryStream GetFileContentFromIStream(this System.Windows.Forms.IDataObject data, STGMEDIUM medium)
    {
        var iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
        Marshal.Release(medium.unionmember);

        var iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
        iStream.Stat(out iStreamStat, 0);

        var content = new byte[(int)iStreamStat.cbSize];
        iStream.Read(content, content.Length, IntPtr.Zero);

        return new MemoryStream(content);
    }

    private static MemoryStream GetFileGroupDescriptor(this System.Windows.Forms.IDataObject data)
    {
        MemoryStream ms = null;
        if (data.GetDataPresent("FileGroupDescriptorW"))
        {
            ms = (MemoryStream)data.GetData("FileGroupDescriptorW", true);
        }

        return ms;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FILEDESCRIPTORW
    {
        public UInt32 dwFlags;
        public Guid clsid;
        public System.Drawing.Size sizel;
        public System.Drawing.Point pointl;
        public UInt32 dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public UInt32 nFileSizeHigh;
        public UInt32 nFileSizeLow;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public String cFileName;
    }
}
