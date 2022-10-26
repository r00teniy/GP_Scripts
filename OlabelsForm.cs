using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace P_Volumes
{
    public partial class OlabelsForm : Form
    {
        public OlabelsForm()
        {
            InitializeComponent();
        }

        private void Plabelbutton_Click(object sender, EventArgs e)
        {
            MyCommands mcom = new MyCommands();
            try
            {
                mcom.DoOlabel(Tnumber.Text, Bnumber.Text, float.Parse(Tdist.Text), float.Parse(Bdist.Text));
                OlabelsForm obj = (OlabelsForm)Application.OpenForms["OlabelsForm"];
                obj.Close();
            }
            catch
            {
                errorLabel.Text = "Где-то ошибка, проверьте";
            }
        }
    }
}
