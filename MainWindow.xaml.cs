using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.ComponentModel; // needed for CancelEventArgs
using System.Windows.Input;

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
        internal int fontSizeParsedFromFile = 10;
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
            ParseOptions.ParseOptionsFromFile("default_options.txt", this);
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
            if (!searchInProgress)
            {
                Keys.INPUT[] Inputs = new Keys.INPUT[2];
                Keys.INPUT Input = new Keys.INPUT();

                Input.type = 1; // 1 = Keyboard Input
                Input.U.ki.wScan = Keys.ScanCodeShort.LCONTROL;
                Input.U.ki.dwFlags = Keys.KEYEVENTF.SCANCODE;
                Inputs[0] = Input;

                Input.type = 1; // 1 = Keyboard Input
                Input.U.ki.wScan = Keys.ScanCodeShort.KEY_C;
                Input.U.ki.dwFlags = Keys.KEYEVENTF.SCANCODE;
                Inputs[1] = Input;

                Keys.SendInput(2, Inputs, Keys.INPUT.Size);
                Thread.Sleep(80);
                StartSearch(Clipboard.GetText(), "");
            }
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
        private async void SetupSearchFilesThreads(string searchString, string filePattern) { // this method sets up and handles parallel searchthread execution
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
                } catch (UnauthorizedAccessException uae) {
                    Console.Error.WriteLine("Error: Unauthorized Access.");
                    tbMainText("Error: Unauthorized Access.");
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
                        await Task.Run(() => tbMainAppend(filename + "\t(" + 0 + ")\t\n"));
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
                } catch (UnauthorizedAccessException uae) {
                    Console.Error.WriteLine("Error: Unauthorized Access.");
                    tbMainText("Error: Unauthorized Access.");
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
                tbMainScrollToEnd();
                cancelSearch = false;
            }
            action = () => { progressBar.IsIndeterminate = false; /* stop animation */ };
            Dispatcher.Invoke(action);
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
                        cancelSearch.ThrowIfCancellationRequested();
                        if (i < text.Length) {
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
                        cancelSearch.ThrowIfCancellationRequested();
                        if (i < textToLower.Length) {
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
                        cancelSearch.ThrowIfCancellationRequested();
                        if (i < text.Length) {
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
                        cancelSearch.ThrowIfCancellationRequested();
                        if (i < textToLower.Length) {
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
            if (e.Key == Key.C && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                CancelSearch();
            }
			if (e.Key == Key.D0 && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) { // Pressing the keys Control+0 resets the fontSize to the one which has been parsed from File earlier
                this.fontSize = fontSizeParsedFromFile;
				tbMainFontSize(this.fontSize);
                if (settingsWindow != null)
                    settingsWindow.SetFontSize(this.fontSize);                
            }
            if (Keyboard.IsKeyDown(Key.F10)) { // this is to work around the windows default operation when pressing F10 key, which is to activate the window menu bar
                ParseOptions.ParseOptionsFromFile("F10.txt", this);
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
                    ParseOptions.ParseOptionsFromFile("F1.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F2:
                    ParseOptions.ParseOptionsFromFile("F2.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F3:
                    ParseOptions.ParseOptionsFromFile("F3.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F4:
                    ParseOptions.ParseOptionsFromFile("F4.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F5:
                    ParseOptions.ParseOptionsFromFile("F5.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F6:
                    ParseOptions.ParseOptionsFromFile("F6.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F7:
                    ParseOptions.ParseOptionsFromFile("F7.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F8:
                    ParseOptions.ParseOptionsFromFile("F8.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F9:
                    ParseOptions.ParseOptionsFromFile("F9.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F11:
                    ParseOptions.ParseOptionsFromFile("F11.txt", this);
                    PrintCurrentSearchPath();
                    break;
                case Key.F12:
                    ParseOptions.ParseOptionsFromFile("F12.txt", this);
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
        private void OnClickMenuItemAssociatedApplication(object sender, RoutedEventArgs e)
        {
            Point pt = PtMouseDown;
            pt.X = 0;
            int idx = tbMain.GetCharacterIndexFromPoint(pt, true);
            string path = tbMain.GetLineText(tbMain.GetLineIndexFromCharacterIndex(idx)).Split('\t')[0];
            if (File.Exists(path))
            {
                try
                { // open the specified Editor and open the file under cursor at the specified line
                    Process.Start(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error when trying to open the Application\n" + ex.StackTrace);
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
