using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using ClientSupport;

namespace FORCServerSupport
{
    class FORCTelemetry : BaseLogger
    {
        // These are identical to the ones in FORCServerConnection, they should
        // probably be moved somewhere common.
        private const String c_machineToken = "machineToken";
        private const String c_authToken = "authToken";
        private const string c_machineIdQuery = "machineId";
        private const string c_timeQuery = "fTime";

        private Uri m_apiRoot;
        private long m_startTime;
        private DateTime m_systemStartTime;
        private FORCManager m_manager;

        public FORCTelemetry(FORCManager manager)
        {
            m_manager = manager;
        }

        public void SetServer(Uri apiRoot)
        {
            m_apiRoot = apiRoot;
        }

        public void SetStartTime(long startTime, DateTime systemStartTime)
        {
            m_startTime = startTime;
            m_systemStartTime = systemStartTime;
        }

        public override void Log(UserDetails user, LogEntry entry)
        {
            if (!entry.IsCommand)
            {
                if ((user.AuthenticationToken != null) && (user.SessionToken != null))
                {
                    // Authenticated so friendly server will hear us.
                    JSONWebQuery query = new JSONWebQuery("eventLog", false);

                    TimeSpan ut = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));
                    query.AddParameter("eventTime", ut.TotalSeconds.ToString());
                    Dictionary<String, object> entryValues = entry.GetValues();
                    String eventname = "unspecified";
                    if (entryValues.ContainsKey("action"))
                    {
                        eventname = entryValues["action"].ToString();
                    }
                    query.AddParameter("event", eventname);
                    query.AddParameter(c_machineToken, user.AuthenticationToken);
                    query.AddParameter(c_authToken, user.SessionToken);
                    query.AddTimeStamp(c_timeQuery, m_systemStartTime, m_startTime);
                    query.AddParameter(c_machineIdQuery, m_manager.MachineIdentifier.GetMachineIdentifier());

                    String notReallyAQueryStringButItIsWhatTheyAskedFor = "";
                    foreach (String key in entryValues.Keys)
                    {
                        if (!String.IsNullOrEmpty(notReallyAQueryStringButItIsWhatTheyAskedFor))
                        {
                            notReallyAQueryStringButItIsWhatTheyAskedFor += "&";
                        }
                        object value = entryValues[key];
                        if (value!=null)
                        {
                            notReallyAQueryStringButItIsWhatTheyAskedFor += key;
                            notReallyAQueryStringButItIsWhatTheyAskedFor += "=";
                            notReallyAQueryStringButItIsWhatTheyAskedFor += value.ToString();
                        }
                    }

                    query.SetBody(notReallyAQueryStringButItIsWhatTheyAskedFor);

                    // Send the request, we do not actually care about the response
                    // since we are only asking the server to keep a log of the
                    // events.
                    Dictionary<String, object> installerResponse;

                    String message = null;
                    try
                    {
                        HttpStatusCode response = query.Execute(m_apiRoot, out installerResponse, out message);
                    }
                    catch (System.Exception)
                    {
                        // Something failed but it only means the log message did
                        // not get there, and what are we going to do about it,
                        // send a log message to tell us the log message failed to
                        // be delivered?
                    }
                }
            }
        }
    }
}
