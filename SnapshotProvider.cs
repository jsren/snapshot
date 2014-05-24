/* SnapshotProvider.cs - (c) James S Renwick 2014
 * ----------------------------------------------
 * Version 1.0.2
 * 
 * Contains the logic necessary to list and create
 * directory snapshots - datestamped compressed (zip)
 * backups.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace snapshot
{
    /// <summary>
    /// Class providing snapshot functionality.
    /// </summary>
    public static class SnapshotProvider
    {
        /// <summary>
        /// The number of retries before failing when copying files.
        /// </summary>
        const int RETRIES = 5;

        /// <summary>
        /// The regular expression used to detect snapshot zipped folders.
        /// </summary>
        static readonly Regex filenameRegex = new Regex(@"snapshot (\d+)-(\d+)-(\d+).*");


        /// <summary>
        /// Gets the relative path with the given base directory. If the base directory
        /// is not a parent directory of the given path, returns the absolute path.
        /// </summary>
        /// <param name="path">The path to make relative.</param>
        /// <param name="baseDirectory">The path to which to make the path relative.</param>
        /// <returns>A relative path if a valid base or the absolute path otherwise.</returns>
        private static string GetRelativePath(string path, string baseDirectory)
        {
            path          = Path.GetFullPath(path);
            baseDirectory = Path.GetFullPath(baseDirectory);

            if (path.StartsWith(baseDirectory))
            {
                path = path.Remove(0, baseDirectory.Length).Trim('\\');
            }
            return path;
        }

        /// <summary>
        /// Returns an enumerable collection of snapshots in the given directory.
        /// </summary>
        /// <param name="directory">The directory in which to search for snapshots.</param>
        /// <returns>An enumerable collection of snapshots.</returns>
        public static IEnumerable<Snapshot> EnumerateSnapshots(string directory)
        {
            if (directory == null)            throw new ArgumentNullException("directory");
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException();
            
            List<Snapshot> output = new List<Snapshot>();

            foreach (string filename in Directory.EnumerateFiles(directory, "*.zip"))
            {
                Version  version;
                DateTime date;
                int      files;
                string   comment;

                try
                {
                    /* Match the filename to ensure a valid snapshot and to read the date. */
                    Match nameMatch = 
                        filenameRegex.Match(Path.GetFileNameWithoutExtension(filename));

                    if (!nameMatch.Success)
                    {
                        continue;
                    }
                    else
                    {
                        date = new DateTime(
                            int.Parse(nameMatch.Groups[1].Value), 
                            int.Parse(nameMatch.Groups[2].Value), 
                            int.Parse(nameMatch.Groups[3].Value)
                        );
                    }

                    /* Open the archive to test validity and attempt to read the version. */
                    using (ZipArchive zip = new ZipArchive(File.OpenRead(filename)))
                    {
                        ZipArchiveEntry versionEntry = zip.GetEntry("\\");

                        using (var reader = new BinaryReader(versionEntry.Open()))
                        {
                            version = new Version(reader.ReadString());
                            comment = reader.ReadString();
                        }

                        /* NOTE: FILE COUNT IS SUBTRACTED BY ONE FOR THE VERSION FILE */
                        files = zip.Entries.Count((entry) => (entry.Name != "" || entry.Length != 0)) - 1;
                    }

                    /* Add the snapshot entry. */
                    output.Add(new Snapshot(filename, date, version, files, comment));
                }
                catch { }
            }
            // Return result
            return output.AsReadOnly();
        }


        /// <summary>
        /// Creates a new snapshot.
        /// </summary>
        /// <param name="directory">The directory for which to create a snapshot.</param>
        /// <param name="version">The version number of the snapshot.</param>
        /// <param name="comment">A comment to add to the snapshot.</param>
        /// <param name="excludePattern">A regex with which to filter added files.</param>
        /// <param name="overwrite">When true, overwrites a previous snapshot made on the same date.</param>
        public static void CreateSnapshot(string directory, Version version, string comment, Regex excludePattern, bool overwrite)
        {
            DateTime now = DateTime.Now;

            // Gets the initial filepath to create
            string filepath = Path.Combine(directory, String.Format("snapshot {0}-{1}-{2}.zip", 
                now.Year.ToString("D4"), now.Month.ToString("D2"), now.Day.ToString("D2")));


            /* If the snapshot already exists, proceed as appropriate. */
            bool exists = File.Exists(filepath);

            // If not overwrite, append a version number.
            if (exists && !overwrite)
            {
                int index = 2;

                while ((exists = File.Exists(filepath)) && index < int.MaxValue)
                {
                    filepath = Path.Combine(directory, String.Format("snapshot {0}-{1}-{2} {3}.zip", 
                        now.Year.ToString("D4"), now.Month.ToString("D2"), now.Day.ToString("D2"), index.ToString()));
                    index++;
                }
                if (exists) throw new InvalidOperationException("Maximum number of snapshots exceeded");
            }

            // Holds the list of files to copy
            var filelist = new List<string>();


            /* Get a list of all directories, including the root */
            List<string> dirs = new List<string>(Directory.EnumerateDirectories(
                directory, "*", SearchOption.AllDirectories))
            {
                directory
            };

            foreach (string dir in dirs)
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    // Skip other snapshots
                    if (filenameRegex.IsMatch(file)) continue;

                    // Check if the file matches an exclusion regex
                    if (excludePattern != null && excludePattern.IsMatch(Path.GetFileName(file)))
                    {
                        continue;
                    }
                    // Otherwise, add to the file list
                    filelist.Add(file);
                }
            }

            /* If it already exists and we're overwriting, empty the existing one. */
            Stream fs = File.OpenWrite(filepath);
            if (exists && overwrite)
            {
                fs.SetLength(0);
            }

            /* Now create the empty archive and copy the files & directories */
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                /* First add directories */
                foreach (string dir in dirs)
                {
                    if (dir == directory) continue;
                    zip.CreateEntry(GetRelativePath(dir, directory) + '\\');
                }
                /* Now add files */
                int attemptNo = 0;
                while (attemptNo++ != RETRIES)
                {
                    for (int i = 0; i < filelist.Count; i++)
                    {
                        try
                        {
                            /* Create new entry & copy file data */
                            zip.CreateEntryFromFile(filelist[i], GetRelativePath(filelist[i], directory));
                            filelist.RemoveAt(i--);
                        }
                        /* If file is locked, skip for now */
                        catch (UnauthorizedAccessException) { }
                    }
                    /* If we're retrying, wait a second first. */
                    if (filelist.Any()) System.Threading.Thread.Sleep(1000);
                    else break;
                }
                /* Throw an exception if copying failed */
                if (filelist.Any()) throw new RetriesExceededException();

                /* Otherwise, write version info & comment */
                ZipArchiveEntry versionEntry = zip.CreateEntry("\\");
                using (BinaryWriter writer = new BinaryWriter(versionEntry.Open()))
                {
                    writer.Write(version.ToString());
                    writer.Write(comment ?? "");
                }
            }
        }
    }
}
