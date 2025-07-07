using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;

using ClientSupport;

namespace FORCServerSupport
{
    class FORCAuthorisationManager
    {
        public const String c_emailQuery = "email";
        public const String c_passwordQuery = "password";
        public const String c_securityBlobName = "encCode";
        public const String c_steamTicket = "steamTicket";

        /// <summary>
        /// Blob used to validate the second factor code required to obtain an
        /// authentication token.
        /// </summary>
        private String m_securityBlob = null;

        private TokenStorage m_token;
        private FORCServerState m_state;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="state">Server state shared with the connection</param>
        public FORCAuthorisationManager(FORCServerState state)
        {
            m_state = state;
            m_token = new TokenStorage();
        }

        /// <summary>
        /// The real server is already logged in if we have an existing token.
        /// </summary>
        /// <returns></returns>
        public bool IsLoggedIn(UserDetails user)
        {
            return user.SessionToken != null;
        }

        /// <summary>
        /// Test whether the user has provided a valid username/password pair
        /// meaning we are now waiting for a second factor code.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private bool HaveLogin(UserDetails user)
        {
            return m_securityBlob != null;
        }

        public ServerInterface.AuthorisationResult GetAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime)
        {
            // The actual server uses two different phases to the login rather
            // than a single gate with multiple responses.
            // Since the client side offers a simpler interface and greater
            // flexibility it is being retained at least until there is a good
            // reason for changing it so this method acts as a translator
            // between what the client expects and what the server expects.

            if (HaveExistingToken(user))
            {
                // Prior to the new flow we could not get here and have a null
                // username/password as AutoLogin would have already rejected
                // the attempt. Now however we do get this far because it is
                // valid to have no username/password but still be able to log
                // in via Steam. In the manual login route although the user can
                // provide a blank username/password they take the form of empty
                // strings not null making that route safe.
                if ((user.Password!=null) && (user.EmailAddress!=null))
                {
                    // Token meaning has changed, it is now a machine token which
                    // is stored locally and used to obtain a session token.
                    // Both the session token and the machine token are required
                    // to make requests of the server.
                    ServerInterface.AuthorisationResult result = RequestSessionToken(user, machine);
                    if ((result == ServerInterface.AuthorisationResult.Authorised) || HaveExistingToken(user))
                    {
                        // If we were authorised, or we failed to authorise, but
                        // still have a valid token then return the result we were
                        // just given. If our token was rejected however continue
                        // as if we did not have one in the first place.
                        user.AuthenticationType = ServerInterface.AuthenticationType.FORC;
                        return result;
                    }
                }
            }

            if (user.Automatic)
            {
                if (user.EpicClientId == null && user.SteamID != UserDetails.InvalidSteamID)
                {
                    ServerInterface.AuthorisationResult result = RequestSteamAuthentication(user, machine);
                    if (result == ServerInterface.AuthorisationResult.Authorised)
                    {
                        user.SteamLinkLink = null;
                        user.SteamRegistrationLink = null;
                        user.AuthenticationType = ServerInterface.AuthenticationType.Steam;
                    }
                    return result;
                } else if (user.EpicClientId != null)
                {

                    ServerInterface.AuthorisationResult result = RequestEpicAuthentication(user, machine);
                    if (result == ServerInterface.AuthorisationResult.Authorised)
                    {
                        user.SteamLinkLink = null;
                        user.SteamRegistrationLink = null;
                        user.AuthenticationType = ServerInterface.AuthenticationType.Epic;
                    }
                    return result;
                }

                return ServerInterface.AuthorisationResult.Denied;
            }

            if (!HaveLogin(user))
            {
                return AttemptLogin(user, machine);
            }

            return AttemptAquireToken(user, machine);
        }

