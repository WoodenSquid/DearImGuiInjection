using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PortableExecutable;

// Original code from https://gist.github.com/augustoproiete/b51f29f74f5f5b2c59c39e47a8afc3a3
// Minus the IMAGE_DEBUG_DIRECTORY part

/// <summary>
/// Reads in the header information of the Portable Executable format.
/// Provides information such as the date the assembly was compiled.
/// </summary>
public class PEReader
{
    #region File Header Structures

    public struct IMAGE_DOS_HEADER
    {      // DOS .EXE header
        public ushort e_magic;              // Magic number
        public ushort e_cblp;               // Bytes on last page of file
        public ushort e_cp;                 // Pages in file
        public ushort e_crlc;               // Relocations
        public ushort e_cparhdr;            // Size of header in paragraphs
        public ushort e_minalloc;           // Minimum extra paragraphs needed
        public ushort e_maxalloc;           // Maximum extra paragraphs needed
        public ushort e_ss;                 // Initial (relative) SS value
        public ushort e_sp;                 // Initial SP value
        public ushort e_csum;               // Checksum
        public ushort e_ip;                 // Initial IP value
        public ushort e_cs;                 // Initial (relative) CS value
        public ushort e_lfarlc;             // File address of relocation table
        public ushort e_ovno;               // Overlay number
        public ushort e_res_0;              // Reserved words
        public ushort e_res_1;              // Reserved words
        public ushort e_res_2;              // Reserved words
        public ushort e_res_3;              // Reserved words
        public ushort e_oemid;              // OEM identifier (for e_oeminfo)
        public ushort e_oeminfo;            // OEM information; e_oemid specific
        public ushort e_res2_0;             // Reserved words
        public ushort e_res2_1;             // Reserved words
        public ushort e_res2_2;             // Reserved words
        public ushort e_res2_3;             // Reserved words
        public ushort e_res2_4;             // Reserved words
        public ushort e_res2_5;             // Reserved words
        public ushort e_res2_6;             // Reserved words
        public ushort e_res2_7;             // Reserved words
        public ushort e_res2_8;             // Reserved words
        public ushort e_res2_9;             // Reserved words
        public uint e_lfanew;             // File address of new exe header
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        public uint ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public uint SizeOfStackReserve;
        public uint SizeOfStackCommit;
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;

        public IMAGE_DATA_DIRECTORY ExportTable;
        public IMAGE_DATA_DIRECTORY ImportTable;
        public IMAGE_DATA_DIRECTORY ResourceTable;
        public IMAGE_DATA_DIRECTORY ExceptionTable;
        public IMAGE_DATA_DIRECTORY CertificateTable;
        public IMAGE_DATA_DIRECTORY BaseRelocationTable;
        public IMAGE_DATA_DIRECTORY Debug;
        public IMAGE_DATA_DIRECTORY Architecture;
        public IMAGE_DATA_DIRECTORY GlobalPtr;
        public IMAGE_DATA_DIRECTORY TLSTable;
        public IMAGE_DATA_DIRECTORY LoadConfigTable;
        public IMAGE_DATA_DIRECTORY BoundImport;
        public IMAGE_DATA_DIRECTORY IAT;
        public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
        public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
        public IMAGE_DATA_DIRECTORY Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;

