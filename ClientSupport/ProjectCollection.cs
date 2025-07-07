using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// A collection of projects.
    /// </summary>
    public class ProjectCollection
    {
        /// <summary>
        /// Map of projects being managed by the client.
        /// </summary>
        private SortedDictionary<String, Project> m_projects = new SortedDictionary<String, Project>();

        /// <summary>
        /// Folder containing all Project directories.
        /// </summary>
        private String m_projectRoot;

		private bool m_missingProducts = false;
		public bool MissingProducts
		{
			get
			{
				return m_missingProducts;
			}
		}

        // Currently the collection is underused and assumes a fixed root for
        // all projects.
        //
        // Replace the singular root with an ordered list of potential
        // locations. The first instance of a project in the list is considered
        // the live one.
        //
        // When adding a new project the first location which is writable
        // (and has sufficient space?) is selected.
        //
        // User can add/remove/reorder the list of potential locations.
        //
        // User can request that (manifest based) installs are moved from the
        // current location to an alternative.

        public ProjectCollection(String root)
        {
            m_projectRoot = root;
        }

        /// <summary>
        /// Returns true if the colection is empty
        /// </summary>
        /// <returns>true if empty, otherwise returns false</returns>
        public bool IsEmpty()
        {
            return (m_projects.Count == 0);
        }

        /// <summary>
        /// Update each of the projects in the provided array.
        /// 
        /// Projects not previously seen will be added, existing projects will
        /// be checked for status changes.
        /// 
        /// Does not currently perform a disk scan, so projects which have
        /// local folders, but are not passed in will not be covered.
        /// </summary>
        /// <param name="projectNames">Array of projects to check.</param>
        public void UpdateProjects(String[] projectNames)
        {
            DiscardInvalidProjects(projectNames);
            UpdateAndAddValidProjects(projectNames);
            AddOfflineProjects();
        }

        /// <summary>
        /// If the user has changed (log out/log in) or has some how got less
        /// products available than previously (time limited demos, or trading
        /// say) then make sure we do not give them access to anything that
        /// has been removed.
        /// </summary>
        /// <param name="projectNames">
        /// The list of projects the user is allowed to access.
        /// </param>
        private void DiscardInvalidProjects(String[] projectNames)
        {
            if (projectNames == null)
            {
                m_projects.Clear();
            }
            List<String> remove = new List<String>();
            foreach (String p in m_projects.Keys)
            {
                Project project = m_projects[p];
                // Leave offline projects hanging around, they will be tidied
                // up later if required.
                if (!project.Offline)
                {
                    if (projectNames != null)
                    {
                        if (!projectNames.Contains(p))
                        {
                            remove.Add(p);
                        }
                    }
                }
            }

            foreach (String p in remove)
            {
                m_projects.Remove(p);
            }
        }

        private void UpdateAndAddValidProjects(String[] projectNames)
        {
            if (projectNames!=null)
            {
                foreach (String projectName in projectNames)
                {
                    Project projectDetails = null;
                    if (m_projects.ContainsKey(projectName))
                    {
                        projectDetails = m_projects[projectName];
                        if (projectDetails.Action != Project.ActionType.Disabled)
                        {
                            // If the project is currently disabled is is being
                            // run/installed so the state is not modified.
                            projectDetails.Update();
                        }
                    }
                    else
                    {
                        projectDetails = new Project(projectName, m_projectRoot);
                        m_projects[projectName] = projectDetails;
                    }
                }
            }
        }

        private void AddOfflineProjects()
        {
            String[] candidates = Directory.GetDirectories(m_projectRoot);

            List<String> remove = new List<String>();
            foreach (String dir in candidates)
            {
                String projectName = Path.GetFileName(dir);
                if (!m_projects.ContainsKey(projectName))
                {
                    Project projectDetails = new Project(projectName, m_projectRoot);
                    if (projectDetails.Offline && projectDetails.Installed)
                    {
                        // New project available off line so add it.
                        m_projects[projectName] = projectDetails;
                    }
                }
                else
                {
                    Project existing = m_projects[projectName];
                    if (existing.Offline)
                    {
                        Project update = new Project(projectName, m_projectRoot);
                        if ((!update.Offline) || (!update.Installed))
                        {
                            // Project was available off line but is no longer
                            // so remove it.
                            remove.Add(projectName);
                        }
                    }
                }
            }
            foreach (String p in remove)
            {
                m_projects.Remove(p);
            }
        }

		public void FilterProjects(String[] filters)
		{
			if (filters!=null)
			{
				if (filters.Length>0)
				{
					bool missing = true;
					String[] projectNames = m_projects.Keys.ToArray();
					foreach (String projectName in projectNames)
					{
						Project p = m_projects[projectName];
						if (p.Filters != null)
						{
							bool found = false;
							foreach (String f in p.Filters)
							{
								if (filters.Contains(f.ToLower()))
								{
									missing = false;
									found = true;
									break;
								}
							}
							if (!found)
							{
								m_projects.Remove(projectName);
							}
						}
					}

					// Found at least one filtered product so no need to
					// register anything, until someone comes up with a more
					// complicated way to do things.
					m_missingProducts = missing;
				}
			}
		}

        /// <summary>
        /// Return a flat array of projects suitable for iteration.
        /// </summary>
        /// <returns>An array of known projects.</returns>
        public Project[] GetProjectArray()
        {
            Project[] result = m_projects.Values.ToArray();
            return result;
        }
    }
}