        public ServerInterface.AuthorisationResult UseAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime, NameValueCollection external)
        {
            String sessionToken = external.Get(FORCServerState.c_authToken);
            String machineToken = external.Get(FORCServerState.c_machineToken);

            if ((sessionToken == null) || (machineToken == null))
            {
                return ServerInterface.AuthorisationResult.Failed;
            }
            user.AuthenticationToken = machineToken;
            user.SessionToken = sessionToken;
            // Trust what we were given.
            // This will not pose a major security risk since if they are
            // invalid the server will reject them when the game tries to use
            // them, it just means the launcher will think it is authenticated
            // when in fact it is not.
            return ServerInterface.AuthorisationResult.Authorised;
        }

        /// <summary>
        /// Check whether we already have a usable token, if so we are
        /// pre-authorised and do not need to talk to the server at all.
        /// 
        /// If pre-authorised the user details will be updated with the
        /// authorisation token.
        /// </summary>
        /// <param name="user">User details</param>
        /// <returns>True if the token has been set, false otherwise.</returns>
        private bool HaveExistingToken(UserDetails user)
        {
            if (m_token == null)
            {
                return false;
            }
            if (!String.IsNullOrEmpty(user.EmailAddress))
            {
                String userToken = m_token.GetTokenFor(user.EmailAddress);
                if (userToken != null)
                {
                    user.AuthenticationToken = userToken;
                    return true;
                }
            }
            return false;
        }

        private ServerInterface.AuthorisationResult RequestSteamAuthentication(UserDetails user, MachineIdentifierInterface machine)
        {
            Queries.RequestSteamSessionTokenQuery rst = new Queries.RequestSteamSessionTokenQuery();

            bool forbidden;
            ServerInterface.AuthorisationResult result = rst.Run(m_state, user, machine, out forbidden);
            if (forbidden)
            {
                // Request was rejected due to invalid credentials, e.g. a 
                // change in machine token. It is therefore necessary to start
                // from scratch since the user will never be able to log in
                // otherwise.
                m_securityBlob = null;
                if (user.EmailAddress!=null)
                {
                    m_token.DiscardToken(user.EmailAddress);
                }
            }
            return result;
        }

        private ServerInterface.AuthorisationResult RequestEpicAuthentication(UserDetails user, MachineIdentifierInterface machine)
        {
            Queries.CheckTokenQuery checkTokenQuery = new Queries.CheckTokenQuery();

            bool forbidden;
            ServerInterface.AuthorisationResult result = checkTokenQuery.Run(m_state, user, machine, out forbidden);
            if (forbidden)
            {
                m_securityBlob = null;
                if (user.EmailAddress != null)
                {
                    m_token.DiscardToken(user.EmailAddress);
                }
            }
            return result;
        }

        private ServerInterface.AuthorisationResult RequestSessionToken(UserDetails user, MachineIdentifierInterface machine)
        {
            Queries.RequestSessionTokenQuery rst = new Queries.RequestSessionTokenQuery();

            bool forbidden;
            ServerInterface.AuthorisationResult result = rst.Run(m_state, user, machine, out forbidden);
            if (forbidden)
            {
                // Request was rejected due to invalid credentials, e.g. a 
                // change in machine token. It is therefore necessary to start
                // from scratch since the user will never be able to log in
                // otherwise.
                m_securityBlob = null;
                m_token.DiscardToken(user.EmailAddress);
            }
            return result;
        }

        /// <summary>
        /// Attempt the first stage login using the provided username and
        /// password pair.
        /// </summary>
        /// <param name="user">Details of the user.</param>
        /// <param name="machine">Source for the machine identifier.</param>
        /// <returns>
        /// Denied (login failure) or RequiresSecondFactor (login successful)
        /// </returns>
        private ServerInterface.AuthorisationResult AttemptLogin(UserDetails user, MachineIdentifierInterface machine)
        {
            Queries.AttemptLoginQuery alq = new Queries.AttemptLoginQuery();

            String securityBlob = null;
            ServerInterface.AuthorisationResult result = alq.Run(this, m_state, user, machine, out securityBlob);
            if (!String.IsNullOrEmpty(securityBlob))
            {
                m_securityBlob = securityBlob;
            }
            return result;
        }

        /// <summary>
        /// Attempt to get a token from the server that can be used on
        /// subsequent communications.
        /// </summary>
        /// <param name="user">
        /// User details that will be filled out with authentication token on
        /// success.
        /// </param>
        /// <param name="machine">Source for the machine identifier.</param>
        /// <returns></returns>
        private ServerInterface.AuthorisationResult AttemptAquireToken(UserDetails user, MachineIdentifierInterface machine)
        {
            Queries.AttemptAcquireTokenQuery aatq = new Queries.AttemptAcquireTokenQuery();

            ServerInterface.AuthorisationResult result = aatq.Run(m_securityBlob, m_state, user, machine);
            switch (result)
            {
                case ServerInterface.AuthorisationResult.Denied:
                    {
                        m_securityBlob = null;
                        break;
                    }
                case ServerInterface.AuthorisationResult.Authorised:
                    {
                        m_token.SetTokenFor(user.EmailAddress, user.AuthenticationToken);
                        return RequestSessionToken(user, machine);
                    }
                default:
                    {
                        break;
                    }
            }
            return result;
        }

        /// <summary>
        /// Reset any existing login attempt.
        /// </summary>
        public void ResetLogin()
        {
            m_securityBlob = null;
        }

        public void LogoutUser(UserDetails details)
        {
            details.EmailAddress = null;
            details.Password = null;
            details.SessionToken = null;
            details.TwoFactor = null;
            details.RegisteredName = null;
            details.SteamSessionToken = null;
            details.SteamLinkLink = null;
            details.SteamRegistrationLink = null;
        }

        /// <summary>
        /// Logout the user and the machine from the server.
        /// </summary>
        /// <param name="details">The details of the user being logged out.</param>
        public void LogoutMachine(UserDetails details)
        {
            if (details!=null)
            {
                if (details.EmailAddress!=null)
                {
                    m_token.DiscardToken(details.EmailAddress);
                }
                LogoutUser(details);
                details.AuthenticationToken = null;
                m_securityBlob = null;
            }
        }
    }
}
