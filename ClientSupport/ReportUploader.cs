using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Reflection;
using LocalResources;

namespace ClientSupport
{
    public class ReportUploader
    {
        public delegate void UploadStartedHandler();
        public event UploadStartedHandler UploadStarted;

        public delegate void UploadProgressHandler(int _percentage);
        public event UploadProgressHandler UploadProgress;

        public delegate void UploadCompletedHandler();
        public event UploadCompletedHandler UploadCompleted;

        public delegate void ErrorRecievedHandler(string _message);
        public event ErrorRecievedHandler ErrorRecieved;
        
        public String m_hostRoot = @"https://api.orerve.net";
        public string Host { get { return m_hostRoot; } }
        public int Port { get { return 443; } }
        public string Request { get { return @"1.5/elite/dump"; } }

        public string MachineToken { get; private set; }
        public string ReportType { get; private set; }
        public string Version { get; private set; }
        public string AuthToken { get; private set; }
        public string MachineId { get; private set; }
        public string Time { get; private set; }
		public string BuildType { get; private set; }
        
        public ReportUploader(String root = null)
        {
            FORCManager.SetTlsProtocol(SecurityProtocolType.Tls12);
            if (!String.IsNullOrEmpty(root))
            {
                m_hostRoot = root;
            }
        }

        public void ReportError(String message)
        {
            if (ErrorRecieved != null)
            {
                ErrorRecieved(message);
            }
        }

        public void Upload(byte[] _data, string _machineToken, string _reportType, string _version,
			string _authToken, string _machineId, string _time,
			string buildType)
        {
            m_dataToSend = _data;

            Upload(_machineToken, _reportType, _version, _authToken, _machineId, _time, buildType);
        }

        public void Upload(string _path, string _machineToken, string _reportType, string _version,
			string _authToken, string _machineId, string _time,
			string buildType)
        {
            if (!System.IO.File.Exists(_path))
            {
                throw new FileNotFoundException();
            }

            m_dataToSend = System.IO.File.ReadAllBytes(_path);

            Upload(_machineToken, _reportType, _version, _authToken, _machineId, _time, buildType);
        }

        void Upload(string _machineToken, string _reportType, string _version,
			string _authToken, string _machineId, string _time,
			string buildType)
        {
            MachineToken = _machineToken;
            ReportType = _reportType;
            Version = _version;
            AuthToken = _authToken;
            MachineId = _machineId;
            Time = _time;
			BuildType = buildType;

            if (string.IsNullOrEmpty(MachineToken))
            {
                throw new ArgumentException("MachineToken can't be empty");
            }

            if (string.IsNullOrEmpty(ReportType))
            {
                throw new ArgumentException("ReportType can't be empty");
            }

            if (string.IsNullOrEmpty(Version))
            {
                throw new ArgumentException("Version can't be empty");
            }

            if (string.IsNullOrEmpty(AuthToken))
            {
                throw new ArgumentException("AuthToken can't be empty");
            }

            if (string.IsNullOrEmpty(MachineId))
            {
                throw new ArgumentException("MachineId can't be empty");
            }

            if (string.IsNullOrEmpty(Time))
            {
                throw new ArgumentException("Time can't be empty");
            }

            ThreadPool.QueueUserWorkItem(OnUpload);
        }

        public void OnUpload(object _state)
        {
            if (UploadStarted != null)
            {
                UploadStarted();
            }

            // generate the MD5
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(m_dataToSend);
            var textHash = DecoderRing.BytesToHex(hash);

            String os = Utils.OSIdent.GetOSIdent();

            var url = string.Format("{0}:{1}/{2}?machineToken={3}&reportType={4}&gameVersion={5}&authToken={6}&machineId={7}&fTime={8}&os={9}", 
                Host, Port, Request, MachineToken, ReportType, Version, AuthToken, MachineId, Time, os);

#if(DEBUG)
            url += "&debug=true";
#endif
			if (!String.IsNullOrEmpty(BuildType))
			{
				url += "&buildType=" + BuildType;
			}

            FORCManager.EnsureIsUsingSecureTlsProtocol();
            var request = WebRequest.Create(new Uri(url)) as HttpWebRequest;

            request.Method = "PUT";
            request.KeepAlive = false;
            request.AllowWriteStreamBuffering = false;
            request.ContentType = @"application/x-www-form-urlencoded";
            request.ContentLength = m_dataToSend.Length;
            request.Headers.Add("Content-MD5", textHash);
            request.UserAgent = MakeUserAgentString(os);

            try
            {
                var dataStream = request.BeginGetRequestStream(OnBeginGetRequestStream, request);
            }
            catch (System.Exception ex)
            {
                String error = String.Format(LocalResources.Properties.Resources.RU_UploadConnectExc, ex.Message);
                ReportError(error);
            }
        }

