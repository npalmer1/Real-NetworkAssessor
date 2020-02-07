using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using Renci.SshNet;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;


namespace SecureRemote2
{
    public partial class Form1 : Form
    {
        struct results
        {
            public int match;
        }
        String theVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        static List<results> matchlist = new List<results>();
        results m;
        //string fout, fcomment;
        int count = 0;
        int selectedFile = 0;
        //public string[] infile = new string[] { "", "", "", "", "", "", "", "", "", "" };
        //public string[] ftemplate = new string[] { "", "", "", "", "", "", "", "", "", "" };
        string baseip = "192.168.1.0";

        string remoteStr = "";
        string localStr = "";
        //public int maxtasks = 200;
        //static int maxlines = 500;
        // int defaultMark = 1;
        //bool[] lineused = new bool[maxlines];
        bool assessReady = false;
        static int maxPCs = 254;
        int[] connPCs = new int[maxPCs + 1];
        int noPCs = 0;
        int ipfrom = 1;
        int ipto = maxPCs;
        bool allowMaster = true;
        bool allowSelect = true; //allowed to select PCs even if no conenctivity
        int MasterPC = 26;
        int selectedNet = 0;
        NetForm Networks = new NetForm();   //stores Networks in a tree

        string pc, remoteuser, pass, initdir, Server;

        string ConfigDir = "C:\\NetworkAssessor";
        string scriptDir = "C:\\Users\\Administrator\\Documents\\";
        bool checkall = true;
        static int MaxFiles = 6;
        string nl = Environment.NewLine;

        //password to encrypt passwords when saving:
        string encpass = "savesecure";

        /*struct tasks
        {
            public bool taskexist;
            public int tasktotal;
            public int taskmax;
        }
        //public List<tasks> tasklist = new List<tasks>();
        tasks[] tasklist = new tasks[maxtasks];*/
        //int tasktotal = 0;

        string DefaultDir = "C:\\NetworkAssessor\\";
        string AssessFile = "";
        //string AssessFilePath = "C:";
        string AssessFilePath = "C:\\NetworkAssessor\\";
        string rootDir = "C:\\NetworkAssessor\\Root";


        Parser parser = new Parser();

        static int oneDir = -1;
        double[] resultPCs = new double[maxPCs]; //results for each PC
        string[] rawresultPCs = new string[maxPCs];
        string[] rPCs = new string[maxPCs];
        bool windows = true; //can be used for future if linux paths to be used

        int nextline = 0;
        string prompt = ""; //iused to record command line prompt 
        string cmd = "";  //command string

        string secondFile = "";
        bool timerCount = false;


        SshClient ssh; //used by ssh shell later on
        ShellStream SSHstream; //stream used by ssh shell

        bool BlinkTest = false;
        string commandResult = ""; //return reult of command

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Networks.Activate();
            this.Activate();
            scriptDirBox.Text = scriptDir;
            parser.fout = "";
            parser.fcomment = "";
            Server = "\\\\172.16.101.3\\";
            //EnableDisableText(); //enable or disable text boxes according to check box selection - initially all disabled
            remoteuser = "administrator";
            MastercheckBox.Checked = true;

            //baseip = "172.16.199.0";
            this.Text = "Real Network Assessor                   " + theVersion + "                               (c) 2020                    ";
            try
            {
                bool isExists = Directory.Exists(ConfigDir);

                if (!isExists)
                    System.IO.Directory.CreateDirectory(ConfigDir + "\\").Create();
                //load configs
                if (!Read_Configs())
                { MessageBox.Show("Unable to read configuration - go to Configuration tab and save settings"); }
                dirBox.Text = PCDirBox.Text;
                dirBox2.Text = PCDirBox.Text;
                dirBox.ReadOnly = PCCheckBox.Checked;
                dirBox2.ReadOnly = PCCheckBox.Checked;

            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);

            }

            try
            {
                //find the lab PCs that are connected and put them into the list box:

                Parallel.Invoke(() => Show_Label("Please wait, building PC list"), () => findPCsParallel(connectableCheckBox.Checked));
                Populate_ListBox();
                //Find_Shares();
                //ensure that all hosts are trusted to share files/folders:
                System.Diagnostics.Process.Start("winrm", "s winrm/config/client @{TrustedHosts=\"*\"}");
                //LoadVMList();
            }
            catch { Show_Label("Error finding PCs"); }

            try
            {
                if (RunningPlatform() == Platform.Windows)
                {
                    DefaultDir = "C:\\NetworkAssessor\\";
                    Directory.CreateDirectory(DefaultDir);
                }
                else if (RunningPlatform() == Platform.Linux)
                {
                    var homePath = Environment.GetEnvironmentVariable("HOME");
                    DefaultDir = Path.Combine(homePath, "NetworkAssessor");
                    Directory.CreateDirectory(DefaultDir);
                    DefaultDir = DefaultDir + "/";
                }
            }
            catch { }

