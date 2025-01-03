﻿namespace VeryLargeTextFile.Utilities;

class InputFileStreamFactory : IInputFileStreamFactory
{
    public Stream CreateInputStream(FileInfo fileInfo)
    {
        const int bufferSize = 128 * 1024;
        return new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize);
    }
}