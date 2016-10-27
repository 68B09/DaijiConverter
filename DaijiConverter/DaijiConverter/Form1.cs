using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DaijiConverter
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			DaijiConverters.DaijiConverter conv = new DaijiConverters.DaijiConverter();
			conv.Append壱To千百十 = this.chk壱千.Checked;
			this.txtResult.Text = conv.MakeDaijiString(this.txtNumber.Text);
		}
	}
}
