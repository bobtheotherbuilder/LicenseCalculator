using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System;
using System.Windows.Threading;
using System.Linq;

namespace LicenseCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int targetAppId = 374;
        private static List<AppInstallInfo> targetAppList;
        private DispatcherTimer t;
        private DateTime readStartTime;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "CSV file (.csv)|*.csv";

            if (dlg.ShowDialog() == true)
            {
                txtFileName.Text = dlg.FileName;
                txtResult.Text = string.Empty;
                btnRead.IsEnabled = true;
                btnCalc.IsEnabled = false;
            }
        }

        private async void btnRead_ClickAsync(object sender, RoutedEventArgs e)
        {
            btnRead.IsEnabled = false;
            btnBrowse.IsEnabled = false;

            List<AppInstallInfo> installs = new List<AppInstallInfo>();
            if (!File.Exists(txtFileName.Text))
            {
                ResetWithMessage("File does not exist.");
                return;
            }

            Stream content;
            try
            {
                content = File.OpenRead(txtFileName.Text);
            }
            catch (Exception ex)
            {
                ResetWithMessage(ex.Message);
                return;
            }

            t = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 50), DispatcherPriority.Background,
                ShowProcessTime, Dispatcher.CurrentDispatcher);
            t.IsEnabled = true;
            readStartTime = DateTime.Now;

            using (var reader = new StreamReader(content))
            {
                string headerline = reader.ReadLine();
                int appIdIndex = GetAppIdIndexFromHeaderLine(headerline);

                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        break;

                    parseLine(appIdIndex, installs, line);
                }
            }

            targetAppList = installs;
            txtResult.Text = $"{ShowTime()}. \nTarget rows(distinct) identified: {installs.Count.ToString()}";
            btnCalc.IsEnabled = true;
            btnBrowse.IsEnabled = true;
        }

        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            var newList = targetAppList.GroupBy(x => x.UserID)
                .Select(g => 
                            new { ID = g.First().UserID,
                                  Count = CountLicNum(g.Count(y => y.ComputerType == "computer"), g.Count(y => y.ComputerType == "laptop")) });

            txtResult.Text = $"Total license needed: {newList.Sum(x => x.Count).ToString()}";
        }

        /// <summary>
        /// Logic to calculate total licenses required for a given user
        /// </summary>
        /// <param name="numOfPC"></param>
        /// <param name="numOfLaptop"></param>
        /// <returns></returns>
        public int CountLicNum(int numOfPC, int numOfLaptop)
        {
            if (numOfLaptop <= numOfPC)
                return numOfPC;

            return numOfPC + (int)Math.Ceiling((decimal)(numOfLaptop - numOfPC)/2);
        }

        private static int GetAppIdIndexFromHeaderLine(string headerline)
        {
            string[] headers = headerline.Split(',');

            int appIdIndex = 0;
            while (appIdIndex < headers.Length)
            {
                if (headers[appIdIndex].ToLower() == "applicationid")
                {
                    break;
                }
                appIdIndex++;
            }

            return appIdIndex;
        }

        private void parseLine(int appIdIndex, List<AppInstallInfo> installs, string line)
        {
            int appId;
            string[] lineData = line.Split(',');
            int.TryParse(lineData[appIdIndex], out appId);

            if (appId == targetAppId)
            {
                try
                {
                    var install = new AppInstallInfo()
                    {
                        ComputerID = int.Parse(lineData[0]),
                        UserID = int.Parse(lineData[1]),
                        ComputerType = lineData[3].Trim().ToLower(),
                    };
                    if (!installs.Contains(install))
                    {
                        installs.Add(install);
                    }
                }
                catch (Exception)
                {
                    // skip invalid row
                }
            }
        }

        private void ResetWithMessage(string message)
        {
            txtResult.Text = message;
            txtFileName.Text = "";
            btnBrowse.IsEnabled = true;
            btnRead.IsEnabled = false;
        }

        private void ShowProcessTime(object sender, EventArgs e)
        {
            if (btnCalc.IsEnabled)
                return;
            txtResult.Text = ShowTime();
        }

        private string ShowTime()
        {
            return $"Time elapsed: { Convert.ToString(DateTime.Now - readStartTime)}";
        }

    }
}
