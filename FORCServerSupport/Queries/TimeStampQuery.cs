using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace FORCServerSupport.Queries
{
    class TimeStampQuery : JSONWebQuery
    {
        private const string c_time = "unixTimestamp";

        public TimeStampQuery() : base("server/time")
        {

        }

        public String Run(FORCServerState state)
        {
            RunV1_1(state);

            state.m_remoteLogger.SetStartTime(state.m_startTime, state.m_systemStartTime);
            return state.m_remoteTime;
        }

        private void RunV1_1(FORCServerState state)
        {
            Dictionary<String, object> timeResponse;

            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V1_1), out timeResponse, out state.m_message);

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        if (timeResponse.ContainsKey(c_time))
                        {
                            state.SetTime((int)timeResponse[c_time]);
                            state.SupportedAPI = FORCServerState.APIVersion.V1_1;
                        }
                        break;
                    }
                default:
                    {
                        state.m_remoteTime = null;
                        break;
                    }
            }
        }
    }
}
