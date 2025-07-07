using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CobraBay
{
	/// <summary>
	/// Interaction logic for WOMessageBox.xaml
	/// </summary>
	public partial class WOMessageBox : Window, INotifyPropertyChanged
	{
		public WOMessageBox(bool forceSoftwareRendering)
		{
			if (forceSoftwareRendering)
			{
				this.Loaded += OnLoaded_ForceSoftwareRendering;
			}

			InitializeComponent();
			DataContext = this;
		}

		public enum Result {
			Left,
			Right,
			Close
		}

		public Result MessageBoxResult = Result.Close;

		public String m_titleText = "Message";
		public String TitleText
		{
			get
			{
				return m_titleText;
			}
			set
			{
				if (m_titleText != value)
				{
					m_titleText = value;
					RaisePropertyChanged("TitleText");
				}
			}
		}

		public String m_messageText = "Message content";
		public String MessageText
		{
			get
			{
				return m_messageText;
			}
			set
			{
				if (m_messageText != value)
				{
					m_messageText = value;
					MessageTextControl.Text = m_messageText;

					// Support binding but see SetMessageWithLinks below
					RaisePropertyChanged("MessageText");
				}
			}
		}

		public String m_leftButtonText = "OK";
		public String LeftButtonText
		{
			get
			{
				return m_leftButtonText;
			}
			set
			{
				if (m_leftButtonText != value)
				{
					m_leftButtonText = value;
					if (String.IsNullOrEmpty(m_leftButtonText))
					{
						LeftButton.Visibility = Visibility.Collapsed;
					}
					else
					{
						LeftButton.Visibility = Visibility.Visible;
					}
					RaisePropertyChanged("LeftButtonText");
				}
			}
		}

		public String m_rightButtonText = "Cancel";
		public String RightButtonText
		{
			get
			{
				return m_rightButtonText;
			}
			set
			{
				if (m_rightButtonText != value)
				{
					m_rightButtonText = value;
					if (String.IsNullOrEmpty(m_rightButtonText))
					{
						RightButton.Visibility = Visibility.Collapsed;
					}
					else
					{
						RightButton.Visibility = Visibility.Visible;
					}
					RaisePropertyChanged("RightButtonText");
				}
			}
		}

		private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
		{
			HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			HwndTarget hwndTarget = hwndSource.CompositionTarget;
			hwndTarget.RenderMode = RenderMode.SoftwareOnly;
		}

		private void OnClose(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void KeyPressed(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					{
						e.Handled = true;
						OnLeftClick(null, null);
						break;
					}
				case Key.Escape:
					{
						e.Handled = true;
						OnRightClick(null, null);
						break;
					}
			}
		}

		public void OnLeftClick(object sender, RoutedEventArgs e)
		{
			MessageBoxResult = Result.Left;
			Close();
		}

		public void OnRightClick(object sender, RoutedEventArgs e)
		{
			MessageBoxResult = Result.Right;
			Close();
		}

		/// <summary>
		/// Replace the default text block contents dynamically.
		/// 
		/// This will not work if the text property of the TextBlock is bound
		/// since the binding will reset it on the next update.
		/// </summary>
		/// <param name="message"></param>
		public void SetMessageWithLinks(String message)
		{
			SetMessageWithLinks(MessageTextControl, message);
		}

		/// <summary>
		/// Produce a series of Text and hyperlink runs for inline content to
		/// a target TextBlock.
		/// 
		/// This may be useful elsewhere in which case it can be moved out.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="message"></param>
		public void SetMessageWithLinks(TextBlock target, String message)
		{
			target.Inlines.Clear();
			string[] lines = message.Split('\n');
			foreach (String line in lines)
			{
				if (target.Inlines.Count()>0)
				{
					target.Inlines.Add(new LineBreak());
				}
				String lineText = line;
				while (!String.IsNullOrEmpty(lineText))
				{
					int segment = lineText.IndexOf('[');
					if (segment<0)
					{
						target.Inlines.Add(lineText);
						lineText = null;
					}
					else
					{
						if (segment == 0)
						{
							String linkText;
							int linkEnd = lineText.IndexOf(']');
							if (linkEnd>=0)
							{
								linkText = lineText.Substring(1, linkEnd-1);
								lineText = lineText.Substring(linkEnd+1);
							}
							else
							{
								linkText = lineText.Substring(1);
								lineText = null;
							}
							if (!String.IsNullOrEmpty(linkText))
							{
								Hyperlink link = new Hyperlink(new Run(linkText));
								link.NavigateUri = new Uri(linkText);
								target.Inlines.Add(link);
								link.Click += (s, a) =>
								{
									Process.Start(linkText);
								};
							}
						}
						else
						{
							target.Inlines.Add(lineText.Substring(0, segment));
							lineText = lineText.Substring(segment);
						}
					}
				}
			}
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
