using System;
using System.Threading;
using System.Windows.Forms;
namespace Memory_Editor
{
    public partial class MemoryEditor : Form
    {
        public MemoryEditor()
        {
            InitializeComponent();
        }
        private readonly Resources t = new Resources();
        private int PCSX2Version;
        private Resources.CheatList l;
        private Resources.CheatBlock l2;
        private Resources.BaseEditor z;
        private Resources.CheatEngine x;
        private void button1_Click(object sender, EventArgs e)
        {
            if (t.ConnectPCSX2())
            {
                if (pCSX2Dis15ToolStripMenuItem.Checked)
                {
                    PCSX2Version = 1;
                }
                else if (pCSX216ToolStripMenuItem.Checked)
                {
                    PCSX2Version = 2;
                }
                else if (pCSX217ToolStripMenuItem.Checked)
                {
                    PCSX2Version = 3;
                }
                else
                {
                    MessageBox.Show("Please Select Your PCSX2 Version First", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                z = new Resources.BaseEditor();
                x = new Resources.CheatEngine(z, t.SetM(), 0x20000000);
                l = new Resources.CheatList(textBox1.Text);
                l.Parse(textBox1.Text);
                l2 = Resources.CheatBlock.Parse(textBox1.Text);
                if (l2.Cheats.Count == 0)
                {
                    MessageBox.Show("Error Writting Codes", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                try
                {
                    if (t.TestR())
                    {
                        x.PatchMemory(l);
                        t.ResetEE(PCSX2Version);
                        timer2.Start();
                        MessageBox.Show("Codes Sent Successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error Writting Codes", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch
                {
                    MessageBox.Show("Error Writting Codes", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!t.ConnectPCSX2())
            {
                MessageBox.Show("PCSX2 Not Connected", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void connectToPCSX2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (t.pcsx2running)
            {
                MessageBox.Show("PCSX2 Is Already Attached!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (t.ConnectPCSX2())
            {
                label2.Text = "Connected";
                t.pcsx2running = true;
                t.disconnected = false;
                MessageBox.Show("PCSX2 Connected", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                timer1.Start();
                button1.Enabled = true;
            }
            else if (!t.ConnectPCSX2())
            {
                label2.Text = "Disconnected";
                t.pcsx2running = false;
                MessageBox.Show("Unable To Attach To PCSX2. Make Sure It Is Opened", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                timer1.Stop();
                button1.Enabled = false;
            }
        }
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (t.pcsx2running)
            {
                t.disconnected = true;
                t.pcsx2running = false;
                timer1.Stop();
                timer2.Stop();
                label2.Text = "Disconnected";
                MessageBox.Show("PCSX2 Successfully Disconnected", "Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button1.Enabled = false;
            }
            else if (!t.pcsx2running)
            {
                MessageBox.Show("PCSX2 Is Not Attached", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void exitProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dispose();
            Close();
        }
        private void pCSX2Dis15ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pCSX2Dis15ToolStripMenuItem.Checked = true;
            pCSX217ToolStripMenuItem.Checked = false;
            pCSX216ToolStripMenuItem.Checked = false;
        }
        private void pCSX216ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pCSX216ToolStripMenuItem.Checked = true;
            pCSX217ToolStripMenuItem.Checked = false;
            pCSX2Dis15ToolStripMenuItem.Checked = false;
        }
        private void pCSX217ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pCSX217ToolStripMenuItem.Checked = true;
            pCSX216ToolStripMenuItem.Checked = false;
            pCSX2Dis15ToolStripMenuItem.Checked = false;

        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy != true)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }
        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (t.ConnectPCSX2())
            {
                t.pcsx2running = true;
                t.disconnected = false;
                backgroundWorker1.ReportProgress(100);
            }
            else if (!t.ConnectPCSX2())
            {
                backgroundWorker1.ReportProgress(100);
                t.pcsx2running = false;
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            while (!t.pcsx2running)
            {
                WindowState = FormWindowState.Minimized;
                Show();
                WindowState = FormWindowState.Normal;
                Resources.FlashWindow.TrayAndWindow(this);
                t.pcsx2running = false;
                timer1.Stop();
                timer2.Stop();
                backgroundWorker2.CancelAsync();
                backgroundWorker3.CancelAsync();
                label2.Text = "Disconnected";
                DialogResult result = MessageBox.Show("PCSX2 Connection Lost!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Resources.FlashWindow.Stop(this);
                    button1.Enabled = false;
                    return;
                }
            }
            if (t.pcsx2running)
            {
                t.pcsx2running = true;
                t.disconnected = false;
                label2.Text = "Connected";
                button1.Enabled = true;
            }
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (backgroundWorker2.IsBusy != true)
            {
                backgroundWorker2.RunWorkerAsync();
            }
        }
        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {  
            if (t.pcsx2running)
            {
                if (l != null && x != null && t.SetM() != null)
                {
                    try
                    {
                        if (t.TestR())
                        {
                            {
                                if (backgroundWorker2.CancellationPending)
                                {
                                    e.Cancel = true;
                                    return;
                                }
                                x.PatchMemory(l);
                            }
                        }
                        else
                        {
                            backgroundWorker2.ReportProgress(100);
                        }
                    }
                    catch
                    {
                        backgroundWorker2.ReportProgress(100);
                    }
                }
            }
            else
            {
                backgroundWorker2.ReportProgress(100);
            }
        }
        private void backgroundWorker2_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (t.pcsx2running)
            {
                while (!t.TestR())
                {
                    timer2.Stop();
                    backgroundWorker2.CancelAsync();
                    backgroundWorker3.CancelAsync();
                    WindowState = FormWindowState.Minimized;
                    Show();
                    WindowState = FormWindowState.Normal;
                    Resources.FlashWindow.TrayAndWindow(this);
                    DialogResult result = MessageBox.Show("Game Not Detected. Codes Have Stopped Being Written", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (result == DialogResult.OK)
                    {
                        Resources.FlashWindow.Stop(this);
                        button1.Enabled = false;
                        return;
                    }
                }
            }
        }
        private void MemoryEditor_Load(object sender, EventArgs e)
        {
            button1.Enabled = false;
        }
        private void informationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Memory Editor For PCSX2. " +
              "\n Works With Multiple Versions Of PCSX2." +
              "\n Supports Multiple Code Types. 0x00, 0x10, 0x20, 0x50, 0x70," +
              "\n 0xB0, 0xC0, 0xD0." +
              "\n\nCredits: Bismofunyuns. Along With Help From NightFyre, And Others Whose Names Will Not Be Mention Since They Are A Whiny Bitch.", "Features", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void timer3_Tick(object sender, EventArgs e)
        {
            if (backgroundWorker3.IsBusy != true)
            {
                backgroundWorker3.RunWorkerAsync();
            }
        }
        private void backgroundWorker3_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // No longer implemented.
            if (l != null && x != null && t.SetM() != null)
            {
                if (backgroundWorker3.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                Thread.Sleep(520000);
                t.ResetEE(PCSX2Version);
            }
            else
            {
                return;
            }
        }
    }
}
