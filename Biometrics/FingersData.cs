using System;
using System.Collections.Generic;
using System.Text;

namespace zsi.Biometrics
{
	public delegate void OnChangeHandler();	

	public class FingersData
    {
        public event OnChangeHandler DataChanged; 		
        public const int MaxFingers = 10;
        public System.Drawing.Image Image { get; set; }
        public DPFP.Template[] Templates = new DPFP.Template[MaxFingers];
        public DPFP.Sample[] Samples = new DPFP.Sample[MaxFingers];
        public void UpdateTemplates(int FingerPosition,DPFP.Template Template)
        {
            this.Templates[FingerPosition] = Template;
            if (DataChanged != null)this.DataChanged();            
        }
        public void UpdateSamples(int FingerPosition, DPFP.Sample Sample)
        {
            this.Samples[FingerPosition] = Sample;
            if (DataChanged != null) this.DataChanged();
        }
	}
}
