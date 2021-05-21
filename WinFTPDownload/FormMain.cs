using CoreFtp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFTPDownload
{
    public partial class FormMain : Form
    {
        public const string CONFIGFILENAME = "MyConfig.json";
        public FormMain()
        {
            InitializeComponent();
        }

        private List<FtpFile> DownloadFileList { get; set; }
        CoreFtp.FtpClient ftpClient { get; set; }
        WinConfig myConfig { get; set; }

        #region Config
        public void BindingConfig(bool isSave)
        {
            try
            {

                if (isSave)
                {
                    myConfig.Host = Host.Text;
                    myConfig.Username = Username.Text;
                    myConfig.Password = Password.Text;
                    myConfig.Port = Port.Text;
                    myConfig.DownloadPath = DownloadPath.Text;
                    myConfig.IsOverwrite = chkOverwrite.Checked;

                }
                else
                {
                    Host.Text = myConfig.Host;
                    Username.Text = myConfig.Username;
                    Password.Text = myConfig.Password;
                    Port.Text = myConfig.Port;
                    DownloadPath.Text = myConfig.DownloadPath;
                    chkOverwrite.Checked = myConfig.IsOverwrite;
                }



            }
            catch (Exception ex)
            {

                ex.AlertError();
            }

        }

        public void LoadConfig()
        {
            try
            {
                var jsonString = CONFIGFILENAME.ReadAllLine();
                if (jsonString.IsNotEmpty())
                {
                    myConfig = jsonString.ToObject<WinConfig>();
                    if (myConfig == null)
                    {
                        myConfig = new WinConfig();
                    }
                }
                else
                {
                    myConfig = new WinConfig();
                }

                BindingConfig(false);
            }
            catch (Exception ex)
            {

                ex.AlertError();
            }
        }

        public void SaveConfig()
        {
            try
            {
                BindingConfig(true);

                var jsonString = myConfig.ToJson();

                CONFIGFILENAME.SaveJson(jsonString);
            }
            catch (Exception ex)
            {

                ex.AlertError();
            }
        }
        #endregion

        /// <summary>
        /// Download Selected Filed
        /// </summary>
        /// <returns></returns>
        public async Task Download()
        {
            btnLogin.DisableBtn();
            btnDownload.DisableBtn();
            try
            {
                if (myConfig.DownloadPath.IsEmpty())
                {
                    "Download Path Empty".AlertError();
                    btnDownload.EnableBtn();
                    return;
                }

                if (!myConfig.DownloadPath.IsDirExist())
                {
                    "Download Path Not Exist".AlertError();
                    btnDownload.EnableBtn();
                    return;
                }

                progressBar1.Value = 0;
                progressBar1.Maximum = DownloadFileList.Count;
                Application.DoEvents();
                int iTotal = DownloadFileList.Count;
                int iCount = 1;
                foreach (var file in DownloadFileList)
                {
                    lblStatus.Text = $"{iCount++} / {iTotal}";

                    var localPath = new FileInfo(Path.Combine(myConfig.DownloadPath, file.Name));
                    using (var ftpReadStream = await ftpClient.OpenFileReadStreamAsync(file.Name))
                    {
                        using (var fileWriteStream = localPath.OpenWrite())
                        {


                            await ftpReadStream.CopyToAsync(fileWriteStream);
                        }
                    }

                    progressBar1.Increment(1);
                    Application.DoEvents();
                }

                "Done".AlertInfo();
            }
            catch (Exception ex)
            {

                ex.AlertError();
            }
            btnDownload.EnableBtn();
            btnLogin.EnableBtn();
        }

        /// <summary>
        /// Login & Get All the File List
        /// </summary>
        /// <returns></returns>
        public async Task Login()
        {
            btnLogin.DisableBtn();
            btnDownload.DisableBtn();

            SaveConfig();

            ftpClient = new CoreFtp.FtpClient(new FtpClientConfiguration
            {
                Host = myConfig.Host,
                Username = myConfig.Username,
                Password = myConfig.Password,
                Port = myConfig.Port.ParseToInt(),
                IgnoreCertificateErrors = true
            });

            try
            {
                await ftpClient.LoginAsync();

                var ftpFileList = await ftpClient.ListFilesAsync();

                DownloadFileList = new List<FtpFile>();
                foreach (var file in ftpFileList)
                {
                    var entity = new FtpFile()
                    {
                        IsDownload = true,
                        Name = file.Name,
                        Size = file.Size.ConvertBytesToMegabytes(),
                        DateModified = file.DateModified,
                        NodeType = file.NodeType,

                    };
                    DownloadFileList.Add(entity);
                }

                dataGridView1.AutoGenerateColumns = false;
                dataGridView1.DataSource = DownloadFileList;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                if (DownloadFileList.Any())
                {
                    btnDownload.EnableBtn();
                }


            }
            catch (Exception ex)
            {

                ex.AlertError();
            }

            btnLogin.EnableBtn();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            DownloadPath.BrowseFolder(this);
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            await Download();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            await Login();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            DownloadPath.OpenFolder();
        }

        private void btnSaveSetting_Click(object sender, EventArgs e)
        {
            SaveConfig();
            "Done".AlertInfo();
        }

        private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var item in DownloadFileList)
            {
                item.IsDownload = chkSelectAll.Checked;
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            LoadConfig();
            btnDownload.DisableBtn();
        }
    }
}
