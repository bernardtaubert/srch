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

namespace Srch
{
    /// <summary>
    /// Interaction logic for SearchMultilineWindow.xaml
    /// </summary>
    public partial class SearchMultilineWindow : Window
    {
        MainWindow mainWindow;
        public bool IsOpened = false;
        private bool show1EntryPerLineState;
        private bool ignoreComments;
        public SearchMultilineWindow(MainWindow mainWindow)
        {
            InitializeWindow(mainWindow);
        }
        public SearchMultilineWindow(MainWindow mainWindow, string filename)
        {
            InitializeWindow(mainWindow);
            tbFilePattern.Text = filename;
        }
        public void InitializeWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            cbCaseSensitive.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.CaseSensitive);
            rbWholeWordsOnly.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.WholeWordsOnly);
            //rbNETRegEx.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.NETRegEx);
            //rbFastRegEx.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.FastRegEx);
            rbMultiAll.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.SearchMultiAllStrings);
            rbMultiAny.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.SearchMultiAnyString);
            rbMultiNone.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.SearchMultiNoneOfStrings);
            rbDefault.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.Default);
            if ((bool)rbMultiAll.IsChecked || (bool)rbMultiNone.IsChecked)
            {
                rbWholeWordsOnly.IsChecked = false;
                rbWholeWordsOnly.IsEnabled = false;
                rbDefault.IsChecked = true;
            }
            else
                rbWholeWordsOnly.IsEnabled = true;
            cbIgnoreComments.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.IgnoreComments);
            cbOnlyShow1EntryPerLine.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.OnlyShow1EntryPerLine);
            //rbFastRegEx.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.FastRegEx);
            cbSearchSubDirectories.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.SearchSubDirectories);
            if (mainWindow.searchMultilineHistory.Count > 10)
            {
                mainWindow.searchMultilineHistory.Dequeue();
            }
            for (int i = mainWindow.searchMultilineHistory.Count - 1; i >= 0; i--)
            {
                cbSearchBox.Items.Add(mainWindow.searchMultilineHistory.ElementAt(i));
            }
            tbSearchBox.Text = mainWindow.searchString;
            tbFilePattern.Text = mainWindow.fileFilter;
            tbSearchBoxSelectAll();
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
        public new void Show()
        {
            IsOpened = true;
            base.Show();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            mainWindow.fileFilter = tbFilePattern.Text;
            IsOpened = false;
        }
        private void cbCaseSensitiveCheckedChanged(object sender, RoutedEventArgs e)
        {
            mainWindow.options.SetValue(Options.AvailableOptions.CaseSensitive, (bool)cbCaseSensitive.IsChecked);
        }
        private void cbIgnoreCommentsCheckedChanged(object sender, RoutedEventArgs e)
        {
            mainWindow.options.SetValue(Options.AvailableOptions.IgnoreComments, (bool)cbIgnoreComments.IsChecked);
        }
        private void cbOnlyShow1EntryPerLineCheckedChanged(object sender, RoutedEventArgs e)
        {
            mainWindow.options.SetValue(Options.AvailableOptions.OnlyShow1EntryPerLine, (bool)cbOnlyShow1EntryPerLine.IsChecked);
        }
        private void cbSearchSubDirectoriesCheckedChanged(object sender, RoutedEventArgs e)
        {
            mainWindow.options.SetValue(Options.AvailableOptions.SearchSubDirectories, (bool)cbSearchSubDirectories.IsChecked);
        }
        private async void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !((Keyboard.Modifiers & (ModifierKeys.Shift)) == (ModifierKeys.Shift)))
            {
                string searchString = null;
                string filePattern = null;
                Action action = () => { searchString = tbSearchBox.Text; filePattern = tbFilePattern.Text; }; Dispatcher.Invoke(action);
                if (searchString != "")
                {
                    mainWindow.searchString = searchString;
                    this.Close();
                    await Task.Run(() => mainWindow.StartMultiSearch(searchString, filePattern));
                }
                if (!mainWindow.searchMultilineHistory.Contains(searchString))
                {
                    mainWindow.searchMultilineHistory.Enqueue(searchString);
                    if (mainWindow.searchMultilineHistory.Count > 10)
                    {
                        mainWindow.searchMultilineHistory.Dequeue();
                    }
                }
                else
                {
                    Queue<string> tmpsearchMultilineHistory = new Queue<string>();
                    for (int i = 0; i < mainWindow.searchMultilineHistory.Count; i++)
                    {
                        if (mainWindow.searchMultilineHistory.ElementAt(i).Equals(searchString))
                        {
                            /* ignore the existing item */
                        }
                        else
                        {
                            tmpsearchMultilineHistory.Enqueue(mainWindow.searchMultilineHistory.ElementAt(i)); /* generate the new Queue by iterating over the existing one */
                        }
                    }
                    tmpsearchMultilineHistory.Enqueue(searchString); /* enqueue the most recent search string at last */
                    mainWindow.searchMultilineHistory = tmpsearchMultilineHistory; /* overwrite history */
                }
            }
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            if (e.Key == Key.Up)
            {
                if (!cbSearchBox.IsDropDownOpen)
                    cbSearchBox.IsDropDownOpen = true;
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex > 0)
                    cbSearchBox.SelectedIndex--;
            }
            if (e.Key == Key.Down)
            {
                if (!cbSearchBox.IsDropDownOpen)
                    cbSearchBox.IsDropDownOpen = true;
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex < (cbSearchBox.Items.Count - 1))
                    cbSearchBox.SelectedIndex++;
            }
        }
        private void OnMouseWheelTbSearchBox(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex > 0)
                    cbSearchBox.SelectedIndex--;
            }
            else
            {
                if (cbSearchBox.Items.Count > 1 && cbSearchBox.SelectedIndex < (cbSearchBox.Items.Count - 1))
                    cbSearchBox.SelectedIndex++;
            }
            e.Handled = true;
        }
        private void cbSearchBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbSearchBox.Text = cbSearchBox.Items[cbSearchBox.SelectedIndex].ToString();
        }
        internal void tbSearchBoxSelectAll()
        {
            tbSearchBox.Focus();
            tbSearchBox.SelectAll();
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            //mainWindow.options.SetValue(Options.AvailableOptions.NETRegEx, (bool)rbNETRegEx.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.WholeWordsOnly, (bool)rbWholeWordsOnly.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.Default, (bool)rbDefault.IsChecked);
            //mainWindow.options.SetValue(Options.AvailableOptions.FastRegEx, (bool)rbFastRegEx.IsChecked);
        }
        private void MultiRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            mainWindow.options.SetValue(Options.AvailableOptions.SearchMultiAllStrings, (bool)rbMultiAll.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.SearchMultiAnyString, (bool)rbMultiAny.IsChecked);
            mainWindow.options.SetValue(Options.AvailableOptions.SearchMultiNoneOfStrings, (bool)rbMultiNone.IsChecked);
            if ((bool)rbMultiAll.IsChecked || (bool)rbMultiNone.IsChecked)
            {
                //rbWholeWordsOnly.IsChecked = false;
                //rbDefault.IsChecked = true;
                mainWindow.options.SetValue(Options.AvailableOptions.Default, true);
                mainWindow.options.SetValue(Options.AvailableOptions.WholeWordsOnly, false);
                rbDefault.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.Default);
                rbWholeWordsOnly.IsChecked = mainWindow.options.GetValue(Options.AvailableOptions.WholeWordsOnly);
                rbWholeWordsOnly.IsEnabled = false;            
            }
            else
                rbWholeWordsOnly.IsEnabled = true;
        }
    }
}
