/* Program.cs - (c) James S Renwick 2014
 * -------------------------------------
 * Version 1.0.1
 * 
 * Another quick and dirty CLI for creating
 * directory snapshots. Useful for projects.
 */
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace snapshot
{
    class Program
    {
        /// <summary>
        /// Writes an error message to stderr.
        /// </summary>
        /// <param name="message">The message to print.</param>
        static void errorOut(string message)
        {
            Console.Error.WriteLine("[ERROR] " + message);
            Environment.Exit(-1);
        }

        static void printUsage()
        {
            Console.WriteLine(@"
Directory Snapshot Utility
=========================
Version 1.0 (c) James S Renwick 2014

usage:
    snapshot /?                       Prints this help message.
    snapshot <directory> [<options>]  Creates a snapshot of the given directory.
    snapshot /L <directory>           Lists the snapshots in the given folder.
                                      

options:
    /C       Logs a comment with the snapshot.
    /F       Overwrites a previous snapshot made on the same date.
    /E       A .NET regular expression matching files to exclude from the
             snapshot.

");
        }


        static void Main(string[] args)
        {
            // Output usage info
            if (args.Length == 0 || args.Contains("/?"))
            {
                printUsage();
            }
            // Output snapshots in given folder
            else if (args[0] == "/L")
            {
                if (args.Length == 1)
                {
                    errorOut("Expected parameter for option '/L'");
                }
                else PrintSnapshotList(args[1]);
            }
            // Create a new snapshot
            else
            {
                string directory = null;
                string comment   = null;
                bool   overwrite = false;
                Regex  exclude   = null;

                // Read directory path
                if (args[0].StartsWith("/"))
                {
                    errorOut("Invalid parameter at {0} - expected valid directory path");
                }
                else directory = args[0];

                // Read optional arguments
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "/C")
                    {
                        if (args.Length == i + 1) {
                            errorOut("Expected parameter for option '/C'");
                        }
                        else comment = args[++i];
                    }
                    else if (args[i] == "/F")
                    {
                        overwrite = true;
                    }
                    else if (args[i] == "/E")
                    {
                        if (args.Length == i + 1) {
                            errorOut("Expected parameter for option '/E'");
                        }
                        else exclude = new Regex(args[++i]);
                    }
                    else errorOut("Unexpected parameter: " + args[i]);
                }
                // Take snapshot
                try
                {
                    SnapshotProvider.CreateSnapshot(directory, new Version(1, 0), 
                        comment, exclude, overwrite);
                }
                catch (Exception e)
                {
                    errorOut(e.Message);
                }
            }
        }

        static void PrintSnapshotList(string directory)
        {
            Console.WriteLine("Existing Snapshots:\n");
            foreach (Snapshot snap in SnapshotProvider.EnumerateSnapshots(directory))
            {
                Console.WriteLine("   snapshot {0} ({1} files) - \"{2}\"", 
                    snap.CreationDate.ToShortDateString(), snap.FileCount, snap.Comment);
            }
        }
    }
}
