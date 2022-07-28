using System;
using System.Collections;

using System.IO;
using System.Text;

using System.Xml;
using System.Text.RegularExpressions;

namespace DigitalPlatform.Xml
{	
	public class Ns
	{
		public const string dc = "http://purl.org/dc/elements/1.1/";
		public const string xlink = "http://www.w3.org/1999/xlink";
		public const string xml = "http://www.w3.org/XML/1998/namespace";
        public const string usmarcxml = "http://www.loc.gov/MARC21/slim";
		public const string unimarcxml = "http://dp2003.com/UNIMARC";
		public const string marcxchange = "info:lc/xmlns/marcxchange-v1";	// 2022/6/17
	}

	public class DpNs
	{
		public const string dprms = "http://dp2003.com/dprms";
		public const string dpdc = "http://dp2003.com/dpdc";
		public const string unimarcxml = "http://dp2003.com/UNIMARC";
	}

}	
