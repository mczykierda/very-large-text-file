﻿using VeryLargeTextFile.Utilities;

namespace VeryLargeTextFile.Sorter.FileSplitting;

class InputFileSplitter(IInputFileStreamFactory inputFileStreamFactory,
                        ITempFolderOperations tempFolder,
                        IOutputFileStreamFactory outputFileStreamFactory) : IInputFileSplitter
{
    public async Task<SplittingResult> SplitInputFileIntoSmallerFilesAndSortThem(FileInfo inputFileInfo, InputFileSplitterConfig config, CancellationToken cancellationToken)
    {
        tempFolder.Create(config);

        var result = new List<SplittedFile>();
        var buffer = new Buffer(config);
        var currentFileNumber = 0;

        await using var inputStream = inputFileStreamFactory.CreateInputStream(inputFileInfo);

        while (!inputStream.EndOfStream())
        {
            cancellationToken.ThrowIfCancellationRequested();

            buffer.ReadFromStream(inputStream);

            var splittedFileInfo = tempFolder.GetFileInfoForSplittedFile(currentFileNumber);
            await using var outputStream = outputFileStreamFactory.CreateOutputStream(splittedFileInfo);

            await buffer.SaveToStream(outputStream, cancellationToken);
            result.Add(new SplittedFile(splittedFileInfo, buffer.RecordsCount));

            buffer.Clear();
            currentFileNumber++;
        }

        return new SplittingResult(result);
    }
}
