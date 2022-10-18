using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using MissionPlanner.Controls.PreFlight;
using MissionPlanner.Controls;
using System.Linq;
using System.Drawing;
using MissionPlanner;
using MissionPlanner.Plugin;
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace UDPWeatherStation
{
    public class UDPWeatherStationPlugin : Plugin
    {
        private int PORT;
        private IPEndPoint iPEndPoint;
        private UdpClient udpClient;
        private TimeSpan connectionTimeout;
        private DateTime lastWeatherUpdate;

        private Label messageLabel, disconnectedLabel;
        private SplitContainer splitContainer;
        private TabPage tabPage;

        #region Plugin info
        public override string Name
        {
            get { return "UDP Weather Station"; }
        }

        public override string Version
        {
            get { return "0.1"; }
        }

        public override string Author
        {
            get { return "Daniel Szilagyi (LEGIONBOSS)"; }
        }
        #endregion

        //[DebuggerHidden]
        public override bool Init()
		//Init called when the plugin dll is loaded
        {
            loopratehz = 1;  //Loop runs every second (The value is in Hertz, so 2 means every 500ms, 0.1f means every 10 second...)
            return true;	 // If it is false then plugin will not load
        }

        public override bool Loaded()
        //Loaded called after the plugin dll successfully loaded
        {
            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                // Setup labels
                disconnectedLabel = new Label();
                disconnectedLabel.Name = "labelDisconnected";
                disconnectedLabel.Text = "disconnected";
                disconnectedLabel.TextAlign = ContentAlignment.TopLeft;
                disconnectedLabel.Dock = DockStyle.Fill;
                //disconnectedLabel.Font = new Font(FontFamily.GenericSansSerif, 12);

                messageLabel = new Label();
                messageLabel.Name = "labelWeather";
                messageLabel.Text = "weather";
                messageLabel.TextAlign = ContentAlignment.TopLeft;
                messageLabel.Dock = DockStyle.Fill;
                //messageLabel.Font = new Font(FontFamily.GenericSansSerif, 12);

                // Create splitcontainer
                splitContainer = new SplitContainer();
                splitContainer.Name = "splitContainerWeather";
                splitContainer.Orientation = Orientation.Horizontal;
                splitContainer.IsSplitterFixed = true;
                splitContainer.Dock = DockStyle.Fill;
                splitContainer.Panel1.Controls.Add(disconnectedLabel);
                splitContainer.Panel1Collapsed = true;
                splitContainer.Panel2.Controls.Add(messageLabel);

                // Create tabpage
                tabPage = new TabPage();
                tabPage.Name = "tabWeather";
                tabPage.Text = "Weather";
                tabPage.Controls.Add(splitContainer);
                ThemeManager.ApplyThemeTo(tabPage);

                // Add tab to places
                int index = 1;
                List<string> list = Settings.Instance.GetList("tabcontrolactions").ToList();
                list.Insert(index, "tabWeather");
                Settings.Instance.SetList("tabcontrolactions", list);
                Host.MainForm.FlightData.TabListOriginal.Insert(index, tabPage);
                Host.MainForm.FlightData.tabControlactions.TabPages.Insert(index, tabPage);

                // Setup UDP broadcast listener
                PORT = 3333;
                iPEndPoint = new IPEndPoint(IPAddress.Any, PORT);
                udpClient = new UdpClient(PORT);
                udpClient.BeginReceive(new AsyncCallback(PorcessMessage), null);
                connectionTimeout = TimeSpan.FromSeconds(2);
                lastWeatherUpdate = DateTime.Now;

                /*
                // test what exists at this point
                string message = "";
                if (!Settings.Instance.ContainsKey("tabcontrolactions"))
                    message += Environment.NewLine + "1. No tabcontrolactions key in XML";
                else
                    message += Environment.NewLine + "1. Tabcontrolactions key in XML";
                if (!Settings.Instance.GetList("tabcontrolactions").Contains(tabPage.Name))
                    message += Environment.NewLine + $"2. No {tabPage.Name} in XML";
                else
                    message += Environment.NewLine + $"2. {tabPage.Name} in XML";
                if (!Host.MainForm.FlightData.TabListOriginal.Contains(tabPage))
                    message += Environment.NewLine + $"3. No {tabPage.Name} in TabListOriginal";
                else
                    message += Environment.NewLine + $"3. {tabPage.Name} in TabListOriginal";
                if (!Host.MainForm.FlightData.tabControlactions.TabPages.Contains(tabPage))
                    message += Environment.NewLine + $"4. No {tabPage.Name} in UI";
                else
                    message += Environment.NewLine + $"4. {tabPage.Name} in UI";
                if (!string.IsNullOrEmpty(message)) CustomMessageBox.Show($"Plugin.Loaded:{message}");
                */
            }));

            return true;     //If it is false plugin will not start (loop will not called)
        }

        public override bool Loop()
		//Loop is called in regular intervalls (set by loopratehz)
        {
            if (DateTime.Now.Subtract(lastWeatherUpdate) > connectionTimeout)
                DisplayDisconnectedMessage();
            return true;	//Return value is not used
        }

        private void PorcessMessage(IAsyncResult result)
        {
            // Get message
            string message = Encoding.UTF8.GetString(udpClient.EndReceive(result, ref iPEndPoint));

            // Restart listener
            udpClient.BeginReceive(new AsyncCallback(PorcessMessage), null);

            // save update time
            lastWeatherUpdate = DateTime.Now;

            // Process message
            Console.WriteLine($"UDP broadcast on port {PORT}: {message}");
            DisplayMessage(message);
        }

        private void DisplayMessage(string message)
        // It's a label for now
        {
            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                message = message
                .Replace('.', (0.1).ToString()[1])
                .Replace(',', (0.1).ToString()[1]); // suck it invariant culture!
                string labelText =
                    "Time: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + Environment.NewLine
                    + "Station direction: " + double.Parse(message.Split('|')[0]).ToString() + "°" + Environment.NewLine
                    + "Wind speed: " + double.Parse(message.Split('|')[1]).ToString() + "m/s" + Environment.NewLine
                    + "Wind direction: " + double.Parse(message.Split('|')[2]).ToString() + "°" + Environment.NewLine
                    + "Air pressure: " + double.Parse(message.Split('|')[3]).ToString() + "mBar" + Environment.NewLine
                    + "Internal temperature: " + double.Parse(message.Split('|')[4]).ToString() + "°C" + Environment.NewLine
                    + "Humidity: " + double.Parse(message.Split('|')[5]).ToString() + "%" + Environment.NewLine
                    + "External temperature: " + double.Parse(message.Split('|')[6]).ToString() + "°C" + Environment.NewLine
                    + "Battery voltage: " + double.Parse(message.Split('|')[7]).ToString() + "V";
                messageLabel.Text = labelText;
                splitContainer.Panel1Collapsed = true;
                splitContainer.Panel2Collapsed = false;
            }));
        }

        private void DisplayDisconnectedMessage()
        {
            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                disconnectedLabel.Text = $"Weather station disconnected{Environment.NewLine}({(int)DateTime.Now.Subtract(lastWeatherUpdate).TotalSeconds}s ago)";
                splitContainer.Panel1Collapsed = false;
                splitContainer.Panel2Collapsed = true;
            }));
        }

        public override bool Exit()
		//Exit called when plugin is terminated (usually when Mission Planner is exiting)
        {
            return true;	//Return value is not used
        }
    }
}
