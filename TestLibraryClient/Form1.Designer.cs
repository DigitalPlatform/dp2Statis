namespace TestLibraryClient
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuItem_file = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_file_exit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage_settings = new System.Windows.Forms.TabPage();
            this.checkBox_settings_patron = new System.Windows.Forms.CheckBox();
            this.textBox_settings_password = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_settings_userName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_settings_serverUrl = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage_login = new System.Windows.Forms.TabPage();
            this.textBox_login_parameters = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button_login_login = new System.Windows.Forms.Button();
            this.textBox_login_password = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_login_userName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tabPage_getRecord = new System.Windows.Forms.TabPage();
            this.textBox_getRecord_path = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.button_getRecord_request = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage_settings.SuspendLayout();
            this.tabPage_login.SuspendLayout();
            this.tabPage_getRecord.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_file});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 36);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MenuItem_file
            // 
            this.MenuItem_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_file_exit});
            this.MenuItem_file.Name = "MenuItem_file";
            this.MenuItem_file.Size = new System.Drawing.Size(97, 32);
            this.MenuItem_file.Text = "文件(&F)";
            // 
            // MenuItem_file_exit
            // 
            this.MenuItem_file_exit.Name = "MenuItem_file_exit";
            this.MenuItem_file_exit.Size = new System.Drawing.Size(199, 40);
            this.MenuItem_file_exit.Text = "退出(&X)";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.toolStrip1.Location = new System.Drawing.Point(0, 36);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.statusStrip1.Location = new System.Drawing.Point(0, 428);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage_settings);
            this.tabControl1.Controls.Add(this.tabPage_login);
            this.tabControl1.Controls.Add(this.tabPage_getRecord);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 61);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(800, 367);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage_settings
            // 
            this.tabPage_settings.Controls.Add(this.checkBox_settings_patron);
            this.tabPage_settings.Controls.Add(this.textBox_settings_password);
            this.tabPage_settings.Controls.Add(this.label3);
            this.tabPage_settings.Controls.Add(this.textBox_settings_userName);
            this.tabPage_settings.Controls.Add(this.label2);
            this.tabPage_settings.Controls.Add(this.comboBox_settings_serverUrl);
            this.tabPage_settings.Controls.Add(this.label1);
            this.tabPage_settings.Location = new System.Drawing.Point(4, 37);
            this.tabPage_settings.Name = "tabPage_settings";
            this.tabPage_settings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_settings.Size = new System.Drawing.Size(792, 326);
            this.tabPage_settings.TabIndex = 0;
            this.tabPage_settings.Text = "设置";
            this.tabPage_settings.UseVisualStyleBackColor = true;
            // 
            // checkBox_settings_patron
            // 
            this.checkBox_settings_patron.AutoSize = true;
            this.checkBox_settings_patron.Location = new System.Drawing.Point(116, 196);
            this.checkBox_settings_patron.Name = "checkBox_settings_patron";
            this.checkBox_settings_patron.Size = new System.Drawing.Size(122, 32);
            this.checkBox_settings_patron.TabIndex = 6;
            this.checkBox_settings_patron.Text = "读者身份";
            this.checkBox_settings_patron.UseVisualStyleBackColor = true;
            // 
            // textBox_settings_password
            // 
            this.textBox_settings_password.Location = new System.Drawing.Point(116, 144);
            this.textBox_settings_password.Name = "textBox_settings_password";
            this.textBox_settings_password.PasswordChar = '*';
            this.textBox_settings_password.Size = new System.Drawing.Size(333, 34);
            this.textBox_settings_password.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 147);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 28);
            this.label3.TabIndex = 4;
            this.label3.Text = "密码:";
            // 
            // textBox_settings_userName
            // 
            this.textBox_settings_userName.Location = new System.Drawing.Point(116, 104);
            this.textBox_settings_userName.Name = "textBox_settings_userName";
            this.textBox_settings_userName.Size = new System.Drawing.Size(333, 34);
            this.textBox_settings_userName.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 107);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 28);
            this.label2.TabIndex = 2;
            this.label2.Text = "用户名:";
            // 
            // comboBox_settings_serverUrl
            // 
            this.comboBox_settings_serverUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_settings_serverUrl.FormattingEnabled = true;
            this.comboBox_settings_serverUrl.Location = new System.Drawing.Point(8, 50);
            this.comboBox_settings_serverUrl.Name = "comboBox_settings_serverUrl";
            this.comboBox_settings_serverUrl.Size = new System.Drawing.Size(776, 36);
            this.comboBox_settings_serverUrl.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(235, 28);
            this.label1.TabIndex = 0;
            this.label1.Text = "dp2library 服务器 URL:";
            // 
            // tabPage_login
            // 
            this.tabPage_login.Controls.Add(this.textBox_login_parameters);
            this.tabPage_login.Controls.Add(this.label6);
            this.tabPage_login.Controls.Add(this.button_login_login);
            this.tabPage_login.Controls.Add(this.textBox_login_password);
            this.tabPage_login.Controls.Add(this.label4);
            this.tabPage_login.Controls.Add(this.textBox_login_userName);
            this.tabPage_login.Controls.Add(this.label5);
            this.tabPage_login.Location = new System.Drawing.Point(4, 37);
            this.tabPage_login.Name = "tabPage_login";
            this.tabPage_login.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_login.Size = new System.Drawing.Size(792, 326);
            this.tabPage_login.TabIndex = 1;
            this.tabPage_login.Text = "Login()";
            this.tabPage_login.UseVisualStyleBackColor = true;
            // 
            // textBox_login_parameters
            // 
            this.textBox_login_parameters.Location = new System.Drawing.Point(179, 114);
            this.textBox_login_parameters.Name = "textBox_login_parameters";
            this.textBox_login_parameters.Size = new System.Drawing.Size(333, 34);
            this.textBox_login_parameters.TabIndex = 12;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 117);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(158, 28);
            this.label6.TabIndex = 11;
            this.label6.Text = "strParameters:";
            // 
            // button_login_login
            // 
            this.button_login_login.Location = new System.Drawing.Point(179, 184);
            this.button_login_login.Name = "button_login_login";
            this.button_login_login.Size = new System.Drawing.Size(131, 40);
            this.button_login_login.TabIndex = 10;
            this.button_login_login.Text = "登录";
            this.button_login_login.UseVisualStyleBackColor = true;
            this.button_login_login.Click += new System.EventHandler(this.button_login_login_Click);
            // 
            // textBox_login_password
            // 
            this.textBox_login_password.Location = new System.Drawing.Point(179, 74);
            this.textBox_login_password.Name = "textBox_login_password";
            this.textBox_login_password.PasswordChar = '*';
            this.textBox_login_password.Size = new System.Drawing.Size(333, 34);
            this.textBox_login_password.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 77);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 28);
            this.label4.TabIndex = 8;
            this.label4.Text = "密码:";
            // 
            // textBox_login_userName
            // 
            this.textBox_login_userName.Location = new System.Drawing.Point(179, 34);
            this.textBox_login_userName.Name = "textBox_login_userName";
            this.textBox_login_userName.Size = new System.Drawing.Size(333, 34);
            this.textBox_login_userName.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 37);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 28);
            this.label5.TabIndex = 6;
            this.label5.Text = "用户名:";
            // 
            // tabPage_getRecord
            // 
            this.tabPage_getRecord.Controls.Add(this.button_getRecord_request);
            this.tabPage_getRecord.Controls.Add(this.textBox_getRecord_path);
            this.tabPage_getRecord.Controls.Add(this.label7);
            this.tabPage_getRecord.Location = new System.Drawing.Point(4, 37);
            this.tabPage_getRecord.Name = "tabPage_getRecord";
            this.tabPage_getRecord.Size = new System.Drawing.Size(792, 326);
            this.tabPage_getRecord.TabIndex = 2;
            this.tabPage_getRecord.Text = "GetRecord()";
            this.tabPage_getRecord.UseVisualStyleBackColor = true;
            // 
            // textBox_getRecord_path
            // 
            this.textBox_getRecord_path.Location = new System.Drawing.Point(173, 34);
            this.textBox_getRecord_path.Name = "textBox_getRecord_path";
            this.textBox_getRecord_path.Size = new System.Drawing.Size(333, 34);
            this.textBox_getRecord_path.TabIndex = 9;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 37);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(101, 28);
            this.label7.TabIndex = 8;
            this.label7.Text = "记录路径:";
            // 
            // button_getRecord_request
            // 
            this.button_getRecord_request.Location = new System.Drawing.Point(173, 129);
            this.button_getRecord_request.Name = "button_getRecord_request";
            this.button_getRecord_request.Size = new System.Drawing.Size(131, 40);
            this.button_getRecord_request.TabIndex = 11;
            this.button_getRecord_request.Text = "请求";
            this.button_getRecord_request.UseVisualStyleBackColor = true;
            this.button_getRecord_request.Click += new System.EventHandler(this.button_getRecord_request_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "TestLibraryClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage_settings.ResumeLayout(false);
            this.tabPage_settings.PerformLayout();
            this.tabPage_login.ResumeLayout(false);
            this.tabPage_login.PerformLayout();
            this.tabPage_getRecord.ResumeLayout(false);
            this.tabPage_getRecord.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStrip toolStrip1;
        private StatusStrip statusStrip1;
        private TabControl tabControl1;
        private TabPage tabPage_settings;
        private TabPage tabPage_login;
        private ComboBox comboBox_settings_serverUrl;
        private Label label1;
        private TextBox textBox_settings_password;
        private Label label3;
        private TextBox textBox_settings_userName;
        private Label label2;
        private TextBox textBox_login_password;
        private Label label4;
        private TextBox textBox_login_userName;
        private Label label5;
        private Button button_login_login;
        private TextBox textBox_login_parameters;
        private Label label6;
        private CheckBox checkBox_settings_patron;
        private ToolStripMenuItem MenuItem_file;
        private ToolStripMenuItem MenuItem_file_exit;
        private TabPage tabPage_getRecord;
        private TextBox textBox_getRecord_path;
        private Label label7;
        private Button button_getRecord_request;
    }
}