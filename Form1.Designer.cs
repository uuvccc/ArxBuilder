using System;
using System.Drawing;
using System.Windows.Forms;

namespace ArxBuilder;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    private Label lblTitle;
    private Label lblCppPath;
    private TextBox txtCppPath;
    private Button btnBrowseCpp;
    private Label lblSdkPath;
    private TextBox txtSdkPath;
    private Button btnBrowseSdk;
    private Button btnBuild;
    private RichTextBox rtbLog;
    private Panel pnlStatus;
    private Label lblCMakeStatus;
    private Label lblMSBuildStatus;
    private Button btnRefreshTools;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblCppPath = new Label();
        txtCppPath = new TextBox();
        btnBrowseCpp = new Button();
        lblSdkPath = new Label();
        txtSdkPath = new TextBox();
        btnBrowseSdk = new Button();
        btnBuild = new Button();
        rtbLog = new RichTextBox();
        pnlStatus = new Panel();
        lblCMakeStatus = new Label();
        lblMSBuildStatus = new Label();
        btnRefreshTools = new Button();
        pnlStatus.SuspendLayout();
        SuspendLayout();

        // === Form ===
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(600, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ObjectARX Builder";
        BackColor = Color.White;
        FormClosing += (s, e) => SaveSettings();

        // === Title ===
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(0, 80, 160);
        lblTitle.Location = new Point(20, 16);
        lblTitle.Text = "ObjectARX \u2014 One-Click Builder";

        // === Cpp Path ===
        lblCppPath.AutoSize = true;
        lblCppPath.Font = new Font("Segoe UI", 9F);
        lblCppPath.Location = new Point(22, 54);
        lblCppPath.Text = "C++ Source File (.cpp):";

        txtCppPath.Font = new Font("Consolas", 9F);
        txtCppPath.Location = new Point(22, 74);
        txtCppPath.Size = new Size(590, 23);
        txtCppPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;


        btnBrowseCpp.Font = new Font("Segoe UI", 9F);
        btnBrowseCpp.Location = new Point(620, 72);
        btnBrowseCpp.Size = new Size(78, 27);
        btnBrowseCpp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseCpp.Text = "Browse...";
        btnBrowseCpp.UseVisualStyleBackColor = true;
        btnBrowseCpp.Click += btnBrowseCpp_Click;

        // === SDK Path ===
        lblSdkPath.AutoSize = true;
        lblSdkPath.Font = new Font("Segoe UI", 9F);
        lblSdkPath.Location = new Point(22, 108);
        lblSdkPath.Text = "ObjectARX SDK Path:";

        txtSdkPath.Font = new Font("Consolas", 9F);
        txtSdkPath.Location = new Point(22, 128);
        txtSdkPath.Size = new Size(590, 23);
        txtSdkPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSdkPath.Text = "";


        btnBrowseSdk.Font = new Font("Segoe UI", 9F);
        btnBrowseSdk.Location = new Point(620, 126);
        btnBrowseSdk.Size = new Size(78, 27);
        btnBrowseSdk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseSdk.Text = "Browse...";
        btnBrowseSdk.UseVisualStyleBackColor = true;
        btnBrowseSdk.Click += btnBrowseSdk_Click;

        // === Status Panel ===
        pnlStatus.BackColor = Color.FromArgb(245, 245, 250);
        pnlStatus.Location = new Point(0, 162);
        pnlStatus.Size = new Size(720, 34);
        pnlStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        lblCMakeStatus.AutoSize = true;
        lblCMakeStatus.Font = new Font("Segoe UI", 8.5F);
        lblCMakeStatus.ForeColor = Color.Gray;
        lblCMakeStatus.Location = new Point(22, 9);
        lblCMakeStatus.Text = "CMake: checking...";

        lblMSBuildStatus.AutoSize = true;
        lblMSBuildStatus.Font = new Font("Segoe UI", 8.5F);
        lblMSBuildStatus.ForeColor = Color.Gray;
        lblMSBuildStatus.Location = new Point(170, 9);
        lblMSBuildStatus.Text = "MSBuild: checking...";

        btnRefreshTools.Font = new Font("Segoe UI", 8F);
        btnRefreshTools.FlatStyle = FlatStyle.Flat;
        btnRefreshTools.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 210);
        btnRefreshTools.Location = new Point(618, 5);
        btnRefreshTools.Size = new Size(80, 24);
        btnRefreshTools.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRefreshTools.Text = "Refresh";
        btnRefreshTools.UseVisualStyleBackColor = true;
        btnRefreshTools.Click += btnRefreshTools_Click;

        pnlStatus.Controls.Add(lblCMakeStatus);
        pnlStatus.Controls.Add(lblMSBuildStatus);
        pnlStatus.Controls.Add(btnRefreshTools);

        // === Log TextBox ===
        rtbLog.Font = new Font("Consolas", 9F);
        rtbLog.Location = new Point(20, 206);
        rtbLog.Size = new Size(680, 276);
        rtbLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        rtbLog.BackColor = Color.FromArgb(30, 30, 30);
        rtbLog.ForeColor = Color.FromArgb(200, 200, 200);
        rtbLog.ReadOnly = true;
        rtbLog.BorderStyle = BorderStyle.FixedSingle;

        // === Build Button ===
        btnBuild.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
        btnBuild.Location = new Point(20, 492);
        btnBuild.Size = new Size(680, 44);
        btnBuild.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        btnBuild.Text = "Build .arx";
        btnBuild.BackColor = Color.FromArgb(0, 100, 200);
        btnBuild.ForeColor = Color.White;
        btnBuild.FlatStyle = FlatStyle.Flat;
        btnBuild.FlatAppearance.BorderSize = 0;
        btnBuild.Cursor = Cursors.Hand;
        btnBuild.UseVisualStyleBackColor = false;
        btnBuild.Click += btnBuild_Click;

        // === Add Controls ===
        Controls.Add(lblTitle);
        Controls.Add(lblCppPath);
        Controls.Add(txtCppPath);
        Controls.Add(btnBrowseCpp);
        Controls.Add(lblSdkPath);
        Controls.Add(txtSdkPath);
        Controls.Add(btnBrowseSdk);
        Controls.Add(pnlStatus);
        Controls.Add(rtbLog);
        Controls.Add(btnBuild);

        pnlStatus.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
