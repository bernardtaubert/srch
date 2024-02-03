using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Srch
{
    internal class ParseOptions
    {
        enum ParseOption
        {
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
        private static int currentOption = 0;

        internal static void ParseOptionsFromFile(string optionsFile, MainWindow mainWindow)
        {
            string line;
            System.IO.StreamReader streamReader = null;
            try
            {
                streamReader = new StreamReader(optionsFile); // read options file
                while ((line = streamReader.ReadLine()) != null)
                { // Read the file line by line.
                    if (line.Equals("<Options>"))
                    {
                        currentOption = (int)ParseOption.SearchOptions;
                        continue;
                    }
                    else if (line.Equals("<SourcePath>"))
                    {
                        currentOption = (int)ParseOption.SourcePath;
                        mainWindow.searchPaths.Clear();
                        continue;
                    }
                    else if (line.Equals("<Extensions>"))
                    {
                        currentOption = (int)ParseOption.Extensions;
                        continue;
                    }
                    else if (line.Equals("<Editor1>"))
                    {
                        currentOption = (int)ParseOption.Editor1;
                        continue;
                    }
                    else if (line.Equals("<Editor2>"))
                    {
                        currentOption = (int)ParseOption.Editor2;
                        continue;
                    }
                    else if (line.Equals("<Editor3>"))
                    {
                        currentOption = (int)ParseOption.Editor3;
                        continue;
                    }
                    else if (line.Equals("<Fontsize>"))
                    {
                        currentOption = (int)ParseOption.Fontsize;
                        continue;
                    }
                    else if (line.Equals("<Color>"))
                    {
                        currentOption = (int)ParseOption.Color;
                        continue;
                    }
                    switch (currentOption)
                    {
                        case (int)ParseOption.SearchOptions:
                            int indexOfEqual = line.IndexOf('=');
                            string lineTrimmed = line.Trim();
                            if (indexOfEqual != -1)
                            {
                                string lineOption = lineTrimmed.Substring(0, indexOfEqual);
                                int optionId = 0;
                                foreach (Option o in mainWindow.options.GetList())
                                {
                                    if (o.ToString().Equals(lineOption))
                                    {
                                        optionId = o.GetId();
                                        if (!lineTrimmed.Substring(indexOfEqual + 1).Equals("0"))
                                            mainWindow.options.SetValue(optionId, true);
                                    }
                                }
                            }
                            break;
                        case (int)ParseOption.SourcePath:
                            if (!line.Equals(""))
                                mainWindow.searchPaths.Add(line.TrimStart().TrimEnd());
                            break;
                        case (int)ParseOption.Extensions:
                            if (!line.Equals(""))
                            {
                                string[] extensions = line.TrimStart().TrimEnd().Split(';');
                                if (extensions != null)
                                {
                                    mainWindow.extensions.Clear();
                                    for (int i = 0; i < extensions.Length; i++)
                                    { /* cleanup extensions */
                                        if (!extensions[i].Equals(""))
                                        {
                                            if (extensions[i].Equals("*"))
                                            {
                                                mainWindow.extensions.Clear();
                                                mainWindow.extensions.Add("*"); // wildcard found, so do not filter extensions
                                                break;
                                            }
                                            Match match = Regex.Match(extensions[i], "^[a-zA-Z][a-zA-Z0-9]*$");
                                            if (match.Success)
                                            {
                                                mainWindow.extensions.Add(extensions[i]);
                                            }
                                        }
                                    }
                                    if (extensions.Length == 0)
                                        mainWindow.extensions.Add("*"); // use wildcard 
                                }
                            }
                            break;
                        case (int)ParseOption.Editor1:
                            if (!line.Equals(""))
                                mainWindow.editor1 = line.TrimStart().TrimEnd();
                            break;
                        case (int)ParseOption.Editor2:
                            if (!line.Equals(""))
                            {
                                if (line.StartsWith("Name="))
                                {
                                    indexOfEqual = line.IndexOf('=');
                                    lineTrimmed = line.Trim();
                                    mainWindow.miEditor2.Header = "Open with " + lineTrimmed.Substring(indexOfEqual + 1);
                                    break;
                                }
                                mainWindow.editor2 = line.TrimStart().TrimEnd();
                            }
                            break;
                        case (int)ParseOption.Editor3:
                            if (!line.Equals(""))
                            {
                                if (line.StartsWith("Name="))
                                {
                                    indexOfEqual = line.IndexOf('=');
                                    lineTrimmed = line.Trim();
                                    mainWindow.miEditor3.Header = "Open with " + lineTrimmed.Substring(indexOfEqual + 1);
                                    break;
                                }
                                mainWindow.editor3 = line.TrimStart().TrimEnd();
                            }
                            break;
                        case (int)ParseOption.Fontsize:
                            try
                            {
                                int fontSize = Int32.Parse(line.Trim());
                                if (fontSize > 48)
                                {
                                    mainWindow.fontSize = 48;
                                }
                                else if (fontSize < 1)
                                {
                                    mainWindow.fontSize = 1;
                                }
                                else
                                {
                                    mainWindow.fontSize = fontSize;
                                }
                                mainWindow.fontSizeParsedFromFile = mainWindow.fontSize;
                                mainWindow.tbMainFontSize(mainWindow.fontSize);
                            }
                            catch (Exception e)
                            { /* ignore exceptions during the read of this sub-option */
                            }
                            break;
                        case (int)ParseOption.Color:
                            try
                            {
                                int value = Int32.Parse(line.Trim());
                                if (value > 540)
                                {
                                    mainWindow.color = 540;
                                }
                                else if (value < 0)
                                {
                                    mainWindow.color = 0;
                                }
                                else
                                {
                                    mainWindow.color = value;
                                }
                                byte r = 237;
                                byte g = 237;
                                byte b = 237;

                                if (value == 0)
                                {
                                    r = 255;
                                    g = 255;
                                    b = 255;
                                }
                                else if (value < 180)
                                {
                                    r += (byte)((180 - value) / 10);
                                    g += (byte)(value / 10);
                                    b = 255;
                                }
                                else if (value > 360)
                                {
                                    value -= 360;
                                    r = 255;
                                    g += (byte)((180 - value) / 10);
                                    b += (byte)(value / 10);
                                }
                                else
                                {
                                    value -= 180;
                                    r += (byte)(value / 10);
                                    b += (byte)((180 - value) / 10);
                                    g = 255;
                                }
                                SolidColorBrush scb = new SolidColorBrush();
                                scb.Color = Color.FromRgb(r, g, b);
                                mainWindow.tbMain.Background = scb;
                            }
                            catch (Exception e)
                            { /* ignore exceptions during the read of this sub-option */
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (FileNotFoundException fnfe)
            { /* do not fetch options from file and use system default instead */
            }
            catch (Exception e)
            { /* ignore and use default options, if there are any exceptions during the read of the options file */
            }
            finally
            {
                if (streamReader != null)
                    streamReader.Close();
            }
            currentOption = 0;
        }
    }
}
