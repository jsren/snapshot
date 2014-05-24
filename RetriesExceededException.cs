/* RetriesExceededException.cs - (c) James S Renwick 2014
 * ------------------------------------------------------
 * Version 1.0.0
 */
using System;

namespace snapshot
{
    /// <summary>
    /// Exception thrown when the set number of retries for
    /// an operation has been exceeded.
    /// </summary>
    [Serializable]
    public class RetriesExceededException : Exception
    {
        
    }
}
