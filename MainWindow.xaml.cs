using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.ComponentModel; // needed for CancelEventArgs
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Srch {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        #region Variables
        // Internal
        public ICommand WindowClosing { get; private set; }
        Point PtMouseDown = new Point(0, 0);

        // Runtime Data
        internal Options options = null;
        private Options tmpOptions = null; // copy of Options while the search is ongoing
        enum ParseOptions {
            None,
            SearchOptions,
            SourcePath,
            Extensions,
            Editor1,
            Editor2,
            Editor3,
            Fontsize,
            Color
        };
        private int parseOptions = 0;
        private string[] searchResults = null;
        internal string searchString = null;
        internal string fileFilter = null;
        internal string fileSearchFileFilter = null;
        internal string searchFilePattern = null;
        internal string searchFilesString = null;
        internal string editor1 = null;
        internal string editor2 = null;
        internal string editor3 = null;
        internal int fontSize = 10;
        internal int color = 0;
        internal List<string> extensions = new List<string>();
        internal List<string> searchPaths = new List<string>();
        private string searchDir = null;
        private int counter = 0;
        private int fileCounter = 0;
        private static List<string> files = null;
        internal static StreamWriter sw = null;
        private static SearchWindow searchWindow = null;
        private static SearchFilesWindow searchFilesWindow = null;
        internal SettingsWindow settingsWindow = null;
        internal Queue<string> searchHistory = new Queue<string>();
        internal Queue<string> searchFilesHistory = new Queue<string>();

        // Threading
        static private int threads = Environment.ProcessorCount; // number of search threads
        static private bool[] threadInProgress = new bool[threads];
        private bool searchInProgress = false;
        private bool cancelSearch = false;
        private bool outOfMemory = false;
        private CancellationTokenSource[] cancelSearchArr = new CancellationTokenSource[threads];

        // Hotkeys
        private HwndSource _source;
        private const int HOTKEY_ID = 9000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKeyHelper(
            [In] IntPtr hWnd,
            [In] int id);

        #region KeyboardEmulation
            enum KeyModifier {
                None = 0,
                Alt = 1,
                Control = 2,
                Shift = 4,
                WinKey = 8
            }
            [DllImport("user32.dll")]
            internal static extern uint SendInput(
                uint nInputs,
                [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
                int cbSize);
            // Declare the INPUT struct
            [StructLayout(LayoutKind.Sequential)]
            public struct INPUT {
                internal uint type;
                internal InputUnion U;
                internal static int Size {
                    get { return Marshal.SizeOf(typeof(INPUT)); }
                }
            }
            // Declare the InputUnion struct
            [StructLayout(LayoutKind.Explicit)]
            internal struct InputUnion {
                [FieldOffset(0)]
                internal MOUSEINPUT mi;
                [FieldOffset(0)]
                internal KEYBDINPUT ki;
                [FieldOffset(0)]
                internal HARDWAREINPUT hi;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct MOUSEINPUT {
                internal int dx;
                internal int dy;
                internal MouseEventDataXButtons mouseData;
                internal MOUSEEVENTF dwFlags;
                internal uint time;
                internal UIntPtr dwExtraInfo;
            }
            [Flags]
            internal enum MouseEventDataXButtons : uint {
                Nothing = 0x00000000,
                XBUTTON1 = 0x00000001,
                XBUTTON2 = 0x00000002
            }
            [Flags]
            internal enum MOUSEEVENTF : uint {
                ABSOLUTE = 0x8000,
                HWHEEL = 0x01000,
                MOVE = 0x0001,
                MOVE_NOCOALESCE = 0x2000,
                LEFTDOWN = 0x0002,
                LEFTUP = 0x0004,
                RIGHTDOWN = 0x0008,
                RIGHTUP = 0x0010,
                MIDDLEDOWN = 0x0020,
                MIDDLEUP = 0x0040,
                VIRTUALDESK = 0x4000,
                WHEEL = 0x0800,
                XDOWN = 0x0080,
                XUP = 0x0100
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct KEYBDINPUT {
                internal VirtualKeyShort wVk;
                internal ScanCodeShort wScan;
                internal KEYEVENTF dwFlags;
                internal int time;
                internal UIntPtr dwExtraInfo;
            }

            [Flags]
            internal enum KEYEVENTF : uint {
                EXTENDEDKEY = 0x0001,
                KEYUP = 0x0002,
                SCANCODE = 0x0008,
                UNICODE = 0x0004
            }
            internal enum VirtualKeyShort : short {
                ///<summary>
                ///Left mouse button
                ///</summary>
                LBUTTON = 0x01,
                ///<summary>
                ///Right mouse button
                ///</summary>
                RBUTTON = 0x02,
                ///<summary>
                ///Control-break processing
                ///</summary>
                CANCEL = 0x03,
                ///<summary>
                ///Middle mouse button (three-button mouse)
                ///</summary>
                MBUTTON = 0x04,
                ///<summary>
                ///Windows 2000/XP: X1 mouse button
                ///</summary>
                XBUTTON1 = 0x05,
                ///<summary>
                ///Windows 2000/XP: X2 mouse button
                ///</summary>
                XBUTTON2 = 0x06,
                ///<summary>
                ///BACKSPACE key
                ///</summary>
                BACK = 0x08,
                ///<summary>
                ///TAB key
                ///</summary>
                TAB = 0x09,
                ///<summary>
                ///CLEAR key
                ///</summary>
                CLEAR = 0x0C,
                ///<summary>
                ///ENTER key
                ///</summary>
                RETURN = 0x0D,
                ///<summary>
                ///SHIFT key
                ///</summary>
                SHIFT = 0x10,
                ///<summary>
                ///CTRL key
                ///</summary>
                CONTROL = 0x11,
                ///<summary>
                ///ALT key
                ///</summary>
                MENU = 0x12,
                ///<summary>
                ///PAUSE key
                ///</summary>
                PAUSE = 0x13,
                ///<summary>
                ///CAPS LOCK key
                ///</summary>
                CAPITAL = 0x14,
                ///<summary>
                ///Input Method Editor (IME) Kana mode
                ///</summary>
                KANA = 0x15,
                ///<summary>
                ///IME Hangul mode
                ///</summary>
                HANGUL = 0x15,
                ///<summary>
                ///IME Junja mode
                ///</summary>
                JUNJA = 0x17,
                ///<summary>
                ///IME final mode
                ///</summary>
                FINAL = 0x18,
                ///<summary>
                ///IME Hanja mode
                ///</summary>
                HANJA = 0x19,
                ///<summary>
                ///IME Kanji mode
                ///</summary>
                KANJI = 0x19,
                ///<summary>
                ///ESC key
                ///</summary>
                ESCAPE = 0x1B,
                ///<summary>
                ///IME convert
                ///</summary>
                CONVERT = 0x1C,
                ///<summary>
                ///IME nonconvert
                ///</summary>
                NONCONVERT = 0x1D,
                ///<summary>
                ///IME accept
                ///</summary>
                ACCEPT = 0x1E,
                ///<summary>
                ///IME mode change request
                ///</summary>
                MODECHANGE = 0x1F,
                ///<summary>
                ///SPACEBAR
                ///</summary>
                SPACE = 0x20,
                ///<summary>
                ///PAGE UP key
                ///</summary>
                PRIOR = 0x21,
                ///<summary>
                ///PAGE DOWN key
                ///</summary>
                NEXT = 0x22,
                ///<summary>
                ///END key
                ///</summary>
                END = 0x23,
                ///<summary>
                ///HOME key
                ///</summary>
                HOME = 0x24,
                ///<summary>
                ///LEFT ARROW key
                ///</summary>
                LEFT = 0x25,
                ///<summary>
                ///UP ARROW key
                ///</summary>
                UP = 0x26,
                ///<summary>
                ///RIGHT ARROW key
                ///</summary>
                RIGHT = 0x27,
                ///<summary>
                ///DOWN ARROW key
                ///</summary>
                DOWN = 0x28,
                ///<summary>
                ///SELECT key
                ///</summary>
                SELECT = 0x29,
                ///<summary>
                ///PRINT key
                ///</summary>
                PRINT = 0x2A,
                ///<summary>
                ///EXECUTE key
                ///</summary>
                EXECUTE = 0x2B,
                ///<summary>
                ///PRINT SCREEN key
                ///</summary>
                SNAPSHOT = 0x2C,
                ///<summary>
                ///INS key
                ///</summary>
                INSERT = 0x2D,
                ///<summary>
                ///DEL key
                ///</summary>
                DELETE = 0x2E,
                ///<summary>
                ///HELP key
                ///</summary>
                HELP = 0x2F,
                ///<summary>
                ///0 key
                ///</summary>
                KEY_0 = 0x30,
                ///<summary>
                ///1 key
                ///</summary>
                KEY_1 = 0x31,
                ///<summary>
                ///2 key
                ///</summary>
                KEY_2 = 0x32,
                ///<summary>
                ///3 key
                ///</summary>
                KEY_3 = 0x33,
                ///<summary>
                ///4 key
                ///</summary>
                KEY_4 = 0x34,
                ///<summary>
                ///5 key
                ///</summary>
                KEY_5 = 0x35,
                ///<summary>
                ///6 key
                ///</summary>
                KEY_6 = 0x36,
                ///<summary>
                ///7 key
                ///</summary>
                KEY_7 = 0x37,
                ///<summary>
                ///8 key
                ///</summary>
                KEY_8 = 0x38,
                ///<summary>
                ///9 key
                ///</summary>
                KEY_9 = 0x39,
                ///<summary>
                ///A key
                ///</summary>
                KEY_A = 0x41,
                ///<summary>
                ///B key
                ///</summary>
                KEY_B = 0x42,
                ///<summary>
                ///C key
                ///</summary>
                KEY_C = 0x43,
                ///<summary>
                ///D key
                ///</summary>
                KEY_D = 0x44,
                ///<summary>
                ///E key
                ///</summary>
                KEY_E = 0x45,
                ///<summary>
                ///F key
                ///</summary>
                KEY_F = 0x46,
                ///<summary>
                ///G key
                ///</summary>
                KEY_G = 0x47,
                ///<summary>
                ///H key
                ///</summary>
                KEY_H = 0x48,
                ///<summary>
                ///I key
                ///</summary>
                KEY_I = 0x49,
                ///<summary>
                ///J key
                ///</summary>
                KEY_J = 0x4A,
                ///<summary>
                ///K key
                ///</summary>
                KEY_K = 0x4B,
                ///<summary>
                ///L key
                ///</summary>
                KEY_L = 0x4C,
                ///<summary>
                ///M key
                ///</summary>
                KEY_M = 0x4D,
                ///<summary>
                ///N key
                ///</summary>
                KEY_N = 0x4E,
                ///<summary>
                ///O key
                ///</summary>
                KEY_O = 0x4F,
                ///<summary>
                ///P key
                ///</summary>
                KEY_P = 0x50,
                ///<summary>
                ///Q key
                ///</summary>
                KEY_Q = 0x51,
                ///<summary>
                ///R key
                ///</summary>
                KEY_R = 0x52,
                ///<summary>
                ///S key
                ///</summary>
                KEY_S = 0x53,
                ///<summary>
                ///T key
                ///</summary>
                KEY_T = 0x54,
                ///<summary>
                ///U key
                ///</summary>
                KEY_U = 0x55,
                ///<summary>
                ///V key
                ///</summary>
                KEY_V = 0x56,
                ///<summary>
                ///W key
                ///</summary>
                KEY_W = 0x57,
                ///<summary>
                ///X key
                ///</summary>
                KEY_X = 0x58,
                ///<summary>
                ///Y key
                ///</summary>
                KEY_Y = 0x59,
                ///<summary>
                ///Z key
                ///</summary>
                KEY_Z = 0x5A,
                ///<summary>
                ///Left Windows key (Microsoft Natural keyboard) 
                ///</summary>
                LWIN = 0x5B,
                ///<summary>
                ///Right Windows key (Natural keyboard)
                ///</summary>
                RWIN = 0x5C,
                ///<summary>
                ///Applications key (Natural keyboard)
                ///</summary>
                APPS = 0x5D,
                ///<summary>
                ///Computer Sleep key
                ///</summary>
                SLEEP = 0x5F,
                ///<summary>
                ///Numeric keypad 0 key
                ///</summary>
                NUMPAD0 = 0x60,
                ///<summary>
                ///Numeric keypad 1 key
                ///</summary>
                NUMPAD1 = 0x61,
                ///<summary>
                ///Numeric keypad 2 key
                ///</summary>
                NUMPAD2 = 0x62,
                ///<summary>
                ///Numeric keypad 3 key
                ///</summary>
                NUMPAD3 = 0x63,
                ///<summary>
                ///Numeric keypad 4 key
                ///</summary>
                NUMPAD4 = 0x64,
                ///<summary>
                ///Numeric keypad 5 key
                ///</summary>
                NUMPAD5 = 0x65,
                ///<summary>
                ///Numeric keypad 6 key
                ///</summary>
                NUMPAD6 = 0x66,
                ///<summary>
                ///Numeric keypad 7 key
                ///</summary>
                NUMPAD7 = 0x67,
                ///<summary>
                ///Numeric keypad 8 key
                ///</summary>
                NUMPAD8 = 0x68,
                ///<summary>
                ///Numeric keypad 9 key
                ///</summary>
                NUMPAD9 = 0x69,
                ///<summary>
                ///Multiply key
                ///</summary>
                MULTIPLY = 0x6A,
                ///<summary>
                ///Add key
                ///</summary>
                ADD = 0x6B,
                ///<summary>
                ///Separator key
                ///</summary>
                SEPARATOR = 0x6C,
                ///<summary>
                ///Subtract key
                ///</summary>
                SUBTRACT = 0x6D,
                ///<summary>
                ///Decimal key
                ///</summary>
                DECIMAL = 0x6E,
                ///<summary>
                ///Divide key
                ///</summary>
                DIVIDE = 0x6F,
                ///<summary>
                ///F1 key
                ///</summary>
                F1 = 0x70,
                ///<summary>
                ///F2 key
                ///</summary>
                F2 = 0x71,
                ///<summary>
                ///F3 key
                ///</summary>
                F3 = 0x72,
                ///<summary>
                ///F4 key
                ///</summary>
                F4 = 0x73,
                ///<summary>
                ///F5 key
                ///</summary>
                F5 = 0x74,
                ///<summary>
                ///F6 key
                ///</summary>
                F6 = 0x75,
                ///<summary>
                ///F7 key
                ///</summary>
                F7 = 0x76,
                ///<summary>
                ///F8 key
                ///</summary>
                F8 = 0x77,
                ///<summary>
                ///F9 key
                ///</summary>
                F9 = 0x78,
                ///<summary>
                ///F10 key
                ///</summary>
                F10 = 0x79,
                ///<summary>
                ///F11 key
                ///</summary>
                F11 = 0x7A,
                ///<summary>
                ///F12 key
                ///</summary>
                F12 = 0x7B,
                ///<summary>
                ///F13 key
                ///</summary>
                F13 = 0x7C,
                ///<summary>
                ///F14 key
                ///</summary>
                F14 = 0x7D,
                ///<summary>
                ///F15 key
                ///</summary>
                F15 = 0x7E,
                ///<summary>
                ///F16 key
                ///</summary>
                F16 = 0x7F,
                ///<summary>
                ///F17 key  
                ///</summary>
                F17 = 0x80,
                ///<summary>
                ///F18 key  
                ///</summary>
                F18 = 0x81,
                ///<summary>
                ///F19 key  
                ///</summary>
                F19 = 0x82,
                ///<summary>
                ///F20 key  
                ///</summary>
                F20 = 0x83,
                ///<summary>
                ///F21 key  
                ///</summary>
                F21 = 0x84,
                ///<summary>
                ///F22 key, (PPC only) Key used to lock device.
                ///</summary>
                F22 = 0x85,
                ///<summary>
                ///F23 key  
                ///</summary>
                F23 = 0x86,
                ///<summary>
                ///F24 key  
                ///</summary>
                F24 = 0x87,
                ///<summary>
                ///NUM LOCK key
                ///</summary>
                NUMLOCK = 0x90,
                ///<summary>
                ///SCROLL LOCK key
                ///</summary>
                SCROLL = 0x91,
                ///<summary>
                ///Left SHIFT key
                ///</summary>
                LSHIFT = 0xA0,
                ///<summary>
                ///Right SHIFT key
                ///</summary>
                RSHIFT = 0xA1,
                ///<summary>
                ///Left CONTROL key
                ///</summary>
                LCONTROL = 0xA2,
                ///<summary>
                ///Right CONTROL key
                ///</summary>
                RCONTROL = 0xA3,
                ///<summary>
                ///Left MENU key
                ///</summary>
                LMENU = 0xA4,
                ///<summary>
                ///Right MENU key
                ///</summary>
                RMENU = 0xA5,
                ///<summary>
                ///Windows 2000/XP: Browser Back key
                ///</summary>
                BROWSER_BACK = 0xA6,
                ///<summary>
                ///Windows 2000/XP: Browser Forward key
                ///</summary>
                BROWSER_FORWARD = 0xA7,
                ///<summary>
                ///Windows 2000/XP: Browser Refresh key
                ///</summary>
                BROWSER_REFRESH = 0xA8,
                ///<summary>
                ///Windows 2000/XP: Browser Stop key
                ///</summary>
                BROWSER_STOP = 0xA9,
                ///<summary>
                ///Windows 2000/XP: Browser Search key 
                ///</summary>
                BROWSER_SEARCH = 0xAA,
                ///<summary>
                ///Windows 2000/XP: Browser Favorites key
                ///</summary>
                BROWSER_FAVORITES = 0xAB,
                ///<summary>
                ///Windows 2000/XP: Browser Start and Home key
                ///</summary>
                BROWSER_HOME = 0xAC,
                ///<summary>
                ///Windows 2000/XP: Volume Mute key
                ///</summary>
                VOLUME_MUTE = 0xAD,
                ///<summary>
                ///Windows 2000/XP: Volume Down key
                ///</summary>
                VOLUME_DOWN = 0xAE,
                ///<summary>
                ///Windows 2000/XP: Volume Up key
                ///</summary>
                VOLUME_UP = 0xAF,
                ///<summary>
                ///Windows 2000/XP: Next Track key
                ///</summary>
                MEDIA_NEXT_TRACK = 0xB0,
                ///<summary>
                ///Windows 2000/XP: Previous Track key
                ///</summary>
                MEDIA_PREV_TRACK = 0xB1,
                ///<summary>
                ///Windows 2000/XP: Stop Media key
                ///</summary>
                MEDIA_STOP = 0xB2,
                ///<summary>
                ///Windows 2000/XP: Play/Pause Media key
                ///</summary>
                MEDIA_PLAY_PAUSE = 0xB3,
                ///<summary>
                ///Windows 2000/XP: Start Mail key
                ///</summary>
                LAUNCH_MAIL = 0xB4,
                ///<summary>
                ///Windows 2000/XP: Select Media key
                ///</summary>
                LAUNCH_MEDIA_SELECT = 0xB5,
                ///<summary>
                ///Windows 2000/XP: Start Application 1 key
                ///</summary>
                LAUNCH_APP1 = 0xB6,
                ///<summary>
                ///Windows 2000/XP: Start Application 2 key
                ///</summary>
                LAUNCH_APP2 = 0xB7,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard.
                ///</summary>
                OEM_1 = 0xBA,
                ///<summary>
                ///Windows 2000/XP: For any country/region, the '+' key
                ///</summary>
                OEM_PLUS = 0xBB,
                ///<summary>
                ///Windows 2000/XP: For any country/region, the ',' key
                ///</summary>
                OEM_COMMA = 0xBC,
                ///<summary>
                ///Windows 2000/XP: For any country/region, the '-' key
                ///</summary>
                OEM_MINUS = 0xBD,
                ///<summary>
                ///Windows 2000/XP: For any country/region, the '.' key
                ///</summary>
                OEM_PERIOD = 0xBE,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard.
                ///</summary>
                OEM_2 = 0xBF,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard. 
                ///</summary>
                OEM_3 = 0xC0,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard. 
                ///</summary>
                OEM_4 = 0xDB,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard. 
                ///</summary>
                OEM_5 = 0xDC,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard. 
                ///</summary>
                OEM_6 = 0xDD,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard. 
                ///</summary>
                OEM_7 = 0xDE,
                ///<summary>
                ///Used for miscellaneous characters; it can vary by keyboard.
                ///</summary>
                OEM_8 = 0xDF,
                ///<summary>
                ///Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard
                ///</summary>
                OEM_102 = 0xE2,
                ///<summary>
                ///Windows 95/98/Me, Windows NT 4.0, Windows 2000/XP: IME PROCESS key
                ///</summary>
                PROCESSKEY = 0xE5,
                ///<summary>
                ///Windows 2000/XP: Used to pass Unicode characters as if they were keystrokes.
                ///The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information,
                ///see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
                ///</summary>
                PACKET = 0xE7,
                ///<summary>
                ///Attn key
                ///</summary>
                ATTN = 0xF6,
                ///<summary>
                ///CrSel key
                ///</summary>
                CRSEL = 0xF7,
                ///<summary>
                ///ExSel key
                ///</summary>
                EXSEL = 0xF8,
                ///<summary>
                ///Erase EOF key
                ///</summary>
                EREOF = 0xF9,
                ///<summary>
                ///Play key
                ///</summary>
                PLAY = 0xFA,
                ///<summary>
                ///Zoom key
                ///</summary>
                ZOOM = 0xFB,
                ///<summary>
                ///Reserved 
                ///</summary>
                NONAME = 0xFC,
                ///<summary>
                ///PA1 key
                ///</summary>
                PA1 = 0xFD,
                ///<summary>
                ///Clear key
                ///</summary>
                OEM_CLEAR = 0xFE
            }
            internal enum ScanCodeShort : short {
                LBUTTON = 0,
                RBUTTON = 0,
                CANCEL = 70,
                MBUTTON = 0,
                XBUTTON1 = 0,
                XBUTTON2 = 0,
                BACK = 14,
                TAB = 15,
                CLEAR = 76,
                RETURN = 28,
                SHIFT = 42,
                CONTROL = 29,
                MENU = 56,
                PAUSE = 0,
                CAPITAL = 58,
                KANA = 0,
                HANGUL = 0,
                JUNJA = 0,
                FINAL = 0,
                HANJA = 0,
                KANJI = 0,
                ESCAPE = 1,
                CONVERT = 0,
                NONCONVERT = 0,
                ACCEPT = 0,
                MODECHANGE = 0,
                SPACE = 57,
                PRIOR = 73,
                NEXT = 81,
                END = 79,
                HOME = 71,
                LEFT = 75,
                UP = 72,
                RIGHT = 77,
                DOWN = 80,
                SELECT = 0,
                PRINT = 0,
                EXECUTE = 0,
                SNAPSHOT = 84,
                INSERT = 82,
                DELETE = 83,
                HELP = 99,
                KEY_0 = 11,
                KEY_1 = 2,
                KEY_2 = 3,
                KEY_3 = 4,
                KEY_4 = 5,
                KEY_5 = 6,
                KEY_6 = 7,
                KEY_7 = 8,
                KEY_8 = 9,
                KEY_9 = 10,
                KEY_A = 30,
                KEY_B = 48,
                KEY_C = 46,
                KEY_D = 32,
                KEY_E = 18,
                KEY_F = 33,
                KEY_G = 34,
                KEY_H = 35,
                KEY_I = 23,
                KEY_J = 36,
                KEY_K = 37,
                KEY_L = 38,
                KEY_M = 50,
                KEY_N = 49,
                KEY_O = 24,
                KEY_P = 25,
                KEY_Q = 16,
                KEY_R = 19,
                KEY_S = 31,
                KEY_T = 20,
                KEY_U = 22,
                KEY_V = 47,
                KEY_W = 17,
                KEY_X = 45,
                KEY_Y = 21,
                KEY_Z = 44,
                LWIN = 91,
                RWIN = 92,
                APPS = 93,
                SLEEP = 95,
                NUMPAD0 = 82,
                NUMPAD1 = 79,
                NUMPAD2 = 80,
                NUMPAD3 = 81,
                NUMPAD4 = 75,
                NUMPAD5 = 76,
                NUMPAD6 = 77,
                NUMPAD7 = 71,
                NUMPAD8 = 72,
                NUMPAD9 = 73,
                MULTIPLY = 55,
                ADD = 78,
                SEPARATOR = 0,
                SUBTRACT = 74,
                DECIMAL = 83,
                DIVIDE = 53,
                F1 = 59,
                F2 = 60,
                F3 = 61,
                F4 = 62,
                F5 = 63,
                F6 = 64,
                F7 = 65,
                F8 = 66,
                F9 = 67,
                F10 = 68,
                F11 = 87,
                F12 = 88,
                F13 = 100,
                F14 = 101,
                F15 = 102,
                F16 = 103,
                F17 = 104,
                F18 = 105,
                F19 = 106,
                F20 = 107,
                F21 = 108,
                F22 = 109,
                F23 = 110,
                F24 = 118,
                NUMLOCK = 69,
                SCROLL = 70,
                LSHIFT = 42,
                RSHIFT = 54,
                LCONTROL = 29,
                RCONTROL = 29,
                LMENU = 56,
                RMENU = 56,
                BROWSER_BACK = 106,
                BROWSER_FORWARD = 105,
                BROWSER_REFRESH = 103,
                BROWSER_STOP = 104,
                BROWSER_SEARCH = 101,
                BROWSER_FAVORITES = 102,
                BROWSER_HOME = 50,
                VOLUME_MUTE = 32,
                VOLUME_DOWN = 46,
                VOLUME_UP = 48,
                MEDIA_NEXT_TRACK = 25,
                MEDIA_PREV_TRACK = 16,
                MEDIA_STOP = 36,
                MEDIA_PLAY_PAUSE = 34,
                LAUNCH_MAIL = 108,
                LAUNCH_MEDIA_SELECT = 109,
                LAUNCH_APP1 = 107,
                LAUNCH_APP2 = 33,
                OEM_1 = 39,
                OEM_PLUS = 13,
                OEM_COMMA = 51,
                OEM_MINUS = 12,
                OEM_PERIOD = 52,
                OEM_2 = 53,
                OEM_3 = 41,
                OEM_4 = 26,
                OEM_5 = 43,
                OEM_6 = 27,
                OEM_7 = 40,
                OEM_8 = 0,
                OEM_102 = 86,
                PROCESSKEY = 0,
                PACKET = 0,
                ATTN = 0,
                CRSEL = 0,
                EXSEL = 0,
                EREOF = 93,
                PLAY = 0,
                ZOOM = 98,
                NONAME = 0,
                PA1 = 0,
                OEM_CLEAR = 0,
            }
            /// <summary>
            /// Define HARDWAREINPUT struct
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct HARDWAREINPUT {
                internal int uMsg;
                internal short wParamL;
                internal short wParamH;
            }
        #endregion
        #endregion
        public MainWindow() {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            options = new Options(); // search options container
            #region ParseFromCommandLine
            switch (args.Length) {
                case 1:
                    break;
                case 2:
                    searchPaths.Add(args[1]);
                    break;
                default:
                    break;
            }
            #endregion
            ParseOptionsFromFile("default_options.txt");
            searchResults = new string[threads];
            sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
            Action action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
            Dispatcher.Invoke(action);
            PrintCurrentSearchPath();
        }
        #region GlobalHotkeyRegistration
        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey(); 
        }
        private void RegisterHotKey() {
            var helper = new WindowInteropHelper(this);
            const uint VK_ENTER = 0x0D;
            const uint MOD_CTRL = 0x0002;
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL, VK_ENTER)) {
                tbMainText("Error: Global hotkeys are already registered to another application.");
            }
        }
        private void UnregisterHotKeyHelper() {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            const int WM_HOTKEY = 0x0312;
            switch (msg) {
                case WM_HOTKEY:
                    switch (wParam.ToInt32()) {
                        case HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }
        private void OnHotKeyPressed() {
            INPUT[] Inputs = new INPUT[2];
            INPUT Input = new INPUT();

            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.LCONTROL;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[0] = Input;

            Input.type = 1; // 1 = Keyboard Input
            Input.U.ki.wScan = ScanCodeShort.KEY_C;
            Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
            Inputs[1] = Input;

            SendInput(2, Inputs, INPUT.Size);
            Thread.Sleep(80);
            StartSearch(Clipboard.GetText(), "");
        }
        protected override void OnClosed(EventArgs e) {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKeyHelper();
            base.OnClosed(e);
        }
        #endregion
        internal async void StartSearchFiles(string searchString, string filePattern) {
            if (!searchInProgress) {
                if (filePattern.Equals("")) filePattern = "*";
                cancelSearch = false;
                for (int i = 0; i < threads; i++) cancelSearchArr[i] = new CancellationTokenSource();
                searchInProgress = true;
                tbMainText("Searching for <" + searchString + ">\n");
                await Task.Run(() => SetupSearchFilesThreads(searchString, filePattern));
            } else {
                CancelSearch();
                while (searchInProgress) ; /* wait until search has stopped */
                if (filePattern.Equals("")) filePattern = "*";
                cancelSearch = false;
                for (int i = 0; i < threads; i++) cancelSearchArr[i] = new CancellationTokenSource();
                searchInProgress = true;
                tbMainText("");
                await Task.Run(() => SetupSearchFilesThreads(searchString, filePattern));
            }
        }
        private void SetupSearchFilesThreads(string searchString, string filePattern) { // this method sets up and handles parallel searchthread execution
            counter = 0;
            long startTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            Action action = () => { progressBar.IsIndeterminate = true; /* start animation */ };
            Dispatcher.Invoke(action);
            files = new List<string>(); // create an Array of search Files filtered by search pattern
            List<string> tmpSearchPaths = new List<string>(); tmpSearchPaths = this.searchPaths; // create a temporary copy of the working data while the search is ongoing.
            List<string> tmpExtensions = new List<string>();  tmpExtensions = this.extensions;
            tmpOptions = this.options;
            foreach (string s in tmpSearchPaths) {
                string[] tmpFiles = null;
                try {
                    if (tmpOptions.GetValue(Options.AvailableOptions.SearchSubDirectories))
                        tmpFiles = Directory.GetFiles(s, filePattern, SearchOption.AllDirectories);
                    else
                        tmpFiles = Directory.GetFiles(s, filePattern, SearchOption.TopDirectoryOnly);
                } catch (DirectoryNotFoundException dnfe) {
                    Console.Error.WriteLine("Error: Search path not found.");
                    tbMainText("Error: Search path not found.");
                    action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                    Dispatcher.Invoke(action);
                    searchInProgress = false;
                    return;
                } catch (InvalidOperationException ioe) {
                    Console.Error.WriteLine("Error: Filepattern invalid.");
                    tbMainText("Error: Filepattern invalid.");
                    action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                    Dispatcher.Invoke(action);
                    searchInProgress = false;
                    return;
                } catch (ArgumentException ae) {
                    Console.Error.WriteLine("Error: Filepattern invalid.");
                    tbMainText("Error: Filepattern invalid.");
                    action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                    Dispatcher.Invoke(action);
                    searchInProgress = false;
                    return;
                }
                if (tmpExtensions[0].Equals("*"))
                    for (int i = 0; i < tmpFiles.Length; i++)
                        files.Add(tmpFiles[i].ToLower()); /* do not filter files, just add them as is */
                else {
                    for (int i = 0; i < tmpFiles.Length; i++) {
                        string fileToLower = tmpFiles[i].ToLower();
                        foreach (string ext in tmpExtensions) {
                            if (fileToLower.EndsWith("." + ext.ToLower()))
                                files.Add(tmpFiles[i]);
                        }
                    }
                }
            }
            tbMainText("");
            foreach (string filename in files) {
                try {
                    Match match = Regex.Match(filename, searchString, RegexOptions.IgnoreCase); // before beginning the search, check if the RegEx Format is correct
                    if (match.Success) {
                        tbMainAppend(filename + "\t(" + 0 + ")\t\n");
                        counter++;
                    }
                } catch (System.ArgumentException e) {
                    tbMainText("Unrecognized RegEx format.");
                    break;
                }
            }
            if (cancelSearch) {
                tbMainText("Search canceled.");
                if (outOfMemory) {
                    Console.Error.WriteLine("Error: Out of Memory.");
                    tbMainText("Error: Out of Memory.");
                    outOfMemory = false;
                }
            } else {
                long endTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                tbMainAppend("(" + (endTime - startTime) + "ms): <" + searchString + "> was found in " + counter + " files");
            }
            action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
            Dispatcher.Invoke(action);
            tbMainScrollToEnd();
            searchInProgress = false;
            cancelSearch = false;    
        }
        internal async void StartSearch(string searchString, string filePattern) {
            if (!searchInProgress) {
                if (filePattern.Equals("")) filePattern = "*";
                cancelSearch = false;
                for (int i = 0; i < threads; i++) cancelSearchArr[i] = new CancellationTokenSource();
                searchInProgress = true;
                tbMainText("Searching for <" + searchString + ">\n");
                await Task.Run(() => SetupSearchThreads(searchString, filePattern));
            } else {
                CancelSearch();
                while(searchInProgress); /* wait until search has stopped */
                if (filePattern.Equals("")) filePattern = "*";
                cancelSearch = false;
                for (int i = 0; i < threads; i++) cancelSearchArr[i] = new CancellationTokenSource();
                searchInProgress = true;
                tbMainText("");
                await Task.Run(() => SetupSearchThreads(searchString, filePattern));
            }
        }
        private async void SetupSearchThreads(string searchString, string filePattern) { // this method sets up and handles parallel searchthread execution
            bool unrecognizedRegExFormat = false;
            int start = 0;
            counter = 0;
            fileCounter = 0;
            int charIndex = 0;
            char searchChar = (char) 0;
            long startTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            Action action = () => { progressBar.IsIndeterminate = true; /* start animation */ };            
            Dispatcher.Invoke(action);
            files = new List<string>(); // create an Array of search Files filtered by search pattern
            List<string> tmpSearchPaths = new List<string>(); tmpSearchPaths = this.searchPaths; // create a temporary copy of the working data while the search is ongoing.
            List<string> tmpExtensions = new List<string>();  tmpExtensions = this.extensions;
            tmpOptions = this.options;
            foreach (string s in tmpSearchPaths) {
                string[] tmpFiles = null;
                try {
                    if (tmpOptions.GetValue(Options.AvailableOptions.SearchSubDirectories))
                        tmpFiles = Directory.GetFiles(s, filePattern, SearchOption.AllDirectories);
                    else
                        tmpFiles = Directory.GetFiles(s, filePattern, SearchOption.TopDirectoryOnly);
                } catch (DirectoryNotFoundException dnfe) {
                    Console.Error.WriteLine("Error: Search path not found.");
                    tbMainText("Error: Search path not found.");
                    action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                    Dispatcher.Invoke(action);
                    searchInProgress = false;
                    return;
                } catch (InvalidOperationException ioe) {
                    Console.Error.WriteLine("Error: Filepattern invalid.");
                    tbMainText("Error: Filepattern invalid.");
                    action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                    Dispatcher.Invoke(action);
                    searchInProgress = false;
                    return;
                } catch (ArgumentException ae) {
                    Console.Error.WriteLine("Error: Filepattern invalid.");
                    tbMainText("Error: Filepattern invalid.");
                    action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                    Dispatcher.Invoke(action);
                    searchInProgress = false;
                    return;
                }
                if (tmpExtensions[0].Equals("*"))
                    for (int i = 0; i < tmpFiles.Length; i++)
                        files.Add(tmpFiles[i].ToLower()); /* do not filter files, just add them as is */
                else {
                    for (int i = 0; i < tmpFiles.Length; i++) {
                        string fileToLower = tmpFiles[i].ToLower();
                        foreach (string ext in tmpExtensions) {
                            if (fileToLower.EndsWith("." + ext.ToLower()))
                                files.Add(tmpFiles[i]);
                        }
                    }
                }
            }
            int filesCount = files.Count;
            int filesPerThread = filesCount / threads;
            int[] filesPerThreadArr = new int[threads];
            filesPerThreadArr[0] = filesCount - (filesPerThread * (threads - 1));
            for (int i = 1; i < threads; i++) {
                filesPerThreadArr[i] = filesPerThread;
            }
            List<string>[] filesArr = new List<string>[threads];
            for (int i = 0; i < filesArr.Length; i++) {
                filesArr[i] = files.GetRange(start, filesPerThreadArr[i]);
                start += filesPerThreadArr[i];
            }
            int optionId = 0; /* selector */
            List<Option> oList = tmpOptions.GetList();
            for (int i = 0; i < 4; i++) { /* first 4 options are the radio buttons to specifiy the RegEx mode */
                if (oList[i].GetValue() == true)
                    optionId = i;
            }
            switch (optionId) {
                case (int)Options.AvailableOptions.Default:
                    charIndex = LanguageConventions.GetRarestCharIndex((string)searchString); // default search w/o RegEx speed can be improved by searching for the rarest char
                    searchChar = ((string)searchString)[charIndex];
                    if (tmpOptions.GetValue(Options.AvailableOptions.CaseSensitive)) {
                        for (int i = 0; i < threads; i++) { // create multiple searchthreads
                            int id = i;
                            threadInProgress[id] = true;
                            try {
                                await Task.Run(() => ParseFiles(filesArr[id].ToArray(), (string)searchString, searchChar, charIndex, id, cancelSearchArr[id].Token), cancelSearchArr[id].Token);
                            } catch (OperationCanceledException e) { break; }
                        }
                    } else {
                        char searchCh = Char.ToLower(searchChar);
                        string searchStr = (string)searchString.ToLower();
                        for (int i = 0; i < threads; i++) { // create multiple searchthreads
                            int id = i;
                            threadInProgress[id] = true;
                            try {
                                await Task.Run(() => ParseFilesCaseInsensitive(filesArr[id].ToArray(), searchStr, searchCh, charIndex, id, cancelSearchArr[id].Token), cancelSearchArr[id].Token);
                            } catch (OperationCanceledException e) { break; }
                        }
                    }
                    break;
                case (int)Options.AvailableOptions.WholeWordsOnly:
                    charIndex = LanguageConventions.GetRarestCharIndex((string)searchString); // default search w/o RegEx speed can be improved by searching for the rarest char
                    searchChar = ((string)searchString)[charIndex];
                    if (tmpOptions.GetValue(Options.AvailableOptions.CaseSensitive)) {
                        for (int i = 0; i < threads; i++) { // create multiple searchthreads
                            int id = i;
                            threadInProgress[id] = true;
                            try {
                                await Task.Run(() => ParseFilesWholeWordsOnly(filesArr[id].ToArray(), (string)searchString, Char.ToLower(searchChar), charIndex, id, cancelSearchArr[id].Token), cancelSearchArr[id].Token);
                            } catch (OperationCanceledException e) { break; }
                        }
                    } else {
                        char searchCh = Char.ToLower(searchChar);
                        string searchStr = (string)searchString.ToLower();
                        for (int i = 0; i < threads; i++) { // create multiple searchthreads
                            int id = i;
                            threadInProgress[id] = true;
                            try {
                                await Task.Run(() => ParseFilesCaseInsensitiveWholeWordsOnly(filesArr[id].ToArray(), searchStr, searchCh, charIndex, id, cancelSearchArr[id].Token), cancelSearchArr[id].Token);
                            } catch (OperationCanceledException e) { break; }
                        }
                    }
                    break;
                case (int)Options.AvailableOptions.FastRegEx:
                    FastRegEx fREx;
                    try {
                        fREx = new FastRegEx(searchString);
                    } catch (FormatException fex) {
                        tbMainText("Unrecognized RegEx format.");
                        unrecognizedRegExFormat = true;
                        break;
                    }
                    for (int i = 0; i < threads; i++) { // create multiple searchthreads
                        int id = i;
                        threadInProgress[id] = true;
                        try {
                            await Task.Run(() => ParseFilesFastRegEx(filesArr[id].ToArray(), fREx, tmpOptions.GetValue(Options.AvailableOptions.CaseSensitive), id, cancelSearchArr[id].Token), cancelSearchArr[id].Token);
                        } catch (OperationCanceledException e) { break; }
                    }
                    break;
                case (int)Options.AvailableOptions.NETRegEx:
                    try {
                        Match match = Regex.Match("", searchString, RegexOptions.IgnoreCase); // before beginning the search, check if the RegEx Format is correct
                    } catch (System.ArgumentException e) {
                        tbMainText("Unrecognized RegEx format.");
                        unrecognizedRegExFormat = true;
                    }
                    if (!unrecognizedRegExFormat) {
                        for (int i = 0; i < threads; i++) { // create multiple searchthreads
                            int id = i;
                            threadInProgress[id] = true;
                            try {
                                await Task.Run(() => ParseFilesRegEx(filesArr[id].ToArray(), (string)searchString.ToLower(), tmpOptions.GetValue(Options.AvailableOptions.CaseSensitive), id, cancelSearchArr[id].Token), cancelSearchArr[id].Token);
                            } catch (OperationCanceledException e) { break; }
                        }
                    }
                    break;
                default:
                    break;
            }
            if (!unrecognizedRegExFormat) {
                bool allThreadsCompleted = true;
                bool[] resultsShown = new bool[threads];
                for (int i = 0; i < resultsShown.Length; i++) resultsShown[i] = false;
                action = () => { progressBar.IsIndeterminate = true; /* start animation */ };
                Dispatcher.Invoke(action);
                while (allThreadsCompleted) {
                    if (cancelSearch) break;
                    allThreadsCompleted = false;
                    for (int i = 0; i < threads; i++) {
                        if (!threadInProgress[i]) {
                        } else {
                            allThreadsCompleted = true;
                        }
                    } // busywait until all threads finish 
                }
                tbMainText("");
                for (int i = 0; i < threads; i++) { // update UI on completion of all threads
                    tbMainAppend(searchResults[i]);
                }
                if (cancelSearch) {
                    tbMainText("Search canceled.");
                    if (outOfMemory) {
                        Console.Error.WriteLine("Error: Out of Memory.");
                        tbMainText("Error: Out of Memory.");
                        outOfMemory = false;
                    }
                } else {
                    long endTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                    tbMainAppend("(" + (endTime - startTime) + "ms): <" + searchString + "> was found " + counter + " times in " + fileCounter + " files");
                }
                action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
                Dispatcher.Invoke(action);
                tbMainScrollToEnd();
                cancelSearch = false;
            }
            searchInProgress = false;
        }
        private void OpenEditor(ProcessStartInfo startInfo) {
            try {
                using (Process exeProcess = Process.Start(startInfo)) { // Start the process with the info specified. Call WaitForExit and then the using statement will close.
                    exeProcess.WaitForExit();
                }
            } catch (Exception e) {
                tbMainText("Error: Unknown Error when opening the Editor.");
            }
        }
        private void CancelSearch() {
            if (searchInProgress) { /* cancel ongoing search */
                cancelSearch = true;
                for (int i = 0; i < threads; i++) {
                    if (cancelSearchArr[i] != null)
                        cancelSearchArr[i].Cancel();
                }
            }
        }
        private async void OpenEditorAsync(ProcessStartInfo startInfo) {
            await Task.Run(() => OpenEditor(startInfo));
        }
        private void ParseFiles(string[] files, string searchString, char searchChar, int charIndex, int ThreadId, CancellationToken cancelSearch) {
            StringWriter stringWriter = new StringWriter();
            try {
                int charIndexLast = (searchString.Length - charIndex - 1); // number of chars after the charIndex (inside the searchString)
                foreach (string file in files) {
                    bool foundInFile = false;
                    FileInfo f = new FileInfo(file);
                    string text = File.ReadAllText(file);
                    int index = 0;
                    index = text.IndexOf(searchChar);
                    while (index != -1) {
                        bool found = true;
                        int i = index + charIndexLast;
                        if (i < text.Length) {
                            cancelSearch.ThrowIfCancellationRequested();
                            if (index - charIndex >= 0) {
                                for (int j = searchString.Length - 1; j >= 0; j--) {
                                    if (searchString[j] == text[i]) {
                                    } else {
                                        found = false; // abort on wrong char
                                        break;
                                    }
                                    i--;
                                }
                                if (found) {
                                    counter++;
                                    if (!foundInFile) {
                                        foundInFile = true;
                                        fileCounter++;
                                    }
                                    int lineStart = 0;
                                    if (i != -1)
                                        lineStart = text.LastIndexOf(LanguageConventions.newLine[1], i);
                                    int lineEnd = text.IndexOf(LanguageConventions.newLine[1], index + charIndexLast + 1);
                                    if (lineStart != -1 && lineEnd != -1) {
                                        int lineNumber = StringUtil.GetLineNumberFromIndex(text, lineStart + 1);
                                        if (lineNumber == 1)
                                            stringWriter.WriteLine(f.FullName + "\t(" + lineNumber  + ")\t" + text.Substring(lineStart, lineEnd - 1 - lineStart - 1).TrimStart());
                                        else
                                            stringWriter.WriteLine(f.FullName + "\t(" + lineNumber + ")\t" + text.Substring(lineStart + 1, lineEnd - 1 - lineStart - 1).TrimStart());                                       
                                    } else if (lineStart == -1 && lineEnd != -1) {
                                        stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart) + ")\t" + text.Substring(0, lineEnd - 1 - lineStart - 1).TrimStart());
                                    } else if (lineStart != -1 && lineEnd == -1) {
                                        stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart + 1) + ")\t" + text.Substring(lineStart + 1, text.Length - lineStart - 1).TrimStart());
                                    } else {
                                        stringWriter.WriteLine(f.FullName + "\t" + "Error @ Index" + index);
                                    }
                                    if (tmpOptions.GetValue(Options.AvailableOptions.OnlyShow1EntryPerLine)) {
                                        if (lineEnd != -1) /* end of file ? */
                                            index = text.IndexOf(searchChar, lineEnd + 1);
                                        else
                                            break;
                                    } else {
                                        index = text.IndexOf(searchChar, index + charIndexLast + 1);
                                    }
                                } else {
                                    index = text.IndexOf(searchChar, index + 1);
                                }
                            } else {
                                index = text.IndexOf(searchChar, index + 1);
                            }
                        } else {
                            break;
                        }
                    }
                }
                searchResults[ThreadId] = stringWriter.ToString();
            } catch (OperationCanceledException e) {
            } catch (OutOfMemoryException e) {
                this.cancelSearch = true;
                this.outOfMemory = true;
            }
            threadInProgress[ThreadId] = false;
            stringWriter.Close();
        }
        private void ParseFilesFastRegEx(string[] files, FastRegEx fREx, bool caseSensitive, int ThreadId, CancellationToken cancelSearch) {
            StringWriter stringWriter = new StringWriter();
            RegexOptions regExOpt = RegexOptions.None;
            if (!caseSensitive)
                regExOpt = RegexOptions.IgnoreCase;
            try {
                foreach (string file in files) {
                    bool foundInFile = false;
                    FileInfo f = new FileInfo(file);
                    int linenumber = 0;
                    string line;
                    StreamReader streamReader = new StreamReader(file);
                    while ((line = streamReader.ReadLine()) != null) { // Read the file line by line.
                        linenumber++;
                        try {
                            if (fREx.Match(line, regExOpt)) {
                                counter++;
                                if (!foundInFile) {
                                    foundInFile = true;
                                    fileCounter++;
                                }
                                stringWriter.WriteLine(f.FullName + "\t(" + linenumber + ")\t" + line);
                            }
                        } catch (FormatException e) {
                            stringWriter.WriteLine(f.FullName + "\t" + "Error: Unrecognized RegEx format @ Line" + linenumber);
                            break;
                        }
                        cancelSearch.ThrowIfCancellationRequested();
                    }
                    streamReader.Close();
                }
                searchResults[ThreadId] = stringWriter.ToString();
            } catch (OperationCanceledException e) {
            } catch (OutOfMemoryException e) {
                this.cancelSearch = true;
                this.outOfMemory = true;
            }
            threadInProgress[ThreadId] = false;
            stringWriter.Close();
        }
        private void ParseFilesCaseInsensitive(string[] files, string searchString, char searchChar, int charIndex, int ThreadId, CancellationToken cancelSearch) {
            StringWriter stringWriter = new StringWriter();
            try {
                int charIndexLast = (searchString.Length - charIndex - 1); // number of chars after the charIndex (inside the searchString)
                foreach (string file in files) {
                    bool foundInFile = false;
                    FileInfo f = new FileInfo(file);
                    string text = File.ReadAllText(file);
                    string textToLower = text.ToLower();
                    int index = 0;
                    index = textToLower.IndexOf(searchChar);
                    while (index != -1) {
                        bool found = true;
                        int i = index + charIndexLast;
                        if (i < textToLower.Length) {
                            cancelSearch.ThrowIfCancellationRequested();
                            if (index - charIndex >= 0) {
                                for (int j = searchString.Length - 1; j >= 0; j--) {
                                    if (searchString[j] == textToLower[i]) {
                                    } else {
                                        found = false; // abort on wrong char
                                        break;
                                    }
                                    i--;
                                }
                                if (found) {
                                    counter++;
                                    if (!foundInFile) {
                                        foundInFile = true;
                                        fileCounter++;
                                    }
                                    int lineStart = 0;
                                    if (i != -1)
                                        lineStart = textToLower.LastIndexOf(LanguageConventions.newLine[1], i);
                                    int lineEnd = textToLower.IndexOf(LanguageConventions.newLine[1], index + charIndexLast + 1);
                                    if (lineStart != -1 && lineEnd != -1) {
                                        int lineNumber = StringUtil.GetLineNumberFromIndex(text, lineStart + 1);
                                        if (lineNumber == 1)
                                            stringWriter.WriteLine(f.FullName + "\t(" + lineNumber + ")\t" + text.Substring(lineStart, lineEnd - 1 - lineStart - 1).TrimStart());
                                        else
                                            stringWriter.WriteLine(f.FullName + "\t(" + lineNumber + ")\t" + text.Substring(lineStart + 1, lineEnd - 1 - lineStart - 1).TrimStart());
                                    } else if (lineStart == -1 && lineEnd != -1) {
                                        stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart) + ")\t" + text.Substring(0, lineEnd - 1 - lineStart - 1).TrimStart());
                                    } else if (lineStart != -1 && lineEnd == -1) {
                                        stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart + 1) + ")\t" + text.Substring(lineStart + 1, textToLower.Length - lineStart - 1).TrimStart());
                                    } else {
                                        stringWriter.WriteLine(f.FullName + "\t" + "Error @ Index" + index);
                                    }
                                    if (tmpOptions.GetValue(Options.AvailableOptions.OnlyShow1EntryPerLine)) {
                                        if (lineEnd != -1) /* end of file ? */
                                            index = textToLower.IndexOf(searchChar, lineEnd + 1);
                                        else
                                            break;
                                    } else {
                                        index = textToLower.IndexOf(searchChar, index + charIndexLast + 1);
                                    }
                                } else {
                                    index = textToLower.IndexOf(searchChar, index + 1);
                                }
                            } else {
                                index = textToLower.IndexOf(searchChar, index + 1);
                            }
                        } else {
                            break;
                        }
                    }
                } 
                searchResults[ThreadId] = stringWriter.ToString();
            } catch (OperationCanceledException e) {
            } catch (OutOfMemoryException e) {
                this.cancelSearch = true;
                this.outOfMemory = true;
            }
            threadInProgress[ThreadId] = false;
            stringWriter.Close();
        }
        private void ParseFilesWholeWordsOnly(string[] files, string searchString, char searchChar, int charIndex, int ThreadId, CancellationToken cancelSearch) {
            StringWriter stringWriter = new StringWriter();
            try {
                int charIndexLast = (searchString.Length - charIndex - 1); // number of chars after the charIndex (inside the searchString)
                foreach (string file in files) {
                    bool foundInFile = false;
                    FileInfo f = new FileInfo(file);
                    string text = File.ReadAllText(file);
                    int index = 0;
                    index = text.IndexOf(searchChar);
                    while (index != -1) {
                        bool found = true;
                        int i = index + charIndexLast;
                        if (i < text.Length) {
                            cancelSearch.ThrowIfCancellationRequested();
                            if (index - charIndex >= 0) {
                                for (int j = searchString.Length - 1; j >= 0; j--) {
                                    if (searchString[j] == text[i]) {
                                    } else {
                                        found = false; // abort on wrong char
                                        break;
                                    }
                                    i--;
                                }
                                if (found) {
                                    bool lastCharBlank = false;
                                    bool firstCharBlank = false;
                                    if (index + charIndexLast == text.Length - 1)
                                        lastCharBlank = true;
                                    if (0 == (index - charIndex)) 
                                        firstCharBlank = true;
                                    if (!(firstCharBlank && lastCharBlank)) {
                                        if (firstCharBlank) {
                                            foreach (char c in LanguageConventions.spaces) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                            }
                                            foreach (char c in LanguageConventions.newLine) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                            }
                                        } else if (lastCharBlank) {
                                            foreach (char c in LanguageConventions.spaces) {
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                            foreach (char c in LanguageConventions.newLine) {
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                        } else {
                                            foreach (char c in LanguageConventions.spaces) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                            foreach (char c in LanguageConventions.newLine) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                        }
                                    }
                                    if (!(firstCharBlank && lastCharBlank)) { /* cancel, if not a whole word */
                                    } else {
                                        counter++;
                                        if (!foundInFile) {
                                            foundInFile = true;
                                            fileCounter++;
                                        }
                                        int lineStart = 0;
                                        if (i != -1)
                                            lineStart = text.LastIndexOf(LanguageConventions.newLine[1], i);
                                        int lineEnd = text.IndexOf(LanguageConventions.newLine[1], index + charIndexLast + 1);
                                        if (lineStart != -1 && lineEnd != -1) {
                                            stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart + 1) + ")\t" + text.Substring(lineStart + 1, lineEnd - 1 - lineStart - 1).TrimStart());
                                        } else if (lineStart == -1 && lineEnd != -1) {
                                            stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart) + ")\t" + text.Substring(0, lineEnd - 1 - lineStart - 1).TrimStart());
                                        } else if (lineStart != -1 && lineEnd == -1) {
                                            stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart + 1) + ")\t" + text.Substring(lineStart + 1, text.Length - lineStart - 1).TrimStart());
                                        } else {
                                            stringWriter.WriteLine(f.FullName + "\t" + "Error @ Index" + index);
                                        }
                                        if (tmpOptions.GetValue(Options.AvailableOptions.OnlyShow1EntryPerLine)) {
                                            if (lineEnd != -1) /* end of file ? */
                                                index = text.IndexOf(searchChar, lineEnd + 1);
                                            else
                                                break;
                                        } else {
                                            index = text.IndexOf(searchChar, index + charIndexLast + 1);
                                        }
                                    }
                                } else {
                                    index = text.IndexOf(searchChar, index + 1);
                                }
                            } else {
                                index = text.IndexOf(searchChar, index + 1);
                            }
                        } else {
                            break;
                        }
                    }
                }
                searchResults[ThreadId] = stringWriter.ToString();
            } catch (OperationCanceledException e) {
            } catch (OutOfMemoryException e) {
                this.cancelSearch = true;
                this.outOfMemory = true;
            }
            threadInProgress[ThreadId] = false;
            stringWriter.Close();
        }
        private void ParseFilesCaseInsensitiveWholeWordsOnly(string[] files, string searchString, char searchChar, int charIndex, int ThreadId, CancellationToken cancelSearch) {
            StringWriter stringWriter = new StringWriter();
            try {
                int charIndexLast = (searchString.Length - charIndex - 1); // number of chars after the charIndex (inside the searchString)
                foreach (string file in files) {
                    bool foundInFile = false;
                    FileInfo f = new FileInfo(file);
                    string text = File.ReadAllText(file);
                    string textToLower = text.ToLower();
                    int index = 0;
                    index = textToLower.IndexOf(searchChar);
                    while (index != -1) {
                        bool found = true;
                        int i = index + charIndexLast;
                        if (i < textToLower.Length) {
                            cancelSearch.ThrowIfCancellationRequested();
                            if (index - charIndex >= 0) {
                                for (int j = searchString.Length - 1; j >= 0; j--) {
                                    if (searchString[j] == textToLower[i]) {
                                    } else {
                                        found = false; // abort on wrong char
                                        break;
                                    }
                                    i--;
                                }
                                if (found) {
                                    bool lastCharBlank = false;
                                    bool firstCharBlank = false;
                                    if (index + charIndexLast == text.Length - 1)
                                        lastCharBlank = true;
                                    if (0 == (index - charIndex)) 
                                        firstCharBlank = true;
                                    if (!(firstCharBlank && lastCharBlank)) {
                                        if (firstCharBlank) {
                                            foreach (char c in LanguageConventions.spaces) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                            }
                                            foreach (char c in LanguageConventions.newLine) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                            }
                                        } else if (lastCharBlank) {
                                            foreach (char c in LanguageConventions.spaces) {
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                            foreach (char c in LanguageConventions.newLine) {
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                        } else {
                                            foreach (char c in LanguageConventions.spaces) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                            foreach (char c in LanguageConventions.newLine) {
                                                if (c == (char)text[index + charIndexLast + 1])
                                                    lastCharBlank = true;
                                                if (c == (char)text[index - charIndex - 1])
                                                    firstCharBlank = true;
                                            }
                                        }
                                    }
                                    if (!(firstCharBlank && lastCharBlank)) { /* cancel and increment index, if not a whole word was found */
                                        index++;
                                    } else {
                                        counter++;
                                        if (!foundInFile) {
                                            foundInFile = true;
                                            fileCounter++;
                                        }
                                        int lineStart = textToLower.LastIndexOf(LanguageConventions.newLine[1], i);
                                        int lineEnd = textToLower.IndexOf(LanguageConventions.newLine[1], index + charIndexLast + 1);
                                        if (lineStart != -1 && lineEnd != -1) {
                                            stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart + 1) + ")\t" + text.Substring(lineStart + 1, lineEnd - 1 - lineStart - 1).TrimStart());
                                        } else if (lineStart == -1 && lineEnd != -1) {
                                            stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart) + ")\t" + text.Substring(0, lineEnd - 1 - lineStart - 1).TrimStart());
                                        } else if (lineStart != -1 && lineEnd == -1) {
                                            stringWriter.WriteLine(f.FullName + "\t(" + StringUtil.GetLineNumberFromIndex(text, lineStart + 1) + ")\t" + text.Substring(lineStart + 1, textToLower.Length - lineStart - 1).TrimStart());
                                        } else {
                                            stringWriter.WriteLine(f.FullName + "\t" + "Error @ Index" + index);
                                        }
                                        if (tmpOptions.GetValue(Options.AvailableOptions.OnlyShow1EntryPerLine)) {
                                            if (lineEnd != -1) /* end of file ? */
                                                index = textToLower.IndexOf(searchChar, lineEnd + 1);
                                            else
                                                break;
                                        } else {
                                            index = textToLower.IndexOf(searchChar, index + charIndexLast + 1);
                                        }
                                    }
                                } else {
                                    index = textToLower.IndexOf(searchChar, index + 1);
                                }
                            } else {
                                index = textToLower.IndexOf(searchChar, index + 1);
                            }
                        } else {
                            break;
                        }
                    }
                }
                searchResults[ThreadId] = stringWriter.ToString();
            } catch (OperationCanceledException e) {
            } catch (OutOfMemoryException e) {
                this.cancelSearch = true;
                this.outOfMemory = true;
            }
            threadInProgress[ThreadId] = false;
            stringWriter.Close();
        }
        private void ParseFilesRegEx(string[] files, string searchString, bool caseSensitive, int ThreadId, CancellationToken cancelSearch) {
            StringWriter stringWriter = new StringWriter();
            RegexOptions regExOpt = RegexOptions.None;
            if (!caseSensitive)
                regExOpt = RegexOptions.IgnoreCase;
            try {
                foreach (string file in files) {
                    bool foundInFile = false;
                    FileInfo f = new FileInfo(file);
                    int linenumber = 0;
                    string line;
                    StreamReader streamReader = new StreamReader(file);
                    while ((line = streamReader.ReadLine()) != null) { // Read the file line by line.
                        linenumber++;
                        try {
                            Match match = Regex.Match(line, searchString, regExOpt);
                            if (match.Success) {
                                counter++;
                                if (!foundInFile) {
                                    foundInFile = true;
                                    fileCounter++;
                                }
                                stringWriter.WriteLine(f.FullName + "\t(" + linenumber + ")\t" + line);
                            }
                        } catch (Exception e) {
                            stringWriter.WriteLine(f.FullName + "\t" + "Error: Unrecognized RegEx format @ Line" + linenumber);
                            break;
                        }
                        cancelSearch.ThrowIfCancellationRequested();
                    }
                    streamReader.Close();
                }
                searchResults[ThreadId] = stringWriter.ToString();
            } catch (OperationCanceledException e) {
            } catch (OutOfMemoryException e) {
                this.cancelSearch = true;
                this.outOfMemory = true;
            }
            threadInProgress[ThreadId] = false;
            stringWriter.Close();
        }
        private void ParseOptionsFromFile(string optionsFile) {
            string line;
            System.IO.StreamReader streamReader = null;
            try {
                streamReader = new StreamReader(optionsFile); // read options file
                while ((line = streamReader.ReadLine()) != null) { // Read the file line by line.
                    if (line.Equals("<Options>")) {
                        parseOptions = (int)ParseOptions.SearchOptions;
                        continue;
                    } else if (line.Equals("<SourcePath>")) {
                        parseOptions = (int)ParseOptions.SourcePath;
                        searchPaths.Clear();
                        continue;
                    } else if (line.Equals("<Extensions>")) {
                        parseOptions = (int)ParseOptions.Extensions;
                        continue;
                    } else if (line.Equals("<Editor1>")) {
                        parseOptions = (int)ParseOptions.Editor1;
                        continue;
                    } else if (line.Equals("<Editor2>")) {
                        parseOptions = (int)ParseOptions.Editor2;
                        continue;
                    } else if (line.Equals("<Editor3>")) {
                        parseOptions = (int)ParseOptions.Editor3;
                        continue;
                    } else if (line.Equals("<Fontsize>")) {
                        parseOptions = (int)ParseOptions.Fontsize;
                        continue;
                    } else if (line.Equals("<Color>")) {
                        parseOptions = (int)ParseOptions.Color;
                        continue;
                    }
                    switch (parseOptions) {
                        case (int)ParseOptions.SearchOptions:
                            int indexOfEqual = line.IndexOf('=');
                            string lineTrimmed = line.Trim();
                            if (indexOfEqual != -1) {
                                string lineOption = lineTrimmed.Substring(0, indexOfEqual);
                                int optionId = 0;
                                foreach (Option o in options.GetList()) {
                                    if (o.ToString().Equals(lineOption)) {
                                        optionId = o.GetId();
                                        if (!lineTrimmed.Substring(indexOfEqual + 1).Equals("0"))
                                            options.SetValue(optionId, true);
                                    }
                                }
                            }
                            break;
                        case (int)ParseOptions.SourcePath:
                            if (!line.Equals(""))
                                searchPaths.Add(line.TrimStart().TrimEnd());
                            break;
                        case (int)ParseOptions.Extensions:
                            if (!line.Equals("")) {
                                string[] extensions = line.TrimStart().TrimEnd().Split(';');
                                if (extensions != null) {
                                    this.extensions.Clear();
                                    for (int i = 0; i < extensions.Length; i++) { /* cleanup extensions */
                                        if (!extensions[i].Equals("")) {
                                            if (extensions[i].Equals("*")) {
                                                this.extensions.Clear();
                                                this.extensions.Add("*"); // wildcard found, so do not filter extensions
                                                break;
                                            }
                                            Match match = Regex.Match(extensions[i], "^[a-zA-Z][a-zA-Z0-9]*$");
                                            if (match.Success) {
                                                this.extensions.Add(extensions[i]);
                                            }
                                        }
                                    }
                                    if (extensions.Length == 0)
                                        this.extensions.Add("*"); // use wildcard 
                                }
                            }
                            break;
                        case (int)ParseOptions.Editor1:
                            if (!line.Equals(""))
                                editor1 = line.TrimStart().TrimEnd();
                            break;
                        case (int)ParseOptions.Editor2:
                            if (!line.Equals("")) {
                                if (line.StartsWith("Name=")) {
                                    indexOfEqual = line.IndexOf('=');
                                    lineTrimmed = line.Trim();
                                    miEditor2.Header = "Open with " + lineTrimmed.Substring(indexOfEqual + 1);
                                    break;
                                }
                                editor2 = line.TrimStart().TrimEnd();
                            }
                            break;
                        case (int)ParseOptions.Editor3:
                            if (!line.Equals("")) {
                                if (line.StartsWith("Name=")) {
                                    indexOfEqual = line.IndexOf('=');
                                    lineTrimmed = line.Trim();
                                    miEditor3.Header = "Open with " + lineTrimmed.Substring(indexOfEqual + 1);
                                    break;
                                }
                                editor3 = line.TrimStart().TrimEnd();
                            }
                            break;
                        case (int)ParseOptions.Fontsize:
                            try {
                                int fontSize = Int32.Parse(line.Trim());
                                if (fontSize > 48) {
                                    this.fontSize = 48;
                                } else if (fontSize < 1) {
                                    this.fontSize = 1;
                                } else {
                                    this.fontSize = fontSize;
                                }
                                tbMainFontSize(this.fontSize);
                            } catch (Exception e) { /* ignore exceptions during the read of this sub-option */
                            }
                            break;
                        case (int)ParseOptions.Color:
                            try {
                                int value = Int32.Parse(line.Trim());
                                if (value > 540) {
                                    this.color = 540;
                                } else if (value < 0) {
                                    this.color = 0;
                                } else {
                                    this.color = value;
                                }
                                byte r = 237;
                                byte g = 237;
                                byte b = 237;

                                if (value == 0) {
                                    r = 255;
                                    g = 255;
                                    b = 255;
                                } else if (value < 180) {
                                    r += (byte)((180 - value) / 10);
                                    g += (byte)(value / 10);
                                    b = 255;
                                } else if (value > 360) {
                                    value -= 360;
                                    r = 255;
                                    g += (byte)((180 - value) / 10);
                                    b += (byte)(value / 10);
                                } else {
                                    value -= 180;
                                    r += (byte)(value / 10);
                                    b += (byte)((180 - value) / 10);
                                    g = 255;
                                }
                                SolidColorBrush scb = new SolidColorBrush();
                                scb.Color = Color.FromRgb(r, g, b);
                                tbMain.Background = scb;
                            } catch (Exception e) { /* ignore exceptions during the read of this sub-option */
                            }
                            break;
                        default:
                            break;
                    }
                }
            } catch (FileNotFoundException fnfe) { /* do not fetch options from file and use system default instead */
            } catch (Exception e) { /* ignore and use default options, if there are any exceptions during the read of the options file */
            } finally {
                if (streamReader != null)
                    streamReader.Close();
            }
        }
        private void PrintCurrentSearchPath() {
            tbMainText("cd \n");
            for (int i = 0; i < searchPaths.Count; i++) {
                string s = searchPaths.ElementAt(i);
                if (Directory.Exists(s))
                    tbMainAppend(s + "\n");
                else {
                    tbMainAppend("Error: Search path not found: " + s + System.Environment.NewLine);
                    searchPaths.RemoveAt(i);
                }
            }
        }
        private void OnTbMainPreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Home) {
                tbMainScrollToHome();
            }
            if (e.Key == Key.End) {
                tbMainScrollToEnd();
            }
        }
        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.F && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control)) && ((Keyboard.Modifiers & (ModifierKeys.Shift)) == (ModifierKeys.Shift))) {
                if (searchFilesWindow != null && searchFilesWindow.IsOpened) {
                    searchFilesWindow.Focus();
                    searchFilesWindow.tbSearchBoxSelectAll();
                } else {
                    searchFilesWindow = new SearchFilesWindow(this);
                    searchFilesWindow.Show();
                }
            } else {
                if (e.Key == Key.F && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                    if (searchWindow != null && searchWindow.IsOpened) {
                        searchWindow.Focus();
                        searchWindow.tbSearchBoxSelectAll();
                    } else {
                        searchWindow = new SearchWindow(this);
                        searchWindow.Show();
                    }
                }
            }
            if (e.Key == Key.S && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                settingsWindow = new SettingsWindow(this);
                settingsWindow.Show();
            }
            if (e.Key == Key.Q && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                CancelSearch();
            }
            if (Keyboard.IsKeyDown(Key.F10)) { // this is to work around the windows default operation when pressing F10 key, which is to activate the window menu bar
                ParseOptionsFromFile("F10.txt");
                PrintCurrentSearchPath();
                e.Handled = true;
            }
            switch (e.Key) {
                case Key.Home: // && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                    tbMainScrollToHome();
                    break;
                case Key.End: // && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                    tbMainScrollToEnd();
                    break;
                case Key.PageUp:
                    tbMainScrollUp();
                    break;
                case Key.PageDown:
                    tbMainScrollDown();
                    break;
                case Key.F1:
                    ParseOptionsFromFile("F1.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F2:
                    ParseOptionsFromFile("F2.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F3:
                    ParseOptionsFromFile("F3.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F4:
                    ParseOptionsFromFile("F4.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F5:
                    ParseOptionsFromFile("F5.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F6:
                    ParseOptionsFromFile("F6.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F7:
                    ParseOptionsFromFile("F7.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F8:
                    ParseOptionsFromFile("F8.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F9:
                    ParseOptionsFromFile("F9.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F11:
                    ParseOptionsFromFile("F11.txt");
                    PrintCurrentSearchPath();
                    break;
                case Key.F12:
                    ParseOptionsFromFile("F12.txt");
                    PrintCurrentSearchPath();
                    break;
                default:
                    break;
            }
        }
        private void OnSvPreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Home) {
                tbMainScrollToHome();
            }
            if (e.Key == Key.End) {
                tbMainScrollToEnd();
            }
        }
        private void OnPbPreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Home) {
                tbMainScrollToHome();
            }
            if (e.Key == Key.End) {
                tbMainScrollToEnd();
            }
        }
        private void OnTbPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            PtMouseDown = e.GetPosition(tbMain); // remember mouseclick position
        }
        private void OnClickMenuItemEditor2(object sender, RoutedEventArgs e) {
            Point pt = PtMouseDown;
            pt.X = 0;
            int idx = tbMain.GetCharacterIndexFromPoint(pt, true);
            string path = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[0];
            if (File.Exists(path)) {
                string sLinenumber = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[1];
                try { // open the specified Editor and open the file under cursor at the specified line
                    int linenumber = int.Parse(sLinenumber.Substring(1, sLinenumber.Length - 2));
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    #region GetPathToEditor
                    string editor;
                    string args;
                    int idxFirstDelimiter = editor2.IndexOf(Path.DirectorySeparatorChar);
                    int idxFirstQuote = editor2.IndexOf('\"');
                    if (idxFirstQuote != -1) { // path in quotes
                        int idxSecondQuote = editor2.IndexOf('\"', 1);
                        editor = editor2.Substring(0, idxSecondQuote); // second quote marks the end of path
                        startInfo.FileName = editor + "\"";
                        args = editor2.Substring(editor2.IndexOf(' ', idxSecondQuote)).TrimStart();
                    } else { // no quotes in path
                        editor = editor2.Substring(0, editor2.IndexOf(' ', 1));
                        startInfo.FileName = editor;
                        args = editor2.Substring(editor2.IndexOf(' ') + 1).TrimStart();
                    }
                    args = args.Replace("%path", path);
                    args = args.Replace("%linenumber", "" + linenumber);
                    #endregion
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = args;
                    OpenEditorAsync(startInfo);
                } catch (Exception ex) {
                    MessageBox.Show("Error when trying to open the Editor\n" + ex.StackTrace);
                }
            }
        }
        private void OnClickMenuItemEditor3(object sender, RoutedEventArgs e) {
            Point pt = PtMouseDown;
            pt.X = 0;
            int idx = tbMain.GetCharacterIndexFromPoint(pt, true);
            string path = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[0];
            if (File.Exists(path)) {
                string sLinenumber = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[1];
                try { // open the specified Editor and open the file under cursor at the specified line
                    int linenumber = int.Parse(sLinenumber.Substring(1, sLinenumber.Length - 2));
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    #region GetPathToEditor
                    string editor;
                    string args;
                    int idxFirstDelimiter = editor3.IndexOf(Path.DirectorySeparatorChar);
                    int idxFirstQuote = editor3.IndexOf('\"');
                    if (idxFirstQuote != -1) { // path in quotes
                        int idxSecondQuote = editor3.IndexOf('\"', 1);
                        editor = editor3.Substring(0, idxSecondQuote); // second quote marks the end of path
                        startInfo.FileName = editor + "\"";
                        args = editor3.Substring(editor3.IndexOf(' ', idxSecondQuote)).TrimStart();
                    } else { // no quotes in path
                        editor = editor3.Substring(0, editor3.IndexOf(' ', 1));
                        startInfo.FileName = editor;
                        args = editor3.Substring(editor3.IndexOf(' ') + 1).TrimStart();
                    }
                    args = args.Replace("%path", path);
                    args = args.Replace("%linenumber", "" + linenumber);
                    #endregion
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = args;
                    OpenEditorAsync(startInfo);
                } catch (Exception ex) {
                    MessageBox.Show("Error when trying to open the Editor\n" + ex.StackTrace);
                }
            }
        }
        private void OnClickMenuItemOpenFolder(object sender, RoutedEventArgs e) {
            Point pt = PtMouseDown;
            pt.X = 0;
            int idx = tbMain.GetCharacterIndexFromPoint(pt, true);
            string path = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[0];
            if (File.Exists(path)) {
                try { // open the specified Editor and open the file under cursor at the specified line
                    System.Diagnostics.Process.Start("explorer.exe", string.Format("/select, \"{0}\"", path));
                } catch (Exception ex) {
                    MessageBox.Show("Error when trying to open the Editor\n" + ex.StackTrace);
                }
            }
        }
        private void OnClickMenuSearchInFile(object sender, RoutedEventArgs e) {
            Point pt = PtMouseDown;
            pt.X = 0;
            int idx = tbMain.GetCharacterIndexFromPoint(pt, true);
            string path = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[0];
            FileInfo f = new FileInfo(path);
            if (File.Exists(f.FullName)) {
                if (searchWindow != null && searchWindow.IsOpened) {
                    searchWindow.Focus();
                    searchWindow.tbSearchBoxSelectAll();
                    searchWindow.tbFilePattern.Text = f.Name;
                } else {
                    searchWindow = new SearchWindow(this, f.Name);
                    searchWindow.Show();
                }
            }
        }
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            Point pt = e.MouseDevice.GetPosition(tbMain);
            pt.X = 0;
            int idx = tbMain.GetCharacterIndexFromPoint(pt, true);
            string path = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[0];
            if (File.Exists(path)) {
                string sLinenumber = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[1];
                try { // open the specified Editor and open the file under cursor at the specified line
                    int linenumber = int.Parse(sLinenumber.Substring(1, sLinenumber.Length - 2));
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    #region GetPathToEditor
                    string editor;
                    string args;
                    int idxFirstDelimiter = editor1.IndexOf(Path.DirectorySeparatorChar);
                    int idxFirstQuote = editor1.IndexOf('\"');
                    if (idxFirstQuote != -1) { // path in quotes
                            int idxSecondQuote = editor1.IndexOf('\"', 1);
                            editor = editor1.Substring(0, idxSecondQuote); // second quote marks the end of path
                            startInfo.FileName = editor + "\"";
                            args = editor1.Substring(editor1.IndexOf(' ', idxSecondQuote)).TrimStart();
                    } else { // no quotes in path
                        editor = editor1.Substring(0, editor1.IndexOf(' ', 1));
                        startInfo.FileName = editor;
                        args = editor1.Substring(editor1.IndexOf(' ') + 1).TrimStart();
                    }
                    args = args.Replace("%path", path);
                    args = args.Replace("%linenumber", "" + linenumber);
                    #endregion
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = args;
                    OpenEditorAsync(startInfo);
                } catch (Exception ex) {
                    MessageBox.Show("Error when trying to open the Editor\n" + ex.StackTrace);
                }
            }
        }
        private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
            if ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control)) {
                if (e.Delta > 0) {
                    this.fontSize++;
                    if (fontSize > 48)
                        this.fontSize = 48;
                } else {
                    this.fontSize--;
                    if (fontSize < 1) 
                        this.fontSize = 1;
                }
                tbMainFontSize(this.fontSize);
                if (settingsWindow != null)
                    settingsWindow.SetFontSize(this.fontSize);
                e.Handled = true;
            }
        }
        private void OnDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
        }
        private void OnDragDrop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            searchPaths.Clear();
            foreach (string s in files) {
                searchPaths.Add(s);
            }
            if (settingsWindow != null && settingsWindow.IsOpened) {
                settingsWindow.lbSearchPaths_Add(searchPaths);
            }
            if (searchPaths != null)
                PrintCurrentSearchPath();
            e.Effects = DragDropEffects.Copy;
        }
        private void OnWindowClosing(object sender, CancelEventArgs e) {
        }
        private void OnSizeChanged(object sender, EventArgs e) {
        }
        private void OnStateChanged(object sender, EventArgs e) {
        }
        private void tbMainAppend(string s) {
            Action action = () => { tbMain.AppendText(s); };
            Dispatcher.Invoke(action);
        }
        private void tbMainText(string s) {
            Action action = () => { tbMain.Text = s; };
            Dispatcher.Invoke(action);
        }
        private void tbMainScrollToEnd() {
            Action action = () => {
                tbMain.Select(0, 0);
                tbMain.Focus();
                tbMain.SelectionStart = tbMain.Text.Length;
                tbMain.ScrollToEnd();
            };
            Dispatcher.Invoke(action);
        }
        private void tbMainScrollUp() {
            Action action = () => {
                tbMain.Focus();
                tbMain.ScrollToVerticalOffset(tbMain.VerticalOffset - 65);
            };
            Dispatcher.Invoke(action);
        }
        private void tbMainScrollDown() {
            Action action = () => {
                tbMain.Focus();
                tbMain.ScrollToVerticalOffset(tbMain.VerticalOffset + 65);
            };
            Dispatcher.Invoke(action);
        }
        private void tbMainScrollToHome() {
            Action action = () => {
                tbMain.Focus();
                if (tbMain.Text.Length > 0)
                    tbMain.SelectionStart = tbMain.Text.ElementAt(0);
                tbMain.ScrollToHome();
                tbMain.Select(0, 0);
            };
            Dispatcher.Invoke(action);
        }
        internal void tbMainFontSize(int size) {
            Action action = () => { tbMain.FontSize = size; };
            Dispatcher.Invoke(action);
        }
    }
}
