using System;
using System.Collections.Generic;
using System.IO;
using AiAssistant.ExecuteSandbox;
using Microsoft.VisualBasic.FileIO;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// File system operations: read, write, copy, move, delete, enumerate.
    /// All public methods are sandboxed via Sandbox.Exec.
    /// </summary>
    public class IOUnit
    {
        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
        {
            new CapabilityInfo
            {
                Name = "GetFiles",
                Description = "List all files in a directory",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Path",      Type = "string", Description = "Target directory path" },
                    new ParameterInfo { Name = "Recursive", Type = "bool",   Description = "Include subdirectories" }
                }
            },
            new CapabilityInfo
            {
                Name = "ReadText",
                Description = "Read text content from a file",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Path", Type = "string", Description = "File path to read" }
                }
            },
            new CapabilityInfo
            {
                Name = "WriteText",
                Description = "Write text content to a file",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Path",      Type = "string", Description = "Destination file path" },
                    new ParameterInfo { Name = "Content",   Type = "string", Description = "Text content to write" },
                    new ParameterInfo { Name = "Overwrite", Type = "bool",   Description = "Overwrite if file exists" }
                }
            },
            new CapabilityInfo
            {
                Name = "Move",
                Description = "Move a file or directory to a new location",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Source",    Type = "string", Description = "Source path" },
                    new ParameterInfo { Name = "Dest",      Type = "string", Description = "Destination path" },
                    new ParameterInfo { Name = "Overwrite", Type = "bool",   Description = "Overwrite existing destination" }
                }
            },
            new CapabilityInfo
            {
                Name = "Copy",
                Description = "Copy a file or directory",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Source",    Type = "string", Description = "Source path" },
                    new ParameterInfo { Name = "Dest",      Type = "string", Description = "Destination path" },
                    new ParameterInfo { Name = "Overwrite", Type = "bool",   Description = "Overwrite existing destination" }
                }
            },
            new CapabilityInfo
            {
                Name = "DeleteToRecycleBin",
                Description = "Safely delete a file or folder by sending it to the recycle bin",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Path", Type = "string", Description = "Target path to delete" }
                }
            },
            new CapabilityInfo
            {
                Name = "CreateDirectory",
                Description = "Create a directory (and any missing parents)",
                Params = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Path", Type = "string", Description = "Directory path to create" }
                }
            }
        };

        #endregion

        #region Exists

        /// <summary>Raw existence check without sandbox restrictions.</summary>
        public bool ExistsRaw(string Path)
            => File.Exists(Path) || Directory.Exists(Path);

        /// <summary>Sandboxed existence check for files or directories.</summary>
        public bool Exists(string Path)
            => Sandbox.Exec(nameof(Exists), () => ExistsRaw(Path), Path);

        #endregion

        #region Directory Operations

        /// <summary>Returns a list of file paths inside the given directory.</summary>
        public List<string> GetFiles(string Path, bool Recursive = false)
            => Sandbox.Exec(nameof(GetFiles), () =>
            {
                EnsureDirectory(Path);

                string[] Results = Directory.GetFiles(
                    Path,
                    "*",
                    Recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly
                );

                return new List<string>(Results);
            }, Path, Recursive);

        /// <summary>Returns a list of subdirectory paths inside the given directory.</summary>
        public List<string> GetDirectories(string Path, bool Recursive = false)
            => Sandbox.Exec(nameof(GetDirectories), () =>
            {
                EnsureDirectory(Path);

                string[] Results = Directory.GetDirectories(
                    Path,
                    "*",
                    Recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly
                );

                return new List<string>(Results);
            }, Path, Recursive);

        /// <summary>Returns both files and subdirectories as FileSystemEntry objects.</summary>
        public List<FileSystemEntry> GetEntries(string Path, bool Recursive = false)
            => Sandbox.Exec(nameof(GetEntries), () =>
            {
                EnsureDirectory(Path);

                var Results = new List<FileSystemEntry>();
                System.IO.SearchOption Option = Recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;

                foreach (string DirectoryPath in Directory.GetDirectories(Path, "*", Option))
                {
                    Results.Add(new FileSystemEntry
                    {
                        Path        = DirectoryPath,
                        Name        = System.IO.Path.GetFileName(DirectoryPath),
                        IsDirectory = true
                    });
                }

                foreach (string FilePath in Directory.GetFiles(Path, "*", Option))
                {
                    Results.Add(new FileSystemEntry
                    {
                        Path        = FilePath,
                        Name        = System.IO.Path.GetFileName(FilePath),
                        IsDirectory = false
                    });
                }

                return Results;
            }, Path, Recursive);

        #endregion

        #region File I/O

        /// <summary>Reads and returns the full text content of a file.</summary>
        public string ReadText(string Path)
            => Sandbox.Exec(nameof(ReadText), () =>
            {
                EnsureFile(Path);
                return File.ReadAllText(Path);
            }, Path);

        /// <summary>Reads and returns the raw bytes of a file.</summary>
        public byte[] ReadBytes(string Path)
            => Sandbox.Exec(nameof(ReadBytes), () =>
            {
                EnsureFile(Path);
                return File.ReadAllBytes(Path);
            }, Path);

        /// <summary>Writes text content to a file, optionally overwriting an existing one.</summary>
        public void WriteText(string Path, string Content, bool Overwrite = true)
            => Sandbox.Exec(nameof(WriteText), () =>
            {
                EnsureDirForFile(Path);

                if (!Overwrite && File.Exists(Path))
                    throw new Exception("File already exists and Overwrite is false.");

                File.WriteAllText(Path, Content);
            }, Path, Content, Overwrite);

        /// <summary>Writes binary bytes to a file, optionally overwriting an existing one.</summary>
        public void WriteBytes(string Path, byte[] Data, bool Overwrite = true)
            => Sandbox.Exec(nameof(WriteBytes), () =>
            {
                EnsureDirForFile(Path);

                if (!Overwrite && File.Exists(Path))
                    throw new Exception("File already exists and Overwrite is false.");

                File.WriteAllBytes(Path, Data);
            }, Path, Data, Overwrite);

        #endregion

        #region File Operations

        /// <summary>Moves a file or directory. Optionally overwrites the destination.</summary>
        public void Move(string Source, string Dest, bool Overwrite = true)
            => Sandbox.Exec(nameof(Move), () =>
            {
                EnsureExists(Source);

                if (File.Exists(Source))
                {
                    if (File.Exists(Dest) && Overwrite)
                        File.Delete(Dest);

                    File.Move(Source, Dest);
                }
                else if (Directory.Exists(Source))
                {
                    if (Directory.Exists(Dest) && Overwrite)
                        Directory.Delete(Dest, recursive: true);

                    Directory.Move(Source, Dest);
                }
                else
                {
                    throw new Exception("Source path not found: " + Source);
                }
            }, Source, Dest, Overwrite);

        /// <summary>Copies a file or entire directory tree. Optionally overwrites destination.</summary>
        public void Copy(string Source, string Dest, bool Overwrite = true)
            => Sandbox.Exec(nameof(Copy), () =>
            {
                EnsureExists(Source);

                if (File.Exists(Source))
                    File.Copy(Source, Dest, Overwrite);
                else if (Directory.Exists(Source))
                    CopyDirectory(Source, Dest, Overwrite);
            }, Source, Dest, Overwrite);

        /// <summary>Sends a file or directory to the recycle bin instead of permanently deleting it.</summary>
        public void DeleteToRecycleBin(string Path)
            => Sandbox.Exec(nameof(DeleteToRecycleBin), () =>
            {
                EnsureExists(Path);

                if (File.Exists(Path))
                {
                    FileSystem.DeleteFile(Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                else if (Directory.Exists(Path))
                {
                    FileSystem.DeleteDirectory(Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }, Path);

        /// <summary>Creates a directory and all intermediate parent directories.</summary>
        public void CreateDirectory(string Path)
            => Sandbox.Exec(nameof(CreateDirectory), () =>
            {
                Directory.CreateDirectory(Path);
            }, Path);

        #endregion

        #region Info

        /// <summary>Returns metadata (name, size, last modified) for a file or directory.</summary>
        public FileSystemEntry GetInfo(string Path)
            => Sandbox.Exec(nameof(GetInfo), () =>
            {
                EnsureExists(Path);

                if (File.Exists(Path))
                {
                    var FileInfoObject = new FileInfo(Path);
                    return new FileSystemEntry
                    {
                        Path         = Path,
                        Name         = FileInfoObject.Name,
                        IsDirectory  = false,
                        Size         = FileInfoObject.Length,
                        LastModified = FileInfoObject.LastWriteTime
                    };
                }
                else
                {
                    var DirectoryInfoObject = new DirectoryInfo(Path);
                    return new FileSystemEntry
                    {
                        Path         = Path,
                        Name         = DirectoryInfoObject.Name,
                        IsDirectory  = true,
                        LastModified = DirectoryInfoObject.LastWriteTime
                    };
                }
            }, Path);

        #endregion

        #region Private Helpers

        /// <summary>Throws FileNotFoundException if the path does not point to an existing file.</summary>
        private void EnsureFile(string Path)
        {
            if (!File.Exists(Path))
                throw new FileNotFoundException("File not found: " + Path);
        }

        /// <summary>Throws DirectoryNotFoundException if the path does not point to an existing directory.</summary>
        private void EnsureDirectory(string Path)
        {
            if (!Directory.Exists(Path))
                throw new DirectoryNotFoundException("Directory not found: " + Path);
        }

        /// <summary>Throws an exception if the path (file or directory) does not exist.</summary>
        private void EnsureExists(string Path)
        {
            if (!ExistsRaw(Path))
                throw new Exception("Path not found: " + Path);
        }

        /// <summary>Creates the parent directory of a file path if it does not already exist.</summary>
        private void EnsureDirForFile(string FilePath)
        {
            string DirectoryPath = System.IO.Path.GetDirectoryName(FilePath);

            if (!string.IsNullOrEmpty(DirectoryPath) && !Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
        }

        /// <summary>Recursively copies a directory and all its contents to a new location.</summary>
        private void CopyDirectory(string Source, string Dest, bool Overwrite)
        {
            Directory.CreateDirectory(Dest);

            foreach (string SourceFile in Directory.GetFiles(Source))
            {
                string DestFile = System.IO.Path.Combine(Dest, System.IO.Path.GetFileName(SourceFile));
                File.Copy(SourceFile, DestFile, Overwrite);
            }

            foreach (string SourceSubDir in Directory.GetDirectories(Source))
            {
                string DestSubDir = System.IO.Path.Combine(Dest, System.IO.Path.GetFileName(SourceSubDir));
                CopyDirectory(SourceSubDir, DestSubDir, Overwrite);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a file or directory entry with metadata.
    /// </summary>
    public class FileSystemEntry
    {
        public string    Path         { get; set; }
        public string    Name         { get; set; }
        public bool      IsDirectory  { get; set; }
        public long?     Size         { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
