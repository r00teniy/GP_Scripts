using System;
using System.Windows.Forms;

namespace P_Volumes
{
    public partial class hCheckForm : Form
    {
        MyCommands mcom = new MyCommands();
        public hCheckForm()
        {
            InitializeComponent();
        }

        private void bSelfInt_Click(object sender, EventArgs e)
        {
            mcom.CheckHatch();
            errLabel.Visible = false;
        }

        private void bHatInt_Click(object sender, EventArgs e)
        {
            try
            {
                mcom.HatInt();
            }
            catch
            {
                errLabel.Visible = true;
            }
        }
        private void clearButton_Click(object sender, EventArgs e)
        {
            mcom.ClearTemp();
        }
        private void closeButton_Click(object sender, EventArgs e)
        {
            hCheckForm obj = (hCheckForm)Application.OpenForms["hCheckForm"];
            obj.Close();
        }

        private void bAddCheck_Click(object sender, EventArgs e)
        {
            mcom.hCheck();
        }
    }
}
