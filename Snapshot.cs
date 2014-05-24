/* Snapshot.cs - (c) James S Renwick 2014
 * --------------------------------------
 * Version 1.0.0
 */
using System;

namespace snapshot
{
    /// <summary>
    /// Object describing a compressed file containing a 
    /// previous snapshot of the contents of a directory.
    /// </summary>
    public sealed class Snapshot
    {
        /// <summary>
        /// The path to the snapshot.
        /// </summary>
        public string Path { get; private set; }
        /// <summary>
        /// The version of the snapshot format.
        /// </summary>
        public Version Version { get; private set; }
        /// <summary>
        /// A user-defined comment.
        /// </summary>
        public string Comment { get; private set; }
        /// <summary>
        /// The date on which the snapshot was made.
        /// </summary>
        public DateTime CreationDate { get; private set; }
        /// <summary>
        /// The number of files in the snapshot.
        /// </summary>
        public int FileCount { get; private set; }


        /// <summary>
        /// Creates a new snapshot object. 
        /// This object merely describes an existing snapshot.
        /// </summary>
        /// <param name="path">The path to the snapshot file.</param>
        /// <param name="creation">The date on which the snapshot was made.</param>
        /// <param name="version">The version of the snapshot format.</param>
        /// <param name="fileCount">The number of files in the snapshot.</param>
        /// <param name="comment">A user-defined comment.</param>
        public Snapshot(string path, DateTime creation, Version version, int fileCount, string comment)
        {
            this.Path         = path;
            this.Version      = version;
            this.Comment      = comment;
            this.CreationDate = creation;
            this.FileCount    = fileCount;
        }
    }
}
