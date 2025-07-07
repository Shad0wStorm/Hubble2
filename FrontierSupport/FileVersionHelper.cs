using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrontierSupport
{
	/// <summary>
	/// Class representing a version number (e.g. FileVersionInfo).
	/// 
	/// Allows conversion from string and comparison (or at least those
	/// implemented).
	/// </summary>
	class FileVersionHelper
	{
		public short FileMajorPart = 0;
		public short FileMinorPart = 0;
		public short FileBuildPart = 0;
		public short FilePrivatePart = 0;

		public FileVersionHelper(String versionString)
		{
			String[] nodes = versionString.Split('.');

			short value;

			if (nodes.Length > 0)
			{
				if (short.TryParse(nodes[0], out value))
				{
					FileMajorPart = value;
					if (nodes.Length > 1)
					{
						if (short.TryParse(nodes[1], out value))
						{
							FileMinorPart = value;
							if (nodes.Length > 2)
							{
								if (short.TryParse(nodes[2], out value))
								{
									FileBuildPart = value;
									if(nodes.Length>3)
									{
										if (short.TryParse(nodes[3], out value))
										{
											FilePrivatePart = value;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public static bool operator >(FileVersionHelper left, FileVersionHelper right)
		{
			if (left.FileMajorPart > right.FileMajorPart)
			{
				return true;
			}
			if (left.FileMajorPart < right.FileMajorPart)
			{
				return false;
			}
			if (left.FileMinorPart > right.FileMinorPart)
			{
				return true;
			}
			if (left.FileMinorPart < right.FileMinorPart)
			{
				return false;
			}
			if (left.FileBuildPart > right.FileBuildPart)
			{
				return true;
			}
			if (left.FileBuildPart < right.FileBuildPart)
			{
				return false;
			}
			if (left.FilePrivatePart > right.FilePrivatePart)
			{
				return true;
			}
			return false;
		}

		public static bool operator <(FileVersionHelper left, FileVersionHelper right)
		{
			if (left.FileMajorPart < right.FileMajorPart)
			{
				return true;
			}
			if (left.FileMajorPart > right.FileMajorPart)
			{
				return false;
			}
			if (left.FileMinorPart < right.FileMinorPart)
			{
				return true;
			}
			if (left.FileMinorPart > right.FileMinorPart)
			{
				return false;
			}
			if (left.FileBuildPart < right.FileBuildPart)
			{
				return true;
			}
			if (left.FileBuildPart > right.FileBuildPart)
			{
				return false;
			}
			if (left.FilePrivatePart < right.FilePrivatePart)
			{
				return true;
			}
			return false;
		}
	}
}
