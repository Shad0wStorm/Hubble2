using System;
using System.Collections.Generic;
using System.Net;

using ClientSupport;

namespace FORCServerSupport.Queries
{
    class RequestSessionTokenQuery : JSONWebQuery
    {
        public RequestSessionTokenQuery()
            : base( "user/frontier/auth", false)
        {

        }

        public ServerInterface.AuthorisationResult Run(FORCServerState state, UserDetails user,
            MachineIdentifierInterface machine, out bool forbidden)
        {
            forbidden = false;
            AddParameter(FORCAuthorisationManager.c_emailQuery,
                HexConverter.Convert(user.EmailAddress));
            AddParameter(FORCAuthorisationManager.c_passwordQuery,
                HexConverter.Convert(user.Password));
            AddParameter(FORCServerState.c_machineIdQuery, machine.GetMachineIdentifier());
            AddParameter(FORCServerState.c_machineToken, user.AuthenticationToken);
            AddParameter(FORCServerState.c_language, state.Language);
            state.AddTimeStamp(this);

            Dictionary<String, object> loginResponse;

            state.m_message = null;
            state.LastErrorCode = FORCServerState.c_ServerStateNoErrorCode;

            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V3_0),
                out loginResponse, out state.m_message);

            ServerInterface.AuthorisationResult result = ServerInterface.AuthorisationResult.Denied;

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        if (loginResponse.ContainsKey(FORCServerState.c_authToken))
                        {
                            user.SessionToken = loginResponse[FORCServerState.c_authToken] as String;
                            result = ServerInterface.AuthorisationResult.Authorised;
                            if (loginResponse.ContainsKey(FORCServerState.c_registeredName))
                            {
                                user.RegisteredName = loginResponse[FORCServerState.c_registeredName] as String;
                            }
                            else
                            {
                                int at = user.EmailAddress.IndexOf('@');
                                if (at < 0)
                                {
                                    user.RegisteredName = user.EmailAddress;
                                }
                                else
                                {
                                    user.RegisteredName = user.EmailAddress.Substring(0, at);
                                }
                            }
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
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Forbidden:
                    {
                        user.AuthenticationToken = null;
                        user.SessionToken = null;
                        forbidden = true;
                        state.m_message = LocalResources.Properties.Resources.FSSRSTQ_Forbidden;
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        break;
                    }
                default:
                    {
                        state.m_message = String.Format(LocalResources.Properties.Resources.FSSRSTQ_ServerResponseX,
                            response);
                        break;
                    }
            }

            // See if we have an errorCode
            state.LastErrorCode = GetResponseInt( loginResponse, FORCServerState.c_errorCode, -FORCServerState.c_ServerStateNoErrorCode );

            return ServerInterface.AuthorisationResult.Denied;
        }
    }
}
