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
    public interface IView
    {
        void SetController(IController controller);
        //Open serial port event
        void OpenComEvent(Object sender, SerialPortEventArgs e);
        //Close serial port event
        void CloseComEvent(Object sender, SerialPortEventArgs e);
        //Serial port receive data event
        void ComReceiveDataEvent(Object sender, SerialPortEventArgs e);
    }
    public partial class DC_MainForm : Form
    {
        private IController controller;
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
    }
}
