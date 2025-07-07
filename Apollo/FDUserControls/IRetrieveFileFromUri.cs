//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! IRetrieveFileFromUri, provides file Uri retrieve interface.
//
//! Author:     Alan MacAree
//! Created:    10 Oct 2022
//----------------------------------------------------------------------

namespace FDUserControls
{
    /// <summary>
    /// Provides an interface to retrieve files from a Uri
    /// </summary>
    public interface IRetrieveFileFromUri
    {
        /// <summary>
        /// The file specified by the _uri should be made available
        /// and the full path (inc filename) should be returned
        /// </summary>
        /// <param name="_uriString">The Uri string to the file</param>
        /// <returns>The full path to the download file, or null if not download</returns>
        string RetrieveFile( string _uriString );
    }
}
