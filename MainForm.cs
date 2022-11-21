using System;
using System.Windows.Forms;

namespace GP_scripts
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void calcvolumes_Click(object sender, EventArgs e)
        {
            int a = Lines3015_1.Checked ? 1 : 2;
            int b = Lines208_1.Checked ? 1 : 2;
            int c = Lines4518_1.Checked ? 1 : 2;

            if (Xrefselect.SelectedIndex > -1)
            {
                MyCommands mcom = new MyCommands();
                mcom.DoCount(Xrefselect.SelectedItem.ToString(), a, b, c);
                MainForm obj = (MainForm)Application.OpenForms["MainForm"];
                obj.Close();
            }
            else
            {
                errLabel.Text = "Выберите внешнюю ссылку основы";
            }

        }
    }
}
