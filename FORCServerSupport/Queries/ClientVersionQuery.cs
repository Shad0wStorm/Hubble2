using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using ClientSupport;

namespace FORCServerSupport.Queries
{
    class ClientVersionQuery : JSONWebQuery
    {
        private const String c_serverClientVersion = "versionLatest";
        private const String c_serverVersionStatus = "versionStatus";

        public ClientVersionQuery()
            : base("client/version")
        {

        }

        public ServerInterface.VersionStatus Run(FORCServerState state, String version, out String current)
        {
            ServerInterface.VersionStatus status = ServerInterface.VersionStatus.Current;
            current = version;

            AddParameter("version", version);

            Dictionary<String, object> checkResponse;

            HttpStatusCode response = Execute(state.GetServerAPI(state.SupportedAPI), out checkResponse, out state.m_message);

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        Dictionary<String, object> versionResponse = checkResponse;

                        if (versionResponse.ContainsKey(c_serverClientVersion))
                        {
                            current = versionResponse[c_serverClientVersion].ToString();
                        }
                        if (versionResponse.ContainsKey(c_serverVersionStatus))
                        {
                            String textStatus = versionResponse[c_serverVersionStatus].ToString().ToLowerInvariant();
                            if (textStatus == "current")
                            {
                                status = ServerInterface.VersionStatus.Current;
                            }
                            else
                            {
                                if (textStatus == "supported")
                                {
                                    status = ServerInterface.VersionStatus.Supported;
                                }
                                else
                                {
                                    if (textStatus == "expired")
                                    {
                                        status = ServerInterface.VersionStatus.Expired;
                                    }
                                    else
                                    {
                                        if (textStatus == "future")
                                        {
                                            status = ServerInterface.VersionStatus.Future;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                case HttpStatusCode.ServiceUnavailable:
                    {
                        // Failed to connect to server so assume we are from the future
                        // (i.e. the server does not support the request yet)
                        // and note the failure, just in case someone actually sees it
                        // in the wild.
                        status = ServerInterface.VersionStatus.Future;
                        current = LocalResources.Properties.Resources.FSSCVQ_Unsupported;
                        break;
                    }
            }
            return status;
        }
    }
}
