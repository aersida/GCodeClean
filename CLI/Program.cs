﻿// Copyright (c) 2020 - Lee HUMPHRIES (lee@md8n.com) and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using GCodeClean.IO;
using GCodeClean.Processing;

namespace GCodeClean.CLI
{
    class Program
    {
        class Options
        {
            [Option("filename", Required = false, HelpText = "Full path to the input filename.")]
            public string filename { get; set; }

            [Usage(ApplicationAlias = "GCodeClean")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    return new List<Example>() {
                        new Example("Clean GCode file", new Options { filename = "facade.nc" })
                    };
                }
            }
        }

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[] { "--help" };
            }
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(RunAsync);
            Console.WriteLine($"Exit code= {Environment.ExitCode}");
        }

        static async Task RunAsync(Options options)
        {
            var inputFile = options.filename;
            var outputFile = inputFile;
            var inputExtension = Path.GetExtension(inputFile);
            Console.WriteLine(inputExtension);
            if (String.IsNullOrEmpty(inputExtension))
            {
                outputFile += "-gcc.nc";
            }
            else
            {
                outputFile = outputFile.Replace(inputExtension, "-gcc" + inputExtension);
            }
            Console.WriteLine("Outputting to:" + outputFile);

            var inputLines = inputFile.ReadLinesAsync();
            var outputLines = inputLines.Tokenize()
                .DedupRepeatedTokens()
                .SingleCommandPerLine()
                .Augment()
                .ConvertArcRadiusToCenter()
                .DedupLinearToArc(0.005M)
                .Clip()
                .DedupRepeatedTokens()
                .DedupLine()
                .DedupLinear(0.0005M)
                .DedupLinear(0.0005M)
                .DedupLinear(0.0005M)
                .DedupLinear(0.0005M)
                //.Annotate()
                .DedupSelectTokens(new List<char> { 'F', 'Z' })
                //.DedupTokens()
                .JoinTokens();
            var lineCount = outputFile.WriteLinesAsync(outputLines);

            await foreach (var line in lineCount)
            {
                Console.WriteLine(line);
            }
        }

        static void RunOptions(Options opts)
        {
            //handle options
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }
    }
}
