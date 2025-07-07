using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace CBViewModel
{
	public class CBTheme : INotifyPropertyChanged
	{
		private static CBTheme m_instance;

		private CBTheme()
		{
			// Initialise default colours
			SetThemeColour(null);
		}

		public static CBTheme Theme
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = new CBTheme();
				}
				return m_instance;
			}
		}

		private Brush m_defaultButtonColour;
		public Brush DefaultButtonColour
		{
			get
			{
				return m_defaultButtonColour;
			}
			set
			{
				if (m_defaultButtonColour != value)
				{
					m_defaultButtonColour = value;
					RaisePropertyChanged("DefaultButtonColour");
				}
			}
		}

		private Brush m_pressedButtonColour;
		public Brush PressedButtonColour
		{
			get
			{
				return m_pressedButtonColour;
			}
			set
			{
				if (m_pressedButtonColour != value)
				{
					m_pressedButtonColour = value;
					RaisePropertyChanged("PressedButtonColour");
				}
			}
		}

		private Brush m_hoverButtonColour;
		public Brush HoverButtonColour
		{
			get
			{
				return m_hoverButtonColour;
			}
			set
			{
				if (m_hoverButtonColour != value)
				{
					m_hoverButtonColour = value;
					RaisePropertyChanged("HoverButtonColour");
				}
			}
		}

		// Just to be confusing this is actually a colour, not a brush.
		private Color m_selectedItemColour;
		public Color SelectedItemColour
		{
			get
			{
				return m_selectedItemColour;
			}
			set
			{
				if (m_selectedItemColour != value)
				{
					m_selectedItemColour = value;
					RaisePropertyChanged("SelectedItemColour");
				}
			}
		}

		public void SetThemeColour(String colour)
		{
			ColorConverter bc = new ColorConverter();
			Color palette = (Color)bc.ConvertFromInvariantString("#CB3301");
			if (!String.IsNullOrEmpty(colour))
			{
				try
				{
					palette = (Color)bc.ConvertFromInvariantString(colour);
				}
				catch (System.Exception)
				{
					// Silently fail and use the default colour
				}
			}
			DefaultButtonColour = new SolidColorBrush(palette);
			HoverButtonColour = new SolidColorBrush(AdjustBrightness(palette, 1.2f));
			SelectedItemColour = AdjustBrightness(palette, 0.7f);
			PressedButtonColour = new SolidColorBrush(SelectedItemColour);
		}

		private Color AdjustBrightness(Color colour, float scale)
		{
			byte r = AdjustChannel(colour.R, scale);
			byte g = AdjustChannel(colour.G, scale);
			byte b = AdjustChannel(colour.B, scale);
			return Color.FromArgb(colour.A, r, g, b);
		}

		private byte AdjustChannel( float value, float scale)
		{
			float result = value * scale;
			if (result > 255)
			{
				result = 255;
			}
			if (result < 0)
			{
				result = 0;
			}
			return (byte)result;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void RaisePropertyChanged(String property)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(property));
			}
		}
	}
}
