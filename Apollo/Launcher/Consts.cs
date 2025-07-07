//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! Consts to be shared in the project
//
//! Author:     Alan MacAree
//! Created:    07 Oct 2022
//----------------------------------------------------------------------

namespace Launcher
{
    public static class Consts
    {
        /// <summary>
        /// The Pre login action
        /// </summary>
        public const string c_preLoginLogAction = "PreLoginLog";

        /// <summary>
        /// The standard timeout for API calls
        /// </summary>
        public const int c_apiCallTimeOutInMS = 2000;

        /// <summary>
        /// The Steam logo image location
        /// </summary>
        public const string c_steamLogoImage = "pack://application:,,,/Images/SteamLogo.png";

        /// <summary>
        /// The Epic logo image location
        /// </summary>
        public const string c_epicLogoImage = "pack://application:,,,/Images/EpicLogo.png";

        /// <summary>
        /// Defines a not found value as an int.
        /// </summary>
        public const int c_NotFound = -1;
    }
}
