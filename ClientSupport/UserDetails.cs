using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Details for the logged in user, or a user attempting to log in.
    /// </summary>
    public class UserDetails
    {
        /// <summary>
        /// Used to indicate Steam will not be used for authentication.
        /// </summary>
        public const UInt64 InvalidSteamID = 0;

        /// <summary>
        /// Email address associated with the account.
        /// </summary>
        public String EmailAddress;
        /// <summary>
        /// Password for the account.
        /// </summary>
        public String Password;
        /// <summary>
        ///  Is the user attempting an automatic login?
        /// </summary>
        public bool Automatic;
        /// <summary>
        /// Two factor code, or null if no code has been provided.
        /// </summary>
        public String TwoFactor;
        /// <summary>
        /// Filled in by the server when a user is authenticated successfully.
        /// This is a machine token and is provided when the user has
        /// authorised a specific machine ID. This is then used in conjunction
        /// with the username and password to obtain a session token.
        /// </summary>
        public String AuthenticationToken;
        /// <summary>
        /// Determine how Authentication was established.
        /// </summary>
        public ServerInterface.AuthenticationType AuthenticationType = ServerInterface.AuthenticationType.FORC;
        /// <summary>
        /// Token used for a single session (run) of the client.
        /// </summary>
        public String SessionToken;
        /// <summary>
        /// Name registered on the store.
        /// </summary>
        public String RegisteredName;

        /// <summary>
        /// Steam User Identifier
        /// </summary>
        public UInt64 SteamID = InvalidSteamID;
        /// <summary>
        /// Steam Session Token
        /// </summary>
        public String SteamSessionToken;
        /// <summary>
        /// Link to open on registration page for new Steam users.
        /// </summary>
        public String SteamRegistrationLink;
		/// <summary>
		/// Link to open on Linking page for existing Steam users.
		/// </summary>
		public String SteamLinkLink;
		/// <summary>
		/// Set when communication with Steam service fails.
		/// </summary>
		public bool SteamUnavailable = false;

        public String EpicClientId;

        public String EpicAccessToken;

        public String DisplayName
        {
            get
            {
                if (AuthenticationType==ServerInterface.AuthenticationType.FORC)
                {
                    return EmailAddress;
                }
                if (AuthenticationType == ServerInterface.AuthenticationType.Steam)
                {
                    if (!String.IsNullOrEmpty(RegisteredName))
                    {
                        return RegisteredName;
                    }
                    else
                    {
                        return "Steam User";
                    }
                }
                return "Unknown";
            }
        }
    }
}
