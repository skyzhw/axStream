using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Threading;
using System.IO;
using axStream.Properties;

namespace axStream
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private Player pl;

        private void Main_Load(object sender, EventArgs e)
        {
            //this.Hide();
            bool donexml = false;
            bool firstrun = false;

            while (!donexml)
            {
                try
                {
                    DSInfo.ReadXml(Environment.GetEnvironmentVariable("appdata", EnvironmentVariableTarget.Process) + @"\axStream\info.xml");
                    donexml = true;
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(Environment.GetEnvironmentVariable("appdata", EnvironmentVariableTarget.Process) + @"\axStream");
                }
                catch (FileNotFoundException)
                {
                    StreamWriter sw = File.CreateText(Environment.GetEnvironmentVariable("appdata", EnvironmentVariableTarget.Process) + @"\axStream\\info.xml");
                    sw.WriteLine("<?xml version=\"r1.0\" standalone=\"yes\"?>");
                    sw.WriteLine("<info>");
                    sw.WriteLine("  <ae>");
                    sw.WriteLine("      <ip>10.0.1.1</ip>");
                    sw.WriteLine("      <volume>-30</volume>");
                    sw.WriteLine("      <systemaudiorouter>false</systemaudiorouter>");
                    sw.WriteLine("  </ae>");
                    sw.WriteLine("</info>\n");
                    sw.Close();

                    DSInfo.ReadXml(Environment.GetEnvironmentVariable("appdata", EnvironmentVariableTarget.Process) + @"\axStream\info.xml");
                    donexml = true;
                    firstrun = true;

                }
            }
            IPTextBox.Text = DSInfo.Tables[0].Rows[0].ItemArray[0].ToString();
            VolumeBar.Value = int.Parse(DSInfo.Tables[0].Rows[0].ItemArray[1].ToString());
            systemaudiorouter.Checked = bool.Parse(DSInfo.Tables[0].Rows[0].ItemArray[2].ToString());

            //check if a device that can route system audio is present (check playback and recording devices for the follow (and require at least one in playback and one in recording: "what u hear", "wave out mix", "CABLE input" "stereo mix", "what u hear", "wave out mix", "CABLE input", "CABLE output")
            int i = 0;
            foreach (string InputDeviceName in WaveLib.WaveNative.EnumInputDevices())
            {
                inputDeviceList.Items.Add(InputDeviceName);
                if (InputDeviceName.ToLower().Contains("waveout mix") || InputDeviceName.ToLower().Contains("what u hear") || InputDeviceName.ToLower().Contains("stereo mix") || InputDeviceName.ToLower().Contains("cable output"))
                {
                    i++;
                }
            }
            if (inputDeviceList.Items.Count != 0)
            {
                inputDeviceList.SelectedIndex = 0;
            }

            int q = 0;
            foreach (string OutputDeviceName in WaveLib.WaveNative.EnumOutputDevices())
            {
                 if ( OutputDeviceName.ToLower().Contains("waveout mix") || OutputDeviceName.ToLower().Contains("what u hear") || OutputDeviceName.ToLower().Contains("stereo mix") || OutputDeviceName.ToLower().Contains("cable input") )
                 {
                     q++;
                 }
            }

            bool systemaudiorouterpresent = false;
            if (i > 0 && q > 0)
            {
                systemaudiorouterpresent = true;
            }


            //if so, verify vbaudio.checked.
            //if it is, then enable the output to VB audio cable, then default to the VB audio cable input...
            if (systemaudiorouter.Checked)
            {
                //Find the member of the inputDeviceList that matches the global device
                //inputDeviceList.SelectedIndex =;
            }

            if (firstrun && !systemaudiorouterpresent)
            {
                MessageBox.Show("It doesn't appear that you have a sound device that can route all system audio over Airplay.  Would you like to visit a web page for assistance in finding a free-as-in-beer one (VB-Audio's Virtual Cable)?", "System audio routing unavailable", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }


            notifyIcon.Text = "";

            contextMenu.Items[0].Visible = true;
            contextMenu.Items[1].Visible = false;
            contextMenu.Items[3].Enabled = true;
            VolumeGB.Enabled = false;
            ConnectionGB.Enabled = true;
            StartButton.Enabled = true;
            StopButton.Enabled = false;
        }

        private void OnConnect(object sender, EventArgs e)
        {
            StatusLabel.Text = "Connected";
            notifyIcon.Icon = Resources.streaming;
            notifyIcon.Text = "Streaming to: " + IPTextBox.Text;
            notifyIcon.ShowBalloonTip(2000,
                                      "Connected",
                                      "Connected successfully to: " + IPTextBox.Text, 
                                      ToolTipIcon.Info);

            DoConnectControls();
        }

        private void DoConnectControls()
        {
            contextMenu.Items[0].Visible = false;
            contextMenu.Items[1].Visible = true;
            VolumeGB.Enabled = true;
            ConnectionGB.Enabled = false;
            StartButton.Enabled = false;
            StopButton.Enabled = true;
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            StatusLabel.Text = "Not connected";
            notifyIcon.Icon = Resources.inactive;
            notifyIcon.Text = "";
            notifyIcon.ShowBalloonTip(2000,
                          "Disconnected",
                          "Disconnected from: " + IPTextBox.Text,
                          ToolTipIcon.Info);

            DoDisconnectControl();
        }

        private void OnError(object sender, axStream.Player.ErrorEventArgs e)
        {
            StatusLabel.Text = "Error";
            notifyIcon.Icon = Resources.error;

            if (e.Error == Player.ErrorEventArgs.ERRORNUMBER.ERRORCONNECTING)
            {
                notifyIcon.Text = "Error connecting to: ";// +IPTextBox.Text + "\n" + e.Exception.ToString();
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "Could not connect to: " + IPTextBox.Text,
                          ToolTipIcon.Error);
            }
            else if (e.Error == Player.ErrorEventArgs.ERRORNUMBER.ERRORSENDING)
            {
                notifyIcon.Text = "Error sending to: ";// +IPTextBox.Text + "\n" + e.Exception.ToString();
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "Transmission was interrupted when sending to: " + IPTextBox.Text,
                          ToolTipIcon.Error);
            }
            else if (e.Error == Player.ErrorEventArgs.ERRORNUMBER.ERRORRECORDING)
            {
                notifyIcon.Text = "Record error: ";// +e.Exception.ToString();
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "Error recording from sound card",
                          ToolTipIcon.Error);
            }
            else
            {
                notifyIcon.Text = "Error...";
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "An unknown error occured",
                          ToolTipIcon.Error);
            }

            DoDisconnectControl();
        }

        private void DoDisconnectControl()
        {
            contextMenu.Items[0].Visible = true;
            contextMenu.Items[1].Visible = false;
            VolumeGB.Enabled = false;
            ConnectionGB.Enabled = true;
            StartButton.Enabled = true;
            StopButton.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartPlaying();
        }

        private void StartPlaying()
        {
            DoConnectControls();

            StatusLabel.Text = "Connecting...";
            notifyIcon.Icon = Resources.active;


            int deviceID = Convert.ToInt32(inputDeviceList.SelectedItem.ToString().Substring("Device ID ".Length, inputDeviceList.SelectedItem.ToString().IndexOf(':') - "Device ID ".Length));

            pl = new Player(IPTextBox.Text, Convert.ToDouble(VolumeBar.Value),deviceID);

            pl.OnConnect += new Player.OnConnectEventHandler(this.OnConnect);
            pl.OnDisconnect += new Player.OnDisconnectEventHandler(this.OnDisconnect);
            pl.OnError += new Player.OnErrorEventHandler(this.OnError);

            //Thread starter = new Thread(new ThreadStart(pl.Start));
            //starter.Start();
            pl.Start();

            /*if (pl.Connected)
            {
                StatusLabel.Text = "Connected";
                notifyIcon.Icon = GetIcon("active.ico");
                notifyIcon.Icon = GetIcon("streaming.ico");

                contextMenu.Items[0].Visible = false;
                contextMenu.Items[1].Visible = true;
                VolumeGB.Enabled = true;
                ConnectionGB.Enabled = false;
                StartButton.Enabled = false;
                StopButton.Enabled = true;
            }
            else
            {
                notifyIcon.Icon = GetIcon("error.ico");
            }*/
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopPlaying();
        }

        private void StopPlaying()
        {
            if (pl != null)
            {
                pl.Stop();
            }

            pl = null;
            StatusLabel.Text = "Not connected";
            notifyIcon.Icon = Resources.inactive;
            notifyIcon.Text = "";

            DoDisconnectControl();
        }

        private void VolumeBar_Scroll(object sender, EventArgs e)
        {
            pl.SetVolume(VolumeBar.Value);
        }

        protected override void OnResize(EventArgs e)
        {
            //hide the form 
            if (this.WindowState == FormWindowState.Minimized)
            {
                contextMenu.Items[3].Enabled = true;
                this.Hide();
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                base.OnResize(e);
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            contextMenu.Items[3].Enabled = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        protected override void OnClosed(EventArgs e)
        {
            ExitApplication();
            base.OnClosed(e);
        }

        private void ExitApplication()
        {
            object[] data = DSInfo.Tables[0].Rows[0].ItemArray;
            data[0] = IPTextBox.Text;
            data[1] = VolumeBar.Value.ToString();
            DSInfo.Tables[0].Rows[0].ItemArray = data;
            DSInfo.WriteXml(Environment.GetEnvironmentVariable("appdata",EnvironmentVariableTarget.Process) + @"\axStream\info.xml");

            StopPlaying();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            StartPlaying();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            StopPlaying();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }

        private void aboutOAEPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }
    }
}