            try
            {
                //load help file:

                richTextBox1.LoadFile("C:\\help\\sremhelp.rtf");
                richTextBox1.SelectionFont = new Font("Verdana", 10, FontStyle.Regular);
            }
            catch
            {
                Show_Label("Unable to load help file");
            }
        }

        public enum Platform
        {
            Windows,
            Linux,
            Mac,
        }

        public static Platform RunningPlatform()    //find platform that this application is running on
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }

        private void Show_Label(string label)
        {
            int t = 2000;
            ShowForm form2 = new ShowForm();
            form2.Passvalue[0] = label;
            form2.Passvalue[1] = Convert.ToString(t);
            form2.Show();
        }

        public static bool IsValidIP(string ipval)
        {
            //check to see if string passed is a valid IP address
            if (ipval.Trim() == "")
            {
                return true;
            }
            var octets = ipval.Split('.');

            // if not 4 octets return false
            if (!(octets.Length == 4))
            {

                return false;
            }

            // for each octet
            foreach (var octet in octets)
            {
                int a;
                if (!Int32.TryParse(octet, out a)
                    || !a.ToString().Length.Equals(octet.Length)
                    || a < 0
                    || a > 255) { return false; }

            }

            return true;
        }
        private void Populate_ListBox()
        {
            int f;
            int t;
            if (IPrangeCheckBox.Checked)
            {
                f = ipfrom;
                t = ipto;
            }
            else
            {
                f = 1;
                t = noPCs;
            }
            //populate the list box with PCs and indicate whether connected (PC) or not (xx)
            for (int i = f; i < t + 1; i++)
            {
                try
                {
                    if (connPCs[i] == 1) //windows
                    { PClistBox.Items.Add("PC" + (i).ToString()); }
                    else if (connPCs[i] == 2) //linux
                    {
                        PClistBox.Items.Add("LC" + (i).ToString());
                    }
                    else if (connPCs[i] == 3)
                    {
                        PClistBox.Items.Add("CD" + (i).ToString());
                    }
                    else if (connPCs[i] == 0) //not connected
                    { PClistBox.Items.Add("xx" + (i).ToString()); }
                    else
                    { PClistBox.Items.Add("  " + (i).ToString()); }
                }
                catch { }
            }
        }

        private bool Modify_ListBox()
        {
            bool ok = true;
            int f;
            int t;
            if (IPrangeCheckBox.Checked)
            {
                f = ipfrom;
                t = ipto;
            }
            else
            {
                f = 1;
                t = noPCs;
            }
            //update the listbox with PCs (called from timer_tick and when requesting to check connectivity now
            for (int i = f; i < t + 1; i++)
            {
                try
                {
                    if (connPCs[i] == 1) //windows
                    { PClistBox.Items[i - 1] = "PC" + (i).ToString(); }  //note: PCListBox starts at 0
                    else if (connPCs[i] == 2) //linux
                    { PClistBox.Items[i - 1] = "LC" + (i).ToString(); }
                    else if (connPCs[i] == 3) //Cisco IOS
                    { PClistBox.Items[i - 1] = "CD" + (i).ToString(); }
                    else if (connPCs[i] == 0)
                    {
                        PClistBox.Items[i - 1] = "xx" + (i).ToString();
                        PClistBox.SetSelected(i - 1, false);
                    }
                    else
                    {
                        PClistBox.Items[i - 1] = "  " + (i).ToString();
                        PClistBox.SetSelected(i - 1, false);
                    }
                }
                catch { ok = false; }
            }
            return ok;
        }


        private bool Ping_Host(string ip)
        {
            //ping host lab PC to see if it is connected
            var ping = new Ping();

            //var options = new PingOptions { DontFragment = true };
            var options = new PingOptions { Ttl = 1 };

            char[] b = new char[1];
            b[0] = 'a';

            //var buffer = Encoding.ASCII.GetBytes(new string('a', 5));
            var buffer = Encoding.ASCII.GetBytes(new string('a', 1));
            //var buffer = Encoding.ASCII.GetBytes(b);

            try
            {
                var reply = ping.Send(ip, 1, buffer, options);
                if (reply == null)
                {
                    return false;
                }

                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private int findOS(string ip)
        {
            //string cmd = "ver";
            string cmd = "uname";
            string ret = "";


            /*if (ret == "nc")
            {
                return 0;
            }*/
            cmd = "ver";
            if (SendCommand(ip, cmd).Contains("Windows"))
            {
                return 1;   //windows
            }
            cmd = "uname";

            if (SendCommand(ip, cmd).Contains("inux"))
            {
                return 2;   //linux
            }
            cmd = Environment.NewLine + "show";

            if (SendCommand(ip, cmd).Contains("%"))
            {
                return 3;   //cisco IOS
            }
            return 0;
        }
        /*private void findPCs()
        {
            //put a list of connected PCs into array connPCs
            string host;          
            int os = 0;
            connPCs[0] = 0; //don't use netdwork address
            for (int i = 1; i < maxPCs + 1; i++)
            {
                host = baseip.Substring(0, baseip.Length - 1) + (i + 1).ToString();
                if (Ping_Host(host))
                {
                    os = findOS(host);
                    connPCs[i] = os; 
                }
                else { connPCs[i] = 0; } //not connected
            }
        }*/

        private void findPCsParallel(bool H)
        {
            //put a list of connected PCs into array connPCs
            try
            {
                Parallel.Invoke(() => PingAll(1, 10, H), () => PingAll(11, 20, H), () => PingAll(21, 30, H), () => PingAll(31, 40, H), () => PingAll(41, 50, H), () => PingAll(51, 60, H), () => PingAll(61, 70, H), () => PingAll(71, 80, H), () => PingAll(81, 90, H), () => PingAll(91, 100, H), () => PingAll(101, 110, H), () => PingAll(111, 120, H), () => PingAll(121, 130, H), () => PingAll(131, 140, H), () => PingAll(141, 150, H), () => PingAll(151, 160, H), () => PingAll(161, 170, H), () => PingAll(171, 180, H), () => PingAll(181, 190, H), () => PingAll(191, 200, H), () => PingAll(201, 210, H), () => PingAll(211, 220, H), () => PingAll(221, 230, H), () => PingAll(231, 240, H), () => PingAll(241, 250, H), () => PingAll(251, 254, H));
            }

            catch { Show_Label("Error - check base IP address"); }
        }

        private void PingAll(int ip1, int ip2, bool markOS)
        {
            string host = "";
            int os = 0;
            try
            {
                for (int i = ip1; i < ip2 + 1; i++)
                {
                    host = baseip.Substring(0, baseip.Length - 1) + (i).ToString();
                    if (IsValidIP(host))
                    {
                        if (Ping_Host(host))
                        {
                            if (markOS)
                            {
                                os = findOS(host);
                            }
                            else
                            {
                                os = 1;
                            }
                            connPCs[i] = os;
                            if (noPCs < i)
                            {
                                noPCs = i;
                            }
                        }
                        else
                        {
                            connPCs[i] = 0;
                        }
                    }
                }
            }
            catch
            {

            }

        }


        //==================================================================================


        public ConnectionInfo CreateConnectionInfo(string ip_address)     //create connection to ubuntu server
        {
            //string ip_address = ipBox.Text;
            string username = userBox.Text;
            string password = passBox.Text;

            ConnectionInfo connectionInfo;

            connectionInfo = new ConnectionInfo(ip_address,
                                        username,
                                        new PasswordAuthenticationMethod(username, password),
                                        new PrivateKeyAuthenticationMethod("rsa.key"));
            /*using (var client = new SftpClient(connectionInfo))
            {
                client.Connect();
            }*/

            /* const string privateKeyFilePath = @"C:\some\private\key.pem";
             ConnectionInfo connectionInfo;
             using (var stream = new FileStream(privateKeyFilePath, FileMode.Open, FileAccess.Read))
             {
                 var privateKeyFile = new PrivateKeyFile(stream);
                 AuthenticationMethod authenticationMethod =
                     new PrivateKeyAuthenticationMethod(userBox.Text.Trim(), privateKeyFile);

                 connectionInfo = new ConnectionInfo(
                     IPBox.Text.Trim(),
                     userBox.Text.Trim(),
                     authenticationMethod);
             }*/
            return connectionInfo;
        }


        private void RunCommands(string ip)
        {
            string cmd = "";
            cmd = commandBox.Text;
            try
            {
                using (var ssh = new SshClient(CreateConnectionInfo(ip)))
                {
                    try
                    {
                        ssh.Connect();
                    }
                    catch {
                        MessageBox.Show("Cannot connect - check connection");
                        return;
                    }
                    //var command = ssh.CreateCommand("uptime"); //get uptime of system
                    for (int i = 0; i < commandBox.Lines.Length; i++)
                    {
                        cmd = commandBox.Lines[i];
                        var command = ssh.CreateCommand(cmd);
                        var result = command.Execute();
                        richCommand.AppendText(result + Environment.NewLine);
                        richCommand.AppendText("----------------------------------------------" + Environment.NewLine);
                        richCommand.SelectionStart = richCommand.Text.Length;
                        richCommand.ScrollToCaret();
                    }
                    try
                    {
                        ssh.Disconnect();
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string RunACommand(string ip, string cmd)
        {
            var result = "";
            try
            {
                using (var ssh = new SshClient(CreateConnectionInfo(ip)))
                {
                    try
                    {
                        ssh.Connect();
                    }
                    catch { return result; }
                    try
                    {
                        var command = ssh.CreateCommand(cmd);
                        result = command.Execute();
                        if (result == "")
                        {
                            result = command.Error;
                        }
                    }
                    catch { MessageBox.Show("ERROR"); }
                    try
                    {
                        ssh.Disconnect();
                        return result;
                    }
                    catch {
                        return result;
                    }
                }
            }
            catch { return result; }
        }

        private bool CallRunCommands()
        {
            bool a;

            int i;
            string istr;
            a = false;
            pc = "";
            bool sel = false;

            int f;

            if (IPrangeCheckBox.Checked)
            {
                f = ipfrom;
            }
            else
            {
                f = 1;
            }
            richCommand.Clear();
            sendLabel.Visible = true;
            //BlinkTest = true;
            //Blink();
            for (i = 0; i < PClistBox.Items.Count; i++)
            {
                if (PClistBox.GetSelected(i))
                {
                    if (PClistBox.Items[i].ToString().Contains("C"))
                    {
                        istr = (i + f).ToString();
                        pc = baseip.Substring(0, baseip.Length - 1) + istr;
                        RunCommands(pc);
                        sel = true;
                    }
                }
            }
            sendLabel.Visible = false;
            //BlinkTest = false;
            if (!sel)
            {
                MessageBox.Show("No PCs selected or none connected");
            }
            return true;
        }

        private void commandButton_Click(object sender, EventArgs e)
        {
            CallRunCommands();
        }


        private async void Blink()
        {
            while (BlinkTest)
            {
                await Task.Delay(500);
                sendLabel.BackColor = sendLabel.BackColor == Color.Red ? Color.Green : Color.Red;
            }
        }

        private bool checkBlankDir(bool device, string remotefile, string localfile) //check to see if local and remote boxes are blank
        {
            bool local = false;
            bool remote = false;
            if (localfile.Trim() == "")
            {
                local = false;
            }
            else
            { local = true; }
            if (!device)
            {
                if (remotefile.Trim() == "")
                {
                    remote = false;
                }
                else
                { remote = true; }
                if (!local && !remote)
                {
                    MessageBox.Show("Both local and remote directory paths are blank");
                    return false;
                }
                else if (!local && remote)
                {
                    MessageBox.Show("Local directory path is blank");
                    return false;
                }
                else if (local && !remote)
                {
                    MessageBox.Show("Remote directory path is blank");
                    return false;
                }
                else
                { return true; }
            }
            else
            {
                return local;   //only need to checkl local
            }
        }

        private bool FindRemoteFolder(string ip, string remote)
        {
            string cmd = "";
            string response1 = "";

            /*cmd = "\"test\" >> " + remote + "/test.txt";
            response1 = SendCommand(ip, cmd).ToUpper();
            cmd = "\"test\" >> " + remote + "\\test.txt";
            response2 = SendCommand(ip, cmd).ToUpper();*/

            cmd = "dir " + remote;
            response1 = SendCommand(ip, cmd).ToUpper();

            if (response1.Contains("NO SUCH FILE OR DIRECTORY") || response1.Contains("FILE NOT FOUND") || response1.Trim() == "")
            {
                return false;   //folder not found or empty
            }
            else
            {
                //folder found           
                return true;
            }
        }

        private string StreamCommand(string ip, string cmd)   //send a single command get a stream back
        {
            string result = "";

            try
            {
                using (var ssh = new SshClient(CreateConnectionInfo(ip)))
                {
                    try
                    {
                        ssh.Connect();
                    }
                    catch { }

                    //var command = ssh.CreateCommand(cmd);
                    //result = ssh.RunCommand(cmd).Result;
                    var strm = ssh.RunCommand(cmd).OutputStream;
                    result = strm.ToString();
                    //var result = command.Execute();

                    try
                    {
                        ssh.Disconnect();
                    }
                    catch { }
                }
            }
            catch { }
            return result;
        }
        private string SendCommand(string ip, string cmd)   //send a single command
        {
            string result = "";

            try
            {
                using (var ssh = new SshClient(CreateConnectionInfo(ip)))
                {
                    try
                    {
                        ssh.Connect();
                    }
                    catch { return "nc"; }

                    //var command = ssh.CreateCommand(cmd);
                    result = ssh.RunCommand(cmd).Result;
                    //var result = command.Execute();

                    try
                    {
                        ssh.Disconnect();
                    }
                    catch { }
                }
            }
            catch { }
            return result;
        }
        private string OSVersion(string ip)
        {
            string cmd = "uname -a";
            string OSVer = "";

            string str = "";
            try
            {
                str = RunACommand(ip, cmd);
            }
            catch
            {
                return "";
            }

            str = str.ToLower();
            if (str.Contains("ubuntu"))
            {
                OSVer = "linux-ubuntu";
            }
            else if (str.Contains("centos"))
            {
                OSVer = "linux-centos";
            }
            else if (str.Contains("cygwin_nt"))
            {
                OSVer = "cygwin_nt";
            }

            else if (str.Contains("linux"))
            {
                OSVer = "linux";
            }
            else if (RunACommand(ip, "ver").ToLower().Contains("windows"))
            {
                OSVer = "windows";
            }
            else if (RunACommand(ip, Environment.NewLine + "show").ToLower().Contains("%"))
            {
                OSVer = "cisco";
            }
            return OSVer;
        }
        private bool GetConfigs(string local, string ip) //to transfer a file from linux to this PC
        {
            string config = "";
            string cmd = "";

            if (checkBlankDir(true, "", local))
            {

                try
                {
                    if (!Directory.Exists(local))
                    {
                        Directory.CreateDirectory(local);
                    }
                }
                catch
                {
                    MessageBox.Show("Cannot create local directory- check pathname");
                    return false;
                }
                cmd = Environment.NewLine + "show running-config";
                //cmd = Environment.NewLine + "ls -l";
                config = SendCommand(ip, cmd);
                if (config.Trim() != "nc")
                {

                    try
                    {
                        using (StreamWriter sw = new StreamWriter(local + "\\" + "running-config"))
                        {
                            sw.Write(config);
                            sw.Close();
                        }
                    }
                    catch { MessageBox.Show("Cannot save to local file"); }
                    if (tabControl1.SelectedTab.Text == "Transfer")
                    {
                        runningRichBox.Text = config;
                    }
                    //scp.Download(remote, new DirectoryInfo(@local));    //otherwise from remote to local    
                }
            }
            return true;
        }
        private char CheckRemoteFile(string remotefilepath, string ip) //remotefilepath - full path to remote file, fname - remote filename, ip
        {   //check if remote file exists
            string fname = remotefilepath.Replace('/', '\\');
            fname = Path.GetFileName(fname);

            string result = "";
            string cmd = "";
            try
            {
                string OSVer = OSVersion(ip);
                if (OSVer.Contains("linux"))
                {
                    cmd = "ls " + remotefilepath;
                    result = RunACommand(ip, cmd);
                    if (result.ToLower().Contains("no such file"))
                    {
                        return 'N';
                    }
                    else if (result.ToLower().Contains(fname))
                    {
                        return 'F';
                    }
                    else
                    {
                        return 'E';
                    }
                }
                else if (OSVer.Contains("windows"))
                {
                    cmd = "dir /B " + remotefilepath;
                    result = RunACommand(ip, cmd);
                    if (result.ToLower().Contains(Path.GetFileName(fname).ToLower()))
                    {
                        return 'F';
                    }
                    else if (result.ToLower().Contains("not found"))
                    {
                        return 'N';
                    }
                    else
                    {
                        return 'E';
                    }
                }
                else if (OSVer.Contains("cygwin"))
                {
                    cmd = "dir " + "\"" + remotefilepath + "\"";
                    result = RunACommand(ip, cmd);
                    if (result.ToLower().Contains(Path.GetFileName(fname).ToLower()))
                    {
                        return 'F';
                    }
                    else if (result.ToLower().Contains("not found"))
                    {
                        return 'N';
                    }
                    else
                    {
                        return 'E';
                    }
                }
            }
            catch
            {
                return 'E';
            }
            return 'E';
        }

        /*private bool CheckRemoteFileExist(string ip, string remote, string outfile, bool append)
        {
            string nl = Environment.NewLine;
            char res = CheckRemoteFile(remote, ip);
            if (res == 'E') //file found
            {
                return true;
            }
            else if (res == 'N') //not found
            {
                WriteOutputFile(outfile, append, "#Remote file not found: " + remote + nl + "#-----------------------------");
            }
            else if (res == 'E')
            {
                WriteOutputFile(outfile, append, "#Error locating remote file: " + remote + nl + "#-----------------------------");
            }
            return false;
        }*/

        private void WriteOutputFile(string outfile, bool append, string msg)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(outfile, append))
                {
                    sw.WriteLine(msg);
                    sw.Close();
                }
            }
            catch
            { }
        }
        private bool GetFiles(string remote, string local, string ip, bool to, bool deleteOld, bool Folder) //to transfer a file from linux to this PC
        {

            string fname = remote.Replace('/', '\\');
            fname = Path.GetFileName(fname);
            string fnamepath = local + "\\" + fname;
            if (!to) //if receiving remote file - backup and delete old file and create local directory if necessary
            {
                try
                {
                    if (!Directory.Exists(local))
                    {
                        Directory.CreateDirectory(local);
                    }
                    else if (deleteOld)
                    {
                        if (File.Exists(fnamepath + ".old"))
                        {
                            File.Delete(fnamepath + ".old");
                        }
                        if (File.Exists(fnamepath))
                        {
                            File.Copy(fnamepath, fnamepath + ".old");
                            File.Delete(fnamepath);
                        }
                    }
                    else { }
                }
                catch
                {
                    MessageBox.Show("Cannot create local file/directory");
                    return false;
                }
            }
            using (var scp = new ScpClient(CreateConnectionInfo(ip)))
            {
                try
                {
                    scp.OperationTimeout = new TimeSpan(0, 0, 2);
                    scp.Connect();
                    //scp.Download("C:/test", new DirectoryInfo(@"C:\Temp\ScpDownloadTest"));
                    //scp.Download("/etc/firefox", new DirectoryInfo(@"C:\Temp\ScpDownloadTest"));
                }
                catch
                {
                    DialogResult r = MessageBox.Show("Cannot connect - check username/password or IP");
                    checkall = false;
                    return false;
                }
                if (checkBlankDir(false, remote, local))
                {
                    try
                    {
                        if (to) //send local folder to remote PC
                        {
                            //scp.Download(local, new DirectoryInfo(@remote));
                            if (Folder)
                            {
                                RunACommand(ip, "mkdir " + remote);
                                scp.Upload(new DirectoryInfo(@local), remote);
                            }
                            else
                            {
                                scp.Upload(new FileInfo(@local + "\\" + fname), remote);
                            }
                        }
                        else  //retrieve folder from remote to local
                        {
                            if (Folder)
                            {
                                scp.Download(remote, new DirectoryInfo(@local));    //otherwise from remote to local
                            }
                            else
                            {
                                scp.Download(remote, new FileInfo(@local + "\\" + fname)); //file and not folder
                            }
                          
                        }
                        scp.Disconnect();
                    }
                    catch
                    {
                        DialogResult r = MessageBox.Show("Cannot connect - check path");
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("Local or remote destination blank");
                    return true; //only return false if cannot connect
                }
            }
            return true;
        }

        private bool GetFiles_PC_List(bool device, bool useBase, string BaseDir, string remote, string local, bool to, bool FolderorFile)
        {

            bool a;
            bool noselected = true;
            int i;
            string istr;
            a = false;
            pc = "";
            int offset = 1;
            int count = 0;
            int foldercount = 0;
            bool noconn = false;
            int rem1 = 0;
            string based = local;
           

            if (IPrangeCheckBox.Checked)
            {
                offset = ipfrom;
            }
            else
            {
                offset = 1;
            }
            //baseip = "172.16.199.0";
            noconn = true;
            for (i = 0; i < PClistBox.Items.Count; i++)
            {
                if (PClistBox.GetSelected(i))
                {
                    if (!PClistBox.Items[i].ToString().Contains("xx")) //if connected, ie. doesn't contain xx
                    {
                        if (BaseDir.Trim().Length > 0 && !to)
                        {
                            if (singleCheckBox.Checked)
                            {
                                if (useBase)
                                {
                                    local = based + "\\" + BaseDir.Trim() + Convert.ToString(offset + i);
                                }
                                else
                                { local = based; }
                            }
                            else
                            {
                                local = based + "\\" + BaseDir.Trim() + Convert.ToString(offset + i);
                            }
                        }
                        i = i + offset;
                        istr = (i).ToString();

                        pc = baseip.Substring(0, baseip.Length - 1) + istr;
                        if (!device) //if its not a network device
                        {
                            //if (FindRemoteFolder(pc, remote))   //folder found and not empty
                            //{
                            a = GetFiles(remote, local, pc, to, true, FolderorFile ); 
                            noconn = false;
                            foldercount++;
                            //}                            
                        }
                        else
                        {
                            GetConfigs(local, pc);
                        }
                        count++;
                    }
                    noselected = false;
                };
            }
            if (!noselected && foldercount == 0)
            {
                MessageBox.Show("Folder on remote is empty or not found");
                return false;
            }
            else if (!noselected && foldercount < count)
            {
                MessageBox.Show("Folder not found on all PCs, or some folders empty");
                return a;
            }
            if (noselected)
            {
                MessageBox.Show("No PCs selected for transfer");
            }
            if (noconn)
            {
                MessageBox.Show("No selected PCs connected");
            }
            return a;
        }

        private void Transfer(bool to)
        {

            //localStr = localBox.Text + "\\" + dirBox.Text;

            checkall = true;
            bool Folder = true;

            if (GetFiles_PC_List(deviceCheckBox.Checked, useBasecheckBox.Checked, dirBox.Text, remoteStr, localStr, to, Folder))
            {
                if (checkall)
                { MessageBox.Show("Folder transferred"); }
                else { MessageBox.Show("Folder not transferred on all PCs"); }
                if (radioButton1.Checked)
                {
                    assessReady = true;
                    assessFileButton.Visible = true;
                }
            }
        }

        private void transferButton_Click(object sender, EventArgs e)
        {
            remoteStr = remoteBox.Text;
            localStr = localBox.Text;
            bool to = false;
            if (radioButton1.Checked)
            { to = false; }
            else { to = true; }
            Transfer(to);   //transfer to/from remote 
        }



        private void loadConfigButton_Click(object sender, EventArgs e)
        {

        }



        private void saveConfigButton_Click_1(object sender, EventArgs e)
        {
            //save configuration sto file serverconsole.conf:
            string fix = "true";
            if (!IsValidIP(ipBox.Text) || ipBox.Text.Trim().Length == 0)
            {
                MessageBox.Show("Invalid Base IP - enter address like 172.16.199.0 ", "IP Error");
                return;
            }
            DialogResult result = MessageBox.Show("Save  configuration?", "Configuration", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {

                try
                {

                    using (StreamWriter sw = new StreamWriter(ConfigDir + "\\Network_Assessor.conf"))
                    {
                        // Write the settings to file.
                        sw.WriteLine(ipBox.Text.Trim() + " ; " + networkLabel.Text.Trim());
                        baseip = ipBox.Text;
                        sw.WriteLine("Networks:");
                        for (int i = 0; i < 50; i++)
                        {
                            if (Networks.NetworkList[i] != null)
                            {
                                sw.WriteLine(Networks.NetworkList[i]);
                            }
                        }
                        sw.WriteLine("EndNetworks");
                        sw.WriteLine(StringCipher.Encrypt(passBox.Text, encpass));
                        pass = passBox.Text;
                        sw.WriteLine(userBox.Text);
                        remoteuser = userBox.Text;

                        if (MastercheckBox.Checked)
                        {
                            sw.WriteLine("allowMaster = true");
                        }
                        else
                        {
                            sw.WriteLine("allowMaster = false");
                        }
                        if (allowDiffcheckbox.Checked)
                        {
                            sw.WriteLine("allowDiff = true");
                        }
                        else
                        {
                            sw.WriteLine("allowDiff = false");
                        }
                        sw.WriteLine("MasterPC: " + Convert.ToString(MasterPC));
                        sw.WriteLine(scriptDirBox.Text);
                        sw.WriteLine("Filter from: " + ipFromBox.Text.Trim());
                        sw.WriteLine("Filter to:" + ipToBox.Text.Trim());
                        sw.WriteLine("PC base: " + PCDirBox.Text.Trim());
                        if (!PCCheckBox.Checked)
                        {
                            fix = "false";
                        }
                        sw.WriteLine("PC base fixed: " + fix);
                        sw.WriteLine("Initial root: " + initialRootBox.Text.Trim());
                        sw.Close();
                    }
                }
                catch (System.Exception excep)
                {
                    StackTrace stackTrace = new StackTrace();
                    MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);

                }
            }
        }
        private bool Read_Configs()
        {
            string str;
            string[] str2;
            networkBox.Text = "";
            networkLabel.Text = "";

            try
            {
                using (StreamReader sw = new StreamReader(ConfigDir + "\\Network_Assessor.conf"))
                {
                    //baase ip:
                    str = sw.ReadLine();
                    try
                    {
                        str2 = str.Split(';');
                        if (str2[0] != null)
                        {
                            networkBox.Text = str2[0].Trim();
                        }
                        if (str2[1] != null)
                        {
                            networkLabel.Text = str2[1].Trim();
                        }
                    }
                    catch
                    {
                    }

                    baseip = networkBox.Text;
                    ipBox.Text = baseip;

                    do
                    {
                        str = sw.ReadLine();
                    } while (!str.StartsWith("Networks:"));
                    int k = 0;
                    while (!str.StartsWith("EndNetworks"))
                    {
                        str = sw.ReadLine();
                        if (str.StartsWith("EndNetworks"))
                        {
                            break;
                        }

                        if (str.Trim().Length > 0)
                        {
                            Networks.NetworkList[k] = str;
                            k++;
                        }

                    }

                    //PC admin password: 
                    str = sw.ReadLine();
                    str = StringCipher.Decrypt(str, encpass);
                    passBox.Text = str;
                    pass = str;
                    //server share username:
                    str = sw.ReadLine();
                    userBox.Text = str;
                    remoteuser = str;
                    //server share password:
                    str = sw.ReadLine();
                    var splitstr1 = str.Split('=');
                    if (splitstr1[1].Trim() == "true")
                    {
                        MastercheckBox.Checked = true;
                        allowMaster = true;
                    }
                    else
                    {
                        MastercheckBox.Checked = false;
                        allowMaster = false;
                    }
                    str = sw.ReadLine();
                    splitstr1 = str.Split('=');
                    if (splitstr1[1].Trim() == "true")
                    {
                        allowDiffcheckbox.Checked = true;
                    }
                    else
                    {
                        allowDiffcheckbox.Checked = false;
                    }
                    str = sw.ReadLine();
                    if (str.Contains("MasterPC:"))
                    {
                        try
                        {
                            str = str.Substring(str.IndexOf("MasterPC:") + "MasterPC:".Length, str.Length - "MasterPC:".Length);
                            str = str.Trim();
                            masterBox.Text = str;
                            MasterPC = Convert.ToInt32(str);
                        }
                        catch { }
                    }
                    str = sw.ReadLine();
                    scriptDir = str;
                    scriptDirBox.Text = str;
                    str = sw.ReadLine();
                    if (str.Contains("Filter from:"))
                    {
                        try
                        {
                            str = str.Substring(str.IndexOf("Filter from:") + "Filter from:".Length, str.Length - "Filter from:".Length);
                            str = str.Trim();
                            ipFromBox.Text = str;
                            ipfrom = Convert.ToInt32(str);
                        }
                        catch { }
                    }
                    str = sw.ReadLine();
                    if (str.Contains("Filter to:"))
                    {
                        try
                        {
                            str = str.Substring(str.IndexOf("Filter to:") + "Filter to:".Length, str.Length - "Filter to:".Length);
                            str = str.Trim();
                            ipToBox.Text = str;
                            ipto = Convert.ToInt32(str);
                        }
                        catch { }
                    }
                    str = sw.ReadLine();
                    if (str.Contains("PC base:"))
                    {
                        try
                        {
                            str = str.Substring(str.IndexOf("PC base:") + "PC base:".Length, str.Length - "PC base:".Length);
                            str = str.Trim();
                            PCDirBox.Text = str;
                            dirBox.Text = str;
                            dirBox2.Text = str;
                        }
                        catch { }
                    }
                    str = sw.ReadLine();
                    if (str.Contains("PC base fixed:"))
                    {
                        try
                        {
                            if (str.Contains("false"))
                            {
                                PCCheckBox.Checked = false;
                            }
                            else { PCCheckBox.Checked = true; }
                        }
                        catch { }
                    }
                    str = sw.ReadLine();
                    if (str.Contains("Initial root:"))
                    {
                        try
                        {
                            str = str.Substring(str.IndexOf("Initial root:") + "Initial root:".Length, str.Length - "Initial root:".Length);
                            str = str.Trim();
                            rootBox.Text = str;
                            initialRootBox.Text = str;
                            liveRootBox.Text = str;
                            rootDir = str;
                        }
                        catch { }
                    }

                    sw.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        //------------------------------------------------------------------------
        private void assessFileButton_Click(object sender, EventArgs e)
        {

            if ((radioButton1.Checked) && (Directory.Exists(localBox.Text)) && assessReady)
            {
                openFileDialog2.InitialDirectory = localBox.Text;
                radioButton3.Checked = true;
                rootBox.Text = localBox.Text;
                tabControl1.SelectTab(2);
                assessReady = false;
            }
            else
            {
                DialogResult r = MessageBox.Show("Folder transfer incomplete");
            }

        }

        /*private double MarkText() //look at tasklist and find total marks for each task
        {
            double max = 0.0;
            double total = 0.0;
            double result = 0.0;
            string maxStr = "";
            string totalStr = "";
            for (int i = 0; i <= tasktotal; i++)
            {
                if (i == 0 && !tasklist[i].taskexist) //if nothing found so no tasks
                {
                    break;
                }
                if (tasklist[i].taskexist)
                {
                    max = max + tasklist[i].taskmax;
                    total = total + tasklist[i].tasktotal;
                }
            }
            if (total > max || max == 0)
            {
                return 0.0;
            }
            else
            {
                result = Math.Round(total / max, 1) * 100;
                rawResultBox.Text = Convert.ToString(Math.Round(total, 1)) + "/" + Convert.ToString(Math.Round(max, 1));
                return result; //overall score from total score divided by max possible score
            }
        }*/

        private bool CheckSelected(int i) //is checkbox selected next to files?
        {
            bool ret = false;
            switch (i)
            {
                case 0:
                    ret = checkBox1.Checked;
                    break;
                case 1:
                    ret = checkBox2.Checked;
                    break;
                case 2:
                    ret = checkBox3.Checked;
                    break;
                case 3:
                    ret = checkBox4.Checked;
                    break;
                case 4:
                    ret = checkBox5.Checked;
                    break;
                case 5:
                    ret = checkBox6.Checked;
                    break;
                default:
                    ret = false;
                    break;
            }
            return ret;
        }
        private bool checkFile(int i)
        {
            switch (i)
            {
                case 0:
                    if (checkBox1.Checked)
                        return true;
                    break;
                case 1:
                    if (checkBox2.Checked)
                        return true;
                    break;
                case 2:
                    if (checkBox3.Checked)
                        return true;
                    break;
                case 3:
                    if (checkBox4.Checked)
                        return true;
                    break;
                case 4:
                    if (checkBox5.Checked)
                        return true;
                    break;
                case 5:
                    if (checkBox6.Checked)
                        return true;
                    break;
                default:
                    return false;
            }
            return false;

        }

        private void assessFile(int nnn, int PCno, string PCDir) //assess files against templates in the text boxes
        {
            double result = 0.0;
            string score = "";
            string str = "";
            int m = 0;
            bool errors = false;
            bool ap = false;
            int ret = 0;
            double totalResult = 0.0;
            int numfiles = 0;
            double totalrawResult = 0.0;
            double totalrawMax = 0.0;
            double overallrawResult = 0.0;
            string resultstr = "";

            string[] splitstr = new string[2];
            string s = Convert.ToString(PCno);
            if (PCno == -1) { s = "n/a"; }

            if (defaultCheckBox.Checked) //is a default mark selected?
            {

                try
                {
                    m = Convert.ToInt32(defaultMBox.Text);
                }
                catch
                {
                    DialogResult r = MessageBox.Show("Invalid default mark");
                    return;
                }
            }

            try
            {
                using (StreamWriter outp = new StreamWriter(parser.fout, ap))
                {

                    outp.WriteLine("PC: " + s);
                    outp.WriteLine("Marks for assessment: " + assessTitleBox.Text);
                    outp.Close();
                }
            }
            catch { }
            try
            {
                using (StreamWriter comment = new StreamWriter(parser.fcomment, ap))
                {
                    comment.WriteLine("PC: " + s);
                    comment.WriteLine("Feedback for assessment: " + assessTitleBox.Text);
                    comment.Close();
                    ap = true;
                }
            }
            catch { }

            for (int i = 0; i < MaxFiles; i++) //for all files in the list
            {

                if (CheckSelected(i)) //only process selected files
                {
                    //if (File.Exists(parser.ftemplate[i]) && File.Exists(parser.infile[i]) && parser.fout.Trim() != "" && parser.fcomment.Trim() != "")
                    if (File.Exists(parser.ftemplate[i]) && parser.fout.Trim() != "" && parser.fcomment.Trim() != "")
                    {
                        try
                        {
                            ret = parser.Parse3(i, ap, defaultCheckBox.Checked); //call the standard assessment method from the parser class
                        }
                        catch { }
                        if (ret > -1)
                        {
                            ap = true;
                            result = parser.MarkText(); //use parser class to calculate marks for input file from this PC - result is %
                            totalResult = totalResult + result;
                            numfiles++;
                            str = parser.rawResult; //show results in box
                            rawResultBox.Text = str; //rawresult of form xx/xx
                            splitstr = str.Split('/');
                            try
                            {
                                totalrawMax = totalrawMax + Convert.ToDouble(splitstr[1]);  //total cumulative lines marked
                                totalrawResult = totalrawResult + Convert.ToDouble(splitstr[0]); //total cumulative lines correct
                            }
                            catch { }


                            score = Convert.ToString(result); //%


                            try
                            {
                                using (StreamWriter outp = new StreamWriter(parser.fout, true))
                                {
                                    outp.WriteLine("Lines correct: " + str);   //write results to the output file
                                    outp.WriteLine("Percentage: " + score);
                                    outp.Close();
                                }
                            }
                            catch { }
                            try
                            {
                                using (StreamWriter comment = new StreamWriter(parser.fcomment, true))
                                {
                                    comment.WriteLine("Lines correct: " + str); //write comnents to comments file
                                    comment.WriteLine("Percentage: " + score);
                                    comment.Close();

                                }
                            }
                            catch { }
                        }
                        else if (ret == -1)
                        {
                            MessageBox.Show("Template file not found");
                        }
                        else
                        {
                            MessageBox.Show("PC number out of range");
                        }

                    }
                    else
                    {
                        errors = true;
                    }
                }
            } // i 0 to <5

            totalResult = totalResult / numfiles; //average percentage of all files
            overallrawResult = Math.Round((totalrawResult / totalrawMax) * 100, 1);  //average percentage of all tasks


            if (resultselectcheckBox.Checked)
            {
                resultstr = Convert.ToString(totalResult);
            }
            else
            {
                resultstr = Convert.ToString(overallrawResult);
            }
            resultBox.Text = resultstr; //put result in % into resultbox
            rawResultBox.Text = totalrawResult.ToString() + "/" + totalrawMax.ToString(); //write total marks from tasks to rawresultbox
            if (oneDir > 0) //if more than one dir
            {
                rawresultPCs[nnn] = rawResultBox.Text; //put raw result as a/b into array
                resultPCs[nnn] = Convert.ToDouble(resultstr); //put result % in an array so that they can be recalled
                rPCs[nnn] = PCDir;    //including raw result
            }

            try
            {
                using (StreamWriter outp = new StreamWriter(parser.fout, true))
                {
                    outp.WriteLine("Overall lines: " + rawResultBox.Text); //write lines correct out of total
                    outp.WriteLine("Overall Result %: " + resultstr);   //write overall results to the output file                      
                    outp.Close();
                }
            }
            catch { }
            try
            {
                using (StreamWriter comment = new StreamWriter(parser.fcomment, true))
                {
                    comment.WriteLine("Overall lines: " + rawResultBox.Text); //write lines correct out of total
                    comment.WriteLine("Overall Result %: " + resultstr); //write overall result to comments file
                    comment.Close();
                }
            }
            catch { }


            if (errors)
            {
                MessageBox.Show("Not all files processed - check filenames");
            }
            else
            {
                MessageBox.Show("Files processed");
            }
        }

        private bool goFiles(string path2, string path1, int n, int PCno, string PCDir)
        {   //path1 is origianl root path - template file is here, path2 is correct path where assesment files and outpur/comment and output files to go
            string file1 = "";
            /*for (int n = 0; n < noPCs; n++)
            {*/
            try
            {
                if (PClistBox.GetSelected(n)) //n = position in listbox from 0 (not PC starting 1)
                {
                    for (int i = 0; i < MaxFiles; i++)
                    {
                        file1 = Path.GetFileName(parser.ftemplate[i]); //template file name - note this is in the original directory!
                        //parser.ftemplate[i] = path1 + "\\" + file1; //template in root directory

                        file1 = Path.GetFileName(parser.infile[i]); //file name of input file to be assessed - this is in the PCs directory (or original if no PCs selected)
                        parser.infile[i] = path2 + "\\" + dirBox2.Text.Trim() + Convert.ToString(PCno) + "\\" + file1; //with its full path, inc PC1 etc
                                                                                                                       //take criteria from boxes
                    }
                    file1 = Path.GetFileName(parser.fout); //file name of output file for results
                    parser.fout = path2 + "\\" + dirBox2.Text.Trim() + Convert.ToString(PCno) + "\\" + file1; //with its full path, inc PC1 etc

                    file1 = Path.GetFileName(parser.fcomment); //file name of comments file
                    parser.fcomment = path2 + "\\" + dirBox2.Text.Trim() + Convert.ToString(PCno) + "\\" + file1; //with ts full path


                    assessFile(n, PCno, PCDir); //asses the files for PCn
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                MessageBox.Show("Error");
                return false;
            }
            //}
        }

        private void setCriteria()
        {
            string str = "1";
            for (int i = 0; i < MaxFiles; i++)
            {
                if (i > 5)
                {
                    return;
                }
                switch (i)
                {
                    case 0:
                        str = criteriaBox1.Text;
                        break;
                    case 1:
                        str = criteriaBox2.Text;
                        break;
                    case 2:
                        str = criteriaBox3.Text;
                        break;
                    case 3:
                        str = criteriaBox4.Text;
                        break;
                    case 4:
                        str = criteriaBox5.Text;
                        break;
                    case 5:
                        str = criteriaBox6.Text;
                        break;
                }
                if (str.Trim() == "")
                {
                    str = "1";
                }
                parser.Criteria[i] = str;
            }
        }
        private bool assessPaths() //for each PC set up the path to the corresponding folder for files (eg. each will be in PC1, PC2 etc.)
        {
            string origrootpath = "";
            string correctrootpath = "";
            string path1up = "";

            string PCDir = "";
            int pos1 = 0;
            int len1 = 0;
            int no = 0;
            bool ok = false;
            int offset = 1;
            int rangemax = 0;


            bool allex = true;
            bool pcSel = false;
            if (IPrangeCheckBox.Checked)
            {
                offset = ipfrom;
                rangemax = ipto;
            }
            else
            {
                offset = 1;
                rangemax = noPCs;
            }
            //origrootpath = Directory.GetParent(Path.GetFullPath(parser.ftemplate[0])).FullName; //parent directory
            origrootpath = rootDir;

            PCDir = dirBox2.Text.Trim();
            if (windows)
            {
                PCDir = PCDir.ToUpper();
                origrootpath = origrootpath.ToUpper();
            }

            pos1 = origrootpath.LastIndexOf(PCDir); //may have PCx in it!
            len1 = dirBox2.Text.Trim().Length;
            if (pos1 > -1)
            {
                try
                {
                    correctrootpath = origrootpath.Substring(0, pos1); //remove the PC# from path

                    Show_Label("Removing " + Directory.GetParent(Path.GetFullPath(parser.ftemplate[0])).Name + " from directory to form root path");
                }
                catch { }
            }
            else
            {
                correctrootpath = origrootpath;
            }
            if (windows)
            {
                correctrootpath = correctrootpath.ToUpper();
            }
            for (int n = 0; n <= rangemax - offset; n++)
            {
                try
                {

                    if (PClistBox.GetSelected(n)) //if PC selected with offset
                    {
                        pcSel = true;
                        PCDir = dirBox2.Text.Trim() + Convert.ToString(n + offset); //eg. PC1
                        //path1 = Directory.GetParent(Path.GetFullPath(parser.ftemplate[0])).FullName; //parent directory
                        if (windows)
                        {
                            PCDir = PCDir.ToUpper();
                        }

                        //if (origrootpath.LastIndexOf(PCDir) > 0)
                        //{
                        if (Directory.Exists(correctrootpath + "\\" + dirBox2.Text.Trim() + Convert.ToString(n + offset)))
                        {
                            no = n;
                            if (goFiles(correctrootpath, origrootpath, n, n + offset, PCDir)) //now start assessing PCn - path2 root path, n = listbox entry for PC., PCDir - PC1 etc
                            {
                                ok = true;
                            }
                        }
                        else
                        {
                            allex = false;
                        }

                        //}
                        //else if (!windows && (path1up.LastIndexOf(PCDir.ToUpper()) > 0))
                        //{
                        //    Show_Label("Directory " + PCDir + " filename case doesn't match");
                        //}
                    }
                }
                catch { allex = false; }
            }
            if (!allex)
            {
                if (ok)
                {
                    MessageBox.Show("Some files processed");
                }
                else
                {
                    MessageBox.Show("No files processed");
                }
                ok = false;
            }
            if (!pcSel)
            {
                MessageBox.Show("No PCs selected to assess");
                ok = false;
            }
            if (ok)
            {
                return true;
            }
            else
            { return false; }
        }

        private void setPaths()
        {
            parser.infile[0] = markBox1.Text;
            parser.infile[1] = markBox2.Text;
            parser.infile[2] = markBox3.Text;
            parser.infile[3] = markBox4.Text;
            parser.infile[4] = markBox5.Text;
            parser.infile[5] = markBox6.Text;
            parser.ftemplate[0] = tempBox1.Text;
            parser.ftemplate[1] = tempBox2.Text;
            parser.ftemplate[2] = tempBox3.Text;
            parser.ftemplate[3] = tempBox4.Text;
            parser.ftemplate[4] = tempBox5.Text;
            parser.ftemplate[5] = tempBox6.Text;
            parser.fout = outBox.Text;
            parser.fcomment = commentBox.Text;
        }
        private bool checkPaths() //check to see if all root paths are the same for all files in the boxes
        {
            string path1 = "";
            string path2 = "";
            string path3 = "";
            string path4 = "";
            int number = 5;
            bool f = true;

            for (int i = 0; i < number; i++)
            {
                if (CheckSelected(i)) //is selected file box checked?
                {
                    try
                    {
                        switch (i)
                        {
                            case 0:
                                {
                                    path1 = Directory.GetParent(Path.GetFullPath(markBox1.Text)).FullName;
                                    path2 = Directory.GetParent(Path.GetFullPath(tempBox1.Text)).FullName;
                                    break;
                                }
                            case 1:
                                {
                                    path1 = Directory.GetParent(Path.GetFullPath(markBox2.Text)).FullName;
                                    path2 = Directory.GetParent(Path.GetFullPath(tempBox2.Text)).FullName;
                                    break;
                                }
                            case 2:
                                {
                                    path1 = Directory.GetParent(Path.GetFullPath(markBox3.Text)).FullName;
                                    path2 = Directory.GetParent(Path.GetFullPath(tempBox3.Text)).FullName;
                                    break;
                                }
                            case 3:
                                {
                                    path1 = Directory.GetParent(Path.GetFullPath(markBox4.Text)).FullName;
                                    path2 = Directory.GetParent(Path.GetFullPath(tempBox4.Text)).FullName;
                                    break;
                                }
                            case 4:
                                {
                                    path1 = Directory.GetParent(Path.GetFullPath(markBox5.Text)).FullName;
                                    path2 = Directory.GetParent(Path.GetFullPath(tempBox5.Text)).FullName;
                                    break;
                                }
                            case 5:
                                {
                                    path1 = Directory.GetParent(Path.GetFullPath(markBox6.Text)).FullName;
                                    path2 = Directory.GetParent(Path.GetFullPath(tempBox6.Text)).FullName;
                                    break;
                                }
                            default:
                                break;
                        }

                    }
                    catch
                    {
                        path1 = "";
                        path2 = "";
                        f = false;
                    }
                }
                try
                {
                    path3 = Directory.GetParent(Path.GetFullPath(outBox.Text)).FullName;
                    path4 = Directory.GetParent(Path.GetFullPath(commentBox.Text)).FullName;
                }
                catch
                {
                    path3 = "";
                    path4 = "";
                    f = false;
                }
            }
            if (!f)
            {
                MessageBox.Show("Some filenames not valid");
                return false;
            }
            else if (path2.Trim() != path1.Trim() || path2.Trim() != path3.Trim())
            {
                if (allowDiffcheckbox.Checked)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("All  master files must all be in the same folder!");
                    return false;
                }
            }
            else if (path1.Trim() != path4.Trim())
            {
                MessageBox.Show("All  master files must all be in the same folder!");
                return false;
            }
            else { return true; }
        }

        private void assessButton_Click(object sender, EventArgs e)
        {
            bool ok = true;
            //do all folder have the same root path?
            setCriteria(); //extract criteria from boxes
            if (allDirsCheckbox.Checked)
            {
                ok = checkPaths(); //check to see if all base files initially in same folder (eg. template and input/outpur files
                if (ok)
                {
                    setPaths(); //take contents of text boxes and put into files
                    if (assessPaths()) //assess files in folder for each selected PC
                    {
                        MessageBox.Show("All files processed");
                    }
                    return;
                }
            }
            else
            {
                ok = checkPaths(); //if applying to only one folder
                if (ok)
                {
                    setPaths();
                    assessFile(oneDir, oneDir, ""); //onedir = -1 (eg. one file only not from listbox)
                }
            }
        }


        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            parser.infile[selectedFile - 1] = openFileDialog2.FileName; //
            switch (selectedFile)
            {
                case 1:
                    markBox1.Text = parser.infile[0];
                    break;
                case 2:
                    markBox2.Text = parser.infile[1];
                    break;
                case 3:
                    markBox3.Text = parser.infile[2];
                    break;
                case 4:
                    markBox4.Text = parser.infile[3];
                    break;
                case 5:
                    markBox5.Text = parser.infile[4];
                    break;
            }
        }


        /*private void tempfileDialog(int n)
        {
            openFileDialog1.DefaultExt = "";
            openFileDialog1.Filter = "";
            openFileDialog1.FileName = "";
            if (n > files)
            {
                files = n;
            }
            openFileDialog1.ShowDialog();
        }*/

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

            parser.ftemplate[selectedFile - 1] = openFileDialog1.FileName;
            switch (selectedFile)
            {
                case 1:
                    tempBox1.Text = parser.ftemplate[0];
                    break;
                case 2:
                    tempBox2.Text = parser.ftemplate[1];
                    break;
                case 3:
                    tempBox3.Text = parser.ftemplate[2];
                    break;
                case 4:
                    tempBox4.Text = parser.ftemplate[3];
                    break;
                case 5:
                    tempBox5.Text = parser.ftemplate[4];
                    break;
            }
            //ftemplate = openFileDialog1.FileName;
            //tempBox1.Text = ftemplate;
        }

        private void infileDialog(int n)
        {
            openFileDialog2.InitialDirectory = rootDir;
            openFileDialog2.DefaultExt = "";
            openFileDialog2.Filter = "";
            openFileDialog2.FileName = "";
            //if (n > selectedFile)
            //{
            selectedFile = n;   //file currently selected - global variable
            //}
            openFileDialog2.ShowDialog();
        }

        private bool validRoot()
        {
            bool ret = false;
            try
            {
                if (Directory.Exists(rootBox.Text.Trim()))
                {
                    ret = true;
                }
            }
            catch
            {
                ret = false;
            }
            return ret;
        }
        private void markDialog(int n)
        {
            bool ck = false;
            if (validRoot())
            {
                switch (n)
                {
                    case 1: ck = checkBox1.Checked;
                        break;
                    case 2:
                        ck = checkBox2.Checked;
                        break;
                    case 3:
                        ck = checkBox3.Checked;
                        break;
                    case 4:
                        ck = checkBox4.Checked;
                        break;
                    case 5:
                        ck = checkBox5.Checked;
                        break;
                    case 6:
                        ck = checkBox6.Checked;
                        break;
                    default:
                        ck = false;
                        break;
                }
                if (ck)
                { infileDialog(n); }
                else { Show_Label("Need to select the checkbox"); };
            }
            else
            {
                MessageBox.Show("Invalid root path");
            }
        }
        private void markButton1_Click(object sender, EventArgs e)
        {
            markDialog(1);
        }

        private void markButton2_Click(object sender, EventArgs e)
        {
            markDialog(2);
        }

        private void markButton3_Click(object sender, EventArgs e)
        {
            markDialog(3);
        }

        private void markButton4_Click(object sender, EventArgs e)
        {
            markDialog(4);
        }

        private void markButton5_Click(object sender, EventArgs e)
        {
            markDialog(5);
        }



        private void outButton_Click(object sender, EventArgs e)
        {

            if (validRoot())
            {
                saveFileDialog1.InitialDirectory = rootDir;
                saveFileDialog1.DefaultExt = "";
                saveFileDialog1.Filter = "";
                saveFileDialog1.FileName = "";
                saveFileDialog1.ShowDialog();
            }
        }

        private void commentFile_Click(object sender, EventArgs e)
        {

            if (validRoot())
            {
                saveFileDialog1.InitialDirectory = rootDir;
                saveFileDialog2.DefaultExt = "";
                saveFileDialog2.Filter = "";
                saveFileDialog2.FileName = "";
                saveFileDialog2.ShowDialog();
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            parser.fout = saveFileDialog1.FileName;
            outBox.Text = parser.fout;
        }

        private void saveFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            parser.fcomment = saveFileDialog2.FileName;
            commentBox.Text = parser.fcomment;
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void selectAllPCButton_Click(object sender, EventArgs e)
        {
            //select all or clear all PCs from listbox
            if (selectAllPCButton.Text == "Select All PC")
            {
                selectAllPCButton.Text = "Clear All PC";
                for (int i = 0; i < PClistBox.Items.Count; i++)
                {

                    if (connPCs[i] == 1)
                    {
                        if (i == 26 && !(allowMaster))
                        {
                            PClistBox.SetSelected(i - 1, false);

                        }
                        else if (i == maxPCs && !(allowMaster))
                        {
                            PClistBox.SetSelected(i - 1, false);
                        }
                        else
                        {
                            PClistBox.SetSelected(i - 1, true);
                        }
                    }

                }

            }
            else
            {
                selectAllPCButton.Text = "Select All PC";
                for (int i = 0; i < PClistBox.Items.Count; i++)
                {
                    PClistBox.SetSelected(i, false);

                }
            }
        }

        private void PClistBox_Click(object sender, EventArgs e)
        {
            int f;
            int t;
            int inc = 0;
            if (IPrangeCheckBox.Checked)
            {
                f = ipfrom - 1;
                t = ipto - 1;
            }
            else
            {
                f = 1;
                t = noPCs;
            }
            //if a list box item is selected update the list, but only allow those PCs connected to be selected
            for (int i = f; i < t + 1; i++)
            {

                try
                {
                    if (connPCs[i] < 1 && !allowSelect) //if not connected - ie. connPCs =0
                    {
                        PClistBox.SetSelected(inc, false);
                    }
                    if ((PClistBox.SelectedIndex + f) == MasterPC && !(allowMaster))
                    {
                        PClistBox.SetSelected(PClistBox.SelectedIndex, false);
                        Show_Label("Master PC cannot be selected");
                    }
                    inc++;
                }
                catch { }

            }
        }

        private void editNetButton_Click(object sender, EventArgs e)
        {
            string[] str;

            //edit selected networks
            try
            {
                Networks.SelectedIP = networkBox.Text;
                Networks.NetSelected = selectedNet;
                Networks.selectNet = false;

                Networks.ShowDialog();
                int index = Networks.NetSelected;
                if (index > -1)
                {
                    str = Networks.NetworkList[index].Split(';');
                    if (str[0] != null)
                    {
                        ipBox.Text = str[0].Trim();
                    }
                    else
                    {
                        ipBox.Text = "";
                    }
                    baseip = ipBox.Text;
                    networkBox.Text = baseip;

                    if (str[1] != null)
                    {
                        networkLabel.Text = str[1];
                    }
                    else
                    {
                        networkLabel.Text = "";
                    }
                    if (selectedNet != index)
                    {
                        Parallel.Invoke(() => Show_Label("Please wait, building new PC list"), () => findPCsParallel(connectableCheckBox.Checked));
                        PClistBox.Items.Clear();
                        Populate_ListBox();
                    }
                    selectedNet = index;
                }
            }
            catch
            {
            }
        }

        private void checkConnButton_Click(object sender, EventArgs e)
        {
            bool result;
            //check connectivity now
            Parallel.Invoke(() => Show_Label("Please wait, testing connectivity"), () => findPCsParallel(connectableCheckBox.Checked));
            result = Modify_ListBox();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void handleScriptDir()
        {
            DialogResult dlgResult = folderBrowserDialog2.ShowDialog();
            if (dlgResult.Equals(DialogResult.OK))
            {
                //Show selected folder path in textbox.
                scriptDirBox.Text = folderBrowserDialog2.SelectedPath + "\\";
                scriptDir = scriptDirBox.Text;
                try
                {
                    if (!Directory.Exists(scriptDir))
                    {
                        DialogResult res = MessageBox.Show("Directory doesn't exist - create it (yes/no)?", "Script directory", MessageBoxButtons.YesNo);
                        if (res == DialogResult.Yes)
                        {
                            Directory.CreateDirectory(scriptDir);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid directory name or cannot create it");
                }
            }
        }
        private void scriptDirButton_Click(object sender, EventArgs e)
        {
            handleScriptDir();
        }

        private string buildChecked()
        {
            char[] t = new char[10];

            if (checkBox1.Checked)
            {
                t[0] = '1';
            }
            if (checkBox2.Checked)
            {
                t[1] = '1';
            }
            if (checkBox3.Checked)
            {
                t[2] = '1';
            }
            if (checkBox4.Checked)
            {
                t[3] = '1';
            }
            if (checkBox5.Checked)
            {
                t[4] = '1';
            }
            if (checkBox6.Checked)
            {
                t[5] = '1';
            }
            return String.Concat(t);
        }

        private void SaveAssessFile(string filename)
        {
            string str = "";
            // write unit to file:
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    sw.WriteLine("Assess Title: " + assessTitleBox.Text);
                    sw.WriteLine("Mark file 1: " + markBox1.Text);
                    sw.WriteLine("Mark file 2: " + markBox2.Text);
                    sw.WriteLine("Mark file 3: " + markBox3.Text);
                    sw.WriteLine("Mark file 4: " + markBox4.Text);
                    sw.WriteLine("Mark file 5: " + markBox5.Text);
                    sw.WriteLine("Mark file 6: " + markBox6.Text);
                    sw.WriteLine("Mark file 7: ");
                    sw.WriteLine("Mark file 8: ");
                    sw.WriteLine("Mark file 9: ");
                    sw.WriteLine("Mark file 10: ");

                    sw.WriteLine("Template file 1: " + tempBox1.Text);
                    sw.WriteLine("Template file 2: " + tempBox2.Text);
                    sw.WriteLine("Template file 3: " + tempBox3.Text);
                    sw.WriteLine("Template file 4: " + tempBox4.Text);
                    sw.WriteLine("Template file 5: " + tempBox5.Text);
                    sw.WriteLine("Template file 6: " + tempBox6.Text);
                    sw.WriteLine("Template file 7: ");
                    sw.WriteLine("Template file 8: ");
                    sw.WriteLine("Template file 9: ");
                    sw.WriteLine("Template file 10: ");

                    str = buildChecked();
                    sw.WriteLine("Files selected: " + str);

                    sw.WriteLine("Output file: " + outBox.Text);
                    sw.WriteLine("Comments file: " + commentBox.Text);
                    sw.WriteLine("Root folder: " + rootBox.Text);
                    string t = "false";
                    if (allDirsCheckbox.Checked)
                    { t = "true"; }
                    sw.WriteLine("Apply to all: " + t);
                    sw.Close();
                    AssessFile = filename;
                    AssessFilePath = Path.GetDirectoryName(filename);
                }
            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);
            }
        }

        private void AssessSaveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            //save unit information           
            SaveAssessFile(AssessSaveFileDialog.FileName);
            //Copy_Form_Data();
            //unitButtons(false);
        }

        private void saveAssessButton_Click(object sender, EventArgs e)
        {
            //save units
            if (assessTitleBox.Text != null && assessTitleBox.Text.Length > 0)
            {
                if (Directory.Exists(AssessFilePath))
                {
                    AssessSaveFileDialog.InitialDirectory = AssessFilePath;
                }
                else
                {
                    MessageBox.Show("Select a directory for new assessment");
                    folderBrowserDialog1.SelectedPath = DefaultDir;
                    folderBrowserDialog1.ShowDialog();
                    AssessFilePath = folderBrowserDialog1.SelectedPath;
                }
                AssessSaveFileDialog.InitialDirectory = AssessFilePath;
                AssessSaveFileDialog.FileName = assessTitleBox.Text.Trim() + ".uni";
                AssessSaveFileDialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("Unit title is empty");
            }
        }

        private void loadAssessButton_Click(object sender, EventArgs e)
        {
            openAssessFileDialog.FileName = "";
            openAssessFileDialog.InitialDirectory = DefaultDir;
            DialogResult dialogResult = MessageBox.Show("Load assessment - this will clear all form data - Yes/No?", "Load Assessment", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                openAssessFileDialog.ShowDialog();
            }
        }

        private void readChecked(string str)
        {
            //checkBox1.Checked = false; //checkbox should alwsys be selected
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;

            if (str[0] == '1')
            {
                checkBox1.Checked = true;
            }
            if (str[1] == '1')
            {
                checkBox2.Checked = true;
            }
            if (str[2] == '1')
            {
                checkBox3.Checked = true;
            }
            if (str[3] == '1')
            {
                checkBox4.Checked = true;
            }
            if (str[4] == '1')
            {
                checkBox5.Checked = true;
            }
            if (str[5] == '1')
            {
                checkBox6.Checked = true;
            }

        }
        private void LoadAssessFile(string filename)
        {
            string[] parts = new string[3];
            string str = "";
            int i = 0;
            int k = 0;
            try
            {
                AssessFile = filename;
                AssessFilePath = Path.GetDirectoryName(filename);
                // Create an instance of StreamWriter to read grades from file:
                using (StreamReader sw = new StreamReader(filename))
                {
                    while (!sw.EndOfStream)
                    {
                        str = sw.ReadLine();
                        if (str.StartsWith("Assess Title: "))
                        {
                            i = str.IndexOf("Assess Title: ");
                            k = "Assess Title: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            assessTitleBox.Text = str;

                        }

                        else if (str.StartsWith("Mark file 1: "))
                        {
                            i = str.IndexOf("Mark file 1: ");
                            k = "Mark file 1: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            markBox1.Text = str;
                            parser.infile[0] = str;
                        }
                        else if (str.StartsWith("Mark file 2: "))
                        {
                            i = str.IndexOf("Mark file 2: ");
                            k = "Mark file 2: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            markBox2.Text = str;
                            parser.infile[1] = str;
                        }
                        else if (str.StartsWith("Mark file 3: "))
                        {
                            i = str.IndexOf("Mark file 3: ");
                            k = "Mark file 3: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            markBox3.Text = str;
                            parser.infile[2] = str;
                        }
                        else if (str.StartsWith("Mark file 4: "))
                        {
                            i = str.IndexOf("Mark file 4: ");
                            k = "Mark file 4: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            markBox4.Text = str;
                            parser.infile[3] = str;
                        }
                        else if (str.StartsWith("Mark file 5: "))
                        {
                            i = str.IndexOf("Mark file 5: ");
                            k = "Mark file 5: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            markBox5.Text = str;
                            parser.infile[4] = str;
                        }


                        else if (str.StartsWith("Template file 1: "))
                        {
                            i = str.IndexOf("Template file 1: ");
                            k = "Template file 1: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            tempBox1.Text = str;
                            parser.ftemplate[0] = str;
                        }
                        else if (str.StartsWith("Template file 2: "))
                        {
                            i = str.IndexOf("Template file 2: ");
                            k = "Template file 2: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            tempBox2.Text = str;
                            parser.ftemplate[1] = str;
                        }
                        else if (str.StartsWith("Template file 3: "))
                        {
                            i = str.IndexOf("Template file 3: ");
                            k = "Template file 3: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            tempBox3.Text = str;
                            parser.ftemplate[2] = str;
                        }
                        else if (str.StartsWith("Template file 4: "))
                        {
                            i = str.IndexOf("Template file 4: ");
                            k = "Template file 4: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            tempBox4.Text = str;
                            parser.ftemplate[3] = str;
                        }
                        else if (str.StartsWith("Template file 5: "))
                        {
                            i = str.IndexOf("Template file 5: ");
                            k = "Template file 5: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            tempBox5.Text = str;
                            parser.ftemplate[4] = str;
                        }

                        else if (str.StartsWith("Output file: "))
                        {
                            i = str.IndexOf("Output file: ");
                            k = "Output file: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            outBox.Text = str;
                            parser.fout = str;
                        }
                        else if (str.StartsWith("Comments file: "))
                        {
                            i = str.IndexOf("Comments file: ");
                            k = "Comments file: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            commentBox.Text = str;
                            parser.fcomment = str;
                        }
                        else if (str.StartsWith("Root folder: "))
                        {
                            i = str.IndexOf("Root folder: ");
                            k = "Root folder: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            rootBox.Text = str;
                            rootDir = str;
                        }
                        else if (str.StartsWith("Files selected: "))
                        {
                            i = str.IndexOf("Files selected: ");
                            k = "Files selected: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            readChecked(str);
                        }
                        else if (str.StartsWith("Apply to all: "))
                        {
                            i = str.IndexOf("Apply to all: ");
                            k = "Apply to all: ".Length;
                            str = str.Substring(i + k, str.Length - k);
                            if (str.Contains("true"))
                            {
                                allDirsCheckbox.Checked = true;
                            }
                            else
                            {
                                allDirsCheckbox.Checked = false;
                            }

                        }
                    }
                    sw.Close();
                }
            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);
            }
        }

        private void Clear_Assess_Form()
        {
            markBox1.Text = "";
            markBox2.Text = "";
            markBox3.Text = "";
            markBox4.Text = "";
            markBox5.Text = "";
            tempBox1.Text = "";
            tempBox2.Text = "";
            tempBox3.Text = "";
            tempBox4.Text = "";
            tempBox5.Text = "";
            outBox.Text = "";
            commentBox.Text = "";
        }
        private void openAssessFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            Clear_Assess_Form();
            LoadAssessFile(openAssessFileDialog.FileName);
            //unitButtons(false);
            //unitEditbutton.Visible = true;

            //unitFoldertextBox.Text = AssessFilePath;
        }

        private void tabPage5_Click(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            assessFileButton.Visible = false;

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            assessFileButton.Visible = false;
        }



        private void ipFromBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void ipToBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void IPrangeCheckBox_Click(object sender, EventArgs e)
        {
            if (IPrangeCheckBox.Checked)
            {
                try
                {
                    ipfrom = Convert.ToInt32(ipFromBox.Text);
                    ipto = Convert.ToInt32(ipToBox.Text);
                    if (ipfrom > ipto)
                    {
                        ipfrom = 1;
                        ipto = noPCs;
                        var res = MessageBox.Show("From value cannot be greater than to value");
                        IPrangeCheckBox.Checked = false;
                        return;
                    }
                    if ((ipfrom > 254) || (ipto > 254) || (ipfrom == 0) || (ipto == 0))
                    {
                        ipfrom = 1;
                        ipto = noPCs;
                        var res = MessageBox.Show("Invalid range - cannot be 0 or > 254");
                        IPrangeCheckBox.Checked = false;
                        return;
                    }
                    PClistBox.Items.Clear();
                    Populate_ListBox();
                    var res1 = MessageBox.Show("Filter applied");
                }
                catch
                {
                    ipfrom = 1;
                    ipto = noPCs;
                    var res = MessageBox.Show("Invalid filter values");
                    IPrangeCheckBox.Checked = false;
                }
            }
            if (!IPrangeCheckBox.Checked)
            {
                ipfrom = 1;
                ipto = noPCs;
                PClistBox.Items.Clear();
                Populate_ListBox();
                var res = MessageBox.Show("Filter cleared");
            }
        }

        private void MastercheckBox_Click(object sender, EventArgs e)
        {
            if (MastercheckBox.Checked)
            {
                allowMaster = true;
            }
            else { allowMaster = false; }
        }

        private void masterBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void masterBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                MasterPC = Convert.ToInt32(masterBox.Text);

                if ((MasterPC > 254) || (MasterPC == 0))
                {
                    MasterPC = 26;
                    var res = MessageBox.Show("Invalid range - cannot be 0 or > 254");
                    return;
                }
            }
            catch
            {
                MasterPC = 26;

                var res = MessageBox.Show("Invalid value");
                IPrangeCheckBox.Checked = false;
            }
        }

        private void defaultMBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }



        private void dirBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void allDirsCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            dirBox2.Visible = allDirsCheckbox.Checked;
            dirLabel2.Visible = allDirsCheckbox.Checked;
            dirBox2.Text = dirBox.Text;
        }

        private void markBox1_TextChanged(object sender, EventArgs e)
        {
            parser.infile[0] = markBox1.Text;
        }

        private void markBox2_TextChanged(object sender, EventArgs e)
        {
            parser.infile[1] = markBox2.Text;
        }

        private void markBox3_TextChanged(object sender, EventArgs e)
        {
            parser.infile[2] = markBox3.Text;
        }

        private void markBox4_TextChanged(object sender, EventArgs e)
        {
            parser.infile[3] = markBox4.Text;
        }

        private void markBox5_TextChanged(object sender, EventArgs e)
        {
            parser.infile[4] = markBox5.Text;
        }

        private void tempBox1_TextChanged(object sender, EventArgs e)
        {
            parser.ftemplate[0] = tempBox1.Text;
        }

        private void tempBox2_TextChanged(object sender, EventArgs e)
        {
            parser.ftemplate[0] = tempBox1.Text;
        }

        private void tempBox3_TextChanged(object sender, EventArgs e)
        {
            parser.ftemplate[0] = tempBox1.Text;
        }

        private void tempBox4_TextChanged(object sender, EventArgs e)
        {
            parser.ftemplate[0] = tempBox1.Text;
        }

        private void tempBox5_TextChanged(object sender, EventArgs e)
        {
            parser.ftemplate[0] = tempBox1.Text;
        }

        private void outBox_TextChanged(object sender, EventArgs e)
        {
            parser.fout = outBox.Text;
        }

        private void commentBox_TextChanged(object sender, EventArgs e)
        {
            parser.fcomment = commentBox.Text;
        }

        private bool testCheck(int n) //is this template selected
        { bool res = false;
            switch (n)
            {
                case 1:
                    res = checkBox1.Checked;
                    break;
                case 2:
                    res = checkBox2.Checked;
                    break;
                case 3:
                    res = checkBox3.Checked;
                    break;
                case 4:
                    res = checkBox4.Checked;
                    break;
                case 5:
                    res = checkBox5.Checked;
                    break;
                case 6:
                    res = checkBox6.Checked;
                    break;
                default:
                    break;
            }
            return res;
        }
        private void tempDialog(int n)
        {
            selectedFile = n;   //file currently selected global variable
            if (validRoot())
            {
                if (testCheck(n))
                {
                    openFileDialog1.InitialDirectory = rootDir;
                    openFileDialog1.DefaultExt = "";
                    openFileDialog1.Filter = "";
                    openFileDialog1.FileName = "";

                    openFileDialog1.ShowDialog();

                }
                else { Show_Label("Need to select the checkbox"); };
            }
            else
            {
                MessageBox.Show("Invalid root path");
            }
        }
        private void tempButton1_Click(object sender, EventArgs e)
        {
            tempDialog(1);
        }
        private void tempButton2_Click(object sender, EventArgs e)
        {
            tempDialog(2);
        }

        private void tempButton3_Click(object sender, EventArgs e)
        {
            tempDialog(3);
        }

        private void tempButton4_Click(object sender, EventArgs e)
        {
            tempDialog(4);
        }

        private void tempButton5_Click(object sender, EventArgs e)
        {
            tempDialog(5);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                allowSelect = false;
            }
            else
            {
                allowSelect = true;
            }
            singleCheckBox.Checked = (tabControl1.SelectedIndex == 4);
        }

        private int findPC(string str)
        {

            int n = PClistBox.FindString(str);

            return n;
        }
        private void PClistBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string str = "";

            if (singleCheckBox.Checked)
            {
                try
                {
                    rawResultBox.Text = "";
                    resultBox.Text = "";
                    str = PClistBox.SelectedItem.ToString();
                    int n = findPC(str);
                    if (n > 0)
                    {
                        str = rawresultPCs[n];
                        if (str != null)
                        {
                            rawResultBox.Text = str; //  xx/xx
                            str = Convert.ToString(resultPCs[n]);
                            resultBox.Text = str; //%
                        }
                    }
                }
                catch { }
            }
        }

        private void singleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (singleCheckBox.Checked)
            {
                PClistBox.SelectionMode = SelectionMode.One;
                useBasecheckBox.Visible = true;

            }
            else
            {
                PClistBox.SelectionMode = SelectionMode.MultiSimple;
                useBasecheckBox.Visible = false;
            }

        }

        private void saveCommandsButton_Click(object sender, EventArgs e)
        {
            //save units         
            if (Directory.Exists(scriptDir))
            {
                AssessSaveFileDialog.InitialDirectory = scriptDir;
            }
            else
            {
                MessageBox.Show("Select a directory for script");
                folderBrowserDialog1.SelectedPath = DefaultDir;
                folderBrowserDialog1.ShowDialog();
                scriptDir = folderBrowserDialog1.SelectedPath;
            }
            saveFileDialog3.InitialDirectory = scriptDir;
            saveFileDialog3.FileName = "";
            saveFileDialog3.ShowDialog();
        }

        private void loadCommandsButton_Click(object sender, EventArgs e)
        {
            openFileDialog3.FileName = "";
            openFileDialog3.InitialDirectory = scriptDir;
            DialogResult dialogResult = MessageBox.Show("Load script - Yes/No?", "Load Script", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                openFileDialog3.ShowDialog();
            }
        }

        private void SaveScript(string fname)
        {
            string cmd = "";
            try
            {
                using (StreamWriter sw = new StreamWriter(fname))
                {
                    for (int i = 0; i < commandBox.Lines.Length; i++)
                    {
                        cmd = commandBox.Lines[i];
                        sw.WriteLine(cmd);
                    }
                    sw.Close();
                }
            }
            catch { }
        }
        private void LoadScript(string fname)
        {
            commandBox.Clear();
            richCommand.Clear();
            string str = "";
            try
            {
                using (StreamReader sw = new StreamReader(fname))
                {
                    while (!sw.EndOfStream)
                    {
                        str = sw.ReadLine();
                        commandBox.AppendText(str + Environment.NewLine);
                    }
                    sw.Close();
                }
            }
            catch
            { }
        }
        private void saveFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            SaveScript(saveFileDialog3.FileName);
        }

        private void openFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            LoadScript(openFileDialog3.FileName);
        }




        private string removeString(string str, string prompt)
        {
            string result = str.Replace(prompt, "");
            return result;
        }

        public bool openShell()
        {
            int f = 0;
            if (IPrangeCheckBox.Checked)
            {
                f = ipfrom;
            }
            else
            {
                f = 1;
            }
            int i = PClistBox.SelectedIndex;
            string temp_string = "";
            if (i < 0)
            {
                MessageBox.Show("Invalid IP or no target PC selected");
                return false;
            }

            if (PClistBox.Items[i].ToString().Contains("xx"))
            {
                MessageBox.Show("No connection to selected PC");
                return false;
            }
            i = i + f;
            string istr = (i).ToString();
            string ip = baseip.Substring(0, baseip.Length - 1) + istr;

            try
            {
                if (IsValidIP(ip))
                {
                    ssh = new SshClient(CreateConnectionInfo(ip));
                    try
                    {
                        ssh.Connect();
                    }
                    catch { MessageBox.Show("Cannot connect - check connection"); return false; }
                    SSHstream = ssh.CreateShellStream("dumb", 0, 0, 0, 0, 1000);
                    SSHstream.Write(Environment.NewLine);
                    Thread.Sleep(1500);
                    temp_string = SSHstream.Read();
                    termBox.AppendText(temp_string);

                    prompt = termBox.Lines[termBox.Lines.Count() - 1];
                    termBox.SelectionStart = termBox.Text.Length;
                    termBox.ScrollToCaret();
                    nextline = termBox.Lines.Count();
                }
                else
                {
                    MessageBox.Show("Invalid IP address");
                    return false;
                }
            }
            catch
            { MessageBox.Show("Error"); return false; }
            return true;
        }

        private bool closeShell()
        {
            try
            {
                SSHstream.Close();
                ssh.Disconnect();
            }
            catch { return false; }
            return true;
        }

        private bool RunShell()
        {
            try
            {
                try
                {
                    cmd = termBox.Lines[nextline - 1];
                    if (prompt.Trim() != "" && prompt != null)
                    {
                        cmd = removeString(cmd, prompt);
                    }
                }
                catch { }
                try
                {
                    SSHstream.Write(cmd + Environment.NewLine);
                    Thread.Sleep(1500);
                    termBox.SelectionStart = termBox.Text.Length - cmd.Length;
                    termBox.SelectionLength = cmd.Length;
                    termBox.SelectedText = ""; //remove the command from the box so that it isn't reprinted

                    string temp_string = SSHstream.Read();

                    termBox.AppendText(temp_string);

                }
                catch
                {
                    MessageBox.Show("Problem running Shell");
                    return false;
                }
                //termBox.AppendText(Environment.NewLine);         
                termBox.SelectionStart = termBox.Text.Length;
                termBox.ScrollToCaret();
                prompt = termBox.Lines[termBox.Lines.Count() - 1];
                nextline = termBox.Lines.Count();
            }
            catch
            { MessageBox.Show("Error"); return false; }
            return true;

        }


        private void ClearTerms()
        {
            termBox.Text = "";
            nextline = 0;
        }
        private void openTermButton_Click(object sender, EventArgs e)
        {

            if (openTermButton.Text == "Open Shell")
            {
                Show_Label("Connecting - please wait");
                ClearTerms();
                if (openShell()) //experimental shell
                {
                    termBox.ReadOnly = false;
                    openTermButton.Text = "Close Shell";
                    termBox.Focus();
                }
            }
            else
            {
                bool ret = closeShell();
                termBox.ReadOnly = true;
                openTermButton.Text = "Open Shell";
                ClearTerms();
            }
        }



        private void termBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                //if (openTerm())
                if (RunShell())
                {
                    nextline = termBox.Lines.Count();
                }
                e.Handled = true;
            }
            if (e.KeyChar == (char)Keys.Up || e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;
            }
            if (e.KeyChar == (char)Keys.Back) //stop backsapce into comamnd prompt
            {
                string temp = termBox.Lines[termBox.Lines.Count() - 1];
                if (temp == prompt)
                {
                    e.Handled = true; //don't go back too far
                }
            }
        }

        private void termBox_Click(object sender, EventArgs e)
        {
            //int nextline = this.termBox.GetLineFromCharIndex(this.termBox.SelectionStart) + 1;
            //int i = termBox.Lines.Count();

            termBox.SelectionStart = termBox.Text.Length;
            termBox.ScrollToCaret();

        }

        private void termBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Back) //stop backsapce into comamnd prompt
            {
                string temp = termBox.Lines[termBox.Lines.Count() - 1];
                if (temp == prompt)
                {
                    e.Handled = true; //don't go back too far
                }
            }
        }

        private void termBox_MouseDown(object sender, MouseEventArgs e)
        {
            termBox.SelectionStart = termBox.Text.Length;
            termBox.ScrollToCaret();
        }

        private void clearShellButton_Click(object sender, EventArgs e)
        {
            ClearTerms();
        }

        private void tabPage5_Leave(object sender, EventArgs e)
        {
            if (!termBox.ReadOnly)
            {
                DialogResult res = MessageBox.Show("Shell is still open - close it (yes/no)", "Shell", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    bool ret = closeShell();
                    termBox.ReadOnly = true;
                    openTermButton.Text = "Open Shell";
                    ClearTerms();
                }
            }
        }

        private void rootButton_Click(object sender, EventArgs e)
        {
            handleRoot();
        }

        private void rootBox_TextChanged(object sender, EventArgs e)
        {
            rootDir = rootBox.Text;
        }

        private void handleRoot()
        {
            rootBrowserDialog.SelectedPath = rootDir;
            DialogResult dlgResult = rootBrowserDialog.ShowDialog();
            if (dlgResult.Equals(DialogResult.OK))
            {
                //Show selected folder path in textbox.
                rootDir = rootBrowserDialog.SelectedPath;
                rootBox.Text = rootDir;
                liveRootBox.Text = rootDir;
                initialRootBox.Text = rootDir;
                try
                {
                    if (!Directory.Exists(rootDir))
                    {
                        DialogResult res = MessageBox.Show("Directory doesn't exist - create it (yes/no)?", "Root directory", MessageBoxButtons.YesNo);
                        if (res == DialogResult.Yes)
                        {
                            Directory.CreateDirectory(rootDir);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid directory name or cannot create it");
                }
            }
        }
        private void initialRootButton_Click(object sender, EventArgs e)
        {
            handleRoot();
        }

        private void deviceCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            remoteBox.Visible = !deviceCheckBox.Checked;
            label8.Visible = !deviceCheckBox.Checked;
            label11.Visible = !deviceCheckBox.Checked;
            deviceLabel.Visible = deviceCheckBox.Checked;
            groupBox1.Visible = !deviceCheckBox.Checked;
            runningRichBox.Visible = deviceCheckBox.Checked;
            outputLabel.Visible = deviceCheckBox.Checked;
        }

        private void connectableCheckBOx_CheckedChanged(object sender, EventArgs e)
        {
            //check connectivity now
            if (connectableCheckBox.Checked)
            {
                Parallel.Invoke(() => Show_Label("Please wait, testing SSH reachability"), () => findPCsParallel(connectableCheckBox.Checked));
            }
            else
            {
                Parallel.Invoke(() => Show_Label("Please wait, testing IP connectivity"), () => findPCsParallel(connectableCheckBox.Checked));
            }
            Modify_ListBox();
        }

        private void tempButton_Click(object sender, EventArgs e)
        {
            int ret = 0;
            setPaths();

            for (int i = 0; i < MaxFiles; i++) //for all files in the list
            {
                ret = parser.SuggestTemplate(i, true);
            }

        }

        private void markButton6_Click(object sender, EventArgs e)
        {
            markDialog(6);
        }

        private void tempButton6_Click(object sender, EventArgs e)
        {
            tempDialog(6);
        }

        private void CRcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (CRcheckBox.Checked)
            {
                CrgroupBox.Visible = true;
            }
            else
            {
                CrgroupBox.Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog4.ShowDialog();
        }
        private void clearResults()
        {
            richDiffResult.Clear();
            richTextResult2.Clear();
            richTextResult3.Clear();
            richTextResult4.Clear();
            ProcessRichResult.Clear();
            RichTestResultBox.Clear();
        }

        private void SaveLiveTests(string filename)
        {
            string str = "";
            // write live test to file:
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    sw.WriteLine("Specific IP: " + textBox9.Text);
                    sw.WriteLine("Dir base: " + dirBox3.Text);
                    sw.WriteLine("Local root: " + liveRootBox.Text);
                    sw.WriteLine("Output: " + OutFileBox.Text);
                    sw.WriteLine("Template file: " + liveTempBox.Text);
                    sw.WriteLine("Remote file: " + liveRemoteBox.Text);
                    sw.WriteLine("Optional path1: " + optPathBox.Text);
                    sw.WriteLine("Optional path2: " + addBox1.Text);
                    sw.WriteLine("Optional path3: " + addBox2.Text);
                    sw.WriteLine("Check8: " + checkBox8.Checked.ToString());
                    sw.WriteLine("Check9: " + checkBox9.Checked.ToString());
                    sw.WriteLine("Check10: " + checkBox10.Checked.ToString());
                    sw.WriteLine("Check11: " + checkBox11.Checked.ToString());
                    sw.WriteLine("Check12: " + checkBox12.Checked.ToString());
                    sw.WriteLine("Check13: " + checkBox13.Checked.ToString());
                    sw.WriteLine("Check14: " + checkBox14.Checked.ToString());
                    sw.WriteLine("Check15: " + checkBox15.Checked.ToString());
                    sw.Close();
                }

            }
            catch
            {

            }
        }

        private string SplitWords(string str, char splitchar, bool reinsert) //if path has a C: in it need to add it
        {
            string s = "";
            if (reinsert)  //reinsert split character after first occurence (accounts fro C: if split char is a colon
            {
                s = splitchar.ToString();
            }
            string[] words = str.Split(splitchar);
            if (words.Count() > 1)
            {
                str = words[1];
            }
            if (words.Count() > 2)
            {
                str = str + s + words[2];
            }
            return str.Trim();
        }
        private void LoadLiveTests(string filename)
        {
            string str = "";
            string str2 = "";
            bool r = true;
            checkBox8.Checked = false;
            checkBox9.Checked = false;
            checkBox10.Checked = false;
            checkBox11.Checked = false;
            checkBox12.Checked = false;
            checkBox13.Checked = false;
            checkBox14.Checked = false;
            checkBox15.Checked = false;
            char colon = ':';
            // write live test to file:
            try
            {
                using (StreamReader rw = new StreamReader(filename))
                {
                    while (!rw.EndOfStream)
                    {
                        str = rw.ReadLine();
                        if (str.StartsWith("Specific IP:"))
                        {
                            textBox9.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Dir base:"))
                        {
                            dirBox3.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Local root:"))
                        {
                            liveRootBox.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Output:"))
                        {
                            OutFileBox.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Template file:"))
                        {
                            liveTempBox.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Remote file:"))
                        {
                            liveRemoteBox.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Optional path1:"))
                        {
                            optPathBox.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Optional path2:"))
                        {
                            addBox1.Text = SplitWords(str, colon, r);
                        }
                        if (str.StartsWith("Optional path3:"))
                        {
                            addBox2.Text = SplitWords(str, colon, r);
                        }
                        if (str.Contains("Check8: True"))
                        {
                            checkBox8.Checked = true;
                        }
                        if (str.Contains("Check9: True"))
                        {
                            checkBox9.Checked = true;
                        }
                        if (str.Contains("Check10: True"))
                        {
                            checkBox10.Checked = true;
                        }
                        if (str.Contains("Check11: True"))
                        {
                            checkBox11.Checked = true;
                        }
                        if (str.Contains("Check12: True"))
                        {
                            checkBox12.Checked = true;
                        }
                        if (str.Contains("Check13: True"))
                        {
                            checkBox13.Checked = true;
                        }
                        if (str.Contains("Check14: True"))
                        {
                            checkBox14.Checked = true;
                        }
                        if (str.Contains("Check15: True"))
                        {
                            checkBox15.Checked = true;
                        }
                    }
                    rw.Close();
                }
            }
            catch
            {
                MessageBox.Show("Error loading live test file");
            }
        }
        private bool CheckBoxes()
        {
            if (checkBox11.Checked && (fileintextBox.Text == ""))
            {
                MessageBox.Show("If Search in additional file signposted from Diff is checked - text box cannot be blank");
                return false;
            }
            if (checkBox12.Checked && (ProcessBox.Text == ""))
            {
                MessageBox.Show("If check process running - box needs a list of processes to run");
                return false;
            }
            if (checkBox13.Checked && (TestBox.Text == ""))
            {
                MessageBox.Show("If run remote tests checked - box needs a list of tests to run");
                return false;
            }
            if (checkBox14.Checked && (addBox1.Text == ""))
            {
                MessageBox.Show("If Search for File 1 checked - need a file to search for");
                return false;
            }
            if (checkBox15.Checked && (addBox2.Text == ""))
            {
                MessageBox.Show("If Search for File 2 checked - need a file to search for");
                return false;
            }

            return true;
        }
        private void Gobutton_Click(object sender, EventArgs e)
        {

            GoTest();
        }
        private void GoTest()
        {
            if (CheckBoxes())
            {
                clearResults();
                if (RunTests())
                {
                    MessageBox.Show("Live tests run");
                }
                else
                {
                    MessageBox.Show("Not all tests successful");
                }
            }
        }
        private bool RunTests()
        {
            string nl = Environment.NewLine;
            string ip = textBox9.Text;
            string rootpath = rootDir + "\\" + ip;
            string savedfile = "";
            string slash = "";
            bool appendfile = false;
            bool connection = true;
            string OutFile = rootpath + "\\" + OutFileBox.Text;
            //check checkboxes
            //open local diff file and remote one
            // open temporary output file
            //go through remote file line by line comparing to diff - iof diff found copy into new temp file
            //close files
            //open temp file
            //go trhoughg line by line until string found 
            //extract filename
            //check if files exist - if so open it
            //open another results output file
            //go yrhough file line by line looking for search string to look for
            //if found register this and record in output file
            if (OutFileBox.Text.Trim() == "")
            {
                MessageBox.Show("Invalid output filename");
                return false;
            }
            else
            {

            }
            if (checkBox9.Checked) //if ip from text box on form
            {

                //added to use common code:
                savedfile = ExtractFilefromPath(liveRemoteBox.Text);
                ManageAllTests(ip, rootpath, savedfile, OutFileBox.Text);
                return true;
                //COMMENT OUT REST WHEN WORKS!

            }
            else //if not single ip but taken from listbox
            {
                rootpath = rootDir;
                savedfile = ExtractFilefromPath(liveRemoteBox.Text);
               
                //not device, use base, base dir, remote dir, local dir, from and not to
                //GetFiles_PC_List(false, true, dirBox3.Text.Trim(), liveRemoteBox.Text, rootpath, false);
                return (CycleDiffFiles(true, dirBox3.Text, OutFileBox.Text, rootpath, savedfile));
            }
            return true;
        }

        private void ManageAllTests(string ipstr, string local, string savedfile, string outfilename)
        {
            bool appendfile = false;
            string savedPath = "";
            string str = "";
            string net = "";
            string host = "";
            string based = local;            
            bool connected = true;
            bool getFile = false;
            
            net = baseip.Substring(0, baseip.Length - 1);
            host = Convert.ToString(ipstr);
            if (!checkBox9.Checked) //if just a number add network to it
            {
               host = net + host;
            }

            savedPath = local;
            string outfilepath = savedPath + "\\" + outfilename;
            WriteOutputFile(outfilepath, false, "#Live test: " + DateTime.Now.ToString() + nl + "#PC: " + ipstr + nl + "#------------------------------");
            appendfile = true;

            if (checkBox8.Checked)
            {
                char exist = CheckRemoteFile(liveRemoteBox.Text, host); //if remote file exists
                if (exist == 'F')   //if remote file found
                {
                    connected = GetFiles(liveRemoteBox.Text, savedPath, host, false, true, getFile);   //transfer files from remote
                    if (connected)
                    {
                        PCTests(savedPath, savedfile, savedPath + "\\Temp.txt", appendfile, host, outfilepath);
                        appendfile = true;
                    }
                }
                else if (exist == 'N')
                {
                    //MessageBox.Show("Remote file not found");
                    WriteOutputFile(outfilepath, appendfile, "#Remote file not found: " + liveRemoteBox.Text + nl + "#----------------------------");
                    appendfile = true;
                }
                else
                {
                    //MessageBox.Show("Error connecting to remote");
                    WriteOutputFile(outfilepath, appendfile, "#Error finding remote: " + liveRemoteBox.Text + nl + "#----------------------------");
                    appendfile = true;
                }
            }
            if (checkBox14.Checked && connected) //additonal file to search
            {
                char exist = CheckRemoteFile(addBox1.Text, host); //if remote file exists
                if (exist == 'F')   //if remote file found
                {
                    if (GetFiles(addBox1.Text, savedPath, host, false, true, getFile)) //to transfer a file from remote to this PC
                    {
                        savedfile = ExtractFilefromPath(addBox1.Text);
                        richTextResult3.Text = SearchDiff(addBox1.Text, savedPath + "\\" + savedfile, outfilepath, appendfile, addStringBox1);
                        appendfile = true;
                    }
                    else { connected = false; }
                }
                else if (exist == 'N')
                {
                    WriteOutputFile(outfilepath, appendfile, "#Remote file not found: " + addBox1.Text + nl + "#----------------------------");
                    appendfile = true;
                }
                else
                {
                    //MessageBox.Show("Error connecting to remote");
                    WriteOutputFile(outfilepath, appendfile, "#Error finding remote: " + addBox1.Text + nl + "#----------------------------");
                    appendfile = true;
                }
            }
            if (checkBox15.Checked && connected) //additonal file to search
            {
                char exist = CheckRemoteFile(addBox1.Text, host); //if remote file exists
                if (exist == 'F')   //if remote file found
                {
                    if (GetFiles(addBox2.Text, savedPath, host, false, true, getFile)) //to transfer a file from remote to this PC
                    {
                        savedfile = ExtractFilefromPath(addBox2.Text);
                        richTextResult4.Text = SearchDiff(addBox2.Text, savedPath + "\\" + savedfile, outfilepath, appendfile, addStringBox2);
                        appendfile = true;
                    }
                }
                else if (exist == 'N')
                {
                    WriteOutputFile(outfilepath, appendfile, "#Remote file not found: " + addBox2.Text + nl + "#----------------------------");
                    appendfile = true;
                }
                else
                {
                    WriteOutputFile(outfilepath, appendfile, "#Error finding remote: " + addBox2.Text + nl + "#----------------------------");
                    appendfile = true;
                }
            }
            CycleTests(host, savedPath + "\\" + OutFileBox.Text, appendfile);

        }

        private bool CycleDiffFiles(bool useBase, string BaseDir, string outfilename, string local, string savedfile)
        {
            int i = 0;
            int f = 0;          
            string ipstr = "";
            
            string based = local;
           
            bool somerun = false; ;

            bool single = checkBox9.Checked; //using ip box on form or not?

            if (IPrangeCheckBox.Checked)
            {
                f = ipfrom;
            }
            else
            {
                f = 1;
            }
                        
            for (i = 0; i < PClistBox.Items.Count; i++)
            {
                if (PClistBox.GetSelected(i))
                {
                    somerun = true;
                    if (!PClistBox.Items[i].ToString().Contains("xx")) //if connected, ie. doesn't contain xx
                    {
                        if (BaseDir.Trim().Length > 0)
                        {
                            if (singleCheckBox.Checked)
                            {
                                if (useBase)
                                {
                                    local = based + "\\" + BaseDir.Trim() + Convert.ToString(f + i);
                                }
                                else
                                {
                                    local = based;
                                }
                            }
                            else
                            {
                                local = based + "\\" + BaseDir.Trim() + Convert.ToString(f + i);
                            }
                        }

                        ipstr = (i + f).ToString(); //offset if filter on
                        ManageAllTests(ipstr, local, savedfile, outfilename);
                    }
                    
                }               
            }
            if (!somerun)
            {
                MessageBox.Show("No PCs selected");
            }

            return somerun;
        }

        private void CycleTests(string ip, string outfile, bool append)
        {
            if (checkBox12.Checked)
            {
                if (ProcessBox.Text.Trim() != "")
                {
                    CheckProcess(ip, outfile, append);
                    append = true;
                }
            }
            if (checkBox13.Checked)
            {
                if (TestBox.Text.Trim() != "")
                {
                    RunTestCommands(ip, outfile, append);
                }
            }
        }
        
        private void PCTests(string savedPath, string savedfile, string tempPath, bool appendfile, string ip, string outfile)
        {
            string OSVer = OSVersion(ip);
            string slash = "";
            string outP = "";
            string fpath = optPathBox.Text.Trim();
            outP =  Diff(liveTempBox.Text, savedPath + "\\" + savedfile, tempPath, outfile, appendfile, liveRemoteBox.Text); //local template, remote file to compare, temp diff file as outpu
            richDiffResult.Text = richDiffResult.Text + outP + nl;
            appendfile = true;
            bool getFile = false;
            if (richDiffResult.Text.Trim() != "")
            {
                //create new output file then append to it
                if (checkBox10.Checked && DiffSearchBox.Text != null && DiffSearchBox.Text.Trim() != "")
                {
                    richTextResult2.Text = SearchDiff("", tempPath, outfile, appendfile, DiffSearchBox );
                    appendfile = true;
                }
                if (optPathBox.Text.Trim() !="")
                {
                    
                    if (OSVer.Contains("linux") || (OSVer.Contains("cygwin")))
                    {
                        slash = "/";
                    }
                    else if (OSVer.Contains("windows"))
                    {
                        slash = "\\";
                    }
                    if (fpath.LastIndexOf(slash) != fpath.Length -1)
                    {
                            fpath = fpath + slash;
                    }                                      
                }
                char exist = CheckRemoteFile(fpath + secondFile, ip); //if remote file exists                
                if (checkBox11.Checked && secondFile != null && secondFile != "" && (exist == 'F'))
                {
                    string fp = secondFile;
                    if (optPathBox.Text.Trim() != "")
                    {
                        if (optPathBox.Text.Contains("/"))
                        {
                            slash = "/";
                        }
                        else if (optPathBox.Text.Contains("\\"))
                        {
                            slash = "\\";
                        }
                        else
                        {
                            slash = "";
                            fp = secondFile;
                        }
                        if (optPathBox.Text.Trim().LastIndexOf(slash) == optPathBox.Text.Trim().Length)
                        {
                            slash = "";
                        }
                        fp = optPathBox.Text.Trim() + slash + secondFile.Trim();

                    }
                   
                    try
                    {
                        GetFiles(fp, savedPath, ip, false, true, getFile); //to transfer a file from remote to this PC
                    }
                    catch
                    {
                        MessageBox.Show("Unable to locate second file");
                        return;
                    }
                    slash = "\\";
                    if (savedPath.Trim().LastIndexOf("\\") == savedPath.Trim().Length)
                    {
                        slash = "";
                    }
                    string secondFilename = ExtractFilefromPath(secondFile);
                    richTextResult2.Text = SearchSecondFile(savedPath + slash + secondFilename, outfile, appendfile);
                }
                else if (exist == 'N')
                {
                    WriteOutputFile(outfile, appendfile, "#Remote file not found: " + fpath + secondFile + nl + "#----------------------------");
                    appendfile = true;
                }
                else if (exist == 'E')
                {
                    WriteOutputFile(outfile, appendfile, "#Error finding remote file: " + fpath + secondFile + nl + "#----------------------------");
                    appendfile = true;
                }
            }
        }
               

        private string ExtractFilefromPath(string filename)
        {
            string str = "";
            int i = 0;
            try
            {
                str = filename.Replace('\\', '/');
                if (filename.Contains("/"))
                {
                    i = filename.LastIndexOf("/") +1;
                    str = filename.Substring(i);
                    return str;
                }
                else if (filename.Contains("\\"))
                {
                    i = filename.LastIndexOf("\\") + 1;
                    str = filename.Substring(i);
                    return str;
                }
                { return filename; }
            }
            catch
            {
                return "";
            }
        }
        private string Diff(string Templatefile, string Compfile, string Diffile, string OutFile, bool append, string remoteFile)
        {   //compare the Compfile against a Templatefile - out anything that is different into the Diffile
            string rstr = "";
            try
            {
                string c, t, d;
                bool found = false;
                bool hasDiff = false;
                               
                if (!File.Exists(Templatefile))
                {
                    MessageBox.Show("Template file to compare against does not exist");
                    return "";
                }
                if (!File.Exists(Compfile))
                {
                    MessageBox.Show("File to compare does not exist");
                    return "";
                }              
                StreamReader comp = new StreamReader(Compfile);
                if (File.Exists(Diffile))
                {
                    File.Delete(Diffile);
                }
                StreamWriter outF = new StreamWriter(OutFile, append); //output file 
                StreamWriter diff = new StreamWriter(Diffile); //temporary diff file                
                using (outF)
                {
                    using (diff)
                    {
                        diff.WriteLine("File found: " + Compfile);
                        richDiffResult.Text = richDiffResult.Text + "File found: " + Compfile + nl;
                        richDiffResult.Text = richDiffResult.Text + "------------------------------------" + nl;
                        richDiffResult.Text = richDiffResult.Text + "#-----------------Difference BEGIN---------------------------" +nl;
                        diff.WriteLine("--------------------------------------------");
                        outF.WriteLine("#Remote File found: " + remoteFile);
                        outF.WriteLine("#-----------------Difference BEGIN---------------------------");
                        using (comp) //file to compare against
                        {
                            while (!comp.EndOfStream)
                            {
                                c = comp.ReadLine();
                                using (StreamReader temp = new StreamReader(Templatefile))    //template file
                                {
                                    found = false;
                                    while (!temp.EndOfStream)
                                    {
                                        t = temp.ReadLine();
                                        if (t == c)
                                        {
                                            found = true;                                           
                                        }
                                    }
                                    if (!found)
                                    {
                                        hasDiff = true;
                                        diff.WriteLine(c); //if the line doesn't exist in the template file - put it in the diff file
                                        outF.WriteLine(c);
                                        rstr = rstr + c + Environment.NewLine;
                                    }
                                    temp.Close();
                                }

                            }
                            comp.Close();
                        }
                        diff.Close();
                    }
                    if (!hasDiff)
                    {
                        outF.WriteLine("#------No Diff--------");
                        richDiffResult.Text = richDiffResult.Text + "#-------No Diff--------" + nl;
                    }
                    outF.WriteLine("#-----------------Difference END---------------------");
                    richDiffResult.Text = richDiffResult.Text + "#-----------------Difference END---------------------------" + nl;
                    outF.Close();
                }
                return rstr;
            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);
                return rstr;
            }

        }
        private string SearchDiff(string foundFName, string Diffile, string outfile, bool append, TextBox SearchBox)
        {       //search in Diffile for string contained in DiffSearchBox
            string s = "";
            string d = "";
            string s1 = "";
            string s2 = "";
            string s3 = "";
            bool f = false;
            int lines = 0;
            string tmp = "";
            string rstr = "";

            try
            {
                StreamWriter outf = new StreamWriter(outfile, append);                

                lines = SearchBox.Lines.Length;
                secondFile = "";
                if (lines > 0)
                {
                   if (File.Exists(Diffile)) //temporary diff file
                   {
                        StreamReader diff = new StreamReader(Diffile);

                        
                        
                        for (int i = 0; i < lines; i++)
                        {
                            f = false;
                            s1 = "";
                            s2 = "";
                            s = SearchBox.Lines[i];
                            if (s.Contains("%filename%"))   //if the search box contains a filename
                            {
                                s = s.Trim(); //remove spaces
                                s1 = s.Substring(0, s.IndexOf("%filename%"));
                                s2 = s.Substring(s.IndexOf("%filename") + "%filename%".Length);
                                f = true;
                                
                            }
                            using (diff)
                            {
                               
                                using (outf) //true to append
                                {
                                    while (!diff.EndOfStream)
                                    {
                                        d = diff.ReadLine();
                                        d = d.Trim();
                                        if (f)  //if a filename found extract its name into secondFile
                                        {
                                            if (d.StartsWith(s1) && d.EndsWith(s2))
                                            {
                                                s3 = d.Replace(s1, "");
                                                secondFile = s3.Replace(s2, ""); //extract the nameof a second file to search through (eg. DNS zone file)
                                                tmp = "Filename: " + secondFile;
                                                if (optPathBox.Text.Trim() !="")
                                                {
                                                    outf.WriteLine("#Found link to " + secondFile + " in " + optPathBox.Text);
                                                }
                                                else
                                                {
                                                    outf.WriteLine("#Found link to " + secondFile);
                                                }
                                                
                                                outf.WriteLine(tmp);
                                                rstr = rstr + tmp + Environment.NewLine;
                                            }
                                        }
                                        else if (d.Contains(s)) //if its not a file its a string
                                        {
                                            tmp = "-Found: " + s + " in: " + d;
                                                outf.WriteLine(tmp); //if found contents of DiffSearchBox in diff file write it out
                                                rstr = rstr + tmp + Environment.NewLine;
                                        }
                                        else
                                        {
                                            rstr = rstr + d + Environment.NewLine;
                                        }

                                    }
                                    
                                }
                                diff.Close();
                            }                            
                        }
                        return rstr;
                    }
                    else
                    {
                        MessageBox.Show("Diff file not found");
                        return rstr;
                    }
                }
                else
                {
                    MessageBox.Show("No lines in box");
                    
                }
                outf.WriteLine("------------------------------------------------");
                outf.Close();
                return rstr;
            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);
                return rstr;
            }
        }
        private string SearchSecondFile(string fname, string outfile, bool append)
        {   //search for strings in second search file (extracted from pervious search)
            string s = "";
            string fs = "";
            int lines = 0;
            string tmp = "";
            bool found = false;
            string rstr = "";
            try
            {
                if (File.Exists(fname))
                {
                    using (StreamWriter outf = new StreamWriter(outfile, append))
                    {
                        outf.WriteLine("--------------BEGIN file-------------------------------");
                        lines = fileintextBox.Lines.Length;
                        for (int i = 0; i < lines; i++)
                        {
                            s = fileintextBox.Lines[i];
                            using (StreamReader f = new StreamReader(fname))
                            {

                                while (!f.EndOfStream)
                                {
                                    fs = f.ReadLine();
                                    if (fs.Contains(s))
                                    {
                                        tmp = "-Found: " + s + " in: " + fs;
                                        outf.WriteLine(tmp);
                                        rstr = rstr + tmp + Environment.NewLine;
                                        found = true;
                                    }
                                    else
                                    {
                                        outf.WriteLine(fs);
                                        rstr = rstr + fs + Environment.NewLine;
                                    }
                                }                               
                                f.Close();
                            }                            
                        }
                        outf.WriteLine("--------------END file---------------------------------");
                        outf.Close();
                    }
                    if (found)
                    {
                        return rstr;
                    }
                    else
                    {
                        return rstr;
                    }
                }                
                else
                {
                    MessageBox.Show("Second file not found");
                }
            }
            catch
            {
                return rstr;
            }
            return rstr;
        }

        private bool CheckProcess(string ip, string outfile, bool append)
        {   //check to see if processes running
            string process = "";
            string cmd = "";
            string result = "";
            int lines = 0;
            ProcessRichResult.Text = "";
            bool ret = false;
            string OSVers = "linux";
            string nl = Environment.NewLine;
            try
            {
                using (StreamWriter outf = new StreamWriter(outfile, append))
                {                   
                    try
                    {
                        OSVers = OSVersion(ip);
                        outf.WriteLine("#-------------------------------------------------");
                        outf.WriteLine("#Process checks:");
                        lines = ProcessBox.Lines.Length;
                        for (int i = 0; i < lines; i++)
                        {
                            process = ProcessBox.Lines[i].Trim();
                            if (OSVers.Contains("linux"))
                            {
                                cmd = "ps -e | grep " + process;
                                result = RunReturnCommand(ip, cmd);
                            }
                            else if (OSVers.Contains("windows"))           //windows                                        )
                            {
                                cmd = "tasklist /FI \"IMAGENAME eq " + process + "\"";
                                result = RunReturnCommand(ip, cmd);                              
                            }                                                     
                                                      
                            if (result.Trim() != "" && result != null)
                            {
                                if (result.Contains(process))
                                {
                                    ProcessRichResult.Text = ProcessRichResult.Text + "Running: " + process +nl;                                   
                                    ret = true;
                                }
                                else if (result.Contains("No connection"))
                                {
                                    ProcessRichResult.Text = "Not connected" +nl;
                                    ret = false;
                                }
                                else
                                {
                                    ProcessRichResult.Text = ProcessRichResult.Text + "Not found: " + process +nl;
                                    ret = false;
                                }
                            }
                            else
                            {
                                ProcessRichResult.Text = ProcessRichResult.Text + "Not found: " + process +nl;
                                ret = false;
                            }
                            
                        }
                        outf.WriteLine(ProcessRichResult.Text);
                    }
                    catch
                    {
                        ret = false;
                    }
                    outf.WriteLine("#--------------------------------------------");
                    outf.Close();
                }
            }
            catch { }
            return ret;
        }

        private bool RunTestCommands(string ip, string outfile, bool append)
        {
            string cmd = "";
            bool run = false;
            int lines = 0;
            int testlines = 0;
            string s = "";
            string test = "";
            string result = "";
            bool ret = false;
            
            string nl = Environment.NewLine;
            RichTestResultBox.Text = "";
            try
            {
                using (StreamWriter outf = new StreamWriter(outfile, append))
                {
                    try
                    {
                       
                        outf.WriteLine("#Tests run:");
                        outf.WriteLine("#-------------------------------------------------");
                        lines = TestBox.Lines.Length;
                        for (int i = 0; i < lines; i++)
                        {
                            s = TestBox.Lines[i];
                            cmd = s.Trim();
                            result = RunReturnCommand(ip, cmd);
                            
                            if (result.Trim() != "" && result != null)
                            {
                                testlines = TestSearchBox.Lines.Length;                                
                                for (int t =0; t < testlines; t++)
                                {
                                    test = TestSearchBox.Lines[t];
                                    if (result.Contains(test))
                                    {
                                        string[] words = result.Split('\n');
                                        foreach (string word in words)
                                        {
                                            if (word.Contains(test)) //if search string found
                                            {
                                                RichTestResultBox.Text = RichTestResultBox.Text + "Found: " + test + " in:   " + "\"" + word + "\"" + nl;
                                            }
                                        }                                                                              
                                    }  
                                    else if (result == "Error")
                                    {
                                        RichTestResultBox.Text = RichTestResultBox.Text + "Command returned not found or error: " + cmd + nl;
                                        outf.WriteLine("#Command: " + cmd + " returned not found or error");
                                    }
                                }
                                
                                RichTestResultBox.Text = RichTestResultBox.Text + result + nl;
                                
                                run = true;
                            }
                            else if (result.Contains("No connection"))
                            {
                                RichTestResultBox.Text = "Not connected" +nl;
                                outf.WriteLine("#Cannot run command: " + cmd);
                            }
                            else
                            {
                                RichTestResultBox.Text = "No return result" +nl;
                                outf.WriteLine("#No return result for: "+ cmd);
                            }

                        }
                    }
                    catch
                    {
                        ret = false;
                    }
                    if (run)
                    {
                        ret =true;
                    }
                    else
                    {
                        ret = false;
                    }
                    outf.WriteLine(RichTestResultBox.Text);
                    outf.WriteLine("#-------------------------------------------------");
                    outf.Close();
                }
            }
            catch { }
            return ret;

        }
        public void wait(int milliseconds)
        {
            System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0) return;
            
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();
            timer1.Tick += (s, e) =>
            {
                
                timer1.Enabled = false;
                timer1.Stop();
               
            };
            while (timer1.Enabled)
            {
                Application.DoEvents();
            }
        }

        private string RunReturnCommand(string ip, string cmd)
        {
            //string commandResult = "";
            var commandResult = "";
            TimeSpan interval = new TimeSpan(0, 0, 15); //timeout of 15 seconds for commands

            //timer1.Interval = 2000; //allow 10 second for command to complete - if not stop it

            try
            {
                               
                using (var ssh = new SshClient(CreateConnectionInfo(ip)))
                {
                    try
                    {
                        ssh.Connect();
                    }
                    catch
                    {
                        MessageBox.Show("Cannot connect - check connection");
                        return "No connection";
                    }
                    SshCommand command = ssh.CreateCommand(cmd);
                    //commandResult = ssh.RunCommand(cmd).BeginExecute().ToString();
                    //IAsyncResult Aresult = command.BeginExecute();
                    command.CommandTimeout = interval;
                    command.Execute();
                    commandResult = command.Result;
                    if (commandResult == "")
                    {
                        commandResult = "Error";
                    }
                    /*//timerCount = false;
                    int c = 0;
                    int i = -1;
                    while (i != 0 && c <10)
                    {
                        wait(1000);
                        i = command.ExitStatus;
                        if (i == 0)
                        {
                            c = 10;
                        }
                        c++;                                                                                             
                    }
                   
                    var result = ssh.RunCommand(cmd).EndExecute(Aresult);
                    commandResult = result;*/

                    try
                    {
                        ssh.Disconnect();
                        ssh.Dispose();
                        return commandResult;
                    }
                    catch { return commandResult; }
                }
                return commandResult;
            }
            catch { return commandResult; }
        }


        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            bool b = checkBox8.Checked;
            groupBox3.Visible = b;
            groupBox4.Visible = b;
            groupBox6.Visible = b;
            groupBox7.Visible = b;
            

        }

        private void DiffSearchBox_TextChanged(object sender, EventArgs e)
        {
            if (DiffSearchBox.Text.Contains("%filename%"))
            {
                optPathBox.Visible = true;
                optPathlabel.Visible = true;
            }
            else
            {
                optPathBox.Visible = false;
                optPathlabel.Visible = false;
            }
        }

        private void openFileDialog4_FileOk(object sender, CancelEventArgs e)
        {
            liveTempBox.Text = openFileDialog4.FileName;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timerCount = true;
            //timer1.Stop();
            
            //timer1.Enabled = false;
        }

        private void initialRootBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void liveRootbutton_Click(object sender, EventArgs e)
        {
            handleRoot();
        }

        private void dirBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPage7_Click(object sender, EventArgs e)
        {

        }

        private void optPathBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void Runbutton1_Click(object sender, EventArgs e)
        {
            GoTest();
        }

        private void Runbutton2_Click(object sender, EventArgs e)
        {
            GoTest();
        }

        private void saveLivebutton_Click(object sender, EventArgs e)
        {
            //SaveLiveTests("C:\\temp2\\live.lv");
            
                if (Directory.Exists(AssessFilePath))
                {
                    AssessSaveFileDialog.InitialDirectory = AssessFilePath;
                }
                else
                {
                    MessageBox.Show("Select a directory for file");
                    folderBrowserDialog3.SelectedPath = DefaultDir;
                    folderBrowserDialog3.ShowDialog();
                    AssessFilePath = folderBrowserDialog3.SelectedPath;
                }
                saveliveFileDialog.InitialDirectory = AssessFilePath;
                saveliveFileDialog.FileName = assessTitleBox.Text.Trim() + ".liv";
                saveliveFileDialog.ShowDialog();
           
        }

        private void loadLivebutton_Click(object sender, EventArgs e)
        {
            openliveFileDialog.FileName = "";
            openliveFileDialog.InitialDirectory = DefaultDir;
            DialogResult dialogResult = MessageBox.Show("Load test - this will clear all form data - Yes/No?", "Load Live Test", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                openliveFileDialog.ShowDialog();
            }
        }

        private void saveliveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            SaveLiveTests(saveliveFileDialog.FileName);
        }

        private void openliveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            LoadLiveTests(openliveFileDialog.FileName);
        }

        private void TestBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void dirBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            groupBox5.Visible = !checkBox9.Checked;
            textBox9.Visible = checkBox9.Checked;
        }


        private void clearForm()
        {
            assessTitleBox.Text = "";
            //rootBox.Text = "";
            markBox1.Text = "";
            markBox2.Text = "";
            markBox3.Text = "";
            markBox4.Text = "";
            markBox5.Text = "";
            tempBox1.Text = "";
            tempBox2.Text = "";
            tempBox3.Text = "";
            tempBox4.Text = "";
            tempBox5.Text = "";
            outBox.Text = "";
            commentBox.Text = "";
            rawResultBox.Text = "";
            resultBox.Text = "";
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Clear the form?", "Clear Form", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                clearForm();
            }

        }

    }
}