        public IMAGE_DATA_DIRECTORY ExportTable;
        public IMAGE_DATA_DIRECTORY ImportTable;
        public IMAGE_DATA_DIRECTORY ResourceTable;
        public IMAGE_DATA_DIRECTORY ExceptionTable;
        public IMAGE_DATA_DIRECTORY CertificateTable;
        public IMAGE_DATA_DIRECTORY BaseRelocationTable;
        public IMAGE_DATA_DIRECTORY Debug;
        public IMAGE_DATA_DIRECTORY Architecture;
        public IMAGE_DATA_DIRECTORY GlobalPtr;
        public IMAGE_DATA_DIRECTORY TLSTable;
        public IMAGE_DATA_DIRECTORY LoadConfigTable;
        public IMAGE_DATA_DIRECTORY BoundImport;
        public IMAGE_DATA_DIRECTORY IAT;
        public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
        public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
        public IMAGE_DATA_DIRECTORY Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    // Grabbed the following 2 definitions from http://www.pinvoke.net/default.aspx/Structures/IMAGE_SECTION_HEADER.html

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    public struct IMAGE_SECTION_HEADER
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string Name;
        [FieldOffset(8)]
        public uint VirtualSize;
        [FieldOffset(12)]
        public uint VirtualAddress;
        [FieldOffset(16)]
        public uint SizeOfRawData;
        [FieldOffset(20)]
        public uint PointerToRawData;
        [FieldOffset(24)]
        public uint PointerToRelocations;
        [FieldOffset(28)]
        public uint PointerToLinenumbers;
        [FieldOffset(32)]
        public ushort NumberOfRelocations;
        [FieldOffset(34)]
        public ushort NumberOfLinenumbers;
        [FieldOffset(36)]
        public DataSectionFlags Characteristics;
    }

    [Flags]
    public enum DataSectionFlags : uint
    {
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeReg = 0x00000000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeDsect = 0x00000001,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeNoLoad = 0x00000002,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeGroup = 0x00000004,
        /// <summary>
        /// The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES. This is valid only for object files.
        /// </summary>
        TypeNoPadded = 0x00000008,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeCopy = 0x00000010,
        /// <summary>
        /// The section contains executable code.
        /// </summary>
        ContentCode = 0x00000020,
        /// <summary>
        /// The section contains initialized data.
        /// </summary>
        ContentInitializedData = 0x00000040,
        /// <summary>
        /// The section contains uninitialized data.
        /// </summary>
        ContentUninitializedData = 0x00000080,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        LinkOther = 0x00000100,
        /// <summary>
        /// The section contains comments or other information. The .drectve section has this type. This is valid for object files only.
        /// </summary>
        LinkInfo = 0x00000200,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeOver = 0x00000400,
        /// <summary>
        /// The section will not become part of the image. This is valid only for object files.
        /// </summary>
        LinkRemove = 0x00000800,
        /// <summary>
        /// The section contains COMDAT data. For more information, see section 5.5.6, COMDAT Sections (Object Only). This is valid only for object files.
        /// </summary>
        LinkComDat = 0x00001000,
        /// <summary>
        /// Reset speculative exceptions handling bits in the TLB entries for this section.
        /// </summary>
        NoDeferSpecExceptions = 0x00004000,
        /// <summary>
        /// The section contains data referenced through the global pointer (GP).
        /// </summary>
        RelativeGP = 0x00008000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemPurgeable = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Memory16Bit = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryLocked = 0x00040000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryPreload = 0x00080000,
        /// <summary>
        /// Align data on a 1-byte boundary. Valid only for object files.
        /// </summary>
        Align1Bytes = 0x00100000,
        /// <summary>
        /// Align data on a 2-byte boundary. Valid only for object files.
        /// </summary>
        Align2Bytes = 0x00200000,
        /// <summary>
        /// Align data on a 4-byte boundary. Valid only for object files.
        /// </summary>
        Align4Bytes = 0x00300000,
        /// <summary>
        /// Align data on an 8-byte boundary. Valid only for object files.
        /// </summary>
        Align8Bytes = 0x00400000,
        /// <summary>
        /// Align data on a 16-byte boundary. Valid only for object files.
        /// </summary>
        Align16Bytes = 0x00500000,
        /// <summary>
        /// Align data on a 32-byte boundary. Valid only for object files.
        /// </summary>
        Align32Bytes = 0x00600000,
        /// <summary>
        /// Align data on a 64-byte boundary. Valid only for object files.
        /// </summary>
        Align64Bytes = 0x00700000,
        /// <summary>
        /// Align data on a 128-byte boundary. Valid only for object files.
        /// </summary>
        Align128Bytes = 0x00800000,
        /// <summary>
        /// Align data on a 256-byte boundary. Valid only for object files.
        /// </summary>
        Align256Bytes = 0x00900000,
        /// <summary>
        /// Align data on a 512-byte boundary. Valid only for object files.
        /// </summary>
        Align512Bytes = 0x00A00000,
        /// <summary>
        /// Align data on a 1024-byte boundary. Valid only for object files.
        /// </summary>
        Align1024Bytes = 0x00B00000,
        /// <summary>
        /// Align data on a 2048-byte boundary. Valid only for object files.
        /// </summary>
        Align2048Bytes = 0x00C00000,
        /// <summary>
        /// Align data on a 4096-byte boundary. Valid only for object files.
        /// </summary>
        Align4096Bytes = 0x00D00000,
        /// <summary>
        /// Align data on an 8192-byte boundary. Valid only for object files.
        /// </summary>
        Align8192Bytes = 0x00E00000,
        /// <summary>
        /// The section contains extended relocations.
        /// </summary>
        LinkExtendedRelocationOverflow = 0x01000000,
        /// <summary>
        /// The section can be discarded as needed.
        /// </summary>
        MemoryDiscardable = 0x02000000,
        /// <summary>
        /// The section cannot be cached.
        /// </summary>
        MemoryNotCached = 0x04000000,
        /// <summary>
        /// The section is not pageable.
        /// </summary>
        MemoryNotPaged = 0x08000000,
        /// <summary>
        /// The section can be shared in memory.
        /// </summary>
        MemoryShared = 0x10000000,
        /// <summary>
        /// The section can be executed as code.
        /// </summary>
        MemoryExecute = 0x20000000,
        /// <summary>
        /// The section can be read.
        /// </summary>
        MemoryRead = 0x40000000,
        /// <summary>
        /// The section can be written to.
        /// </summary>
        MemoryWrite = 0x80000000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DEBUG_DIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public _Type Type;
        public uint SizeOfData;
        public uint AddressOfRawData;
        public uint PointerToRawData;

