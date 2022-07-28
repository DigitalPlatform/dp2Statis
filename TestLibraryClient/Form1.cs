
using System.Reflection;
using System.Security.Cryptography.Xml;

using DigitalPlatform.Core;
using DigitalPlatform.Text;
using DigitalPlatform.LibraryClientOpenApi;

namespace TestLibraryClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }


        private async void button_login_login_Click(object sender, EventArgs e)
        {
            var request = new LoginRequest();
            var result = await Program.libraryService.Login(request);
        }

        ConfigSetting? _config = null;

        static string GetFileName()
        {
            string? DataDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(DataDir, "settings.xml");
        }

        void LoadConfig()
        {
            var fileName = GetFileName();
            _config = new ConfigSetting(fileName, true);

            this.comboBox_settings_serverUrl.Text = _config.Get(
    "libraryServer",
    "Url");
            this.textBox_settings_userName.Text = _config.Get(
                "libraryServer",
                "userName");
            var password = _config.Get(
                "libraryServer",
                "password");
            this.textBox_settings_password.Text = Utility.DecryptPasssword(password);
        }

        void SaveConfig()
        {
            _config.Set(
                "libraryServer",
                "Url",
                this.textBox_cfg_dp2LibraryServerUrl.Text);

            _config.Set(
               "libraryServer",
               "userName",
               this.textBox_cfg_userName.Text);

            var password = Utility.EncryptPassword(this.textBox_cfg_password.Text);
            _config.Set(
            "libraryServer",
            "password",
            password);

            _config.Set(
        "libraryServer",
        "clientLocation",
        this.textBox_cfg_location.Text);

            _config.Set(
        "libraryServer",
        "replicationStart",
        this.textBox_replicationStart.Text);

            /*
            _config.Set(
"palm",
"registerScans",
this.textBox_palm_registerScans.Text);

            _config.Set(
"palm",
"identityThreshold",
this.textBox_palm_identityThreshold.Text);

            _config.Set(
"palm",
"registerQualityThreshold",
this.textBox_palm_registerQualityThreshold.Text);

            _config.Set(
"palm",
"identityQualityThreshold",
this.textBox_palm_identityQualityThreshold.Text);
            */

            if (_config.Changed)
                _config.Save();
        }

        public static string DecryptPasssword(string strEncryptedText)
        {
            if (String.IsNullOrEmpty(strEncryptedText) == false)
            {
                try
                {
                    string strPassword = Cryptography.Decrypt(
        strEncryptedText,
        EncryptKey);
                    return strPassword;
                }
                catch
                {
                    return "errorpassword";
                }
            }

            return "";
        }

        public static string EncryptPassword(string strPlainText)
        {
            return Cryptography.Encrypt(strPlainText, EncryptKey);
        }
    }
}
}