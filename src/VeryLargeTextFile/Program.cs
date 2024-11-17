﻿using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Microsoft.Extensions.Logging;
using VeryLargeTextFile.Generator;
using VeryLargeTextFile.Sorter;

namespace VeryLargeTextFile;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var serviceProvider = GetServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Program");

        var rootCommand = new RootCommand("Generates or sorts a very large text file");
        rootCommand.AddCommand(CreateGenerateCommand(serviceProvider, logger));
        rootCommand.AddCommand(CreateSortCommand(serviceProvider, logger));

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateGenerateCommand(ServiceProvider serviceProvider, ILogger logger)
    {
        var fileOption = new Option<FileInfo>(
            ["--file", "-f"],
            "The very large text file to generate.")
        {
            IsRequired = true,
        };

        var fileSizeOption = new Option<long>(
            ["--file-size", "-fs"],
            () => 1024 * 1024 * 1024,
            $"Approximate size of file in bytes.");

        var textSizeOption = new Option<long>(
            ["--text-size", "-ts"],
            () => 1024,
            description: "Number of bytes for text part of each row.");

        var duplicationFactorOption = new Option<int>(
            ["--duplication-factor", "-df"],
            () => 1,
            description: "Text duplication level: 0 - no duplications, then the larger number the more duplicated rows (the text part)");

        var command = new Command("generate", "Generates the very large text file.")
        {
            fileOption,
            fileSizeOption,
            textSizeOption,
            duplicationFactorOption
        };

        command.SetHandler(
            async (fileInfo, fileSize, textSize, duplicationFactor) =>
            {
                try
                {
                    var generator = serviceProvider.GetRequiredService<IFileGenerator>();
                    await generator.GenerateFile(fileInfo, fileSize, textSize, duplicationFactor);
                    logger.LogDebug("Program finished");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Program failed");
                }
            },
            fileOption,
            fileSizeOption,
            textSizeOption,
            duplicationFactorOption
            );

        return command;
    }

    static Command CreateSortCommand(ServiceProvider serviceProvider, ILogger logger)
    {
        var inputFileOption = new Option<FileInfo>(
            ["--input", "-i"],
            "The very large text file to sort.")
        {
            IsRequired = true,
        };

        var outputFileOption = new Option<FileInfo>(
            ["--output", "-o"],
            "The sorted very large text file to save.")
        {
            IsRequired = true
        };

        var command = new Command("sort", "Sorts the very large text file.")
        {
            inputFileOption,
            outputFileOption,
        };

        command.SetHandler(
            async (inputFileInfo, outputFileInfo) =>
            {
                try
                {
                    var sorter = serviceProvider.GetRequiredService<IFileSorter>();
                    await sorter.SortFile(inputFileInfo, outputFileInfo);
                    logger.LogDebug("Program finished");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Program failed");
                }
            },
            inputFileOption,
            outputFileOption
            );

        return command;
    }

    static ServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
                                        .AddFilter("Microsoft", LogLevel.Warning)
                                        .AddFilter("System", LogLevel.Warning)
                                        .SetMinimumLevel(LogLevel.Trace)
                                        .AddConsole());

        services.Scan(scan => scan.FromCallingAssembly()
                                      .AddClasses(classes => classes.Where(x => !typeof(Exception).IsAssignableFrom(x)))
                                      .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                                      .AsImplementedInterfaces()
                                      .WithTransientLifetime());

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }
}