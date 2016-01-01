using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace agg_platform_win32
{
	public partial class WebStore : Form
	{
		private string storeUrl;

		public WebStore(string storeUrl)
		{
			InitializeComponent();
			this.storeUrl = storeUrl;
		}

		private void WebStore_Load(object sender, EventArgs e)
		{
			webBrowser1.Navigate(storeUrl);
		}
	}
}