        public enum _Type : uint
        {
            IMAGE_DEBUG_TYPE_UNKNOWN = 0,
            IMAGE_DEBUG_TYPE_COFF = 1,
            IMAGE_DEBUG_TYPE_CODEVIEW = 2,
            IMAGE_DEBUG_TYPE_FPO = 3,
            IMAGE_DEBUG_TYPE_MISC = 4,
            IMAGE_DEBUG_TYPE_EXCEPTION = 5,
            IMAGE_DEBUG_TYPE_FIXUP = 6,
            IMAGE_DEBUG_TYPE_OMAP_TO_SRC = 7,
            IMAGE_DEBUG_TYPE_OMAP_FROM_SRC = 8,
            IMAGE_DEBUG_TYPE_BORLAND = 9,
            IMAGE_DEBUG_TYPE_RESERVED10 = 10,
            IMAGE_DEBUG_TYPE_CLSID = 11,
            IMAGE_DEBUG_TYPE_VC_FEATURE = 12,
            IMAGE_DEBUG_TYPE_POGO = 13,
            IMAGE_DEBUG_TYPE_ILTCG = 14,
            IMAGE_DEBUG_TYPE_MPX = 15,
            IMAGE_DEBUG_TYPE_REPRO = 16
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RSDS
    {
        internal const int Magic = 0x53445352;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] Guid;

        internal uint Age;
    }

    #endregion File Header Structures

    #region Private Fields

    /// <summary>
    /// The DOS header
    /// </summary>
    private IMAGE_DOS_HEADER dosHeader;
    /// <summary>
    /// The file header
    /// </summary>
    private IMAGE_FILE_HEADER fileHeader;
    /// <summary>
    /// Optional 32 bit file header 
    /// </summary>
    private IMAGE_OPTIONAL_HEADER32 optionalHeader32;
    /// <summary>
    /// Optional 64 bit file header 
    /// </summary>
    private IMAGE_OPTIONAL_HEADER64 optionalHeader64;

