using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using ClientSupport;

namespace FORCServerSupport.Queries
{
    class InstallerQuery : JSONWebQuery
    {
        public InstallerQuery() : base("user/installer")
        {
        }

        public DownloadManagerBase.InstallerVersionResult Run(
            FORCServerState state, UserDetails user,
            String project, String version,
            ref DownloadManagerBase.RemoteFileDetails details)
        {
            AddParameter(FORCServerState.c_machineToken, user.AuthenticationToken);
            AddParameter(FORCServerState.c_authToken, user.SessionToken);
            AddParameter(FORCServerState.c_machineIdQuery, state.m_manager.MachineIdentifier.GetMachineIdentifier());
            AddParameter("sku", project);
            AddParameter("os", "win"); // At the moment we only support windows.
            state.AddTimeStamp(this);

            if (user.AuthenticationType == FORCServerConnection.AuthenticationType.Epic)
            {
                AddHeader(FORCServerState.c_headerAuthToken, "epic " + user.EpicAccessToken);
            }

            Dictionary<String, object> installerResponse;

            state.m_message = null;
            HttpStatusCode response = Execute(state.GetServerAPI(FORCServerState.APIVersion.V3_0), out installerResponse, out state.m_message);

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        try
                        {
                            String remoteVersion = installerResponse["version"] as String;
                            if (String.IsNullOrEmpty(remoteVersion))
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_NoVersion;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
                            details.RemoteVersion = remoteVersion;
                            String encodedPath = installerResponse["remotePath"] as String;
                            if (String.IsNullOrEmpty(encodedPath))
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_NoDownload;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
                            details.RemotePath = DecoderRing.HexToString(encodedPath);
                            if (String.IsNullOrEmpty(details.RemotePath))
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_InvalidDownload;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
                            String localFile = installerResponse["localFile"] as String;
                            if (String.IsNullOrEmpty(localFile))
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_NoLocalFile;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
                            details.LocalFileName = Path.GetFileName(localFile);
                            if (details.LocalFileName == null)
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_InvalidLocalFile;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
                            details.CheckSum = installerResponse["md5"] as String;
                            if (String.IsNullOrEmpty(details.CheckSum))
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_NoCheckSum;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
                            details.CheckSum = details.CheckSum.Replace("\"", "");
                            if (String.IsNullOrEmpty(details.CheckSum))
                            {
                                state.m_message = LocalResources.Properties.Resources.FSSIQ_InvalidCheckSum;
                                return DownloadManagerBase.InstallerVersionResult.Failed;
                            }
#if DEVELOPMENT
                            String cookieFieldKey = "cdn_cookie";
                            if (installerResponse.ContainsKey(cookieFieldKey)) {
                                Dictionary<String, object> cookieResponse = installerResponse[cookieFieldKey] as Dictionary<String, object>;
                                foreach (String key in cookieResponse.Keys)
                                {
                                    String value = cookieResponse[key].ToString();
                                    details.AccessCookies[key] = value;
                                }
                            }
#endif // DEVELOPMENT
                            String textSize = installerResponse["size"] as String;
                            if (textSize != null)
                            {
                                if (!long.TryParse(textSize, out details.FileSize))
                                {
                                    state.m_message = String.Format(LocalResources.Properties.Resources.FSSIQ_InvaildFileSize,
                                        textSize);
                                    return DownloadManagerBase.InstallerVersionResult.Failed;
                                }
                            }
                            else
                            {
                                details.FileSize = Convert.ToInt64(installerResponse["size"]);
                            }
                            if (remoteVersion == version)
                            {
                                // Server does not perform version checking
                                // currently so we need to check if the version
                                // the server is trying to send us matches the
                                // version we already have installed.
                                state.m_message = "";
                                return DownloadManagerBase.InstallerVersionResult.Current;
                            }
                            return DownloadManagerBase.InstallerVersionResult.Update;
                        }
                        catch (System.Exception ex)
                        {
                            state.m_message = String.Format(LocalResources.Properties.Resources.FSSIQ_ServerResponseX,
                                ex.Message);
                            if ((!state.m_manager.IsReleaseBuild) || (false))
                            {
                                if (installerResponse!=null)
                                {
                                    foreach (String name in installerResponse.Keys)
                                    {
                                        state.m_message += "\n" + name + " = " + installerResponse[name];
                                    }
                                }
                            }
                        }
                        return DownloadManagerBase.InstallerVersionResult.Failed; ;
                    }
                default:
                    {
                        try
                        {
                            String message = installerResponse["message"] as String;
                            if (message != null)
                            {
                                state.m_message = String.Format(LocalResources.Properties.Resources.FSSIQ_Rejected,
                                    message);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            state.m_message = String.Format(LocalResources.Properties.Resources.FSSIQ_RequestX,
                                ex.Message) ;
                        }
                        return DownloadManagerBase.InstallerVersionResult.Failed;
                    }
            }
        }
    }
}
