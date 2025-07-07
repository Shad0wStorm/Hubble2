using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for ProjectSelectionDevUserCtrl.xaml
    /// </summary>
    public partial class ProjectSelectionDevUserCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ProjectSelectionDevUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Our list of AvailableProjects
        /// </summary>
        public List<AvailableProject> ProjectList
        {
            get { return m_projects; }
            set
            {
                m_projects = value;
#if DEBUG
                // Make sure we have only one 
                // project selected.
                EnsureOnlyOneIsSelected();
#endif
            }
        }

        /// <summary>
        /// Developer check method to make sure only one
        /// project is selected. Fails an assert if more 
        /// that one project is found as selected.
        /// </summary>
        private void EnsureOnlyOneIsSelected()
        {
            bool foundASelectedProject = false;

            foreach ( AvailableProject ap in ProjectList )
            {
                if ( ap.IsSelected )
                {
                    if ( foundASelectedProject )
                    {
                        // We should only have a single 
                        // project that is selected.
                        Debug.Assert( false );
                    }
                    foundASelectedProject = true;
                }
            }
        }

        /// <summary>
        /// User clicks on one of the contained items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPart_GridMouseDown( object sender, MouseButtonEventArgs e )
        {
            Grid grid = sender as Grid;
            if ( grid != null )
            {
                AvailableProject availableProject = grid.DataContext as AvailableProject;
                if ( availableProject != null )
                {
                    availableProject.IsSelected = true;
                    foreach ( AvailableProject ap in ProjectList )
                    {
                        // Don't unselect the item we just selected!
                        if ( !ap.PrettyName.Equals( availableProject.PrettyName ) )
                        {
                            ap.IsSelected = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current selected project 
        /// prettyname.
        /// </summary>
        /// <returns></returns>
        public string SelectedProjectName()
        {
            string selectedProjectName = "";
            for ( int idx = 0; idx < ProjectList.Count && selectedProjectName.Length == 0; idx++ )
            {
                if ( ProjectList[idx].IsSelected )
                {
                    selectedProjectName = ProjectList[idx].Name;
                }
            }
#if DEBUG
            // Make sure we have only one 
            // project selected.
            EnsureOnlyOneIsSelected();
#endif

            return selectedProjectName;
        }

        /// <summary>
        /// Our list of AvailableProject
        /// </summary>
        private List<AvailableProject> m_projects;
    }
}





