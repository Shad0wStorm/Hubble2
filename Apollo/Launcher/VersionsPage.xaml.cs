//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! VersionsPage, display all the possible products the user can play,
//!               and allows the user to select one of those products.
//
//! Author:     Alan MacAree
//! Created:    08 Dec 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using JSONConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for VersionsPage.xaml
    /// </summary>
    public partial class VersionsPage : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The owning LauncherWindow</param>
        /// <param name="_frontPage">The FrontPage to inform when the select project changes</param>
        /// <param name="_previousPage">The previous page to return to</param>
        public VersionsPage( LauncherWindow _launcherWindow, FrontPage _frontPage, Page _previousPage )
        {
            Debug.Assert( _launcherWindow != null );
            Debug.Assert( _frontPage != null );
            Debug.Assert( _previousPage != null );

            m_launcherWindow = _launcherWindow;
            m_frontPage = _frontPage;
            m_previousPage = _previousPage;

            InitializeComponent();
            PopulatePage();
        }

        /// <summary>
        /// Populates the page with data.
        /// </summary>
        private void PopulatePage()
        {
            Debug.Assert( m_launcherWindow != null );

            List<AvailableProject> availableProductList = new List<AvailableProject>();

            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    Project selectedProduct = cobraBayView.GetActiveProject();
                    List<Project> projectList  = cobraBayView.m_manager.AvailableProjects.GetProjectArray().ToList();
                    projectList.Sort();

                    foreach ( Project project in projectList )
                    {
                        if ( ShouldDisplayProject( project ) )
                        {
                            string boxImageUri = project.BoxImageURI;
                            if ( string.IsNullOrWhiteSpace( boxImageUri ) )
                            {
                                boxImageUri = c_defaultProjectImage;
                            }

                            string projectName = project.Name;
                            string projectString = project.PrettyName;
#if DEVELOPMENT
                            bool needsLineBreak = (project.PrettyName.Length > 40);
                            if( needsLineBreak )
                            {
                                int stringStart = project.PrettyName.Length - 20;
                                projectString = projectString.Insert(stringStart, Environment.NewLine);
                                projectString = projectString.Substring(21);
                            }
#endif
                            CobraBayView m_cobraBayView = m_launcherWindow.GetCobraBayView();
                            GameDescription m_gameDescription = null;

                            if (m_cobraBayView != null && !project.Name.StartsWith("DEV-"))
                            {
                                FORCManager fORCManager = m_cobraBayView.Manager();
                                Debug.Assert(fORCManager != null);

                                if (fORCManager != null)
                                {
                                    if (project != null)
                                    {
                                        ServerInterface serverInterface = fORCManager.ServerConnection;
                                        Debug.Assert(serverInterface != null);

                                        if (serverInterface != null)
                                        {
                                            string jsonString = serverInterface.GetGameDescription(project);
                                            if (!string.IsNullOrWhiteSpace(jsonString))
                                            {
                                                m_gameDescription = JsonConverter.JsonToGameDescription(jsonString, m_cobraBayView);

                                            }
                                        }
                                    }
                                }
                            }

                            AvailableProject availableProject = new AvailableProject( projectName,
                                                                                      projectString,
                                                                                      "",
                                                                                      boxImageUri,
                                                                                      project.PrettyName.Equals(selectedProduct.PrettyName),
                                                                                      m_gameDescription != null ? m_gameDescription.Description.Replace(".", ".\n").Replace("!", "!\n").Replace("?", "?\n") : ""
                                                                                      );

                            availableProject.StatusText = ProjectStatusAsString( project );

                            availableProductList.Add( availableProject );
                        }
                    }

                    PART_ProjectSelectionUserCtrl.ProjectList = availableProductList;
                    PART_ProjectSelectionUserCtrl.ProjectChangedEventHandler += new SelectionChangedEventHandler( PART_ProjectsOnSelectionChanged );
                }
            }
        }

        /// <summary>
        /// Returns true if the project should be displayed
        /// in the page, this is based on the project status.
        /// </summary>
        /// <param name="_project">The project to check if it needs to be displayed</param>
        /// <returns>Returns true if the project should be displayed</returns>
        private bool ShouldDisplayProject( Project _project )
        {
            bool shouldDisplayProject = false;

            Debug.Assert( _project != null );

            if ( _project != null )
            {
                switch ( _project.Action )
                {
                    case Project.ActionType.Disabled:
                        shouldDisplayProject = false;
                        break;
                    case Project.ActionType.Install:
                        shouldDisplayProject = true;
                        break;
                    case Project.ActionType.Invalid:
                        shouldDisplayProject = false;
                        break;
                    case Project.ActionType.Play:
                        shouldDisplayProject = true;
                        break;
                    case Project.ActionType.Update:
                        shouldDisplayProject = true;
                        break;
                    default:
                        shouldDisplayProject = false;
                        break;
                }
            }

            return shouldDisplayProject;
        }
        /// <summary>
        /// Returns the state of a project as a string to display to the user
        /// </summary>
        /// <param name="_project">The project to the the state of</param>
        /// <returns>The project status to display to the user</returns>
        private string ProjectStatusAsString( Project _project )
        {
            string projectStatusString = LocalResources.Properties.Resources.MSG_VerInvalid; 

            Debug.Assert( _project != null );

            if ( _project != null )
            {
                switch( _project.Action )
                {
                    case Project.ActionType.Disabled:
                        projectStatusString = LocalResources.Properties.Resources.MSG_VerDisabled;
                        break;
                    case Project.ActionType.Install:
                        projectStatusString = LocalResources.Properties.Resources.MSG_VerNotInstalled;
                        break;
                    case Project.ActionType.Invalid:
                        projectStatusString = LocalResources.Properties.Resources.MSG_VerInvalid;
                        break;
                    case Project.ActionType.Play:
                        projectStatusString = LocalResources.Properties.Resources.MSG_VerCanPlay;
                        break;
                    case Project.ActionType.Update:
                        projectStatusString = LocalResources.Properties.Resources.MSG_VerUpdateRequired;
                        break;
                    default:
                        projectStatusString = LocalResources.Properties.Resources.MSG_VerInvalid;
                        break;
                }
            }

            return projectStatusString;
        }

        /// <summary>
        /// User clicks on the back button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackClick( object sender, RoutedEventArgs e )
        {
            // Close the page
            ClosePage();
        }

        /// <summary>
        /// Closes the page and navigates back to the previois page
        /// </summary>
        private void ClosePage()
        {
            // We should go back to the previous page, but we must also allow 
            // for issues in case we don't have one.
            if ( m_previousPage != null )
            {
                NavigationService.Navigate( m_previousPage );
            }
            else
            {
                NavigationService ns = NavigationService.GetNavigationService( this );
                ns.GoBack();
            }

            // Remove the event as we are closing this page.
            PART_ProjectSelectionUserCtrl.ProjectChangedEventHandler -= new SelectionChangedEventHandler( PART_ProjectsOnSelectionChanged );
        }

        /// <summary>
        /// Takes the selected project fom the UI and sets it
        /// in the model.
        /// </summary>
        private async Task SetSelectedProjectAsync()
        {
            await Task.Run( () =>
            {
                if ( m_launcherWindow != null )
                {
                    CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                    Debug.Assert( cobraBayView != null );
                    if ( cobraBayView != null )
                    {
                        cobraBayView.SelectedProject = PART_ProjectSelectionUserCtrl.SelectedProjectName();
                        _ = m_frontPage.UpdateDisplayedProjectAsync();
                        m_frontPage.WaitForAnyUIUpdates();
                    }
                }
            } );

            // Close the page
            ClosePage();
        }

        /// <summary>
        /// User double clicks on project
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_ProjectsOnSelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            // Make sure we set any newly selected project
            _ = SetSelectedProjectAsync();
        }

        /// <summary>
        /// The page we return to when we exit this one
        /// </summary>
        private Page m_previousPage = null;

        /// <summary>
        /// Our main Launcher Window
        /// </summary>
        private LauncherWindow m_launcherWindow;

        /// <summary>
        /// Our FrontPage
        /// </summary>
        private FrontPage m_frontPage;

        /// <summary>
        /// The default project image to use when we don't get one 
        /// from the server.
        /// </summary>
        private const string c_defaultProjectImage = "Images/ProjectDefaultImage.png";

    }
}
