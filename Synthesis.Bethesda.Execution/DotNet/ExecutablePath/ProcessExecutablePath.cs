﻿using System;
using System.IO;
using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath
{
    public interface IProcessExecutablePath
    {
        string Process(FilePath projPath, FilePath execPath);
    }

    public class ProcessExecutablePath : IProcessExecutablePath
    {
        public IFileSystem FileSystem { get; }
        private readonly ILogger _logger;
        public IProvideWorkingDirectory WorkingDirectory { get; }

        public ProcessExecutablePath(
            IFileSystem fileSystem,
            ILogger logger,
            IProvideWorkingDirectory workingDirectory)
        {
            FileSystem = fileSystem;
            _logger = logger;
            WorkingDirectory = workingDirectory;
        }
        
        public string Process(
            FilePath projPath,
            FilePath execPath)
        {
            if (FileSystem.File.Exists(execPath)) return execPath;
            var workingDir = WorkingDirectory.WorkingDirectory;
            if (!projPath.IsUnderneath(workingDir))
            {
                _logger.Warning("Locating executable path unexpectedly was not under working directory. " +
                                "Working Dir: {WorkingDirectory} " +
                                "Exe Path: {ExePath}",
                    workingDir,
                    execPath);
                return execPath;
            }

            var binIndex = execPath.Path.IndexOf("bin", StringComparison.OrdinalIgnoreCase);
            var trim = execPath.Path.Substring(binIndex);
            return Path.Combine(projPath.Directory!.Value.Path, trim);
        }
    }
}