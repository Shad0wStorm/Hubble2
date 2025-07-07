using System;

namespace ClientSupport
{
	/// <summary>
	/// Class used to describe a project on the server.
	/// </summary>
	public class SKUDetails
	{
		public String m_name;
		public String m_sku;
		public String m_sortKey;
		public String m_directory;
		public bool m_testAPI;
		public String m_serverArgs;
		public String m_gameArgs;
		public String m_highlight;
		public String m_page;
		public String[] m_filters;
		public String m_box;
		public String m_hero;
		public String m_logo;
		public String m_esrbRating;
		public String m_pegiRating;
		public string m_gameApi;
		public int m_gameCode;
		public int m_maxDownloadThreads;
		public bool m_noDetails;

		public SKUDetails()
		{
			m_name = null;
			m_sku = null;
			m_sortKey = null;
			m_directory = null;
			m_testAPI = false;
			m_serverArgs = null;
			m_gameArgs = null;
			m_highlight = null;
			m_page = null;
			m_filters = null;
			m_box = null;
			m_hero = null;
			m_logo = null;
			m_esrbRating = null;
			m_pegiRating = null;
			m_gameApi = null;
			m_gameCode = 0;
			m_maxDownloadThreads = 0;
			m_noDetails = true;
		}

		public SKUDetails(SKUDetails copy)
		{
			m_name = copy.m_name;
			m_sku = copy.m_sku;
			m_sortKey = copy.m_sortKey;
			m_directory = copy.m_directory;
			m_testAPI = copy.m_testAPI;
			m_serverArgs = copy.m_serverArgs;
			m_gameArgs = copy.m_gameArgs;
			m_highlight = copy.m_highlight;
			m_page = copy.m_page;
			m_filters = copy.m_filters;
			m_box = copy.m_box;
			m_hero = copy.m_hero;
			m_logo = copy.m_logo;
			m_esrbRating = copy.m_esrbRating;
			m_pegiRating = copy.m_pegiRating;
			m_gameApi = copy.m_gameApi;
			m_gameCode = copy.m_gameCode;
			m_maxDownloadThreads = copy.m_maxDownloadThreads;
			m_noDetails = copy.m_noDetails;
		}
	}
}
