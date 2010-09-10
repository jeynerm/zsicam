using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using System.IO; 
using System.Drawing.Imaging;
using zsi.Biometrics;
using zsi.PhotoFingCapture;
using zsi.WebCamServices;
using zsi.WebFileService;
namespace zsi.PhotoFingCapture
{

    public partial class frmMain : Form
    {
        public string UserId { get; set; }
        public string ProfileNo{get;set;}
        public FingersData FingersData { get; set; }
        private WebCamera WebCam { get; set; }
        public frmMain()
        {
            try
            {
                this.InitializeComponent();
                this.WebCam = new WebCamera(this.picture);
                InitFingerPrintSettings();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void InitFingerPrintSettings(){
            this.FingersData = new FingersData();	
            this.FingersData.EnrolledFingersMask=0;
            this.FingersData.MaxEnrollFingerCount=10;
            this.FingersData.IsEventHandlerSucceeds=true;
            this.FingersData.DataChanged += new OnChangeHandler(OnFingersDataChange);  
            

        }
        private void btnCapture_Click(object sender, EventArgs e)
        {
            
            pbResult.Image = picture.Image;
        }
        private string GetImageFileNameByPosition()
        {
            string _ImagePosition = cbImagePosition.SelectedItem.ToString().ToLower();
            string _fileName = string.Empty;
            switch (_ImagePosition)
            {
                case "front": _fileName = ".jpg"; break;
                case "left": _fileName = "-left.jpg"; break;
                case "right": _fileName = "-right.jpg"; break;
                case "back": _fileName = "-back.jpg"; break;
                default: break;
            }

            _fileName = this.ProfileNo + _fileName;
            if (rdbITProfile.Checked != true) _fileName = "case-" + _fileName;
            return _fileName;
        }
        private void OnFingersDataChange() {
            int _registeredFingers = CountRegisteredFingers(this.FingersData);
            if (_registeredFingers > 0 && string.IsNullOrEmpty(this.ProfileNo)==false ) btnUploadFG.Enabled = true; else btnUploadFG.Enabled = false;
        

        }

        private int CountRegisteredFingers(FingersData data) {
            int _result=0;
            foreach(DPFP.Template item in data.Templates)
            {
                if (item != null) _result += 1;
            }
            return _result;
        }

        public void EnableControls(Boolean IsEnable)
        {
            btnCapture.Enabled = IsEnable;
            btnRegisterFP.Enabled = IsEnable;
            btnVerifyFP.Enabled = IsEnable;
            btnFind.Enabled = IsEnable;
            txtProfileNo.Enabled = IsEnable;
            btnLogOut.Enabled = IsEnable;

        }
        private void btnLogOut_Click(object sender, EventArgs e)
        {
            EnableControls(false);
            btnUploadPhoto.Enabled = false;
            this.UserId = "";

        }
        private void btnSettings_Click(object sender, EventArgs e)
        {
            zsi.PhotoFingCapture.frmSettings frmSettings = new zsi.PhotoFingCapture.frmSettings();
            frmSettings.ShowDialog();
        }
        private void btnFind_Click(object sender, EventArgs e)
        {
            zsi.PhotoFingCapture.WebFileService.WebFileManager wfm = new zsi.PhotoFingCapture.WebFileService.WebFileManager();
            string _profileNo = this.txtProfileNo.Text.Trim() ;            
            try
            {
                if(_profileNo.Length<13) throw new Exception();
                Int64.Parse(_profileNo);
            }
            catch{
                MessageBox.Show("Please enter 13 digit numbers.");
                return;
            }
          
            string _result = wfm.GetProfileInfo(this.UserId, _profileNo);
            if (string.IsNullOrEmpty(_result) == false)
            {
                lblProfileName.Text = _result;
                this.ProfileNo = this.txtProfileNo.Text.Trim();                
                this.btnUploadPhoto.Enabled = true;
            }
            else {
                lblProfileName.Text  ="Profile Not found";
            }
            OnFingersDataChange();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            zsi.Biometrics.MainForm mf = new zsi.Biometrics.MainForm();
            mf.Show();
        }
        private void btnRegisterFP_Click(object sender, EventArgs e)
        {
 
            zsi.Biometrics.frmRegisterFP _frmRegFP = new frmRegisterFP(this.FingersData);
            _frmRegFP.Show();

        }
        private void btnVerifyFP_Click(object sender, EventArgs e)
        {
            zsi.Biometrics.frmVerification _frmVerify = new frmVerification();
            _frmVerify.Show();

        }
        private void btnUploadFG_Click(object sender, EventArgs e)
        {
            try
            {
                btnUploadFG.Text = "Uploading...";
                btnUploadFG.Enabled = false;
                zsi.PhotoFingCapture.WebFileService.WebFileManager wf = new zsi.PhotoFingCapture.WebFileService.WebFileManager();
                System.IO.MemoryStream _MemoryStream = new System.IO.MemoryStream();

                DPFP.Template[] tmps = this.FingersData.Templates;

                for (int i = 0; i < tmps.Length; i++)
                {
                    if(this.FingersData.Templates[i]!=null){
                        Stream _stream = this.FingersData.Templates[i].Serialize(_MemoryStream);
                        byte[] _byte = Util.StreamToByte(_stream);
                        wf.UploadBiometricsData(this.UserId, this.ProfileNo + "-" + i.ToString() + ".fpt", _byte);
                    }
                }
                MessageBox.Show("Finger prints has been uploaded to the server.");
            }            
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally{
                btnUploadFG.Text = "Upload";
                btnUploadFG.Enabled = true;
            }
        }
        private void btnLogin_Click(object sender, EventArgs e)
        {
            frmLogin _frmLogin = new frmLogin(this);

            _frmLogin.ShowDialog();
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            cbImagePosition.SelectedIndex = 0;
            this.Show();
            WebCam.Show();
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            WebCam.Close();
        }

        private void btnUploadPhoto_Click(object sender, EventArgs e)
        {
            try
            {

                if (pbResult.Image == null) {
                    MessageBox.Show("Sorry, No photo has been captured.");
                    return;
                }

                btnUploadPhoto.Text = "Uploading...";
                btnUploadPhoto.Enabled = false;
                string fileName = GetImageFileNameByPosition();
                pbResult.Image.Save(fileName, ImageFormat.Jpeg);
                System.IO.FileInfo oFileInfo = new System.IO.FileInfo(fileName);
                WebFileManager.FileUploadViaWebService(oFileInfo, this.UserId);
                MessageBox.Show("Photo has been uploaded to the server.");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally{
                btnUploadPhoto.Text = "Upload";
                btnUploadPhoto.Enabled = true;
            }
        }

    }

}