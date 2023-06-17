using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FileShrinker;

public static class FileTools
{
    private const int FSCTL_SET_COMPRESSION = 0x9C040;
    private const short COMPRESSION_FORMAT_DEFAULT = 1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int DeviceIoControl(
        IntPtr hDevice,
        int dwIoControlCode,
        ref short lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        ref int lpBytesReturned,
        IntPtr lpOverlapped);

    /// <summary>
    /// Enables NTFS compression on the specified file or directory.
    /// </summary>
    public static bool EnableCompression(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return false;

        int lpBytesReturned = 0;
        short lpInBuffer = COMPRESSION_FORMAT_DEFAULT;

        return DeviceIoControl(handle, FSCTL_SET_COMPRESSION,
            ref lpInBuffer, sizeof(short), IntPtr.Zero, 0,
            ref lpBytesReturned, IntPtr.Zero) != 0;
    }

    /// <summary>
    /// Gets the size of the file on disk.
    /// </summary>
    public static long GetFileSizeOnDisk(string file)
    {
        FileInfo info = new(file);
        if (info == null || info.Directory == null)
        {
            throw new FileNotFoundException($"Could not get file info of '{file}'");
        }

        int getClusterSizeSuccess = GetDiskFreeSpaceW(info.Directory.Root.FullName, out uint sectorsPerCluster, out uint bytesPerSector, out _, out _);
        if (getClusterSizeSuccess == 0) throw new Win32Exception();
        uint clusterSize = sectorsPerCluster * bytesPerSector;
        uint losize = GetCompressedFileSizeW(file, out uint hosize);
        long size;
        size = (long)hosize << 32 | losize;
        return ((size + clusterSize - 1) / clusterSize) * clusterSize;
    }

    [DllImport("kernel32.dll")]
    static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
       [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

    [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
    static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
       out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
       out uint lpTotalNumberOfClusters);

    /// <summary>
    /// Sets the file size to 0 bytes.
    /// </summary>
    public static void NullifyFile(string fileName)
    {
        using FileStream fs = new(fileName, FileMode.Open, FileAccess.Write);
        fs.SetLength(0);
        fs.Close();
    }
}
