using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProclickEmployeeLib
{
    public partial class CredentialsForm : Form
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public CredentialsForm()
        {
            InitializeComponent();
            AcceptButton = loginButton;            
        }

        public void SetText(string headerText = "", string statusText = "")
        {
            HeaderText.Text = headerText;
            HeaderText.ForeColor = Color.Red;
            StatusText.Text = statusText;
        }

        private void Button1_Click(object sender, EventArgs e)
        {            
            if(UsernameInput.Text.Length > 0 && PasswordInput.Text.Length > 0)
            {
                Username = UsernameInput.Text;
                Password = PasswordInput.Text;
                this.Close();
            }
        }

        private void CredentialsForm_Load(object sender, EventArgs e)
        {
            UsernameInput.Text = "";
            PasswordInput.Text = "";
        }

        private void CredentialsForm_Shown(object sender, EventArgs e)
        {
            CenterToScreen();
            UsernameInput.Focus();
        }
    }
}
