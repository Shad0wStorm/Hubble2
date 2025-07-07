using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

using ClientSupport;

namespace FORCServerSupport
{
    class TokenStorage
    {
        /// <summary>
        /// Define an internal class to represent the token on disk.
        /// We do it this way to make it easier to add additional meta data
        /// in the future if we need to.
        /// </summary>
        private class TokenContainer
        {
			public String Token = null;
        }

		private class MappedTokenContainer
		{
			public Dictionary<String, String> Tokens = new Dictionary<String, String>();
		}

        private TokenContainer m_container;
		private MappedTokenContainer m_mapped;
        private DecoderRing m_ring;

        /// <summary>
        /// This is used to determine whether to ignore any existing token
        /// preserved from an earlier run.
        /// 
        /// Increment the value if a new token is required, the first time
        /// the launcher is run the version of the old token will be older and
        /// ignored. Once the new token is written the version number will be
        /// updated and subsequent logins should not require re-authentication.
        /// </summary>
        private const int TokenVersion = 2;

		public String GetTokenFor(String user)
		{
			if (m_mapped != null)
			{
				if (m_mapped.Tokens.ContainsKey(user))
				{
					return m_mapped.Tokens[user];
				}
			}
			else
			{
				if (m_container != null)
				{
					SetTokenFor(user, m_container.Token);
					return m_container.Token;
				}
			}
			return null;
		}

		public void SetTokenFor(String user, String token)
		{
			if (user==null)
			{
				return;
			}
			if (m_mapped == null)
			{
				m_mapped = new MappedTokenContainer();
			}

			if (m_mapped.Tokens.ContainsKey(user))
			{
				if (m_mapped.Tokens[user] == token)
				{
					return;
				}
			}
			if (token == null)
			{
				m_mapped.Tokens.Remove(user);
			}
			else
			{
				m_mapped.Tokens[user] = token;
			}
			StoreToken();
		}

        public TokenStorage()
        {
            m_ring = new DecoderRing();
            ReadToken();
        }

        private void ReadToken()
        {
            try
            {
                int tokenVersion = Properties.Settings.Default.TokenVersion;
                String content = Properties.Settings.Default.MachineToken;
                if (!String.IsNullOrEmpty(content))
                {
                    content = m_ring.Decode(content);
                    if (content != null)
                    {
						switch (tokenVersion)
						{
							case 1:
								{
									JavaScriptSerializer json = new JavaScriptSerializer();
									m_container = json.Deserialize<TokenContainer>(content);
									break;
								}
							case 2:
								{
									JavaScriptSerializer json = new JavaScriptSerializer();
									m_mapped = json.Deserialize<MappedTokenContainer>(content);
									break;
								}
						}
                    }
                }
            }
            catch (System.Exception)
            {
            	// Failed to load the token so the user will need to log in
                // again.
            }
        }

        private void StoreToken()
        {
			try
			{
				if (m_mapped == null) 
				{
					if (m_container != null)
					{
						JavaScriptSerializer json = new JavaScriptSerializer();
						String serialized = json.Serialize(m_container);
						serialized = m_ring.Encode(serialized);
						Properties.Settings.Default.MachineToken = serialized;
						Properties.Settings.Default.TokenVersion = 1;
						Properties.Settings.Default.Save();
					}
					else
					{
						Properties.Settings.Default.MachineToken = "";
						Properties.Settings.Default.TokenVersion = 0;
						Properties.Settings.Default.Save();
					}
				}
				else
				{
					JavaScriptSerializer json = new JavaScriptSerializer();
					String serialized = json.Serialize(m_mapped);
					serialized = m_ring.Encode(serialized);
					Properties.Settings.Default.MachineToken = serialized;
					Properties.Settings.Default.TokenVersion = 2;
					Properties.Settings.Default.Save();
				}
			}
			catch (System.Exception)
			{
				// Failed to store the token, user will have to log in
				// again next time.
			}
        }

        public void DiscardToken(String user)
        {
			SetTokenFor(user, null);
        }
    }
}
