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
    public partial class SearchFilesWindow : Window {
        MainWindow mainWindow;
        public bool IsOpened = false;

        public SearchFilesWindow(MainWindow mainWindow) {
            InitializeComponent();
            this.mainWindow = mainWindow;
            cbSearchFilesSubDirectories.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.SearchFilesSubDirectories);
            if (mainWindow.searchFilesHistory.Count > 10) {
                mainWindow.searchFilesHistory.Dequeue();
            }
            for (int i = mainWindow.searchFilesHistory.Count - 1; i >= 0; i--) {
                cbSearchBox.Items.Add(mainWindow.searchFilesHistory.ElementAt(i));
            }
            tbSearchBox.Text = mainWindow.searchFilesString;
            tbFilePattern.Text = mainWindow.fileSearchFileFilter;
            tbSearchBoxSelectAll();
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
        public new void Show() {
            IsOpened = true;
            base.Show();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e) {
            mainWindow.fileSearchFileFilter = tbFilePattern.Text;
            IsOpened = false;
        }
        private void cbSearchFilesSubDirectoriesCheckedChanged(object sender, RoutedEventArgs e) {
            mainWindow.options.SetValue(Options.AvailableOptions.SearchFilesSubDirectories, (bool)cbSearchFilesSubDirectories.IsChecked);
        }
        private async void OnWindowKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                string searchString = null;
                string filePattern = null;
                Action action = () => { searchString = tbSearchBox.Text; filePattern = tbFilePattern.Text; }; Dispatcher.Invoke(action);     
                mainWindow.searchFilesString = searchString;
                this.Close();
                await Task.Run(() => mainWindow.StartSearchFiles(searchString, filePattern));
                if (!mainWindow.searchFilesHistory.Contains(searchString)) {
                    mainWindow.searchFilesHistory.Enqueue(searchString);
                    if (mainWindow.searchFilesHistory.Count > 10) {
                        mainWindow.searchFilesHistory.Dequeue();
                    }
                } else {
                    Queue<string> tmpSearchFilesHistory = new Queue<string>();
                    for (int i = 0; i < mainWindow.searchFilesHistory.Count; i++) {
                        if (mainWindow.searchFilesHistory.ElementAt(i).Equals(searchString)) {
                            /* ignore the existing item */
                        } else {
                            tmpSearchFilesHistory.Enqueue(mainWindow.searchFilesHistory.ElementAt(i)); /* generate the new Queue by iterating over the existing one */
                        }                            
                    }
                    tmpSearchFilesHistory.Enqueue(searchString); /* enqueue the most recent search string at last */
                    mainWindow.searchFilesHistory = tmpSearchFilesHistory; /* overwrite history */
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
    }
}
