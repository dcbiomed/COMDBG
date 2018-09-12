using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
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

        byte[] cmd;
        enum CmdTable : byte
        {
            ChkConnection = 0x60,
            GetDeviceID = 0x65,
            GetDataInfo = 0x72,
            GetNthPageData = 0x73,
            SetClrFlag = 0x74,
            LoginPwd1 = 0x75 // DCBM123
        }

        public DC_MainForm()
        {
            InitializeComponent();
            this.statusTimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            this.toolStripStatusTx.Text = "Sent: 0";
            this.toolStripStatusRx.Text = "Received: 0";
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.grpBoxFlow.Enabled = false;
        }
        /// <summary>
        /// Set controller
        /// </summary>
        /// <param name="controller"></param>
        public void SetController(IController_DC controller)
        {
            this.controller = controller;
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
            }
            else    //Open failed
            {
                statuslabel.Text = "Open failed !";
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
            receivetbx.AppendText(IController.Bytes2Hex(e.receivedBytes)+Environment.NewLine);
            //update status bar
            receiveBytesCount += e.receivedBytes.Length;
            toolStripStatusRx.Text = "Received: " + receiveBytesCount.ToString();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            SearchDCDevice();
            if (!connPortName.Equals(string.Empty))
            {
                controller.OpenSerialPort(connPortName, "115200", "8", StopBits.One.ToString(), Parity.None.ToString(), Handshake.None.ToString());
                this.grpBoxFlow.Enabled = true;
            }
            else
            {
                MessageBox.Show("The target device doesn't exist !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
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
        private void btnPool_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            switch (btn.Name)
            {
                case "btnChkConn":
                    cmd = new byte[4];
                    cmd = GetFullBytes(cmd, CmdTable.ChkConnection);
                    break;
                case "btnLogin":
                    cmd = new byte[4 + 7];
                    cmd[2] = 0x44;//D
                    cmd[3] = 0x43;//C
                    cmd[4] = 0x42;//B
                    cmd[5] = 0x4D;//M
                    cmd[6] = 0x31;//1
                    cmd[7] = 0x32;//2
                    cmd[8] = 0x33;//3
                    cmd = GetFullBytes(cmd, CmdTable.LoginPwd1);
                    break;
                case "btnGetDeviceID":
                    cmd = new byte[4];
                    cmd = GetFullBytes(cmd, CmdTable.GetDeviceID);
                    break;
                case "btnGetDataInfo":
                    cmd = new byte[4];
                    cmd = GetFullBytes(cmd, CmdTable.GetDataInfo);
                    break;
                case "btnGetNthData":
                    int xth;
                    cmd = new byte[4 + 2];
                    //int.TryParse(tbxPageXth.Text, out xth);
                    //cmd[3] = (byte)(xth & 0x3F);
                    cmd = GetFullBytes(cmd, CmdTable.GetNthPageData);
                    break;
                case "btnSetClearFlag":
                    cmd = new byte[4];
                    cmd = GetFullBytes(cmd, CmdTable.SetClrFlag);
                    break;
            }
            int newLen = cmd.Length + 3;//in order to clear meter buffer
            Array.Resize(ref cmd, newLen);
            SendCMD(btn, cmd);
        }

        private void SendCMD(Button btn, Byte[] bytesBuf)
        {
            bool flag = false;
            //send bytes to serial port
            btn.Enabled = false;//wait return
            flag = controller.SendDataToCom(bytesBuf);
            btn.Enabled = true;
            sendBytesCount += bytesBuf.Length;
            if (flag)
            {
                statuslabel.Text = "Send OK !";
            }
            else
            {
                statuslabel.Text = "Send failed !";
            }
            //update status bar
            toolStripStatusTx.Text = "Sent: " + sendBytesCount.ToString();
        }

        byte[] GetFullBytes(byte[] dataBytes, CmdTable cmd)
        {
            byte[] res = dataBytes;
            res[0] = 0xFF;//Start
            res[1] = (byte)cmd;//Command
            res[res.Length - 2] = GetCheckSumXor(res);//CheckSum
            res[res.Length - 1] = 0xFE;
            return res;
        }
        byte GetCheckSumXor(byte[] raw)
        {
            byte result = raw[1];
            for (int i = 2; i < raw.Length - 2; i++)
            {
                result ^= raw[i];
            }
            return result;
        }
        private void DC_MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            controller.CloseSerialPort();
        }

        private void receivetbx_TextChanged(object sender, EventArgs e)
        {
            receivetbx.SelectionStart = receivetbx.Text.Length;
            receivetbx.ScrollToCaret();
        }

        private void statustimer_Tick(object sender, EventArgs e)
        {
            this.statusTimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }

        private void clearReceivebtn_Click(object sender, EventArgs e)
        {
            receivetbx.Text = "";
            toolStripStatusRx.Text = "Received: 0";
            receiveBytesCount = 0;
        }
    }
}
