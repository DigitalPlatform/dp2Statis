namespace TestReporting
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            menuStrip1 = new MenuStrip();
            MenuItem_file = new ToolStripMenuItem();
            MenuItem_buildPlan = new ToolStripMenuItem();
            MenuItem_continueExcutePlan = new ToolStripMenuItem();
            MenuItem_settings = new ToolStripMenuItem();
            MenuItem_exit = new ToolStripMenuItem();
            MenuItem_test = new ToolStripMenuItem();
            MenuItem_testCreateReport = new ToolStripMenuItem();
            MenuItem_trunAllTest = new ToolStripMenuItem();
            MenuItem_testDeleteBiblioRecord = new ToolStripMenuItem();
            MenuItem_testReplication = new ToolStripMenuItem();
            MenuItem_recreateBlankDatabase = new ToolStripMenuItem();
            MenuItem_report = new ToolStripMenuItem();
            MenuItem_createReport = new ToolStripMenuItem();
            toolStrip1 = new ToolStrip();
            toolStripButton_stop = new ToolStripButton();
            statusStrip1 = new StatusStrip();
            tabControl_main = new TabControl();
            tabPage_history = new TabPage();
            webBrowser1 = new WebBrowser();
            tabPage_config = new TabPage();
            textBox_replicationStart = new TextBox();
            label5 = new Label();
            checkBox_cfg_savePasswordLong = new CheckBox();
            textBox_cfg_location = new TextBox();
            label4 = new Label();
            textBox_cfg_password = new TextBox();
            textBox_cfg_userName = new TextBox();
            label3 = new Label();
            label2 = new Label();
            textBox_cfg_dp2LibraryServerUrl = new TextBox();
            label1 = new Label();
            toolStrip_server = new ToolStrip();
            toolStripButton_cfg_setXeServer = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripButton_cfg_setHongnibaServer = new ToolStripButton();
            menuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            tabControl_main.SuspendLayout();
            tabPage_history.SuspendLayout();
            tabPage_config.SuspendLayout();
            toolStrip_server.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { MenuItem_file, MenuItem_test, MenuItem_report });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(8, 3, 0, 3);
            menuStrip1.Size = new Size(1156, 39);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // MenuItem_file
            // 
            MenuItem_file.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_buildPlan, MenuItem_continueExcutePlan, MenuItem_settings, MenuItem_exit });
            MenuItem_file.Name = "MenuItem_file";
            MenuItem_file.Size = new Size(72, 33);
            MenuItem_file.Text = "文件";
            // 
            // MenuItem_buildPlan
            // 
            MenuItem_buildPlan.Name = "MenuItem_buildPlan";
            MenuItem_buildPlan.Size = new Size(318, 40);
            MenuItem_buildPlan.Text = "创建并执行同步计划";
            MenuItem_buildPlan.Click += MenuItem_buildPlan_Click;
            // 
            // MenuItem_continueExcutePlan
            // 
            MenuItem_continueExcutePlan.Name = "MenuItem_continueExcutePlan";
            MenuItem_continueExcutePlan.Size = new Size(318, 40);
            MenuItem_continueExcutePlan.Text = "继续执行同步计划";
            MenuItem_continueExcutePlan.Click += MenuItem_continueExcutePlan_Click;
            // 
            // MenuItem_settings
            // 
            MenuItem_settings.Name = "MenuItem_settings";
            MenuItem_settings.Size = new Size(318, 40);
            MenuItem_settings.Text = "设置 ...";
            MenuItem_settings.Click += MenuItem_settings_Click;
            // 
            // MenuItem_exit
            // 
            MenuItem_exit.Name = "MenuItem_exit";
            MenuItem_exit.Size = new Size(318, 40);
            MenuItem_exit.Text = "退出";
            MenuItem_exit.Click += MenuItem_exit_Click;
            // 
            // MenuItem_test
            // 
            MenuItem_test.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_testCreateReport, MenuItem_trunAllTest, MenuItem_testDeleteBiblioRecord, MenuItem_testReplication, MenuItem_recreateBlankDatabase });
            MenuItem_test.Name = "MenuItem_test";
            MenuItem_test.Size = new Size(72, 33);
            MenuItem_test.Text = "测试";
            // 
            // MenuItem_testCreateReport
            // 
            MenuItem_testCreateReport.Name = "MenuItem_testCreateReport";
            MenuItem_testCreateReport.Size = new Size(318, 40);
            MenuItem_testCreateReport.Text = "测试创建报表";
            MenuItem_testCreateReport.Click += MenuItem_testCreateReport_Click;
            // 
            // MenuItem_trunAllTest
            // 
            MenuItem_trunAllTest.Name = "MenuItem_trunAllTest";
            MenuItem_trunAllTest.Size = new Size(318, 40);
            MenuItem_trunAllTest.Text = "运行全部测试项目";
            MenuItem_trunAllTest.Click += MenuItem_runAllTest_Click;
            // 
            // MenuItem_testDeleteBiblioRecord
            // 
            MenuItem_testDeleteBiblioRecord.Name = "MenuItem_testDeleteBiblioRecord";
            MenuItem_testDeleteBiblioRecord.Size = new Size(318, 40);
            MenuItem_testDeleteBiblioRecord.Text = "测试删除书目记录";
            MenuItem_testDeleteBiblioRecord.Click += MenuItem_testDeleteBiblioRecord_Click;
            // 
            // MenuItem_testReplication
            // 
            MenuItem_testReplication.Name = "MenuItem_testReplication";
            MenuItem_testReplication.Size = new Size(318, 40);
            MenuItem_testReplication.Text = "测试同步";
            MenuItem_testReplication.Click += MenuItem_testReplication_Click;
            // 
            // MenuItem_recreateBlankDatabase
            // 
            MenuItem_recreateBlankDatabase.Name = "MenuItem_recreateBlankDatabase";
            MenuItem_recreateBlankDatabase.Size = new Size(318, 40);
            MenuItem_recreateBlankDatabase.Text = "重新创建空白数据库";
            MenuItem_recreateBlankDatabase.Click += MenuItem_recreateBlankDatabase_Click;
            // 
            // MenuItem_report
            // 
            MenuItem_report.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_createReport });
            MenuItem_report.Name = "MenuItem_report";
            MenuItem_report.Size = new Size(72, 33);
            MenuItem_report.Text = "报表";
            // 
            // MenuItem_createReport
            // 
            MenuItem_createReport.Name = "MenuItem_createReport";
            MenuItem_createReport.Size = new Size(234, 40);
            MenuItem_createReport.Text = "创建报表 ...";
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(24, 24);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton_stop });
            toolStrip1.Location = new Point(0, 39);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1156, 38);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton_stop
            // 
            toolStripButton_stop.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_stop.Enabled = false;
            toolStripButton_stop.Image = (Image)resources.GetObject("toolStripButton_stop.Image");
            toolStripButton_stop.ImageTransparentColor = Color.Magenta;
            toolStripButton_stop.Name = "toolStripButton_stop";
            toolStripButton_stop.Size = new Size(58, 32);
            toolStripButton_stop.Text = "停止";
            toolStripButton_stop.Click += toolStripButton_stop_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Location = new Point(0, 779);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(1156, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // tabControl_main
            // 
            tabControl_main.Controls.Add(tabPage_history);
            tabControl_main.Controls.Add(tabPage_config);
            tabControl_main.Dock = DockStyle.Fill;
            tabControl_main.Location = new Point(0, 77);
            tabControl_main.Margin = new Padding(5, 4, 5, 4);
            tabControl_main.Name = "tabControl_main";
            tabControl_main.SelectedIndex = 0;
            tabControl_main.Size = new Size(1156, 702);
            tabControl_main.TabIndex = 3;
            // 
            // tabPage_history
            // 
            tabPage_history.Controls.Add(webBrowser1);
            tabPage_history.Location = new Point(4, 37);
            tabPage_history.Margin = new Padding(5, 4, 5, 4);
            tabPage_history.Name = "tabPage_history";
            tabPage_history.Padding = new Padding(5, 4, 5, 4);
            tabPage_history.Size = new Size(1148, 661);
            tabPage_history.TabIndex = 0;
            tabPage_history.Text = "操作历史";
            tabPage_history.UseVisualStyleBackColor = true;
            // 
            // webBrowser1
            // 
            webBrowser1.Dock = DockStyle.Fill;
            webBrowser1.Location = new Point(5, 4);
            webBrowser1.Margin = new Padding(6, 7, 6, 7);
            webBrowser1.MinimumSize = new Size(34, 41);
            webBrowser1.Name = "webBrowser1";
            webBrowser1.Size = new Size(1138, 653);
            webBrowser1.TabIndex = 2;
            // 
            // tabPage_config
            // 
            tabPage_config.Controls.Add(textBox_replicationStart);
            tabPage_config.Controls.Add(label5);
            tabPage_config.Controls.Add(checkBox_cfg_savePasswordLong);
            tabPage_config.Controls.Add(textBox_cfg_location);
            tabPage_config.Controls.Add(label4);
            tabPage_config.Controls.Add(textBox_cfg_password);
            tabPage_config.Controls.Add(textBox_cfg_userName);
            tabPage_config.Controls.Add(label3);
            tabPage_config.Controls.Add(label2);
            tabPage_config.Controls.Add(textBox_cfg_dp2LibraryServerUrl);
            tabPage_config.Controls.Add(label1);
            tabPage_config.Controls.Add(toolStrip_server);
            tabPage_config.Location = new Point(4, 37);
            tabPage_config.Margin = new Padding(5, 4, 5, 4);
            tabPage_config.Name = "tabPage_config";
            tabPage_config.Padding = new Padding(5, 4, 5, 4);
            tabPage_config.Size = new Size(1148, 661);
            tabPage_config.TabIndex = 1;
            tabPage_config.Text = "配置参数";
            tabPage_config.UseVisualStyleBackColor = true;
            // 
            // textBox_replicationStart
            // 
            textBox_replicationStart.ImeMode = ImeMode.Off;
            textBox_replicationStart.Location = new Point(274, 549);
            textBox_replicationStart.Margin = new Padding(7, 8, 7, 8);
            textBox_replicationStart.Name = "textBox_replicationStart";
            textBox_replicationStart.Size = new Size(407, 34);
            textBox_replicationStart.TabIndex = 21;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(14, 553);
            label5.Margin = new Padding(7, 0, 7, 0);
            label5.Name = "label5";
            label5.Size = new Size(171, 28);
            label5.TabIndex = 20;
            label5.Text = "日志同步起点(&R):";
            // 
            // checkBox_cfg_savePasswordLong
            // 
            checkBox_cfg_savePasswordLong.AutoSize = true;
            checkBox_cfg_savePasswordLong.Location = new Point(20, 475);
            checkBox_cfg_savePasswordLong.Margin = new Padding(7, 8, 7, 8);
            checkBox_cfg_savePasswordLong.Name = "checkBox_cfg_savePasswordLong";
            checkBox_cfg_savePasswordLong.Size = new Size(147, 32);
            checkBox_cfg_savePasswordLong.TabIndex = 19;
            checkBox_cfg_savePasswordLong.Text = "保存密码(&L)";
            // 
            // textBox_cfg_location
            // 
            textBox_cfg_location.ImeMode = ImeMode.Off;
            textBox_cfg_location.Location = new Point(274, 404);
            textBox_cfg_location.Margin = new Padding(7, 8, 7, 8);
            textBox_cfg_location.Name = "textBox_cfg_location";
            textBox_cfg_location.Size = new Size(407, 34);
            textBox_cfg_location.TabIndex = 18;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(15, 409);
            label4.Margin = new Padding(7, 0, 7, 0);
            label4.Name = "label4";
            label4.Size = new Size(152, 28);
            label4.TabIndex = 17;
            label4.Text = "工作台号(&W)：";
            // 
            // textBox_cfg_password
            // 
            textBox_cfg_password.ImeMode = ImeMode.Off;
            textBox_cfg_password.Location = new Point(274, 329);
            textBox_cfg_password.Margin = new Padding(7, 8, 7, 8);
            textBox_cfg_password.Name = "textBox_cfg_password";
            textBox_cfg_password.PasswordChar = '*';
            textBox_cfg_password.Size = new Size(407, 34);
            textBox_cfg_password.TabIndex = 16;
            // 
            // textBox_cfg_userName
            // 
            textBox_cfg_userName.ImeMode = ImeMode.Off;
            textBox_cfg_userName.Location = new Point(274, 255);
            textBox_cfg_userName.Margin = new Padding(7, 8, 7, 8);
            textBox_cfg_userName.Name = "textBox_cfg_userName";
            textBox_cfg_userName.Size = new Size(407, 34);
            textBox_cfg_userName.TabIndex = 14;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(15, 335);
            label3.Margin = new Padding(7, 0, 7, 0);
            label3.Name = "label3";
            label3.Size = new Size(102, 28);
            label3.TabIndex = 15;
            label3.Text = "密码(&P)：";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 260);
            label2.Margin = new Padding(7, 0, 7, 0);
            label2.Name = "label2";
            label2.Size = new Size(126, 28);
            label2.TabIndex = 13;
            label2.Text = "用户名(&U)：";
            // 
            // textBox_cfg_dp2LibraryServerUrl
            // 
            textBox_cfg_dp2LibraryServerUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox_cfg_dp2LibraryServerUrl.Location = new Point(21, 68);
            textBox_cfg_dp2LibraryServerUrl.Margin = new Padding(7, 8, 7, 8);
            textBox_cfg_dp2LibraryServerUrl.Name = "textBox_cfg_dp2LibraryServerUrl";
            textBox_cfg_dp2LibraryServerUrl.Size = new Size(1107, 34);
            textBox_cfg_dp2LibraryServerUrl.TabIndex = 11;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 19);
            label1.Margin = new Padding(7, 0, 7, 0);
            label1.Name = "label1";
            label1.Size = new Size(240, 28);
            label1.TabIndex = 10;
            label1.Text = "dp2Library 服务器 URL:";
            // 
            // toolStrip_server
            // 
            toolStrip_server.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            toolStrip_server.AutoSize = false;
            toolStrip_server.Dock = DockStyle.None;
            toolStrip_server.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip_server.ImageScalingSize = new Size(24, 24);
            toolStrip_server.Items.AddRange(new ToolStripItem[] { toolStripButton_cfg_setXeServer, toolStripSeparator1, toolStripButton_cfg_setHongnibaServer });
            toolStrip_server.Location = new Point(21, 131);
            toolStrip_server.Name = "toolStrip_server";
            toolStrip_server.Size = new Size(1116, 79);
            toolStrip_server.TabIndex = 12;
            toolStrip_server.Text = "toolStrip1";
            // 
            // toolStripButton_cfg_setXeServer
            // 
            toolStripButton_cfg_setXeServer.Alignment = ToolStripItemAlignment.Right;
            toolStripButton_cfg_setXeServer.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_cfg_setXeServer.ImageTransparentColor = Color.Magenta;
            toolStripButton_cfg_setXeServer.Name = "toolStripButton_cfg_setXeServer";
            toolStripButton_cfg_setXeServer.Size = new Size(142, 73);
            toolStripButton_cfg_setXeServer.Text = "单机版服务器";
            toolStripButton_cfg_setXeServer.ToolTipText = "设为单机版服务器";
            toolStripButton_cfg_setXeServer.Click += toolStripButton_cfg_setXeServer_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Alignment = ToolStripItemAlignment.Right;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 79);
            // 
            // toolStripButton_cfg_setHongnibaServer
            // 
            toolStripButton_cfg_setHongnibaServer.Alignment = ToolStripItemAlignment.Right;
            toolStripButton_cfg_setHongnibaServer.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton_cfg_setHongnibaServer.ImageTransparentColor = Color.Magenta;
            toolStripButton_cfg_setHongnibaServer.Name = "toolStripButton_cfg_setHongnibaServer";
            toolStripButton_cfg_setHongnibaServer.Size = new Size(231, 73);
            toolStripButton_cfg_setHongnibaServer.Text = "红泥巴.数字平台服务器";
            toolStripButton_cfg_setHongnibaServer.ToolTipText = "设为红泥巴.数字平台服务器";
            toolStripButton_cfg_setHongnibaServer.Click += toolStripButton_cfg_setHongnibaServer_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1156, 801);
            Controls.Add(tabControl_main);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(5, 4, 5, 4);
            Name = "Form1";
            ShowIcon = false;
            Text = "TestReporting";
            FormClosing += Form1_FormClosing;
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            tabControl_main.ResumeLayout(false);
            tabPage_history.ResumeLayout(false);
            tabPage_config.ResumeLayout(false);
            tabPage_config.PerformLayout();
            toolStrip_server.ResumeLayout(false);
            toolStrip_server.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem MenuItem_file;
        private ToolStripMenuItem MenuItem_buildPlan;
        private ToolStripMenuItem MenuItem_exit;
        private ToolStripMenuItem MenuItem_test;
        private ToolStrip toolStrip1;
        private StatusStrip statusStrip1;
        private TabControl tabControl_main;
        private TabPage tabPage_history;
        private TabPage tabPage_config;
        public CheckBox checkBox_cfg_savePasswordLong;
        public TextBox textBox_cfg_location;
        private Label label4;
        public TextBox textBox_cfg_password;
        public TextBox textBox_cfg_userName;
        private Label label3;
        private Label label2;
        private TextBox textBox_cfg_dp2LibraryServerUrl;
        private Label label1;
        private ToolStrip toolStrip_server;
        private ToolStripButton toolStripButton_cfg_setXeServer;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton toolStripButton_cfg_setHongnibaServer;
        private WebBrowser webBrowser1;
        public TextBox textBox_replicationStart;
        private Label label5;
        private ToolStripMenuItem MenuItem_continueExcutePlan;
        private ToolStripMenuItem MenuItem_testCreateReport;
        private ToolStripMenuItem MenuItem_report;
        private ToolStripMenuItem MenuItem_createReport;
        private ToolStripButton toolStripButton_stop;
        private ToolStripMenuItem MenuItem_trunAllTest;
        private ToolStripMenuItem MenuItem_testDeleteBiblioRecord;
        private ToolStripMenuItem MenuItem_testReplication;
        private ToolStripMenuItem MenuItem_recreateBlankDatabase;
        private ToolStripMenuItem MenuItem_settings;
    }
}

