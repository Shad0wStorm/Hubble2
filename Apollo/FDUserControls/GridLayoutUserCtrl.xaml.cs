//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! GridLayoutUserCtrl UserControl, controls the dynamic creation
//! of a Grid and manages the placement of UIElements with that grid.
//
//! Author:     Alan MacAree
//! Created:    05 Oct 2022
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for GridLayoutUserCtrl.xaml
    /// </summary>
    public partial class GridLayoutUserCtrl : UserControl
    {
        /// <summary>
        /// Exposes the GapWidth as "GapWidth"
        /// This is used as a horizontal gap between 
        /// UIElements.
        /// </summary>
        public static readonly DependencyProperty GapWidthProperty =
            DependencyProperty.Register( nameof(GapWidth), typeof(double), typeof(GridLayoutUserCtrl));
        public double GapWidth
        {
            get { return (double)GetValue( GapWidthProperty ); }
            set { SetValue( GapWidthProperty, value ); }
        }

        /// <summary>
        /// Exposes the BorderWidth as "BorderWidth"
        /// This is used as a border width around each 
        /// panel within the Grid
        /// </summary>
        public static readonly DependencyProperty BorderWidthProperty =
            DependencyProperty.Register( nameof(BorderWidth), typeof(double), typeof(GridLayoutUserCtrl));
        public double BorderWidth
        {
            get { return (double)GetValue( BorderWidthProperty ); }
            set { SetValue( BorderWidthProperty, value ); }
        }

        /// <summary>
        /// Exposes the BorderHeight as "BorderHeight"
        /// This is used as a border height around each 
        /// panel within the Grid
        /// </summary>
        public static readonly DependencyProperty BorderHeightProperty =
            DependencyProperty.Register( nameof(BorderHeight), typeof(double), typeof(GridLayoutUserCtrl));
        public double BorderHeight
        {
            get { return (double)GetValue( BorderHeightProperty ); }
            set
            {
                SetValue( BorderHeightProperty, value );
            }
        }

        /// <summary>
        /// This is the number of columns that is required across the control
        /// </summary>
        public static readonly DependencyProperty NumberOfColumnsProperty =
            DependencyProperty.Register( nameof(NumberOfColumns), typeof(int), typeof(GridLayoutUserCtrl));
        public int NumberOfColumns
        {
            get { return (int)GetValue( NumberOfColumnsProperty ); }
            set
            {
                SetValue( NumberOfColumnsProperty, value );
            }
        }

        /// <summary>
        /// Determines the type of layout, defaults to UseAllOfGrid
        /// </summary>
        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register( nameof(Layout), typeof(LayoutOptions), typeof(GridLayoutUserCtrl));
        public LayoutOptions Layout
        {
            get { return (LayoutOptions)GetValue( LayoutProperty ); }
            set { SetValue( LayoutProperty, value ); }
        }

        /// <summary>
        /// The possible layout options for this control
        /// </summary>
        public enum LayoutOptions
        {
            UseAllOfGrid = 0,
            UseWholeTopRow,
            UseWholeLeftCol,
            UseWholeTopAndBottomRow,
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GridLayoutUserCtrl()
        {
            InitializeComponent();

            // Set the default values
            GapWidth = c_defaultGapWidth;
            BorderWidth = c_defaultBorderWidth;
            BorderHeight = c_defaultBorderHeight;
            NumberOfColumns = c_defaultNumberOfColumns;
            Layout = c_deaultLayoutOptions;
        }

        /// <summary>
        /// Adds a UIElement to the Grid in the position 
        /// calculated for the _panelNumber.
        /// </summary>
        /// <param name="_panelNumber">The panel number (starts from 1) to place the item in</param>
        /// <param name="_uIElement">The UIElement to add</param>
        /// <returns>true if the UIElement was added</returns>
        public bool AddUIElement( int _panelNumber, UIElement _uIElement )
        {
            bool elementAddedOkay = false;

            // We must have an UIElement to add
            Debug.Assert( _uIElement != null );
            // We must have created the grid
            Debug.Assert( m_gridCreated );
            // We must have a panelNumber > 0 (we don't care if it is bigger
            // that the grid allowed for, as we won't display it.
            Debug.Assert( _panelNumber > 0 );

            if ( m_gridCreated )
            {
                if ( _panelNumber <= m_maxNumberOfPanels )
                {
                    if ( _uIElement != null )
                    {
                        if ( _panelNumber > 0 )
                        {
                            int row = 0;
                            int col = 0;
                            int noOfCols = 1;
                            int noOfRows = 1;

                            PositionInfo( _panelNumber, out row, out col, out noOfRows, out noOfCols );
                            _uIElement.SetValue( Grid.RowProperty, row );
                            _uIElement.SetValue( Grid.ColumnProperty, col );
                            _uIElement.SetValue( Grid.RowSpanProperty, noOfRows );
                            _uIElement.SetValue( Grid.ColumnSpanProperty, noOfCols );

                            PART_DynamicGrid.Children.Add( _uIElement );
                            elementAddedOkay = true;
                        }
                    }
                }
            }

            return elementAddedOkay;
        }

        /// <summary>
        /// Creates the Grid layout using the number of
        /// panels that need to be included in the Grid.
        /// </summary>
        /// <param name="_noOfPanels"></param>
        /// <returns>true if the grid was created correctly</returns>
        public bool CreateGridLayout( int _noOfPanels )
        {
            bool gridCreatedOkay = false;

            // We must have at least one panel to display
            Debug.Assert( _noOfPanels > 0 );

            if ( _noOfPanels > 0 )
            {
                m_maxNumberOfPanels = _noOfPanels;
                m_gridCreated = CreateRowsAndColumns();
                gridCreatedOkay = m_gridCreated;
            }

            return gridCreatedOkay;
        }

        /// <summary>
        /// Gets the position information for a specific panel
        /// </summary>
        /// <param name="_panel">(in) The panel number, starting at 1</param>
        /// <param name="_row">(out) The row to place the panel</param>
        /// <param name="_col">(out) The col to place the panel</param>
        /// <param name="_noOfRows">(out) The number of rows to span</param>
        /// <param name="_noOfCols">(out) The number of cols to span</param>
        /// <returns>true if the row col position were calculated okay</returns>
        private bool PositionInfo( in int _panel, out int _row, out int _col, out int _noOfRows, out int _noOfCols )
        {
            bool positionCalculatedOkay = false;

            _row = 0;
            _col = 0;
            _noOfCols = 1;
            _noOfRows = 1;

            // _panel starts at one, but we index from zero
            if ( _panel <= m_maxNumberOfPanels )
            {
                try
                {
                    switch( Layout )
                    {
                        case LayoutOptions.UseAllOfGrid:
                            {
                                // Use all the Rows and Columns available
                                _row = ((int)Math.Floor( ((double)(_panel - 1) / (double)NumberOfColumns) ) * 2);
                                _col = (int)(((double)(_panel - 1) % (double)NumberOfColumns) * 2);
                                positionCalculatedOkay = true;
                                break;
                            }
                        case LayoutOptions.UseWholeLeftCol:
                            {
                                // Use the whole left col for one item and split the rest into rows and cols
                                if ( _panel == 1 )
                                {
                                    // 1st panel takes out the first column
                                    _row = 0;
                                    _col = 0;
                                    _noOfRows = PART_DynamicGrid.RowDefinitions.Count;
                                }
                                else
                                {
                                    // We have to shift the result right by one col, pretend we have one less col than
                                    // we have, and we pretend we are using one less panel number.
                                    int fakePanelNumber = _panel -1;
                                    _row = ((int)Math.Floor( ((double)(fakePanelNumber - 1) / (double)(NumberOfColumns-1) ) ) * 2);
                                    _col = (int)((double)(fakePanelNumber - 1) % (double)(NumberOfColumns - 1) * 2);
                                    // We add two cols because the first is the whole left col, the 2nd is a gap
                                    _col += 2;
                                }
                                positionCalculatedOkay = true;
                                break;
                            }
                        // We use the whole top row for the 1st item
                        case LayoutOptions.UseWholeTopRow:
                            {
                                // Use the whole top row for one item and split the rest into rows and cols
                                if ( _panel == 1 )
                                {
                                    // 1st panel takes out the top row
                                    _row = 0;
                                    _col = 0;
                                    _noOfCols = PART_DynamicGrid.ColumnDefinitions.Count;
                                }
                                else
                                {
                                    int adjustedPanel = _panel -1;
                                    // We have to shift the rows down by 2
                                    _row = ((int)Math.Floor( ((double)(adjustedPanel - 1) / (double)NumberOfColumns) )*2);
                                    _col = (int)(((double)(adjustedPanel - 1) % (double)NumberOfColumns) * 2);
                                    // We add two rows because one will be a gap
                                    _row += 2;
                                }
                                positionCalculatedOkay = true;
                                break;
                            }
                        case LayoutOptions.UseWholeTopAndBottomRow:
                            {
                                // Use the whole top row for one item and split the rest into rows and cols
                                if (_panel == 1)
                                {
                                    // 1st panel takes out the top row
                                    _row = 0;
                                    _col = 0;
                                    _noOfCols = PART_DynamicGrid.ColumnDefinitions.Count;
                                } else if(_panel == 6) {
                                    _col = 0;
                                    _row = (_panel - 2 / NumberOfColumns) + 1;
                                    _noOfCols = PART_DynamicGrid.ColumnDefinitions.Count;
                                }
                                else
                                {
                                    int adjustedPanel = _panel - 1;
                                    // We have to shift the rows down by 2
                                    _row = ((int)Math.Floor(((double)(adjustedPanel - 1) / (double)NumberOfColumns)) * 2);
                                    _col = (int)(((double)(adjustedPanel - 1) % (double)NumberOfColumns) * 2);
                                    // We add two rows because one will be a gap
                                    _row += 2;
                                }
                                positionCalculatedOkay = true;
                                break;
                            }
                        default:
                            {
                                // If we get here, someone has added a new value into LayoutOptions and
                                // has not allowed for that in this switch statement.
                                Debug.Assert( false );
                                positionCalculatedOkay = false;
                                break;
                            }
                    }
                }
                catch( Exception )
                {
                    // We can't error out, so fail by not doing anything.
                    Debug.Assert( false );
                    positionCalculatedOkay = false;
                }
            }


            return positionCalculatedOkay;
        }

        /// <summary>
        /// Create the number of rows and cols within our Grid.
        /// The existing Rows and Cols are cleared and the new calculated
        /// ones are added, this includes borders.
        /// </summary>
        /// <returns>true if the grid was created correctly</returns>
        private bool CreateRowsAndColumns()
        {
            bool gridCreatedOkay = true;

            PART_DynamicGrid.Children.Clear();

            try
            {
                // Calculate the number of RowDefinitions we need
                m_numberOfRows = CalulateNumberOfRowDefinitions();

                // Add the RowDefinitions that we need
                bool bAlreadyAddedRows = false;
                for ( int rowIdx = 0; rowIdx < m_numberOfRows; rowIdx++ )
                {
                    // Do we need to add a border between rows?
                    if ( bAlreadyAddedRows && rowIdx < m_numberOfRows )
                    {
                        GridLength borderGridLength = new GridLength( BorderHeight );
                        PART_DynamicGrid.RowDefinitions.Add( new RowDefinition() { Height = borderGridLength } );
                    }
                    bAlreadyAddedRows = true;

                    GridLength starGridLength = new GridLength( 1d, GridUnitType.Star );

                    PART_DynamicGrid.RowDefinitions.Add( new RowDefinition() { Height = starGridLength } );
                }

                // Add the ColumnDefinitions that we need
                bool bAlreadyAddedCols = false;
                for ( int colIdx = 0; colIdx < NumberOfColumns; colIdx++ )
                {
                    // Do we need to add a border between cols?
                    if ( bAlreadyAddedCols && colIdx < NumberOfColumns )
                    {
                        GridLength gapGridWidth = new GridLength( GapWidth );
                        PART_DynamicGrid.ColumnDefinitions.Add( new ColumnDefinition() { Width = gapGridWidth } );
                    }

                    GridLength starGridLength = new GridLength( 1d, GridUnitType.Star );

                    PART_DynamicGrid.ColumnDefinitions.Add( new ColumnDefinition() { Width = starGridLength } );

                    bAlreadyAddedCols = true;
                }
            }
            catch( Exception )
            {
                // We can't error out, so fail by not doing anything.
                Debug.Assert( false );
                gridCreatedOkay = false;
            }

            return gridCreatedOkay;
        }

        /// <summary>
        /// Calculates the number of RowDefinitions that we need to add
        /// </summary>
        /// <returns>The number of RowDefinitions</returns>
        private int CalulateNumberOfRowDefinitions()
        {
            int calculatedRowDefinitions = 1;

            switch ( Layout )
            {
                // We are placing items in each col that is not a gap
                case LayoutOptions.UseAllOfGrid:
                    {
                        calculatedRowDefinitions = (int)Math.Ceiling( (double)m_maxNumberOfPanels / (double)NumberOfColumns );
                        break;
                    }
                // We use the whole left col for the 1st item
                case LayoutOptions.UseWholeLeftCol:
                    {
                        if ( m_maxNumberOfPanels > 1 )
                        {
                            calculatedRowDefinitions = (int)Math.Ceiling( (double)(m_maxNumberOfPanels - 1) / (double)(NumberOfColumns - 1) );
                        }
                        else
                        {
                            calculatedRowDefinitions = 1;
                        }
                        break;
                    }
                // We use the whole top row for the 1st item
                case LayoutOptions.UseWholeTopRow:
                    {
                        if ( m_maxNumberOfPanels > 1 )
                        {
                            calculatedRowDefinitions = (int)Math.Ceiling( (((double)m_maxNumberOfPanels - 1) / (double)NumberOfColumns) + 1 );
                        }
                        else
                        {
                            calculatedRowDefinitions = 1;
                        }
                        break;
                    }
                case LayoutOptions.UseWholeTopAndBottomRow:
                    {
                        if (m_maxNumberOfPanels > 1)
                        {
                            calculatedRowDefinitions = (int)Math.Ceiling((((double)m_maxNumberOfPanels - 2) / (double)NumberOfColumns) + 2);
                        }
                        else
                        {
                            calculatedRowDefinitions = 1;
                        }
                        break;
                    }
                default:
                    {
                        // If we get here, someone has added a new value into LayoutOptions and
                        // has not allowed for that in this switch statement.
                        Debug.Assert( false );
                        calculatedRowDefinitions = (int)Math.Ceiling( (double)m_maxNumberOfPanels / (double)NumberOfColumns );
                        break;
                    }
            }

            return calculatedRowDefinitions;
        }

        /// <summary>
        /// The default Border width
        /// </summary>
        private double c_defaultBorderWidth = 0d;

        /// <summary>
        /// The default gap (between panels) width
        /// </summary>
        private double c_defaultGapWidth = 10d;

        /// <summary>
        /// The default border height
        /// </summary>
        private double c_defaultBorderHeight = 10d;

        /// <summary>
        /// The default number of columns
        /// </summary>
        private const int c_defaultNumberOfColumns = 2;

        /// <summary>
        /// The default layout
        /// </summary>
        private const LayoutOptions c_deaultLayoutOptions = LayoutOptions.UseAllOfGrid;

        /// <summary>
        /// The number of panels (places where UIElements may be placed)
        /// </summary>
        private int m_maxNumberOfPanels = 1;

        /// <summary>
        /// The number of rows in our Grid
        /// </summary>
        private int m_numberOfRows = 1;

        /// <summary>
        /// Has the grid been created, this is done by
        /// calling CreateGridLayout with the number
        /// of panels that should be displayed (>0)
        /// </summary>
        private bool m_gridCreated = false;
    }
}
