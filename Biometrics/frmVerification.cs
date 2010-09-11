﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
namespace zsi.Biometrics
{
    public partial class frmVerification : Form 
    {
        public FingersData Data { get; set; }
        public frmVerification()
        {
            InitializeComponent();

        }

        public void OnComplete(object Control, DPFP.FeatureSet FeatureSet, ref DPFP.Gui.EventHandlerStatus Status)
        {
            if (Data == null) {
                Status = DPFP.Gui.EventHandlerStatus.Failure;
                return;
            }


            DPFP.Verification.Verification ver = new DPFP.Verification.Verification();
            DPFP.Verification.Verification.Result res = new DPFP.Verification.Verification.Result();

            // Compare feature set with all stored templates.
            foreach (DPFP.Template template in Data.Templates)
            {
                // Get template from storage.
                if (template != null)
                {
                    // Compare feature set with particular template.
                    ver.Verify(FeatureSet, template, ref res);
                   // Data.IsFeatureSetMatched = res.Verified;
                   // Data.FalseAcceptRate = res.FARAchieved;
                    if (res.Verified)
                        break; // success
                }
            }

            if (!res.Verified)
                Status = DPFP.Gui.EventHandlerStatus.Failure;

            //Data.Update();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmVerification_Load(object sender, EventArgs e)
        {
                //using (FileStream fs = File.OpenRead(open.FileName)
				//	DPFP.Template template = new DPFP.Template(fs);
        }
    }
}
