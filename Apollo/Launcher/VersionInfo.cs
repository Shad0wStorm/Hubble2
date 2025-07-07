//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! VersionInfo, copied from the CobraBay project to provide the same
//!     approach to version numbers
//
//! Author:     Alan MacAree
//! Created:    21 Sept 2022
//----------------------------------------------------------------------

using System.Reflection;

#if DEVELOPMENT
#if (false)
[assembly: AssemblyVersion("1.0.9999.01")]
#else
[assembly: AssemblyVersion("1.0.9999.01")]
#endif
#else
#if (false)
[assembly: AssemblyVersion("1.0.9999.01")]
#else
[assembly: AssemblyVersion( "1.0.9999.01" )]
#endif
#endif
