namespace Jackett.Tray
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemWebUI = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.backgroundMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serviceControlMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItemAutoStart = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemShutdown = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Jackett";
            this.notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemWebUI,
            this.toolStripSeparator1,
            this.backgroundMenuItem,
            this.serviceControlMenuItem,
            this.toolStripSeparator2,
            this.toolStripMenuItemAutoStart,
            this.toolStripMenuItemShutdown});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(292, 148);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // toolStripMenuItemWebUI
            // 
            this.toolStripMenuItemWebUI.Name = "toolStripMenuItemWebUI";
            this.toolStripMenuItemWebUI.Size = new System.Drawing.Size(291, 22);
            this.toolStripMenuItemWebUI.Text = "Open Web UI";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(288, 6);
            // 
            // backgroundMenuItem
            // 
            this.backgroundMenuItem.Enabled = false;
            this.backgroundMenuItem.Name = "backgroundMenuItem";
            this.backgroundMenuItem.Size = new System.Drawing.Size(291, 22);
            this.backgroundMenuItem.Text = "Jacket is running as a background service";
            // 
            // serviceControlMenuItem
            // 
            this.serviceControlMenuItem.Name = "serviceControlMenuItem";
            this.serviceControlMenuItem.Size = new System.Drawing.Size(291, 22);
            this.serviceControlMenuItem.Text = "Start Service";
            this.serviceControlMenuItem.Click += new System.EventHandler(this.serviceControlMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(288, 6);
            // 
            // toolStripMenuItemAutoStart
            // 
            this.toolStripMenuItemAutoStart.CheckOnClick = true;
            this.toolStripMenuItemAutoStart.Name = "toolStripMenuItemAutoStart";
            this.toolStripMenuItemAutoStart.Size = new System.Drawing.Size(291, 22);
            this.toolStripMenuItemAutoStart.Text = "Auto-start on boot";
            this.toolStripMenuItemAutoStart.Visible = false;
            // 
            // toolStripMenuItemShutdown
            // 
            this.toolStripMenuItemShutdown.Name = "toolStripMenuItemShutdown";
            this.toolStripMenuItemShutdown.Size = new System.Drawing.Size(291, 22);
            this.toolStripMenuItemShutdown.Text = "Shutdown";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(326, 176);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Main";
            this.ShowInTaskbar = false;
            this.Text = "Jackett";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemAutoStart;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemWebUI;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemShutdown;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem backgroundMenuItem;
        private System.Windows.Forms.ToolStripMenuItem serviceControlMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    }
}