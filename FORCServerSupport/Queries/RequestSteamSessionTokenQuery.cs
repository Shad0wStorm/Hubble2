using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using ClientSupport;


namespace FORCServerSupport.Queries
{
    class RequestSteamSessionTokenQuery : JSONWebQuery
    {
        public RequestSteamSessionTokenQuery()
            : base("user/steam/auth",true)
        {

        }

        public ServerInterface.AuthorisationResult Run(FORCServerState state, UserDetails user,
            MachineIdentifierInterface machine, out bool forbidden)
        {
            forbidden = false;
            user.SteamUnavailable = false;
            AddParameter(FORCAuthorisationManager.c_steamTicket,user.SteamSessionToken);
            AddParameter(FORCServerState.c_machineIdQuery, machine.GetMachineIdentifier());
            state.AddTimeStamp(this);

            Dictionary<String, object> loginResponse;

            state.m_message = null;

            LogEntry entry = new LogEntry("RequestingSteamAuthentication");
            state.m_manager.Log(entry);

            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V3_0),
                out loginResponse, out state.m_message,false);

            ServerInterface.AuthorisationResult result = ServerInterface.AuthorisationResult.Denied;

            // Local Steam Emulation
            // response = HttpStatusCode.ServiceUnavailable;

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        if (loginResponse.ContainsKey(FORCServerState.c_authToken))
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
                                    user.RegisteredName = "Steam User";
                                }
                                result = ServerInterface.AuthorisationResult.Authorised;
                                entry = new LogEntry("SteamAuthenticated");
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
                        entry = new LogEntry("Unauthorised");
                        entry.AddValue("Error", GetResponseText(loginResponse, FORCServerState.c_errorenum, ""));
                        entry.AddValue("Message", GetResponseText(loginResponse, FORCServerState.c_message, ""));
                        state.m_manager.Log(entry);
                        break;
                    }
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Forbidden:
                    {
                        user.AuthenticationToken = null;
                        user.SessionToken = null;
                        forbidden = true;
                        state.m_message = LocalResources.Properties.Resources.FSSRSTQ_Forbidden;
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        entry = new LogEntry("AuthorisationRejected");
                        entry.AddValue("Reason", (response == HttpStatusCode.BadRequest) ? "BadRequest" : "Forbidden");
                        entry.AddValue("Error", GetResponseText(loginResponse, FORCServerState.c_errorenum, ""));
                        entry.AddValue("Message", GetResponseText(loginResponse, FORCServerState.c_message, ""));
                        state.m_manager.Log(entry);

                        break;
                    }
                case HttpStatusCode.Redirect:
                case HttpStatusCode.TemporaryRedirect:
                    {
                        user.AuthenticationToken = null;
                        user.SessionToken = null;
                        forbidden = true;
                        String target = loginResponse["Location"] as String;
                        if (target!=null)
                        {
                            state.m_message = "Redirected by server, user needs to register with FORC at " + target;
                            switch (response)
                            {
                                case HttpStatusCode.Redirect:
                                    {
                                        user.SteamRegistrationLink = target;
                                        entry = new LogEntry("RegistrationRequired");
#if DEVELOPMENT
                                        entry.AddValue("HttpStatusCode", response.ToString()+" ("+((int)response).ToString()+")");
                                        foreach (string key in loginResponse.Keys)
                                        {
                                            string lrvalue = loginResponse[key].ToString();
                                            if (!String.IsNullOrEmpty(lrvalue))
                                            {
                                                entry.AddValue(key, lrvalue );
                                            }
                                        }
#endif
                                        state.m_manager.Log(entry);
                                        break;
                                    }
                                case HttpStatusCode.TemporaryRedirect:
                                    {
                                        user.SteamLinkLink = target;
                                        entry = new LogEntry("LinkAvailable");
#if DEVELOPMENT
                                        entry.AddValue("HttpStatusCode", response.ToString() + " (" + ((int)response).ToString() + ")");
                                        foreach (string key in loginResponse.Keys)
                                        {
                                            string lrvalue = loginResponse[key].ToString();
                                            if (!String.IsNullOrEmpty(lrvalue))
                                            {
                                                entry.AddValue(key, lrvalue);
                                            }
                                        }
#endif
                                        state.m_manager.Log(entry);
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            state.m_message = "Redirect with no target.";
                            entry = new LogEntry("RedirectWithoutTarget");
                            state.m_manager.Log(entry);
                        }
                        break;
                    }
                case HttpStatusCode.InternalServerError:
                    {
                        user.AuthenticationToken = null;
                        user.SessionToken = null;
                        forbidden = true;
                        state.m_message = "Internal server error\n" + response;
                        entry = new LogEntry("InternalServerError");
                        state.m_manager.Log(entry);
                        break;
                    }
                case HttpStatusCode.ServiceUnavailable:
                    {
                        user.AuthenticationToken = null;
                        user.SessionToken = null;
                        user.SteamUnavailable = true;
                        state.m_message = "Service unavailable.\n";
                        result = ServerInterface.AuthorisationResult.Failed;
                        entry = new LogEntry("ServiceUnavailable");
                        state.m_manager.Log(entry);
                        break;
                    }
                default:
                    {
                        if (String.IsNullOrEmpty(state.m_message))
                        {
                            state.m_message = String.Format(LocalResources.Properties.Resources.FSSRSTQ_ServerResponseX,
                                response);
                            entry = new LogEntry("UnrecognisedResponse");
                            entry.AddValue("Response", response.ToString());
                            state.m_manager.Log(entry);

                        }
                        break;
                    }
            }
            return result;

        }
    }
}
