using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Static methods for performing operations on paths that are not provided
    /// by the standard System.IO.Path library.
    /// </summary>
    public class PathExtensions
    {
        /// <summary>
        /// Find a path relative to the start directory, or an ancestor of the
        /// start directory.
        /// 
        /// The target directory may contain multiple path elements and may
        /// be a file or directory.
        /// 
        /// This is useful for finding a common ancestor when the initial
        /// reference point is not known.
        /// 
        /// The search starts at the start path and tests each parent folder
        /// until either the target path is found or the root is found.
        /// </summary>
        /// <example>
        /// A target <code>D/E<code> is searched for starting at
        /// <code>A/B/C/F/G</code>. The paths
        /// <code>
        /// A/B/C/F/G/D/E
        /// A/B/C/F/D/E
        /// A/B/C/D/E
        /// A/B/D/E
        /// A/D/E
        /// </code>
        /// are considered in order. The first path that corresponds to a real
        /// file or directory on disk is returned. If there is a file
        /// <code>A/B/C/D/E</code> for example then the function will return
        /// that directory.
        /// </example>
        /// <param name="target">The relative path to search for.</param>
        /// <param name="start">The start point for the search.</param>
        /// <returns>
        /// Full path to the directory containing the target or null if the
        /// target is not found.
        /// </returns>
        public static String FindLocalDirectoryEntry(String target, String start)
        {
            String testDirectory = start;
            if (String.IsNullOrEmpty(testDirectory))
            {
                testDirectory = Directory.GetCurrentDirectory();
            }

            while (!String.IsNullOrEmpty(testDirectory))
            {
                String testPath = Path.Combine(testDirectory, target);
                if (File.Exists(testPath))
                {
                    return testPath;
                }
                if (Directory.Exists(testPath))
                {
                    return testPath;
                }
                testDirectory = Path.GetDirectoryName(testDirectory);
            }

            return null;
        }

    }
}
