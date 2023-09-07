using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Collections;

using DigitalPlatform.WinForm;

namespace TestReporting
{
    public partial class BuildReportControl : UserControl
    {
        public BuildReportControl()
        {
            InitializeComponent();

            // 2022/8/14
            SetDpiXY(DpiUtil.GetDpiXY(this));
        }

        const int _LINE_HEIGHT = 20;
        const int _LINE_SEP = 2;
        const int _LABEL_WIDTH = 120;
        const int _TEXTBOX_WIDTH = 120;

        int _lineHeight = _LINE_HEIGHT;
        int _lineSep = _LINE_SEP;
        int _labelWidth = _LABEL_WIDTH;
        int _textboxWidth = _TEXTBOX_WIDTH;

        internal SizeF DpiXY = new SizeF(96, 96);

        public void SetDpiXY(SizeF dpi_xy)
        {
            this.DpiXY = dpi_xy;

            _lineHeight = DpiUtil.GetScalingY(dpi_xy, _LINE_HEIGHT);
            _lineSep = DpiUtil.GetScalingY(dpi_xy, _LINE_SEP);

            _labelWidth = DpiUtil.GetScalingX(dpi_xy, _LABEL_WIDTH);
            _textboxWidth = DpiUtil.GetScalingX(dpi_xy, _TEXTBOX_WIDTH);
        }


        // 参数和控件关系对照表
        // 参数名 --> TextBox 对象
        Hashtable _control_table = new Hashtable();

        // 创建子控件
        public void CreateChildren(XmlElement parameters)
        {
            this.Controls.Clear();
            _control_table.Clear();

            int x = 0;
            int y = 0;
            XmlNodeList nodes = parameters.SelectNodes("parameter");
            foreach (XmlElement parameter in nodes)
            {
                string name = parameter.GetAttribute("name");

                Label label = new Label();
                label.Text = parameter.GetAttribute("comment");
                label.Location = new Point(x, y);
                label.Size = new Size(_labelWidth, _lineHeight);
                this.Controls.Add(label);

                TextBox textbox = new TextBox();
                textbox.Location = new Point(x + _labelWidth, y);
                textbox.Size = new Size(_textboxWidth, _lineHeight);
                this.Controls.Add(textbox);

                _control_table[name] = textbox;

                y += _lineHeight + _lineSep;
            }
        }

        public void SetValue(Hashtable param_table)
        {
            foreach (string name in param_table.Keys)
            {
                Control control = _control_table[name] as Control;
                if (control != null)
                    control.Text = param_table[name] as string;
            }
        }

        public void ClearValue()
        {
            foreach (string name in _control_table.Keys)
            {
                (_control_table[name] as Control).Text = "";
            }
        }

        public Hashtable GetValue()
        {
            Hashtable param_table = new Hashtable();
            foreach(string name in _control_table.Keys)
            {
                string value = (_control_table[name] as Control).Text;
                param_table[name] = value;
            }

            return param_table;
        }
    }
}
