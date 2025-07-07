using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Utils
{
    public class WindowSetup : OptionsHandler
    {
        private bool m_setDimensions;
        private int m_width;
        private int m_height;
        private bool m_setPosition;
        private int m_posx;
        private int m_posy;
        private bool m_borderless;
        private String m_title;
        private System.Text.RegularExpressions.Regex m_geomtryre = new System.Text.RegularExpressions.Regex("(\\d+)x(\\d+)");
        private System.Text.RegularExpressions.Regex m_positionre = new System.Text.RegularExpressions.Regex("(-?\\d+)x(-?\\d+)");



        private Window m_window;

        public WindowSetup()
        {
        }

        public void SetParser(OptionsParser parser)
        {
            parser.AddCommand("--geometry", "800x600");
            parser.AddCommand("--position", null);
            parser.AddFlag("--borderless");
            parser.AddCommand("--title", null);

            parser.AddHandler(this);
        }

        public bool ExtractGeometryValues(OptionsParser parser, String value, bool _negative, ref int x, ref int y)
        {
            String arg = parser.Get(value);

            if (!String.IsNullOrEmpty(arg))
            {
                Match result;
                if (_negative)
                {
                    result = m_positionre.Match(arg);
                }
                else
                {
                    result = m_geomtryre.Match(arg);
                }
                if (result.Success)
                {
                    GroupCollection groups = result.Groups;
                    Group xval = groups[1];
                    Group yval = groups[2];
                    x = int.Parse(xval.Value);
                    y = int.Parse(yval.Value);
                    return true;
                }
            }
            return false;
        }

        public override void OptionsChanged(OptionsParser parser)
        {
            m_setDimensions = ExtractGeometryValues(parser, "--geometry", false, ref m_width, ref m_height);
            m_setPosition = ExtractGeometryValues(parser, "--position", true, ref m_posx, ref m_posy);

            m_borderless = parser.GetFlag("--borderless");

            m_title = parser.Get("--title");
        }

        public void SetupWindow(Window window)
        {
            m_window = window;
            if (m_setDimensions)
            {
                window.Width = m_width;
                window.Height = m_height;
            }
            if (m_setPosition)
            {
                window.Left = m_posx;
                window.Top = m_posy;
            }

            if (!String.IsNullOrEmpty(m_title))
            {
                window.Title = m_title;
            }

            if (m_borderless)
            {
                window.WindowStyle = WindowStyle.None;
                window.ResizeMode = ResizeMode.NoResize;
            }

            window.KeyUp += new System.Windows.Input.KeyEventHandler(KeyHandler);
        }

        void KeyHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                m_window.Close();
            }
        }
    }
}