    private IMAGE_DEBUG_DIRECTORY? imageDebugDirectory;

    private string rsdsPdbFileName;
    private string pdbGuid;
    private string pdbAge;

    /// <summary>
    /// Image Section headers. Number of sections is in the file header.
    /// </summary>
    private IMAGE_SECTION_HEADER[] imageSectionHeaders;

    #endregion Private Fields

    #region Public Methods

    public static PEReader FromFilePath(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return SafeNew(stream);
    }

    public static PEReader FromBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return SafeNew(stream);
    }

    public static PEReader FromStream(Stream stream) => SafeNew(stream);

    private static PEReader SafeNew(Stream stream)
    {
        var instance = new PEReader();
        try
        {
            instance.Init(stream);
        } catch (Exception e)
        {
            Log.Error(e);
            return null;
        }

        return instance;
    }


    private void Init(Stream stream)
    {
        var reader = new BinaryReader(stream);
        dosHeader = FromBinaryReader<IMAGE_DOS_HEADER>(reader);

        // Add 4 bytes to the offset
        stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

        uint ntHeadersSignature = reader.ReadUInt32();
        fileHeader = FromBinaryReader<IMAGE_FILE_HEADER>(reader);

        uint debugRva;
        if (Is32BitHeader)
        {
            optionalHeader32 = FromBinaryReader<IMAGE_OPTIONAL_HEADER32>(reader);
            debugRva = optionalHeader32.Debug.VirtualAddress;
        } else
        {
            optionalHeader64 = FromBinaryReader<IMAGE_OPTIONAL_HEADER64>(reader);
            debugRva = optionalHeader64.Debug.VirtualAddress;
        }

        imageSectionHeaders = new IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];
        for (int headerNo = 0; headerNo < imageSectionHeaders.Length; ++headerNo)
        {
            var imageSectionHeader = FromBinaryReader<IMAGE_SECTION_HEADER>(reader);
            imageSectionHeaders[headerNo] = imageSectionHeader;

            if (imageDebugDirectory == null)
            {
                imageDebugDirectory = TryGetDebugDirectory(reader, debugRva, ref imageSectionHeader);
            }
        }

