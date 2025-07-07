//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ProjectSelectionUserCtrl, display all the possible products the user 
//!                         can play,and allows the user to select one of those products.
//!                         of those products.
//
//! Author:     Alan MacAree
//! Created:    08 Dec 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for ProjectSelectionUserCtrl.xaml
    /// </summary>
    public partial class ProjectSelectionUserCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ProjectSelectionUserCtrl()
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
                // Get the project that has been clicked on
                AvailableProject availableProject = grid.DataContext as AvailableProject;
                if ( availableProject != null )
                {
                    availableProject.IsSelected = true;

                    // Make sure everything else is unselected
                    foreach ( AvailableProject ap in ProjectList )
                    {
                        // Don't unselect the item we just selected!
                        if (! ap.PrettyName.Equals( availableProject.PrettyName )  )
                        {
                            ap.IsSelected = false;
                        }
                    }
                }
            }

            // Raise the event to say the property has been changed
            if ( ProjectChangedEventHandler != null )
            {
                ProjectChangedEventHandler?.Invoke( sender, null );
            }
        }

        /// <summary>
        /// Returns the currently selected project 
        /// prettyname.
        /// </summary>
        /// <returns></returns>
        public string SelectedProjectName()
        {
            string selectedProjectName = "";
            for ( int idx = 0; idx < ProjectList.Count && selectedProjectName.Length == 0; idx++  )
            {
                if ( ProjectList[idx].IsSelected )
                {
                    selectedProjectName = ProjectList[idx].Name;
                }
            }

            return selectedProjectName;
        }

        /// <summary>
        /// Our list of AvailableProject
        /// </summary>
        private List<AvailableProject> m_projects;

        /// <summary>
        /// Our project selection changed handler, use this to be informed when the selected
        /// project is changed.
        /// </summary>
        public event SelectionChangedEventHandler ProjectChangedEventHandler;
    }
}



