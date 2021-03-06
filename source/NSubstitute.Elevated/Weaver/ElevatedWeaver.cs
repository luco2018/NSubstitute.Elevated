using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;
using Unity.Core;

namespace NSubstitute.Elevated.Weaver
{
    public enum PatchTestAssembly { No, Yes }

    public static class ElevatedWeaver
    {
        const string k_PatchBackupExtension = ".orig";

        public static string GetPatchBackupPathFor(string path)
        => path + k_PatchBackupExtension;

        public static IReadOnlyCollection<PatchResult> PatchAllDependentAssemblies(
            [NotNull] string testAssemblyPath,
            PatchTestAssembly patchTestAssembly = PatchTestAssembly.No) // typically we don't want to patch the test assembly itself, only the systems under test
        {
            var testAssemblyFolder = Path.GetDirectoryName(testAssemblyPath);
            if (testAssemblyFolder.IsNullOrEmpty())
                throw new Exception("Unable to find folder for test assembly");
            testAssemblyFolder = Path.GetFullPath(testAssemblyFolder);

            // scope
            {
                var thisAssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (thisAssemblyFolder.IsNullOrEmpty())
                    throw new Exception("Can only patch assemblies on disk");
                thisAssemblyFolder = Path.GetFullPath(thisAssemblyFolder);

                // keep things really simple, at least for now
                if (string.Compare(testAssemblyFolder, thisAssemblyFolder, StringComparison.OrdinalIgnoreCase) != 0)
                    throw new Exception("All assemblies must be in the same folder");
            }

            var nsubElevatedPath = Path.Combine(testAssemblyFolder, "NSubstitute.Elevated.dll");
            using (var nsubElevatedAssembly = AssemblyDefinition.ReadAssembly(nsubElevatedPath))
            {
                var mockInjector = new MockInjector(nsubElevatedAssembly);
                var toProcess = new List<string> { Path.GetFullPath(testAssemblyPath) };
                var patchResults = new Dictionary<string, PatchResult>(StringComparer.OrdinalIgnoreCase);

                for (var toProcessIndex = 0; toProcessIndex < toProcess.Count; ++toProcessIndex)
                {
                    var assemblyToPatchPath = toProcess[toProcessIndex];
                    if (patchResults.ContainsKey(assemblyToPatchPath))
                        continue;

                    if (!Path.IsPathRooted(assemblyToPatchPath))
                        throw new Exception($"Unexpected non-rooted assembly path '{assemblyToPatchPath}'");

                    using (var assemblyToPatch = AssemblyDefinition.ReadAssembly(assemblyToPatchPath))
                    {
                        foreach (var referencedAssembly in assemblyToPatch.Modules.SelectMany(m => m.AssemblyReferences))
                        {
                            // only patch dll's we "own", that are in the same folder as the test assembly
                            var foundPath = Path.Combine(testAssemblyFolder, referencedAssembly.Name + ".dll");

                            if (File.Exists(foundPath))
                                toProcess.Add(foundPath);
                            else if (!patchResults.ContainsKey(referencedAssembly.Name))
                                patchResults.Add(referencedAssembly.Name, new PatchResult(referencedAssembly.Name, null, PatchState.IgnoredOutsideAllowedPaths));
                        }

                        PatchResult patchResult;

                        if (toProcessIndex == 0 && patchTestAssembly == PatchTestAssembly.No)
                            patchResult = new PatchResult(assemblyToPatchPath, null, PatchState.IgnoredTestAssembly);
                        else if (MockInjector.IsPatched(assemblyToPatch))
                            patchResult = new PatchResult(assemblyToPatchPath, null, PatchState.AlreadyPatched);
                        else
                        {
                            mockInjector.Patch(assemblyToPatch);

                            // atomic write of file with backup
                            var tmpPath = assemblyToPatchPath + ".tmp";
                            File.Delete(tmpPath);
                            assemblyToPatch.Write(tmpPath);//$$$$, new WriterParameters { WriteSymbols = true });  // getting exception, haven't looked into it yet
                            assemblyToPatch.Dispose();
                            var originalPath = GetPatchBackupPathFor(assemblyToPatchPath);
                            File.Replace(tmpPath, assemblyToPatchPath, originalPath);
                            // $$$ TODO: move pdb file too

                            patchResult = new PatchResult(assemblyToPatchPath, originalPath, PatchState.Patched);
                        }

                        patchResults.Add(assemblyToPatchPath, patchResult);
                    }
                }

                return patchResults.Values;
            }
        }
    }

    public enum PatchState
    {
        GeneralFailure,             // something else went wrong
        IgnoredTestAssembly,        // don't patch the test assembly itself, as we're requiring that to always be separate from the systems under test
        IgnoredOutsideAllowedPaths, // don't want to patch things that are not "ours"
        //AlreadyPatchedOld,          // assy already patched against an older set of tooling TODO: implement
        AlreadyPatched,             // assy already patched against current tooling
        Patched,                    // assy patched and old one backed up
    }

    public struct PatchResult
    {
        public string Path;
        public string OriginalPath;
        public PatchState PatchState;

        [DebuggerStepThrough]
        public PatchResult(string path, string originalPath, PatchState patchState)
        {
            Path = path;
            OriginalPath = originalPath;
            PatchState = patchState;
        }
    }
}
