using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using ClientSupport;

namespace FORCServerSupport.Queries
{
    class AttemptLoginQuery : JSONWebQuery
    {
        private const String c_debugValidationCode = "dbgPlainCode";

        public AttemptLoginQuery()
            : base( "user/frontier/auth", false)
        {
        }

        public ServerInterface.AuthorisationResult Run(FORCAuthorisationManager auth,
            FORCServerState state, UserDetails user, MachineIdentifierInterface machine,
            out String securityBlob)
        {
            securityBlob = null;
            AddParameter(FORCAuthorisationManager.c_emailQuery, HexConverter.Convert(user.EmailAddress));
            AddParameter(FORCAuthorisationManager.c_passwordQuery, HexConverter.Convert(user.Password));
            AddParameter(FORCServerState.c_machineIdQuery, machine.GetMachineIdentifier());
            AddParameter(FORCServerState.c_language, state.Language);
            
            state.AddTimeStamp(this);

            Dictionary<String, object> loginResponse;

            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V3_0), out loginResponse, out state.m_message);

            ServerInterface.AuthorisationResult result = ServerInterface.AuthorisationResult.Denied;

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        if (loginResponse.ContainsKey(FORCAuthorisationManager.c_securityBlobName))
                        {
                            securityBlob = loginResponse[FORCAuthorisationManager.c_securityBlobName] as String;
                            state.m_message = LocalResources.Properties.Resources.FSSALQ_VerificationMessage;
                            result = ServerInterface.AuthorisationResult.RequiresSecondFactor;
                        }

                        if (loginResponse.ContainsKey(c_debugValidationCode))
                        {
                            state.m_message += String.Format(LocalResources.Properties.Resources.FSSALQ_VerificationMessageDebug,
                                (loginResponse[c_debugValidationCode] as String));
                        }

                        if (result != ServerInterface.AuthorisationResult.Denied)
                        {
                            return result;
                        }

                        state.m_message = LocalResources.Properties.Resources.FSSALQ_InvalidServerResponse;
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
                case HttpStatusCode.InternalServerError:
                    {
                        break;
                    }
                default:
                    {
                        state.m_message = String.Format(LocalResources.Properties.Resources.UnexpectedServerResponse,
                            response);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_errorenum, state.m_message);
                        state.m_message = GetResponseText(loginResponse, FORCServerState.c_message, state.m_message);
                        break;
                    }
            }
            return ServerInterface.AuthorisationResult.Denied;
        }
    }
}
