//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! AvailableProject, is a representation of a project that is
//  available to a user.
//
//! Author:     Alan MacAree
//! Created:    07 Nov 2022
//----------------------------------------------------------------------


using System.Diagnostics;

namespace Launcher
{
    /// <summary>
    /// AvailableProject is a representation of a project that is
    /// available to a user.
    /// </summary>
    public class AvailableProject : PropertyChangedBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_name">The name of the project</param>
        /// <param name="_prettyName">The pretty name of the project</param>
        /// <param name="_noImageName">Displayed if provided</param>
        /// <param name="_statusText">The current Action status of the project</param>
        /// <param name="_imageUri">The image to display for this project</param>
        /// <param name="_isSelected">Is the project selected or not</param>
        /// <param name="_description">Is the project selected or not</param>
        public AvailableProject( string _name,
                                 string _prettyName,
                                 string _statusText,
                                 string _imageUri,
                                 bool _isSelected,
                                 string _description)
        {
            // We must have a name
            Debug.Assert( _name != null );
            // We must have a pretty name
            Debug.Assert( _prettyName != null );

            Name = _name;
            PrettyName = _prettyName;
            StatusText = _statusText;
            ImageUri = _imageUri;
            IsSelected = _isSelected;
            Description = _description;
        }

        /// <summary>
        /// The name for the project
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set
            {
                if ( m_Name != value )
                {
                    m_Name = value;
                    PropertyChange( nameof(Name) );
                }
            }
        }

        /// <summary>
        /// The pretty name for the project
        /// </summary>
        public string PrettyName
        {
            get { return m_prettyName; }
            set
            {
                if ( m_prettyName != value )
                {
                    m_prettyName = value;
                    PropertyChange( nameof( PrettyName ) );
                }
            }
        }

        /// <summary>
        /// Any status text for the project (i.e. NOT INSTALLED)
        /// </summary>c# 
        public string StatusText
        {
            get { return m_statusText; }
            set
            {
                if ( m_statusText != value )
                {
                    m_statusText = value;
                    PropertyChange( nameof( StatusText ) );
                }
            }
        }

        /// <summary>
        /// The project image Uri
        /// </summary>
        public string ImageUri
        {
            get { return m_imageUri; }
            set
            {
                if ( m_imageUri != value )
                {
                    m_imageUri = value;
                    PropertyChange( nameof( ImageUri ) );
                }
            }
        }

        /// <summary>
        /// Is the project selected?
        /// </summary>
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if ( m_isSelected != value )
                {
                    m_isSelected = value;
                    PropertyChange( nameof( IsSelected ) );
                }
            }
        }

        /// <summary>
        /// Description
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            set
            {
                if (m_Description != value)
                {
                    m_Description = value;
                    PropertyChange(nameof(m_Description));
                }
            }
        }

        /// <summary>
        /// Project name
        /// </summary>
        private string m_Name;

        /// <summary>
        /// Project Pretty name
        /// </summary>
        private string m_prettyName;

        /// <summary>
        /// Project status text
        /// </summary>
        private string m_statusText;

        /// <summary>
        /// Project image IRU
        /// </summary>
        private string m_imageUri;

        /// <summary>
        /// Is the project selected
        /// </summary>
        private bool m_isSelected;

        /// <summary>
        /// Description
        /// </summary>
        private string m_Description;
    }
}
