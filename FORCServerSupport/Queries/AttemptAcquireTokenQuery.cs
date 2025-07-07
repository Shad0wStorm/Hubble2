using System;
using System.Collections.Generic;
using System.Net;
using ClientSupport;

namespace FORCServerSupport.Queries
{
    class AttemptAcquireTokenQuery : JSONWebQuery
    {
        private const String c_verificationQuery = "plainCode";

        public AttemptAcquireTokenQuery()
            : base( "user/frontier/token", false)
        {
        }

        public ServerInterface.AuthorisationResult Run(String securityBlob,
            FORCServerState state, UserDetails user, MachineIdentifierInterface machine)
        {
            AddParameter(FORCServerState.c_machineIdQuery, machine.GetMachineIdentifier());
            AddParameter(c_verificationQuery, user.TwoFactor);
            AddParameter(FORCAuthorisationManager.c_securityBlobName, securityBlob);
            AddParameter(FORCServerState.c_language, state.Language);

            Dictionary<String, object> loginResponse;

            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V3_0), out loginResponse, out state.m_message);

            ServerInterface.AuthorisationResult result = ServerInterface.AuthorisationResult.Denied;

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        if (loginResponse.ContainsKey(FORCServerState.c_machineToken))
                        {
                            user.AuthenticationToken = loginResponse[FORCServerState.c_machineToken] as String;
                            return ServerInterface.AuthorisationResult.Authorised;
                        }

                        if (result != ServerInterface.AuthorisationResult.Denied)
                        {
                            return result;
                        }

                        state.m_message = LocalResources.Properties.Resources.FSSAATQ_InvalidServerResponse;
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        break;
                    }
                case HttpStatusCode.Unauthorized:
                    {
                        state.m_message = LocalResources.Properties.Resources.Unauthorised;
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        break;
                    }
            }
            return result;
        }
    }
}
