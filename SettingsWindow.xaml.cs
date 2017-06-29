using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Srch {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        MainWindow mainWindow;
        public bool IsOpened = false;

        public SettingsWindow(MainWindow mainWindow) {
            InitializeComponent();
            this.mainWindow = mainWindow;
            foreach (string s in mainWindow.searchPaths) {
                bool alreadyIn = false;
                for (int i = 0; i < lbSearchPaths.Items.Count; i++) {
                    if (s.Equals(lbSearchPaths.Items.GetItemAt(i).ToString()))
                        alreadyIn = true;
                }
                if (lbSearchPaths.Items.Count == 0 || !alreadyIn)
                    lbSearchPaths.Items.Add(s);
            }
            for (int i = 0; i < mainWindow.extensions.Count; i++) {
                tbExtensions.AppendText(mainWindow.extensions[i]);
                if (i < mainWindow.extensions.Count - 1)
                    tbExtensions.AppendText(";");
            }
            tbEditor1.Text = mainWindow.editor1;
            tbEditor2.Text = mainWindow.editor2;
            tbEditor3.Text = mainWindow.editor3;
            tbFontsize.Text = ""+mainWindow.fontSize;
            slColor.Value = mainWindow.color;
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
        private void OnWindowKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                this.Close();
            }
        }
        public new void Show() {
            IsOpened = true;
            base.Show();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e) {
            IsOpened = false;
            mainWindow.editor1 = tbEditor1.Text; // store on WindowClose
            mainWindow.editor2 = tbEditor2.Text;
            mainWindow.editor3 = tbEditor3.Text;
            UpdateFontSize();
            mainWindow.settingsWindow = null;
            mainWindow.extensions.Clear();
            string[] ext = tbExtensions.Text.Split(';');
            foreach (string s in ext) {
                if (!s.Equals("")) {
                    if (s.Equals("*")) {
                        mainWindow.extensions.Clear();
                        mainWindow.extensions.Add("*"); // wildcard found, so do not filter extensions
                        break;
                    }
                    Match match = Regex.Match(s, "^[a-zA-Z][a-zA-Z0-9]*$");
                    if (match.Success) {
                        mainWindow.extensions.Add(s);
                    }
                }
            }
            if (mainWindow.extensions.Count == 0)
                mainWindow.extensions.Add("*"); // use wildcard 
        }
        internal void UpdateFontSize() {
            try {
                int fontSize = Int32.Parse(tbFontsize.Text);
                if (fontSize > 48) {
                    fontSize = 48;
                    mainWindow.fontSize = fontSize;
                    tbFontsize.Text = "" + fontSize;
                } else if (fontSize < 1) {
                    fontSize = 1;
                    mainWindow.fontSize = fontSize;
                    tbFontsize.Text = "" + fontSize;
                } else {
                    mainWindow.fontSize = fontSize;
                    tbFontsize.Text = "" + fontSize;
                }
                mainWindow.tbMainFontSize(mainWindow.fontSize);
            } catch (Exception ex) {
                mainWindow.fontSize = 10; /* use default of 10 as fontsize on exception */
            }
        }
        internal void SetFontSize(int fontSize) {
            tbFontsize.Text = "" + fontSize;
        }
        internal void lbSearchPaths_Add(List<string> paths) {
            lbSearchPaths.Items.Clear();
            foreach (string s in paths) {
                    lbSearchPaths.Items.Add(s);
            }
        }
        private void lbSearchPaths_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                List<int> removeIndices = new List<int>();
                foreach (string s in lbSearchPaths.SelectedItems) {
                    for (int i = 0; i < lbSearchPaths.Items.Count; i++) {
                        if (s.Equals(lbSearchPaths.Items.GetItemAt(i).ToString()))
                            removeIndices.Add(i);
                    }
                }
                removeIndices.Sort();
                for (int i = removeIndices.Count - 1; i >= 0; i--) {
                    lbSearchPaths.Items.RemoveAt(removeIndices[i]);
                }
                if (lbSearchPaths.Items.Count > 0)
                    lbSearchPaths.SelectedIndex = 0;
                // Update the internal paths
                mainWindow.searchPaths.Clear();
                foreach (string s in lbSearchPaths.Items) {
                    mainWindow.searchPaths.Add(s);
                }
                e.Handled = true;
            }
            if (e.Key == Key.A && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                lbSearchPaths.SelectAll();
            }
            if (e.Key == Key.X && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                List<int> removeIndices = new List<int>();
                foreach (string s in lbSearchPaths.SelectedItems) {
                    for (int i = 0; i < lbSearchPaths.Items.Count; i++) {
                        if (s.Equals(lbSearchPaths.Items.GetItemAt(i).ToString()))
                            removeIndices.Add(i);
                    }
                }
                removeIndices.Sort();
                string toClipBoard = null;
                for (int i = removeIndices.Count - 1; i >= 0; i--) {
                    toClipBoard += (lbSearchPaths.Items.GetItemAt(removeIndices[i]) + System.Environment.NewLine);
                }
                Clipboard.SetText(toClipBoard);  
                for (int i = removeIndices.Count - 1; i >= 0; i--) {
                    lbSearchPaths.Items.RemoveAt(removeIndices[i]);
                }
                if (lbSearchPaths.Items.Count > 0)
                    lbSearchPaths.SelectedIndex = 0;
                // Update the internal paths
                mainWindow.searchPaths.Clear();
                foreach (string s in lbSearchPaths.Items) {
                    mainWindow.searchPaths.Add(s);
                }
                e.Handled = true;
            }
            if (e.Key == Key.C && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                List<int> copyIndices = new List<int>();
                foreach (string s in lbSearchPaths.SelectedItems) {
                    for (int i = 0; i < lbSearchPaths.Items.Count; i++) {
                        if (s.Equals(lbSearchPaths.Items.GetItemAt(i).ToString()))
                            copyIndices.Add(i);
                    }
                }
                copyIndices.Sort();
                string toClipBoard = null;
                for (int i = copyIndices.Count - 1; i >= 0; i--) {
                    toClipBoard += (lbSearchPaths.Items.GetItemAt(copyIndices[i]) + System.Environment.NewLine);
                }
                Clipboard.SetText(toClipBoard);
                e.Handled = true;
            }
            if (e.Key == Key.V && ((Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))) {
                string fromClipBoard = Clipboard.GetText();
                StringReader sr = new StringReader(fromClipBoard);
                string line;
                while ((line = sr.ReadLine()) != null) {
                    line.TrimStart().TrimEnd();
                    if (Directory.Exists(line)) {
                        bool alreadyIn = false;
                        for (int i = 0; i < lbSearchPaths.Items.Count; i++) {
                            if (line.Equals(lbSearchPaths.Items.GetItemAt(i).ToString()))
                                alreadyIn = true;
                        }
                        if (lbSearchPaths.Items.Count == 0 || !alreadyIn) {
                            lbSearchPaths.Items.Add(line);
                            mainWindow.searchPaths.Add(line);
                        }
                    }                        
                }
                e.Handled = true;
            }
        }
        private void lbSearchPaths_DragDrop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string s in files) {
                bool alreadyIn = false;
                for (int i = 0; i < lbSearchPaths.Items.Count; i++) {
                    if (s.Equals(lbSearchPaths.Items.GetItemAt(i).ToString()))
                        alreadyIn = true;
                }
                if (lbSearchPaths.Items.Count == 0 || !alreadyIn) {
                    lbSearchPaths.Items.Add(s);
                    mainWindow.searchPaths.Add(s);
                }
            }
            e.Effects = DragDropEffects.Copy;
        }
        private void slColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var slider = sender as Slider; // ... Get Slider reference.
            double value = slider.Value;   // ... Get Value (between 0 and 300) .
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
            mainWindow.tbMain.Background = scb;
            mainWindow.color = (int)slider.Value;
        }
        void tbFontSize_KeyDown(object sender, KeyEventArgs e) {
            if ((e.Key < Key.D0) || (e.Key > Key.D9)) {
                if (e.KeyboardDevice.GetKeyStates(Key.NumLock) != 0)
                    if ((e.Key < Key.NumPad0) || (e.Key > Key.NumPad9))
                        e.Handled = true;
            } 
            if ((e.Key == Key.Return) || (e.Key == Key.Enter)) {
                UpdateFontSize();
            }
        }
    }            
}
