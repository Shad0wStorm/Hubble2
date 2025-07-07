//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! AspectRationManager, keeps a windows aspect ratio
//
// Not currently used within the launcher project
//
//! Author:     Alan MacAree
//! Created:    07 Nov 2022
//----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace Launcher
{
    internal class AspectRationManager
    {

        private const byte c_posChange = 0x0046;
        private const byte c_zeroMovement = 0x2;

        private double _aspectRatio;
        private bool? _adjustingHeight = null;

        internal enum SWP
        {
            NOMOVE = 0x0002
        }
        internal enum WM
        {
            WINDOWPOSCHANGING = 0x0046,
            EXITSIZEMOVE = 0x0232,
        }


        [StructLayout( LayoutKind.Sequential )]
        internal struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        private const int c_leftEdge = 1;
        private const int c_rightEdge = 2;
        private const int c_topEdge = 3;
        private const int c_topLeftCorner = 4;
        private const int c_topRightCorner = 5;
        private const int c_bottemEdge = 6;
        private const int c_bottemLeftCorner = 7;
        private const int c_bottemRightCorner = 8;

        const int WM_SIZE = 0x0005;
        const int WM_SIZING = 0x0214;
        const int WM_WINDOWPOSCHANGING = 0x0046;

        [DllImport( "user32.dll" )]
        [return: MarshalAs( UnmanagedType.Bool )]
        internal static extern bool GetCursorPos( ref Win32Point pt );

        [StructLayout( LayoutKind.Sequential )]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static Point GetMousePosition() // mouse position relative to screen
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos( ref w32Mouse );
            return new Point( w32Mouse.X, w32Mouse.Y );
        }

        private AspectRationManager( Window _window )
        {
            ( (HwndSource)HwndSource.FromVisual( _window )).AddHook( DragHook );

            _aspectRatio = _window.Width / _window.Height;
        }

        public static void Manage( Window _window )
        {
            new AspectRationManager( _window );
        }

        private IntPtr DragHook( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handeled )
        {
            switch ( (WM)msg )
            {
                case WM.WINDOWPOSCHANGING:
                    {
                        WINDOWPOS pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                        if ( (pos.flags & (int)SWP.NOMOVE) == 0 )
                        {
                            Window wnd = (Window)HwndSource.FromHwnd(hwnd).RootVisual;
                            if ( wnd != null )
                            {
                                // determine what dimension is changed by detecting the mouse position relative to the 
                                // window bounds. if gripped in the corner, either will work.
                                //if ( !_adjustingHeight.HasValue )
                                {
                                    Point p = GetMousePosition();

                                    double diffWidth = Math.Min(Math.Abs(p.X - pos.x), Math.Abs(p.X - pos.x - pos.cx));
                                    double diffHeight = Math.Min(Math.Abs(p.Y - pos.y), Math.Abs(p.Y - pos.y - pos.cy));

                                    Console.WriteLine( "diffHeight={0}, diffWidth={1}", diffHeight, diffWidth );
                                    _adjustingHeight = diffHeight > diffWidth;
                                }

                                if ( _adjustingHeight.Value )
                                {
                                    Console.WriteLine( "Width change caused height to be changed" );
                                    pos.cy = (int)(pos.cx / _aspectRatio);
                                }
                                else
                                {
                                    Console.WriteLine( "Height change caused width to be changed" );
                                    pos.cx = (int)(pos.cy * _aspectRatio);
                                }
                            }
                        }

                        Marshal.StructureToPtr( pos, lParam, true );
                        handeled = true;
                    }
                    break;
                case WM.EXITSIZEMOVE:
                    Console.WriteLine( "EXITSIZEMOVE" );
                    _adjustingHeight = null; 
                    break;
            }

            return IntPtr.Zero;
        }
    }
}
