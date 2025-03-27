using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dianyongkong4._7._2
{
    public partial class PasswordInputForm : Form
    {
        public string Password { get; private set; }
        public PasswordInputForm()
        {
            InitializeComponent();
            this.Text = "请输入密码";
            textBox1.UseSystemPasswordChar = true;
            button1.Text = "确定";
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Password = textBox1.Text;
            this.Close();
        }
    }
}
