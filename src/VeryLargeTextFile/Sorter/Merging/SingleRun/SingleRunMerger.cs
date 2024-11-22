﻿using Microsoft.Extensions.Logging;
using VeryLargeTextFile.Utilities;

namespace VeryLargeTextFile.Sorter.Merging.SingleRun;

class SingleRunMerger(
    IInputFileStreamFactory inputFileStreamFactory,
    ITempFolderOperations tempFolder,
    IFileOperations fileOperations,
    IOutputFileStreamFactory outputFileStreamFactory,
    IComparer<string> comparer,
    ILogger<SingleRunMerger> logger
    ) 
    : ISingleRunMerger
{
    public async Task<FileInfo> MergeFiles(IReadOnlyCollection<FileInfo> sortedFiles, int mergeRunCounter, CancellationToken cancellationToken)
    {
        logger.LogDebug($"Merging run {mergeRunCounter}, files to merge: {sortedFiles.Count}");

        using var filesList = await CreateFilesList(sortedFiles);

        var mergedFileInfo = tempFolder.GetFileInfoForMergedFile(mergeRunCounter);
        using var outputWriter = new StreamWriter(outputFileStreamFactory.CreateOutputStream(mergedFileInfo));
        logger.LogDebug($"Writing merged data to: {mergedFileInfo.Name}");

        var firstLoop = true;
        while (filesList.HasAnyFilesToProcess)
        {
            filesList.Sort(); //for single file does nothing

            if (firstLoop)
            {
                firstLoop = false;
            }
            else
            {
                await outputWriter.WriteAsync(Environment.NewLine);
            }
            await outputWriter.WriteAsync(filesList.Head.Value);
            
            
            if (filesList.Head.IsFullyProcessed)
            {
                logger.LogDebug($"Input file {filesList.Head.FileInfo.Name} is fully processed, removing it.");
                filesList.RemoveHeadFile();
            }
            else
            {
                await filesList.Head.MoveNext();
            }
        }

        logger.LogDebug($"Merge completed: {mergedFileInfo.Name}, size: {mergedFileInfo.Length}");
        return mergedFileInfo;
    }

    async Task<FilesList> CreateFilesList(IEnumerable<FileInfo> sortedFiles)
    {
        var result = new FilesList(sortedFiles.Count(), comparer);
        await result.AddFiles(sortedFiles.Select(x => new File(x, inputFileStreamFactory, fileOperations)));
        return result;
    }
}