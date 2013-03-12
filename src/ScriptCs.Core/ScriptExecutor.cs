﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using ScriptCs.Contracts;

namespace ScriptCs
{
    [Export(Constants.RunContractName, typeof(IScriptExecutor))]
    public class ScriptExecutor : IScriptExecutor
    {
        private readonly IFileSystem _fileSystem;
        private readonly IFilePreProcessor _filePreProcessor;
        private readonly IScriptEngine _scriptEngine;

        [ImportingConstructor]
        public ScriptExecutor(IFileSystem fileSystem, [Import(Constants.RunContractName)]IFilePreProcessor filePreProcessor, [Import(Constants.RunContractName)]IScriptEngine scriptEngine)
        {
            _fileSystem = fileSystem;
            _filePreProcessor = filePreProcessor;
            _scriptEngine = scriptEngine;
        }

        public void Execute(string script, IEnumerable<string> paths, IEnumerable<IScriptPack> scriptPacks)
        {
            var bin = Path.Combine(_fileSystem.GetWorkingDirectory(script), "bin");
            var files = PrepareBinFolder(paths, bin);
    
            var references = new List<string>();
            references.Add("System");
            references.Add("System.Core");
            references.AddRange(files);

            _scriptEngine.BaseDirectory = bin;

            using (var scriptPackSession = new ScriptPackSession(scriptPacks)) {
                var path = Path.IsPathRooted(script) ? script : Path.Combine(_fileSystem.CurrentDirectory, script);
                var code = _filePreProcessor.ProcessFile(path);
            
                _scriptEngine.Execute(
                    code: code,
                    references: references,
                    scriptPackSession: scriptPackSession);
            }
        }

        private IEnumerable<string> PrepareBinFolder(IEnumerable<string> paths, string bin)
        {
            var files = new List<string>();

            if (!_fileSystem.DirectoryExists(bin))
                _fileSystem.CreateDirectory(bin);

            foreach (var file in paths)
            {
                var destFile = Path.Combine(bin, Path.GetFileName(file));
                var sourceFileLastWriteTime = _fileSystem.GetLastWriteTime(file);
                var destFileLastWriteTime = _fileSystem.GetLastWriteTime(destFile);
                if (sourceFileLastWriteTime != destFileLastWriteTime)
                    _fileSystem.Copy(file, destFile, true);
                files.Add(destFile);
            }

            return files;
        }
    }
}