        if (imageDebugDirectory != null)
        {
            TryGetRSDS(reader);
        }
    }

    private IMAGE_DEBUG_DIRECTORY? TryGetDebugDirectory(BinaryReader reader, uint debugRva, ref IMAGE_SECTION_HEADER imageSectionHeader)
    {
        var sectionSize = imageSectionHeader.VirtualSize != 0 ? imageSectionHeader.VirtualSize : imageSectionHeader.SizeOfRawData;

        if (debugRva >= imageSectionHeader.VirtualAddress &&
                debugRva < imageSectionHeader.VirtualAddress + sectionSize)
        {
            var oldPosition = reader.BaseStream.Position;

            var debugDirectoryFileOffset = debugRva - imageSectionHeader.VirtualAddress + imageSectionHeader.PointerToRawData;
            reader.BaseStream.Position = debugDirectoryFileOffset;

            var imageDebugDirectory = FromBinaryReader<IMAGE_DEBUG_DIRECTORY>(reader);

            reader.BaseStream.Position = oldPosition;

            return imageDebugDirectory;
        }

        return null;
    }

    private void TryGetRSDS(BinaryReader reader)
    {
        var debugSectionInfo = new
        {
            RSDSFileOffset = ImageDebugDirectory.Value.PointerToRawData,
            Size = ImageDebugDirectory.Value.SizeOfData
        };

        reader.BaseStream.Position = debugSectionInfo.RSDSFileOffset;
        long sectionEnd = debugSectionInfo.RSDSFileOffset + debugSectionInfo.Size;

        int currentInt;
        while (reader.BaseStream.Position != sectionEnd)
        {
            currentInt = reader.ReadInt32();

            if (currentInt == RSDS.Magic)
            {
                var rsdsStructByteArray = reader.ReadBytes(Marshal.SizeOf<RSDS>());

                var handle = GCHandle.Alloc(rsdsStructByteArray, GCHandleType.Pinned);
                try
                {
                    var rsdsStruct = Marshal.PtrToStructure<RSDS>(handle.AddrOfPinnedObject());

                    pdbGuid = new Guid(rsdsStruct.Guid).ToString("N").ToUpperInvariant();
                    pdbAge = rsdsStruct.Age.ToString();

                    var pdbPathArray = new List<byte>();
                    bool reading = true;
                    while (reading)
                    {
                        var b = reader.ReadByte();
                        if (b != 0x0)
                            pdbPathArray.Add(b);
                        else
                        {
                            reading = false;
                        }
                    }

                    rsdsPdbFileName = Path.GetFileName(Encoding.Default.GetString(pdbPathArray.ToArray()));
                } finally
                {
                    handle.Free();
                }

                break;
            }
        }
    }

    /// <summary>
    /// Gets the header of the .NET assembly that called this function
    /// </summary>
    /// <returns></returns>
    public static PEReader GetCallingAssemblyHeader()
    {
        // Get the path to the calling assembly, which is the path to the
        // DLL or EXE that we want the time of
        string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;

        // Get and return the timestamp
        return FromFilePath(filePath);
    }

    /// <summary>
    /// Gets the header of the .NET assembly that called this function
    /// </summary>
    /// <returns></returns>
    public static PEReader GetAssemblyHeader()
    {
        // Get the path to the calling assembly, which is the path to the
        // DLL or EXE that we want the time of
        string filePath = System.Reflection.Assembly.GetAssembly(typeof(PEReader)).Location;

        // Get and return the timestamp
        return FromFilePath(filePath);
    }

    /// <summary>
    /// Reads in a block from a file and converts it to the struct
    /// type specified by the template parameter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static T FromBinaryReader<T>(BinaryReader reader)
    {
        // Read in a byte array
        byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

        // Pin the managed memory while, copy it out the data, then unpin it
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();

        return theStructure;
    }

    #endregion Public Methods

    #region Properties

    /// <summary>
    /// Gets if the file header is 32 bit or not
    /// </summary>
    public bool Is32BitHeader
    {
        get
        {
            ushort IMAGE_FILE_32BIT_MACHINE = 0x0100;
            return (IMAGE_FILE_32BIT_MACHINE & FileHeader.Characteristics) == IMAGE_FILE_32BIT_MACHINE;
        }
    }

    /// <summary>
    /// Gets the file header
    /// </summary>
    public IMAGE_FILE_HEADER FileHeader => fileHeader;

    /// <summary>
    /// Gets the optional header
    /// </summary>
    public IMAGE_OPTIONAL_HEADER32 OptionalHeader32 => optionalHeader32;

    /// <summary>
    /// Gets the optional header
    /// </summary>
    public IMAGE_OPTIONAL_HEADER64 OptionalHeader64 => optionalHeader64;

    public IMAGE_DEBUG_DIRECTORY? ImageDebugDirectory => imageDebugDirectory;

    public IMAGE_SECTION_HEADER[] ImageSectionHeaders => imageSectionHeaders;

    public string RsdsPdbFileName => rsdsPdbFileName;

    public string PdbGuid => pdbGuid;

    public string PdbAge => pdbAge;

    /// <summary>
    /// Gets the timestamp from the file header
    /// </summary>
    public DateTime TimeStamp
    {
        get
        {
            // Timestamp is a date offset from 1970
            var returnValue = new DateTime(1970, 1, 1, 0, 0, 0);

            // Add in the number of seconds since 1970/1/1
            returnValue = returnValue.AddSeconds(fileHeader.TimeDateStamp);
            // Adjust to local timezone
#pragma warning disable CS0618 // Type or member is obsolete
            returnValue += TimeZone.CurrentTimeZone.GetUtcOffset(returnValue);
#pragma warning restore CS0618 // Type or member is obsolete

            return returnValue;
        }
    }

    #endregion Properties
}