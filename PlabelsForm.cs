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
    public partial class PlabelsForm : Form
    {
        public PlabelsForm()
        {
            InitializeComponent();
        }

        private void Plabelbutton_Click(object sender, EventArgs e)
        {
            string[] a = { pBox1.Text, pBox2.Text, pBox3.Text, pBox4.Text, pBox5.Text, pBox6.Text, pBox7.Text, pBox8.Text, pBox9.Text, pBox10.Text };

            if (Xrefselect.SelectedIndex > -1)
            {
                MyCommands mcom = new MyCommands();
                mcom.DoPlabel(Xrefselect.SelectedItem.ToString(), a);
                PlabelsForm obj = (PlabelsForm)Application.OpenForms["PlabelsForm"];
                obj.Close();
            }
            else
            {
                errLabel.Text = "Выберите внешнюю ссылку основы";
            }
        }
    }
}
