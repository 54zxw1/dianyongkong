namespace ANCA_HMI.SystemInfo
{
    partial class Form_Dianbiao
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lb_title = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1_pic = new System.Windows.Forms.TextBox();
            this.textBox2_met = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn_Modbus_quit = new System.Windows.Forms.Button();
            this.button1_write = new System.Windows.Forms.Button();
            this.button_ture = new System.Windows.Forms.Button();
            this.button3_false = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lb_title
            // 
            this.lb_title.BackColor = System.Drawing.Color.Transparent;
            this.lb_title.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold);
            this.lb_title.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lb_title.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lb_title.Location = new System.Drawing.Point(0, 0);
            this.lb_title.Name = "lb_title";
            this.lb_title.Size = new System.Drawing.Size(535, 45);
            this.lb_title.TabIndex = 32;
            this.lb_title.Text = "Modbus";
            this.lb_title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 15F);
            this.label1.Location = new System.Drawing.Point(22, 55);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 20);
            this.label1.TabIndex = 34;
            this.label1.Text = "2.拍照";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 15F);
            this.label2.Location = new System.Drawing.Point(22, 105);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 20);
            this.label2.TabIndex = 35;
            this.label2.Text = "6.管材种类";
            // 
            // textBox1_pic
            // 
            this.textBox1_pic.Font = new System.Drawing.Font("宋体", 15F);
            this.textBox1_pic.Location = new System.Drawing.Point(154, 50);
            this.textBox1_pic.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1_pic.Name = "textBox1_pic";
            this.textBox1_pic.Size = new System.Drawing.Size(110, 30);
            this.textBox1_pic.TabIndex = 38;
            // 
            // textBox2_met
            // 
            this.textBox2_met.Font = new System.Drawing.Font("宋体", 15F);
            this.textBox2_met.Location = new System.Drawing.Point(154, 100);
            this.textBox2_met.Margin = new System.Windows.Forms.Padding(2);
            this.textBox2_met.Name = "textBox2_met";
            this.textBox2_met.Size = new System.Drawing.Size(110, 30);
            this.textBox2_met.TabIndex = 39;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button3_false);
            this.groupBox1.Controls.Add(this.button_ture);
            this.groupBox1.Controls.Add(this.button1_write);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.textBox1_pic);
            this.groupBox1.Controls.Add(this.textBox2_met);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox1.Font = new System.Drawing.Font("宋体", 15F);
            this.groupBox1.Location = new System.Drawing.Point(0, 49);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(587, 398);
            this.groupBox1.TabIndex = 42;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "变量监控";
            // 
            // btn_Modbus_quit
            // 
            this.btn_Modbus_quit.Location = new System.Drawing.Point(459, 21);
            this.btn_Modbus_quit.Name = "btn_Modbus_quit";
            this.btn_Modbus_quit.Size = new System.Drawing.Size(75, 23);
            this.btn_Modbus_quit.TabIndex = 43;
            this.btn_Modbus_quit.Text = "退出";
            this.btn_Modbus_quit.UseVisualStyleBackColor = true;
            this.btn_Modbus_quit.Click += new System.EventHandler(this.btn_Modbus_quit_Click);
            // 
            // button1_write
            // 
            this.button1_write.Location = new System.Drawing.Point(26, 306);
            this.button1_write.Name = "button1_write";
            this.button1_write.Size = new System.Drawing.Size(125, 34);
            this.button1_write.TabIndex = 56;
            this.button1_write.Text = "写";
            this.button1_write.UseVisualStyleBackColor = true;
            this.button1_write.Click += new System.EventHandler(this.button1_Click);
            // 
            // button_ture
            // 
            this.button_ture.Location = new System.Drawing.Point(350, 251);
            this.button_ture.Name = "button_ture";
            this.button_ture.Size = new System.Drawing.Size(99, 34);
            this.button_ture.TabIndex = 57;
            this.button_ture.Text = "真1";
            this.button_ture.UseVisualStyleBackColor = true;
            this.button_ture.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3_false
            // 
            this.button3_false.Location = new System.Drawing.Point(350, 306);
            this.button3_false.Name = "button3_false";
            this.button3_false.Size = new System.Drawing.Size(99, 34);
            this.button3_false.TabIndex = 58;
            this.button3_false.Text = "假2";
            this.button3_false.UseVisualStyleBackColor = true;
            this.button3_false.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form_Dianbiao
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.ClientSize = new System.Drawing.Size(587, 447);
            this.Controls.Add(this.btn_Modbus_quit);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lb_title);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form_Dianbiao";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form_LiuLiangJi";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Label lb_title;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1_pic;
        private System.Windows.Forms.TextBox textBox2_met;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btn_Modbus_quit;
        private System.Windows.Forms.Button button1_write;
        private System.Windows.Forms.Button button_ture;
        private System.Windows.Forms.Button button3_false;
    }
}