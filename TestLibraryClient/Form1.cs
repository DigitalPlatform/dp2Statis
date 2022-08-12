
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
            LoadConfig();

            Program.libraryService.BeforeLogin += (s, e) =>
            {
                if (e.FirstTry == false)
                {
                    e.Cancel = true;
                    return;
                }
                e.UserName = this.textBox_login_userName.Text;
                e.Password = this.textBox_login_password.Text;
                e.Parameters = "client=testLibraryClient|0.01";
            };
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveConfig();
        }


        private async void button_login_login_Click(object sender, EventArgs e)
        {
            Program.libraryService.Url = this.comboBox_settings_serverUrl.Text;

            var request = new LoginRequest { 
            StrUserName = this.textBox_login_userName.Text,
            StrPassword = this.textBox_login_password.Text,
            StrParameters = this.textBox_login_parameters.Text,
            };
            var result = await Program.libraryService.LoginAsync(request);

            MessageBox.Show(this, $"Value={result.LoginResult.Value}, ErrorInfo={result.LoginResult.ErrorInfo}, ErrorCode={result.LoginResult.ErrorCode}");
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

            // Page settings

            this.comboBox_settings_serverUrl.Text = _config.Get(
    "libraryServer",
    "Url");
            this.textBox_settings_userName.Text = _config.Get(
                "libraryServer",
                "userName");
            {
                var password = _config.Get(
                    "libraryServer",
                    "password");
                this.textBox_settings_password.Text = DecryptPasssword(password);
            }

            // Page login
            this.textBox_login_userName.Text = _config.Get(
    "login",
    "userName");
            {
                var password = _config.Get(
                    "login",
                    "password");
                this.textBox_login_password.Text = DecryptPasssword(password);
            }
            this.textBox_login_parameters.Text = _config.Get(
"login",
"parameters");

            // Page getRecord
            this.textBox_getRecord_path.Text = _config.Get(
"getRecord",
"path");
        }

        void SaveConfig()
        {
            if (_config == null)
                return;

            // Page settings

            _config.Set(
                "libraryServer",
                "Url",
                this.comboBox_settings_serverUrl.Text);

            _config.Set(
               "libraryServer",
               "userName",
               this.textBox_settings_userName.Text);

            {
                var password = EncryptPassword(this.textBox_settings_password.Text);
                _config.Set(
                "libraryServer",
                "password",
                password);
            }

            // Page login
            _config.Set(
    "login",
    "userName",
    this.textBox_login_userName.Text);
            {
                var password = EncryptPassword(this.textBox_login_password.Text);
                _config.Set(
                "login",
                "password",
                password);
            }
            _config.Set(
"login",
"parameters",
this.textBox_login_parameters.Text);

            // Page getRecord
            _config.Set(
"getRecord",
"path",
this.textBox_getRecord_path.Text);

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

        static string EncryptKey = "testlibraryclient_password_key";

        private async void button_getRecord_request_Click(object sender, EventArgs e)
        {
            /*
            Program.libraryService.Url = this.comboBox_settings_serverUrl.Text;

            var request = new GetRecordRequest
            {
                StrPath = this.textBox_getRecord_path.Text,
            };
            var result = await Program.libraryService.GetRecordAsync(request);

            MessageBox.Show(this, $"Value={result.GetRecordResult.Value}, ErrorInfo={result.GetRecordResult.ErrorInfo}, ErrorCode={result.GetRecordResult.ErrorCode}, Xml={result.StrXml}");
            */

            /*
            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.comboBox_settings_serverUrl.Text;
            var request = new GetRecordRequest
            {
                StrPath = this.textBox_getRecord_path.Text,
            };
            var result = await channel.GetRecordAsync(request);

            MessageBox.Show(this, $"Value={result.GetRecordResult.Value}, ErrorInfo={result.GetRecordResult.ErrorInfo}, ErrorCode={result.GetRecordResult.ErrorCode}, Xml={result.StrXml}");
            */
        }
    }
}