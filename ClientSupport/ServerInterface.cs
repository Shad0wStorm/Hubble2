using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Interface through which the client communicates with the server
    /// providing store, installers and authentication services.
    /// </summary>
    public abstract class ServerInterface : LogServer
    {
        /// <summary>
        /// Result of an authentication attempt.
        /// </summary>
        public enum AuthorisationResult
        {
            Denied, // User authentication was rejected.
            RequiresSecondFactor, // User authentication was rejected, but the user has been sent a code to use to allow authentication.
            Authorised, // User has been authorised.
            Failed // Attempt failed externally, for example due to network issues.
        };

        /// <summary>
        /// Determine the method used to determine authorisation.
        /// </summary>
        public enum AuthenticationType
        {
            /// <summary>
            /// Authorised directly via FORC.
            /// </summary>
            FORC,
            /// <summary>
            /// Authorised by FORC based on Steam Identity not FORC identity.
            /// </summary>
            Steam,

            /// <summary>
            /// Authorised by FORC based on Epic Identity not FORC identity.
            /// </summary>
            Epic
        }

        /// <summary>
        /// Does the server think the user is already logged in?
        /// </summary>
        /// <returns>True if the user is logged in, false otherwise.</returns>
        public abstract bool IsLoggedIn(UserDetails user);

        /// <summary>
        /// Returns the product server URL to use to allow users to
        /// purchase items
        /// </summary>
        /// <returns>The product server URL</returns>
        public abstract string GetProductServer();

        /// <summary>
        /// Return a timestamp from the server. This is passed back to the
        /// server during authorisation to ensure that the request has been
        /// made recently. Clients do not interpret the value they simply pass
        /// it back unchanged so the server can use any representation it
        /// chooses.
        /// </summary>
        /// <returns></returns>
        public abstract String GetTimeStamp();

        /// <summary>
        /// Return the current time adjusting for any offset between the
        /// local machine and the server.
        /// </summary>
        /// <returns>Time string</returns>
        public abstract String GetCurrentTimeStamp();

        /// <summary>
        /// Enum representing the status of the current application version.
        /// </summary>
        public enum VersionStatus
        {
            Current,    // Version passed is most recent version.
            Supported,  // Newer version available, but this version will continue to work.
            Expired,    // This version is no longer supported, upgrade required.
            Future      // This version is newer than the most recent version.
        }

        /// <summary>
        /// Check the version of the client against the most recent version
        /// known to the server.
        /// </summary>
        /// <param name="version">Version of this client.</param>
        /// <param name="current">String indicating the most recent version known.</param>
        /// <returns>Value indicating the status of the version.</returns>
        public abstract VersionStatus CheckClientVersion(String version, out String current);

        /// <summary>
        /// Return the download manager to handle downloading files from the server.
        /// </summary>
        /// <returns>Download manager instance</returns>
        public abstract DownloadManagerBase GetDownloadManager();

        /// <summary>
        /// Return the root path for the server. This may be useful for
        /// requests that are not made through the server interface for
        /// secondary features that could be provided by other servers.
        /// </summary>
        /// <returns></returns>
        public abstract String GetServerPath(String key);

        /// <summary>
        /// Request authorisation from the server based on the provided user
        /// details, machine identifier and a time previously returned from
        /// GetTimeStamp.
        /// </summary>
        /// <param name="user">
        /// User details for the user attempting to log in. The email and
        /// password fields are used to authenticate, along with the two factor
        /// code if provided. If successfully authorised (method returns
        /// AuthorisationResult.Authorised) then the user details
        /// AuthenticationToken will be updated with a value provided by the
        /// ServerInterface.
        /// </param>
        /// <param name="machine">
        /// Source to use to obtain an identifier for the current machine. If
        /// the server does not recognise the identifier it may request a two
        /// factor authentication token before authorising the user.
        /// </param>
        /// <param name="serverTime">
        /// The current server time as returned previously by GetTimeStamp().
        /// </param>
        /// <returns></returns>
        public abstract AuthorisationResult GetAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime);

        /// <summary>
        /// Use (and potentially validate) externally provided authorisation tokens.
        /// </summary>
        /// <param name="user">As for GetAuthorisation.</param>
        /// <param name="machine">As for GetAuthorisation.</param>
        /// <param name="serverTime">As for GetAuthorisation.</param>
        /// <param name="external">Collection of properties representing the
        /// externally provided authorisation information.
        /// </param>
        /// <returns></returns>
        public abstract AuthorisationResult UseAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime, NameValueCollection external);

        /// <summary>
        /// Return the list of projects the user is authorised to access.
        /// </summary>
        /// <param name="user">
        /// The AuthenticationToken for the user is used to validate with the
        /// server.
        /// </param>
        /// <param name="serverTime">
        /// Time previously retrieved via GetTimeStamp() (unused).
        /// </param>
        /// <returns>
        /// A list of project (safe) names that the user is allowed to access.
        /// </returns>
        public abstract String[] GetProjectList(UserDetails user, String serverTime);

        /// <summary>
        /// Return a more user friendly name associated with a project SKU.
        /// </summary>
        /// <param name="project">The SKU</param>
        /// <returns>A title for the SKU.</returns>
        public abstract String GetTitleForProject(String project);

        /// <summary>
        /// Return a string used to order the projects.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public abstract String GetSortKeyForProject(String project);

        /// <summary>
        /// Return the directory name used to store the project.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public abstract String GetDirectoryForProject(String project);

        /// <summary>
        /// Return the project highlight colour.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public abstract String GetHighlightColourForProject(String project);

        /// <summary>
        /// Return the project title page.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public abstract String GetTitlePageForProject(String project);

        /// <summary>
        /// Return a list of filters corresponding to the project.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public abstract String[] GetFiltersForProject(String project);

        /// <summary>
        /// Returns the projects Box Image URI
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects box image URI</returns>
        public abstract String GetBoxImageURIForProject( String project );

        /// <summary>
        /// Returns the projects Hero Image URI
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects hero image URI</returns>
        public abstract String GetHeroImageURIForProject( String project );

        /// <summary>
        /// Returns the project's logo Image URI
        /// </summary>
        /// <param name="_project">The project to get the logo image URI for</param>
        /// <returns>The projects logo image URI, this can be null</returns>
        public abstract String GetLogoImageURIForProject( String _project );

        /// <summary>
        /// Returns the project's no_details setting
        /// </summary>
        /// <param name="_project">The project to get the no_details setting for</param>
        /// <returns>The project's no_details setting, it is true by default</returns>
        public abstract bool GetNoDetailsForProject(String _project);

        /// <summary>
        /// Returns the project's ESRB Rating
        /// </summary>
        /// <param name="_project">The project to get the ESRB Rating for</param>
        /// <returns>The project's ESRB Rating, this can be null</returns>
        public abstract String GetESRBRatingForProject( String _project );

        /// <summary>
        /// Returns the project's PEGI Rating
        /// </summary>
        /// <param name="_project">The project to get the PEGI Rating for</param>
        /// <returns>The project's PEGI Rating, this can be null</returns>
        public abstract String GetPEGIRatingForProject( String _project );

        /// <summary>
        /// Returns the projects game api.
        /// </summary>
        /// <param name="_project">The project to get the game api for</param>
        /// <returns>The projects game api, this can be null</returns>
        public abstract String GetGameApiForProject( String _project );

        /// <summary>
        /// Gets the game description as a json string
        /// </summary>
        /// <param name="_project">The project to get the information for</param>
        /// <returns>The game description as a json string, this can be null</returns>
        public abstract string GetGameDescription( Project _project );

        /// <summary>
        /// Returns the current server status for the given project
        /// </summary>
        /// <param name="_project">The project to get the server status for</param>
        /// <param name="_serverStatusText">An out parameter, the server status text in the current language, can be null</param>
        /// <param name="_serverMessage">An out parameter, server state message in the current language, can be null</param>
        /// <returns>An ID representing the server state, -1 = NOT OKAY, 0 = Maintenance, 1 = OK</returns>
        public abstract int GetServerStatus( Project _project, out string _serverStatusText, out string _serverMessage );

        /// <summary>
        /// Returns the projects game code (used in cms).
        /// </summary>
        /// <param name="_project">The project to get the game code for</param>
        /// <returns>The projects game code</returns>
        public abstract int GetGameCodeForProject( String _project );

        /// <summary>
        /// Return the server recommended number of threads to use for download.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public abstract int GetMaxDownloadThreadsForProject(String project);

        /// <summary>
        /// Return arguments that the server wants passed to a product when it
        /// is started, e.g. details of which server to connect too.
        /// 
        /// Modified to take the project for which arguments are requested
        /// so a more educated result can be returned.
        /// </summary>
        /// <returns></returns>
        public abstract String ServerExtraRunArguments(Project project);

        /// <summary>
        /// Return the content of the last response from the server.
        /// 
        /// This is typically used to present context relevant information to
        /// the user on failure or completion. For example a call to 
        /// GetAuthorisation may fail, after which GetLastContent may provide
        /// information as to why the attempt failed and what the user could
        /// try to fix it, such as where to look for a Two Factor
        /// authentication code set to the user via a different method.
        /// </summary>
        /// <returns>Text of the message.</returns>
        public abstract String GetLastContent();

        /// <summary>
        /// Returns a last error code
        /// </summary>
        /// <returns>-The last known error code, or -1 if no error code exists</returns>
        public abstract int GetLastErrorCode();

        /// <summary>
        /// The manager is intending to start a new login process to discard
        /// any state the server connection is holding regarding the current
        /// login.
        /// </summary>
        public abstract void ResetLogin();

        /// <summary>
        /// Logout the user from the server.
        /// </summary>
        /// <param name="details">The user being logged out.</param>
        public abstract void LogoutUser(UserDetails details);

        /// <summary>
        /// Logout the user and the machine from the server.
        /// </summary>
        /// <param name="details">The details of the user being logged out.</param>
        public abstract void LogoutMachine(UserDetails details);

        /// <summary>
        /// Gets the featured products as a Json string
        /// </summary>
        /// <param name="_project">The project to get the information for</param>
        /// <returns>The featured products as a json string.</returns>
        public abstract string GetFeaturedProducts( Project _project );

        /// <summary>
        /// Creates a new FD account
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_firstName">The users first name</param>
        /// <param name="_lastName">The users last name</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_passwordConfirm">The users conformation password</param>
        /// <param name="_newsAndPromoSignUp">Is the user signing-up for news and promotions</param>
        /// <returns>A JSONWebPostResult containing the result of the call, this can be null</returns>
        public abstract JSONWebPutsAndPostsResult CreateFrontierAccount( UserDetails _userDetails, 
                                                                         string _firstName,
                                                                         string _lastName,
                                                                         string _email,
                                                                         string _password,
                                                                         string _passwordConfirm,
                                                                         bool _newsAndPromoSignUp );

        /// <summary>
        /// Confirms a new FD account by passing a verification code to the server.
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_otp">The otp (verification) code</param>
        /// <param name="_machineId">The id of this machine</param>
        /// <returns>A JSONWebPostResult containing the result of the call, this can be null</returns>
        public abstract JSONWebPutsAndPostsResult ConfirmFrontierAccount( UserDetails _userDetails, 
                                                                          string _email,
                                                                          string _password,
                                                                          string _otp,
                                                                          string _machineId );

        /// <summary>
        /// Checks the password to see if it meets the password complexity rules.
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_password">The users password</param>
        /// <returns>A JSONWebPostResult containing the result of the call, this can be null</returns>
        public abstract JSONWebPutsAndPostsResult CheckPasswordComplexity( UserDetails _userDetails, 
                                                                           string _password );

        /// <summary>
        /// Attempts to link a store (e.g. Steam, Epic) account to a Frontier account.
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_storeAuthorisation">The stores authorisations code</param>
        /// <param name="_storeClientId">The stores client Id</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_overRideAnyExistingLinks">Should existing links be ignored? if not then
        /// the call can produce a HttpStatusCode.PreconditionFailed, meaning linked to
        /// another account..</param>
        /// <returns>JSONWebPutsAndPostsResult containing the result of the call, this can be null</returns>
        public abstract JSONWebPutsAndPostsResult LinkAccounts( UserDetails _userDetails,
                                                                string _storeAuthorisation,
                                                                string _storeClientId,
                                                                string _email,
                                                                string _password,
                                                                bool _overRideAnyExistingLinks );

        /// <summary>
        /// Attempts to remove a link between a store and a Frontier account
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_storeAuthorisation">The stores authorisations code</param>
        /// <param name="_storeClientId">The stores client Id</param>
        /// <returns>JSONWebPutsAndPostsResult containing the result of the call, this can be null</returns>
        public abstract JSONWebPutsAndPostsResult RemoveLinkAccounts( UserDetails _userDetails, 
                                                                      string _storeAuthorisation,
                                                                      string _storeClientId );

        /// <summary>
        /// Redeems a game code for the current user.
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_machine">The MachineIdentifierInterface</param>
        /// <param name="_code">The game code to redeem</param>
        /// <returns>JSONWebPutsAndPostsResult containing the result of the call, this can be null</returns>
        public abstract JSONWebPutsAndPostsResult RedeemCode( UserDetails _userDetails,
                                                              MachineIdentifierInterface _machine,
                                                              string _code );

    }
}
