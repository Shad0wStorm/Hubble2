using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

using Epic.OnlineServices;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Logging;

namespace EosIF
{
    public struct EpicCommandLineArgInfo
    {
        
        public String m_epicLocale;
        public String m_epicUserId;
        public String m_epicAuthType;
        public String m_epicAuthPassword;
        public String m_epicEnvironment;
        public String m_epicAuthPort;
        public String m_epicAuthTokenName;
        public String m_epicRefreshToken;
        public bool m_writeToLog;
    }
    class LogHelper
    {
        private String m_path;
        public LogHelper(String path)
        {
            m_path = path;
            if (m_path != null)
            {
                String dirname = Path.GetDirectoryName(m_path);
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }
                File.Delete(m_path);
                Log("Reset Log");
            }
        }

        public void Log(String message)
        {
            if (m_path != null)
            {
                using (StreamWriter log = File.AppendText(m_path))
                {
                    log.WriteLine(DateTime.UtcNow + ":\t" + message);
                    log.Flush();
                }
            }
        }
    }

    public class EosInterface
    {
        public bool m_loginInProgress { get; private set; }
        public PlatformInterface Platform { get; private set; }
        private Epic.OnlineServices.Auth.AuthInterface m_authInterface = null;
        private LoginStatus m_loginStatus = LoginStatus.NotLoggedIn;
        private String m_defaultDevToolPort = "8888";
        private String m_defaultCredentialTokenName = "test";
        private LoginCredentialType m_loginMethod = LoginCredentialType.Developer;
        private EpicCommandLineArgInfo m_commandLineConfig;
        private EpicAccountId m_epicAccountId;
        private DateTime m_timeOfNextCredentialRefresh = DateTime.UtcNow;
        private Token m_authToken = null;
        private bool m_shouldShutDown = false;

        String m_logPath = null;
        LogHelper m_log;
        const bool c_saveLog = false;

        public bool HasAuthToken()
        {
            return m_authToken != null;
        }
        public EosInterface()
        {
            m_loginInProgress = false;
        }

        private String m_clientId = "";

        public bool IsLoggedIn()
        {
            return m_loginStatus == LoginStatus.LoggedIn;
        }

        public String GetRefreshTokenExpiryInfo()
        {
            String result = "";
            if (HasAuthToken())
            {
                result = m_authToken.RefreshExpiresAt;
                result += " // " + DateTime.Parse(result);
            }
            return result;
        }
        public bool IsRefreshTokenValid()
        {
            DateTime expiryTime = DateTime.Parse(m_authToken.RefreshExpiresAt).ToUniversalTime();
            DateTime currentTime = DateTime.Now.ToUniversalTime();

            TimeSpan tenSeconds = new TimeSpan(0, 0, 10);
            TimeSpan timeRemaining = expiryTime.Subtract(currentTime);

            return timeRemaining > tenSeconds;
        }

        public bool HasValidRefreshToken()
        {
            bool hasToken = HasAuthToken() && m_authToken.RefreshToken.Length > 0;
            if (hasToken)
            {
                hasToken = IsRefreshTokenValid();
            }
            return hasToken;
        }

        public String GetRefreshToken()
        {
            String token = "";
            if (HasAuthToken())
            {
                token = m_authToken.RefreshToken;
            }
            return token;
        }
        public String GetClientId()
        {
            return m_clientId;
        }

        public String GetAccessToken()
        {
            if (HasAuthToken())
            {
                return m_authToken.AccessToken;
            } else
            {
                return "";
            }
        }

        ~EosInterface()
        {
            if (m_log != null)
            {
                m_log.Log("Shutting Down Epic resources");
            }
            Cleanup();
        }

        void Cleanup()
        {
            if (Platform != null)
            {
                Platform.Release();
                Platform = null;

                PlatformInterface.Shutdown();
            }
        }

        public Dictionary<String, String> CheckAuthToken()
        {
            Dictionary<String, String> authTokenIncongruities = new Dictionary<string, string>();
            if (HasAuthToken())
            {
                DateTime accessTokenExpiry = DateTime.Parse(m_authToken.ExpiresAt).ToUniversalTime();
                DateTime refreshTokenExpiry = DateTime.Parse(m_authToken.RefreshExpiresAt).ToUniversalTime();
                DateTime now = DateTime.UtcNow;
                

                if (accessTokenExpiry.Subtract(now).TotalMilliseconds <= 0 )
                {
                    authTokenIncongruities.Add("AccessTokenExpiry has passed", 
                        "Expired at: " + accessTokenExpiry + ", Current time: " + now);
                }

                if (m_authToken.ExpiresIn <= 0.0f)
                {
                    authTokenIncongruities.Add("AccessTokenExpiresIn", m_authToken.ExpiresIn.ToString());
                }
                if (refreshTokenExpiry.Subtract(now).TotalMilliseconds <= 0)
                {
                    authTokenIncongruities.Add("RefreshTokenExpiry has passed",
                        "Expired at: " + refreshTokenExpiry + ", Current time: " + now);
                }
                if (m_authToken.RefreshExpiresIn <= 0.0f)
                {
                    authTokenIncongruities.Add("RefreshTokenExpiresIn", m_authToken.RefreshExpiresIn.ToString());
                }
            }
            return authTokenIncongruities;
        }
        
        public void Tick()
        {
            if (Platform != null)
            {
                Platform.Tick();

                if (HasAuthToken() && IsLoggedIn() && !m_loginInProgress)
                {
                    if ((ShouldRefreshCredentials() || IsAccessTokenAboutToExpire()))
                    {
                        m_log.Log("Time to refresh token credentials.");
                        GetAccessTokenFromSdk();

                        m_timeOfNextCredentialRefresh = DateTime.UtcNow.AddMinutes(5); // Check again in five minutes
                    }
                }
            }
        }

        private void GetAccessTokenFromSdk()
        {
            if (m_epicAccountId != null && m_epicAccountId.IsValid())
            {
                String oldAccessToken = (HasAuthToken() ? m_authToken.AccessToken : "");
                String oldRefreshToken = (HasAuthToken() ? m_authToken.RefreshToken : "");
                CopyUserAuthTokenOptions copyUserAuthTokenOptions = new CopyUserAuthTokenOptions();
                Result authTokenCopyResult = m_authInterface.CopyUserAuthToken(copyUserAuthTokenOptions, m_epicAccountId, out m_authToken);

                String newRefreshToken = "";
                String newAccessToken = "";
                if (HasAuthToken())
                {
                    newAccessToken = m_authToken.AccessToken;
                    newRefreshToken = m_authToken.RefreshToken;
                    //m_log.Log("copy auth token result: " + authTokenCopyResult.ToString());
                    //m_log.Log("Access token: " + m_authToken.AccessToken + "\nRefresh token: " + m_authToken.RefreshToken);
                }
                if (!newAccessToken.Equals(oldAccessToken))
                {
                    m_log.Log("Copy auth token result: " + authTokenCopyResult.ToString());
                    m_log.Log("Access token updated: " + newAccessToken);
                }
                if (!newRefreshToken.Equals(oldRefreshToken))
                {
                    m_log.Log("Refresh token updated: " + newRefreshToken);
                }
            }
        }
        private void OnLoginCallback(LoginCallbackInfo loginCallbackInfo)
        {
            string accountIdString;

            if (loginCallbackInfo.LocalUserId != null && loginCallbackInfo.LocalUserId.IsValid())
            {
                m_epicAccountId = loginCallbackInfo.LocalUserId;
            }

            if (loginCallbackInfo.ResultCode == Result.Success)
            {
                Result stringRes = m_epicAccountId.ToString(out accountIdString);

                m_log.Log("Attempt to create string result: " + stringRes.ToString());
                m_clientId = accountIdString;

                m_loginMethod = LoginCredentialType.RefreshToken;
                m_loginStatus = LoginStatus.LoggedIn;

                m_timeOfNextCredentialRefresh = DateTime.UtcNow.AddMinutes(5);
            }
            m_log.Log("Log in attempt result: " + loginCallbackInfo.ResultCode.ToString());
            m_log.Log("Client ID: " + m_clientId);
            m_log.Log(m_loginStatus.ToString());

            GetAccessTokenFromSdk();

            m_loginInProgress = false;

        }

        private void SetEosLogging()
        {
            // The SDK outputs lots of information that is useful for debugging.
            // Make sure to set up the logging interface as early as possible: after initializing.
            Epic.OnlineServices.Logging.LoggingInterface.SetLogLevel(Epic.OnlineServices.Logging.LogCategory.AllCategories, LogLevel.VeryVerbose);
            Epic.OnlineServices.Logging.LoggingInterface.SetCallback((Epic.OnlineServices.Logging.LogMessage logMessage) =>
            {
                m_log.Log("[EOS-SDK] " + logMessage.Message);
            });
        }

        private void InitialiseEosPlatformInterface()
        {
            InitializeOptions initOptions = new InitializeOptions();
            initOptions.ProductName = "Callisto";
            initOptions.ProductVersion = "1.0";

            m_log.Log("Initializing EOS IF...");
            Result res = PlatformInterface.Initialize(initOptions);

            SetEosLogging();
            m_log.Log("Init result " + res.ToString() + " \n");
        }

        private void CreateEpicInterfaces()
        {
            m_log.Log("Creating Platform Interface...");
            Options options = CreatePlatformOptions();
            Platform = PlatformInterface.Create(options);

            m_log.Log("Fetching Auth Interface...");
            m_authInterface = Platform.GetAuthInterface();

            m_log.Log("Interfaces initialised.");
        }

        private ClientCredentials CreateClientCredentials()
        {
            const String clientId = "xyza7891b1gxkZRvfD1kiRrax74Oz5D2";
            const String clientSecret = "0v3iO7GH5529YQk5yR19lGNblxHXaNz1UYDLzMrlX64";
            ClientCredentials clientCredentials = new ClientCredentials
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };
            return clientCredentials;
        }

        private Options CreatePlatformOptions()
        {
            const string productId = "f92ad09c5eb84033825d8ee1741172b8";
            const String sandboxId = "3db17abfd650423f993291624b1b2ac1";
            ClientCredentials clientCreds = CreateClientCredentials();
            const String deploymentId = "73d227f537e34a72a994a4524386c6f3";

            Options options = new Options
            {
                ProductId = productId,
                SandboxId = sandboxId,
                ClientCredentials = clientCreds,
                Flags = PlatformFlags.DisableOverlay,
                DeploymentId = deploymentId
            };
            return options;
        }

        private Credentials CreatePlatformCredentials()
        {
            m_log.Log("Detecting settings to determine login method...");
            Credentials credentials = new Credentials
            {
                Type = m_loginMethod
            };
            switch (m_loginMethod)
            {
                case LoginCredentialType.ExchangeCode:
                    {
                        m_log.Log("Login method set to exchange code");
                        credentials.Token = m_commandLineConfig.m_epicAuthPassword;
                        break;
                    }
                case LoginCredentialType.Developer:
                    {
                        m_log.Log("Login method set to Developer");
                        String localHostIp = "127.0.0.1:" + m_commandLineConfig.m_epicAuthPort;
                        credentials.Token = m_commandLineConfig.m_epicAuthTokenName;
                        credentials.Id = localHostIp;
                        break;
                    }
                case LoginCredentialType.RefreshToken:
                    {
                        credentials.Token = GetCurrentRefreshTokenForLogin();
                        break;
                    }
            }

            return credentials;
        }

        private String GetCurrentRefreshTokenForLogin()
        {
            String refreshToken = "";
            m_log.Log("Login method set to Refresh Token");
            if (HasAuthToken())
            {
                refreshToken = m_authToken.RefreshToken;
            }
            else if (m_commandLineConfig.m_epicRefreshToken != null 
                && m_commandLineConfig.m_epicRefreshToken.Length > 0)

            {
                refreshToken = m_commandLineConfig.m_epicRefreshToken;
            } else
            {
                m_log.Log("Could not find Refresh token, no available AuthToken or Command line input");
            }


            return refreshToken;
        }

        private LoginOptions CreateLoginOptions()
        {
            LoginOptions loginOptions = new LoginOptions
            {
                Credentials = CreatePlatformCredentials(),
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence
            };

            return loginOptions;
        }

        private void AttemptLogin()
        {
            m_log.Log("Attempting login...");
            LoginOptions loginOptions = CreateLoginOptions();

            m_loginInProgress = true;
            m_authInterface.Login(loginOptions, null, OnLoginCallback);

        }

        private void InitialiseLog()
        { 
            String path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            String logPath = Path.Combine(path, "logs");
            m_logPath = c_saveLog || m_commandLineConfig.m_writeToLog ? Path.Combine(logPath, "eos.log") : null;
            m_log = new LogHelper(m_logPath);
        }

        private void SetLoginMethod()
        {
            if (m_commandLineConfig.m_epicRefreshToken != null 
                && m_commandLineConfig.m_epicRefreshToken.Length > 0)
            {
                m_loginMethod = LoginCredentialType.RefreshToken;
            } else if (m_commandLineConfig.m_epicAuthType != null 
                && m_commandLineConfig.m_epicAuthType.ToLower().Equals("exchangecode")
                && m_commandLineConfig.m_epicAuthPassword != null
                && m_commandLineConfig.m_epicAuthPassword.Length > 0) {
                m_loginMethod = LoginCredentialType.ExchangeCode;
            } else if ( m_commandLineConfig.m_epicAuthPort != null 
                && m_commandLineConfig.m_epicAuthPort.Length > 0 
                && m_commandLineConfig.m_epicAuthTokenName != null
                && m_commandLineConfig.m_epicAuthTokenName.Length > 0)
            {
                m_loginMethod = LoginCredentialType.Developer;
            } else
            {
                // Default case
                m_loginMethod = LoginCredentialType.Developer;
                m_commandLineConfig.m_epicAuthPort = m_defaultDevToolPort;
                m_commandLineConfig.m_epicAuthTokenName = m_defaultCredentialTokenName;
            }
        }

        public void OnLoginStatusChangedCallback(LoginStatusChangedCallbackInfo callbackInfo)
        {
            bool wasLoggedIn = callbackInfo.PrevStatus == LoginStatus.LoggedIn;
            bool notLoggedIn = callbackInfo.CurrentStatus == LoginStatus.NotLoggedIn;

            if (wasLoggedIn && notLoggedIn)
            {
                m_log.Log("Login Status changed, no longer logged in!");
                m_shouldShutDown = true;
            }

            m_loginStatus = callbackInfo.CurrentStatus;
        }

        public bool ShouldShutDown()
        {
            return m_shouldShutDown;
        }

        public void SetupLoginStatusChangedCallback()
        {
            if (m_authInterface != null)
            {
                m_log.Log("Setting up login status callback");
                AddNotifyLoginStatusChangedOptions options = new AddNotifyLoginStatusChangedOptions();

                m_authInterface.AddNotifyLoginStatusChanged(options, null, OnLoginStatusChangedCallback);
                m_log.Log("Set up login status callback created");
            } else
            {
                m_log.Log("Auth Interface is not initialised, could not setup login status callback.");

            }
        }
        public void Initialise(EpicCommandLineArgInfo epicConfig)
        {
            m_commandLineConfig = epicConfig;
            SetLoginMethod();
         
            InitialiseLog();
            InitialiseEosPlatformInterface();
            CreateEpicInterfaces();
            SetupLoginStatusChangedCallback();

            m_log.Log("Login Status: " + m_loginStatus);

            AttemptLogin(); // Try to login immediately
        }

        public bool IsAccessTokenAboutToExpire()
        {
            DateTime accessTokenExpiryTime = DateTime.Parse(m_authToken.ExpiresAt).ToUniversalTime();
            DateTime currentTimeInUtc = DateTime.Now.ToUniversalTime();

            TimeSpan timeRemaining = accessTokenExpiryTime.Subtract(currentTimeInUtc);

            TimeSpan minimumTimeForToken = new TimeSpan(0, 0, 50); // 50 seconds

            return timeRemaining < minimumTimeForToken;
        }

        private bool ShouldRefreshCredentials()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan timeUntilNextRefresh = m_timeOfNextCredentialRefresh.Subtract(now);

            return (timeUntilNextRefresh.Milliseconds < 0);
        }
    }
}
