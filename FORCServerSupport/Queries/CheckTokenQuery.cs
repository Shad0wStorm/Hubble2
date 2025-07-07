using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;

using ClientSupport;

namespace FORCServerSupport.Queries
{
    class CheckTokenQuery : JSONWebQuery
    {

        public CheckTokenQuery() 
            : base("user/forctoken")
        {
        }

        public ServerInterface.AuthorisationResult Run(FORCServerState state, UserDetails user,
                MachineIdentifierInterface machine, out bool forbidden)
        {
            forbidden = false;
            String authHeader = "epic " + user.EpicAccessToken;
            AddHeader(FORCServerState.c_headerAuthToken, authHeader);
            AddParameter(FORCServerState.c_machineIdQuery, machine.GetMachineIdentifier());

            Dictionary<String, object> loginResponse;

            bool c_followRedirect = false;
            LogEntry entry = new LogEntry("RequestingEpicAuthentication");
            state.m_message = null;
            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V3_0),
                out loginResponse, out state.m_message, c_followRedirect);

            ServerInterface.AuthorisationResult result = ServerInterface.AuthorisationResult.Denied;

            switch (response)
            {
                case HttpStatusCode.OK:
                    {

                        if (loginResponse != null && loginResponse.ContainsKey(FORCServerState.c_authToken))
                        {
                            try
                            {
                                user.SessionToken = loginResponse[FORCServerState.c_authToken] as String;
                                user.AuthenticationToken = loginResponse[FORCServerState.c_machineToken] as String;
                                if (loginResponse.ContainsKey(FORCServerState.c_registeredName))
                                {
                                    user.RegisteredName = loginResponse[FORCServerState.c_registeredName] as String;
                                }
                                else
                                {
                                    user.RegisteredName = "Epic User";
                                }
                                result = ServerInterface.AuthorisationResult.Authorised;
                                entry = new LogEntry("EpicAuthenticated");
                                state.m_manager.Log(entry);
                            }
                            catch (KeyNotFoundException ex)
                            {
                                entry = new LogEntry("ParsingSuccessfulResponse");
                                entry.AddValue("KeyNotFoundException", ex.Message);
                                state.m_manager.Log(entry);
                                state.m_message = ex.Message;
                                return ServerInterface.AuthorisationResult.Denied;
                            }
                        }
                        else
                        {
                            entry = new LogEntry("MissingAuthToken");
                            state.m_manager.Log(entry);
                        }

                        if (result != ServerInterface.AuthorisationResult.Denied)
                        {
                            return result;
                        }

                        if (state.m_message == null)
                        {
                            state.m_message = LocalResources.Properties.Resources.FSSRSTQ_InvalidServerResponse;
                            state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                            state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                            entry = new LogEntry("InvalidServerResponse");
                            entry.AddValue("Error", GetResponseText(loginResponse, FORCServerState.c_errorenum, ""));
                            entry.AddValue("Message", GetResponseText(loginResponse, FORCServerState.c_message, ""));
                            state.m_manager.Log(entry);
                        }

                        break;
                    }
                case HttpStatusCode.Unauthorized:
                    {
                        state.m_message = LocalResources.Properties.Resources.Unauthorised;
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        break;
                    }
                case HttpStatusCode.RedirectKeepVerb:
                    {
                        // We aren't yet linked, we need to make sure we set the account linking URL and then we're unauthorized
                        if (loginResponse.ContainsKey("Location"))
                        {
                            user.SteamLinkLink = loginResponse["Location"] as String;
                            if (user.SteamLinkLink.Contains("?"))
                            {
                                user.SteamLinkLink += "&";
                            } else
                            {
                                user.SteamLinkLink += "?";
                            }
                            user.SteamLinkLink += "lang=" + state.m_language;
                        }
                        break;
                    }
                case HttpStatusCode.BadRequest:
                    {
                        user.AuthenticationToken = null;
                        user.SessionToken = null;
                        forbidden = true;
                        state.m_message = LocalResources.Properties.Resources.FSSRSTQ_Forbidden;
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        break;
                    }

                case HttpStatusCode.Forbidden:
                    {
                        result = ServerInterface.AuthorisationResult.Denied;
                        user.SteamLinkLink = "http://user-staging.frontierstore.net/epic/register/" + user.EpicAccessToken;
                        break;
                    }
                default:
                    {
                        state.m_message = String.Format(LocalResources.Properties.Resources.FSSRSTQ_ServerResponseX,
                            response);
                        break;
                    }
            }
            return result;
        }
    }
}