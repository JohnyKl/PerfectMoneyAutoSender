using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace PerfectMoneyAutoSender
{
    public partial class Form1 : Form
    {
        private Sender sender;
        private Thread sendingThread;

        public Form1()
        {
            InitializeComponent();

            sender = new Sender();

            sender.Start(pictureBoxCaptcha);            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            progressBarPageLoad.Value = 0;

            textBoxCaptcha.Enabled = false;
            textBoxAccount.Enabled = false;
            textBoxLogin.Enabled = false;
            textBoxPassword.Enabled = false;

            if (btnLogin.Text == "Logout")
            {
                btnLogin.Text = "Login";
            }
            else
            {
                if (textBoxLogin.Text.Length > 0 && textBoxPassword.Text.Length > 0 && textBoxCaptcha.Text.Length > 0 && textBoxAccount.Text.Length > 0)
                {
                    progressBarPageLoad.Value = 10;

                    bool result = this.sender.LoginUser(textBoxLogin.Text.Trim(), textBoxPassword.Text.Trim(), textBoxCaptcha.Text.Trim(), textBoxAccount.Text.Trim());

                    progressBarPageLoad.Value = 50;

                    if (result)
                    {
                        btnStart.Enabled = true;

                        progressBarPageLoad.Value = 100;

                        btnLogin.Text = "Logout";

                        CacheLoginData();
                        btnStart.Focus();
                    }
                    else
                    {
                        textBoxCaptcha.Enabled = true;
                        textBoxAccount.Enabled = true;
                        textBoxLogin.Enabled = true;
                        textBoxPassword.Enabled = true;

                        this.sender.Start(pictureBoxCaptcha);
                    }
                }
            }            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (btnStart.Text.Equals("Start"))
            {
                this.sender.send = true;
                XMLDataReader.ReadExcelDocument();
                //XMLDataReader.ReadDocument();
                
                this.sender.progressBar = progressBarPageLoad;
                this.sender.startStop = btnStart;

                /*this.sender.Sending();*/

                sendingThread = new Thread(new ThreadStart(this.sender.Sending));
                
                sendingThread.Start();

                btnStart.Text = "Stop";
            }
            else
            {
                this.sender.send = false;

                if (sendingThread.IsAlive)
                {
                    sendingThread.Abort();
                }
                
                btnStart.Text = "Start";
            }            
        }

        private void SetCachedLoginData()
        {
            try
            {
                if (File.Exists(@"data.l"))
                {
                    string text = Cryptor.Decrypt(File.ReadAllText(@"data.l"), true);

                    int start = text.IndexOf(":");
                    int end = text.IndexOf(";");

                    textBoxLogin.Text = text.Substring(start + 1, end - start - 1);

                    text = text.Remove(0, end + 1);

                    start = text.IndexOf(":");
                    end = text.IndexOf(";");

                    textBoxPassword.Text = text.Substring(start + 1, end - start - 1);

                    text = text.Remove(0, end + 1);

                    start = text.IndexOf(":");
                    end = text.IndexOf(";");

                    textBoxAccount.Text = text.Substring(start + 1, end - start - 1);
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine("error");
                textBoxAccount.Text = "";
                textBoxLogin.Text = "";
                textBoxPassword.Text = "";                
            }
        }

        private void CacheLoginData()
        {
            string data = "login:" + textBoxLogin.Text.Trim() + ";\npassword:" + textBoxPassword.Text.Trim() + ";\naccount:" + textBoxAccount.Text.Trim() + ";";

            File.WriteAllText(@"data.l", Cryptor.Encrypt(data, true));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetCachedLoginData();
        }       
                
        private void btnReload_Click(object sender, EventArgs e)
        {
            this.sender.Start(pictureBoxCaptcha);
            btnStart.Enabled = false;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.sender.Stop();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin_Click(null, null);
            }
        }
    }
}
