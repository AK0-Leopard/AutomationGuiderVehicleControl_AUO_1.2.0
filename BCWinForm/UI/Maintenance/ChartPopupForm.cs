using com.mirle.ibg3k0.bc.winform.App;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class ChartPopupForm : Form
    {
        BCMainForm form = null;
        BCApplication bcApp = null;
        public ChartPopupForm(BCMainForm _form)
        {
            InitializeComponent();
            form = _form;
            //uctlReserveSectionView1.Start(form.BCApp);
            bcApp = _form.BCApp;
            uc_SP_Chart1.Start(bcApp);
        }

        private void ChartPopupForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            form.removeForm(this.Name);
        }
    }
}
