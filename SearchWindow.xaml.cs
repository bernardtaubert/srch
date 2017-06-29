using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Srch {
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window {
        MainWindow mainWindow;
        public bool IsOpened = false;
        private bool show1EntryPerLineState;
        public SearchWindow(MainWindow mainWindow) {
            InitializeWindow(mainWindow);
        }
        public SearchWindow(MainWindow mainWindow, string filename) {
            InitializeWindow(mainWindow);
            tbFilePattern.Text = filename;
        }
        public void InitializeWindow(MainWindow mainWindow) {
            InitializeComponent();
            this.mainWindow = mainWindow;
            cbCaseSensitive.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.CaseSensitive);
            rbWholeWordsOnly.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.WholeWordsOnly);
            rbNETRegEx.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.NETRegEx);
            rbFastRegEx.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.FastRegEx);
            if (((bool)rbNETRegEx.IsChecked || (bool)rbFastRegEx.IsChecked)) {
                show1EntryPerLineState = true;
                cbOnlyShow1EntryPerLine.IsChecked = true;
            } else {
                cbOnlyShow1EntryPerLine.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.OnlyShow1EntryPerLine);
                show1EntryPerLineState = (bool)cbOnlyShow1EntryPerLine.IsChecked;
            }
            rbFastRegEx.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.FastRegEx);
            rbDefault.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.Default);
            cbSearchSubDirectories.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.SearchSubDirectories);
            if (mainWindow.searchHistory.Count > 10) {
                mainWindow.searchHistory.Dequeue();
            }
            for (int i = mainWindow.searchHistory.Count - 1; i >= 0; i--) {
                cbSearchBox.Items.Add(mainWindow.searchHistory.ElementAt(i));
            }
            tbSearchBox.Text = mainWindow.searchString;
            tbFilePattern.Text = mainWindow.fileFilter;
            tbSearchBoxSelectAll();
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
        public new void Show() {
            IsOpened = true;
            base.Show();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e) {
            mainWindow.fileFilter = tbFilePattern.Text;
            IsOpened = false;
        }
        private void cbCaseSensitiveCheckedChanged(object sender, RoutedEventArgs e) {
            mainWindow.options.SetValue(Options.AvailableOptions.CaseSensitive, (bool)cbCaseSensitive.IsChecked);
        }   
        private void cbOnlyShow1EntryPerLineCheckedChanged(object sender, RoutedEventArgs e) {
            mainWindow.options.SetValue(Options.AvailableOptions.OnlyShow1EntryPerLine, (bool)cbOnlyShow1EntryPerLine.IsChecked);
        }
        private void cbSearchSubDirectoriesCheckedChanged(object sender, RoutedEventArgs e) {
            mainWindow.options.SetValue(Options.AvailableOptions.SearchSubDirectories, (bool)cbSearchSubDirectories.IsChecked);
        }
        private async void OnWindowKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                string searchString = null;
                string filePattern = null;
                Action action = () => { searchString = tbSearchBox.Text; filePattern = tbFilePattern.Text; }; Dispatcher.Invoke(action);       
                if (searchString != "") {
                    mainWindow.searchString = searchString;
                    this.Close();
                    await Task.Run(() => mainWindow.StartSearch(searchString, filePattern));
                }
                if (!mainWindow.searchHistory.Contains(searchString)) {
                    mainWindow.searchHistory.Enqueue(searchString);
                    if (mainWindow.searchHistory.Count > 10) {
                        mainWindow.searchHistory.Dequeue();
                    }
                } else {
                    Queue<string> tmpSearchHistory = new Queue<string>();
                    for (int i = 0; i < mainWindow.searchHistory.Count; i++) {
                        if (mainWindow.searchHistory.ElementAt(i).Equals(searchString)) {
                            /* ignore the existing item */
                        } else {
                            tmpSearchHistory.Enqueue(mainWindow.searchHistory.ElementAt(i)); /* generate the new Queue by iterating over the existing one */
                        }                            
                    }
                    tmpSearchHistory.Enqueue(searchString); /* enqueue the most recent search string at last */
                    mainWindow.searchHistory = tmpSearchHistory; /* overwrite history */
                }
            }
            if (e.Key == Key.Escape) {
                this.Close();
            }
            if (e.Key == Key.Up) {
                if (!cbSearchBox.IsDropDownOpen)
                    cbSearchBox.IsDropDownOpen = true;
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex > 0)
                    cbSearchBox.SelectedIndex--;
            }
            if (e.Key == Key.Down) {
                if (!cbSearchBox.IsDropDownOpen)
                    cbSearchBox.IsDropDownOpen = true;
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex < (cbSearchBox.Items.Count - 1))
                    cbSearchBox.SelectedIndex++;
            }
        }
        private void OnMouseWheelTbSearchBox(object sender, MouseWheelEventArgs e) {
            if (e.Delta > 0) {
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex > 0)
                    cbSearchBox.SelectedIndex--;
            } else {
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex < (cbSearchBox.Items.Count - 1))
                    cbSearchBox.SelectedIndex++;
            }
            e.Handled = true;
        }
        private void cbSearchBoxSelectionChanged(object sender, SelectionChangedEventArgs e) {
            tbSearchBox.Text = cbSearchBox.Items[cbSearchBox.SelectedIndex].ToString();
        }
        internal void tbSearchBoxSelectAll() {
            tbSearchBox.Focus();
            tbSearchBox.SelectAll();
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e) {
            mainWindow.options.SetValue(Options.AvailableOptions.NETRegEx, (bool)rbNETRegEx.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.WholeWordsOnly, (bool)rbWholeWordsOnly.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.Default, (bool)rbDefault.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.FastRegEx, (bool)rbFastRegEx.IsChecked);
            if ((bool)rbNETRegEx.IsChecked || (bool)rbFastRegEx.IsChecked) {
                this.show1EntryPerLineState = (bool)cbOnlyShow1EntryPerLine.IsChecked;
                cbOnlyShow1EntryPerLine.IsChecked = true;
                cbOnlyShow1EntryPerLine.IsEnabled = false;
            } else {
                cbOnlyShow1EntryPerLine.IsEnabled = true;
                cbOnlyShow1EntryPerLine.IsChecked = show1EntryPerLineState;
            }
        }
    }
}
