using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DemoViewer.Properties;
using PSDFile;


namespace DemoViewer
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            this.Text = Application.ProductName;
        }

        private void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
                e.Effect = DragDropEffects.All;
        }

        private void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;
            OpenFile(files[0]);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog();
            openDlg.DefaultExt = ".*";
            openDlg.CheckFileExists = true;
            openDlg.Title = Resources.openDlgTitle;
            openDlg.Filter = "All Files (*.*)|*.*";
            openDlg.FilterIndex = 1;
            if (openDlg.ShowDialog() == DialogResult.Cancel) return;
            OpenFile(openDlg.FileName);
        }

        private void OpenFile(string fileName)
        {
            try
            {
                Bitmap bmp = null;

                var psdFile = new PsdFile(fileName, new LoadContext());

                bmp = psdFile.BaseLayer.GetBitmap();

                if (bmp == null)
                    throw new ApplicationException(Resources.errorLoadFailed);

                pictureBox1.Image = bmp;
                pictureBox1.Size = bmp.Size;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        } 

    }
}