        private void OnBeginGetRequestStream(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;

            try
            {
                using (var uploadStream = request.EndGetRequestStream(result))
                {
                    bool writeError = false;
                    try
                    {
                        int bytesWritten = 0;
                        int lastProgressUpdate = -1;
                        while (bytesWritten != m_dataToSend.Length)
                        {
                            var amountToWrite = Math.Min(4096, m_dataToSend.Length - bytesWritten);

                            try
                            {
                                uploadStream.Write(m_dataToSend, bytesWritten, amountToWrite);
                                uploadStream.Flush();
                            }
                            catch (System.Exception ex)
                            {
                                // We want to single out this write as a potential
                                // source of error, but we also want to abort the
                                // upload so we rethrow the exception.
                                ReportError(String.Format(LocalResources.Properties.Resources.RU_ServerWriteExc,
                                    ex.Message));
                                writeError = true;
                                throw;
                            }
                            bytesWritten += amountToWrite;

                            if (UploadProgress != null)
                            {
                                var p = (int)(100.0 * ((double)bytesWritten / (double)m_dataToSend.Length));
                                if (p != lastProgressUpdate)
                                {
                                    UploadProgress((int)p);
                                    lastProgressUpdate = p;
                                }
                            }
                        }
                        uploadStream.Close();
                    }
                    catch (System.Exception ex)
                    {
                        if (!writeError)
                        {
                            ReportError(String.Format(LocalResources.Properties.Resources.RU_ServerWriteManageExc,
                                ex.Message));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ReportError(String.Format(LocalResources.Properties.Resources.RU_ServerWriteStreamExc,
                    ex.Message));
            }

            try
            {
                using(var response = (HttpWebResponse)request.GetResponse())
                using (var resStream = response.GetResponseStream())
                {
                    if (ErrorRecieved!=null)
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            ErrorRecieved(String.Format(LocalResources.Properties.Resources.RU_ServerWriteResponseFail,
                                response.StatusCode));
                        }
                    }
                    var reader = new StreamReader(resStream);
                    var responseFromServer = reader.ReadToEnd();
#if(DEBUG)
                    Process.Start("explorer.exe", responseFromServer);
#endif
                    resStream.Close();
                }
            }
            catch (WebException we)
            {
                using (WebResponse response = we.Response)
                {
                    try
                    {
                        using( HttpWebResponse httpResponse = (HttpWebResponse)response)
                        {
                            JavaScriptSerializer jss = new JavaScriptSerializer();
                            Stream responseStream = response.GetResponseStream();
                            StreamReader r = new StreamReader(responseStream);

                            if (ErrorRecieved != null)
                            {
                                ErrorRecieved(r.ReadToEnd());
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        if (ErrorRecieved != null)
                        {
                            ErrorRecieved(String.Format(LocalResources.Properties.Resources.RU_ServerWriteResponseExc,
                                ex.Message));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ErrorRecieved != null)
                {
                    ErrorRecieved(ex.Message);
                }
            }
            if (UploadCompleted != null)
            {
                UploadCompleted();
            }
        }

        private String MakeUserAgentString(String os)
        {
            AssemblyName name = Assembly.GetEntryAssembly().GetName();

            String result  = name.Name + "/" + name.Version.ToString();
            if (os != null)
            {
                result = result + "/" + os;
            }
            return result;
        }

        byte[] m_dataToSend;
    }
}
