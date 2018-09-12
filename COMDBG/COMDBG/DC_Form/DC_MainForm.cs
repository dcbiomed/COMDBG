using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace COMDBG.DC_Form
{
    public interface IView_DC
    {
        void SetController(IController_DC controller);
        //Open serial port event
        void OpenComEvent(Object sender, SerialPortEventArgs e);
        //Close serial port event
        void CloseComEvent(Object sender, SerialPortEventArgs e);
        //Serial port receive data event
        void ComReceiveDataEvent(Object sender, SerialPortEventArgs e);
    }
    public partial class DC_MainForm : Form, IView_DC
    {
        private IController_DC controller;
        private int sendBytesCount = 0;
        private int receiveBytesCount = 0;
        private string dcDeviceName = "Prolific USB-to-Serial Comm Port";
        private Dictionary<string, string> dictionaryPortName = new Dictionary<string, string>();
        private int countPort = 0;
        private string connPortName = string.Empty;

        public DC_MainForm()
        {
            InitializeComponent();
            SearchDCDevice();
            this.statusTimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            this.toolStripStatusTx.Text = "Sent: 0";
            this.toolStripStatusRx.Text = "Received: 0";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }
        /// <summary>
        /// Set controller
        /// </summary>
        /// <param name="controller"></param>
        public void SetController(IController_DC controller)
        {
            this.controller = controller;
        }
        private void SearchDCDevice()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
                //{4d36e978-e325-11ce-bfc1-08002be10318}為設備類別port（端口（COM&LPT））的GUID
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string fullName = queryObj.GetPropertyValue("Name").ToString();
                    string[] aryName = Array.ConvertAll(fullName.Split(new char[2] { '(', ')' }), str => str.Trim());
                    dictionaryPortName.Add(aryName[1], aryName[0]);
                }
                GetDCDevice();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred : " + e.Message);
            }
        }
        void GetDCDevice()
        {
            bool contains = dictionaryPortName.Values.Any(p => p.Equals(dcDeviceName));
            if (contains)
            {
                foreach (var element in dictionaryPortName)
                {
                    if (string.Equals(dcDeviceName, element.Value))
                    {
                        connPortName = element.Key;
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// update status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenComEvent(Object sender, SerialPortEventArgs e)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action<Object, SerialPortEventArgs>(OpenComEvent), sender, e);
                return;
            }

            if (e.isOpend)  //Open successfully
            {
                statuslabel.Text = " Opend";
                openCloseSpbtn.Text = "Close";
                sendbtn.Enabled = true;
                refreshbtn.Enabled = false;
            }
            else    //Open failed
            {
                statuslabel.Text = "Open failed !";
                sendbtn.Enabled = false;
            }
        }

        /// <summary>
        /// update status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CloseComEvent(Object sender, SerialPortEventArgs e)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action<Object, SerialPortEventArgs>(CloseComEvent), sender, e);
                return;
            }

            if (!e.isOpend) //close successfully
            {
                statuslabel.Text = " Closed";
                openCloseSpbtn.Text = "Open";
                sendbtn.Enabled = false;
                refreshbtn.Enabled = true;
            }
        }

        /// <summary>
        /// Display received data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComReceiveDataEvent(Object sender, SerialPortEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    Invoke(new Action<Object, SerialPortEventArgs>(ComReceiveDataEvent), sender, e);
                }
                catch (System.Exception)
                {
                    //disable form destroy exception
                }
                return;
            }
            //display as hex
            if (receivetbx.Text.Length > 0)
            {
                receivetbx.AppendText("-");
            }
            receivetbx.AppendText(IController.Bytes2Hex(e.receivedBytes));
            //update status bar
            receiveBytesCount += e.receivedBytes.Length;
            toolStripStatusRx.Text = "Received: " + receiveBytesCount.ToString();
        }
    }
}
