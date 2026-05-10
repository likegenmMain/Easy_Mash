using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

static class Program
{
    static System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer();
    static System.Windows.Forms.Timer dragTimer = new System.Windows.Forms.Timer();
    static System.Windows.Forms.Timer animTimer1 = new System.Windows.Forms.Timer();
    static System.Windows.Forms.Timer animTimer2 = new System.Windows.Forms.Timer();
    static Button animBtn1 = null;
    static Button animBtn2 = null;
    static int animStartW1, animStartH1, animEndW1, animEndH1;
    static int animStartX1, animStartY1, animEndX1, animEndY1;
    static int animStartW2, animStartH2, animEndW2, animEndH2;
    static int animStartX2, animStartY2, animEndX2, animEndY2;
    static int animStep1 = 0, animStep2 = 0;
    static int animTotal = 15;
    static Form form;
    static bool dragging = false;
    static Point dragPoint;
    static Point targetLocation;
    static Label injectStatusLabel;
    static bool browserAttached = false;
    static string attachedBrowserName = "";

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        if (!CheckDotNet8())
        {
            DialogResult result = MessageBox.Show(".NET 8.0 Runtime not found!\n\nDownload now?", "Easy Mash", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                Process.Start("https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.0-windows-x64-installer");
            return;
        }

        form = new Form { Text = "", Size = new Size(550, 280), StartPosition = FormStartPosition.CenterScreen, BackColor = Color.Black, FormBorderStyle = FormBorderStyle.None, Opacity = 0 };

        GraphicsPath path = new GraphicsPath(); path.AddArc(0, 0, 20, 20, 180, 90); path.AddArc(form.Width - 20, 0, 20, 20, 270, 90); path.AddArc(form.Width - 20, form.Height - 20, 20, 20, 0, 90); path.AddArc(0, form.Height - 20, 20, 20, 90, 90);
        form.Region = new Region(path);

        System.Windows.Forms.Timer fadeIn = new System.Windows.Forms.Timer { Interval = 16 };
        int fadeStep = 0; int fadeTotal = 120;
        fadeIn.Tick += (s, e) => { fadeStep++; form.Opacity = (double)fadeStep / fadeTotal; if (fadeStep >= fadeTotal) { form.Opacity = 1; fadeIn.Stop(); } };
        form.Load += (s, e) => fadeIn.Start();

        Panel topBar = new Panel { Size = new Size(form.Width, 30), Location = new Point(0, 0), BackColor = Color.Black };
        Button closeBtn = new Button { Text = "x", Size = new Size(30, 30), Location = new Point(form.Width - 30, 0), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 10f) };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(200, 40, 40); closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Black;
        closeBtn.Click += (s, e) => { if (MessageBox.Show("Close Easy Mash?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Application.Exit(); };
        topBar.Controls.Add(closeBtn);

        topBar.MouseDown += (s, e) => { dragging = true; dragPoint = e.Location; };
        topBar.MouseMove += (s, e) => { if (dragging) { targetLocation = new Point(Cursor.Position.X - dragPoint.X, Cursor.Position.Y - dragPoint.Y); if (!dragTimer.Enabled) { dragTimer.Interval = 16; dragTimer.Tick += DragTick; dragTimer.Start(); } } };
        topBar.MouseUp += (s, e) => { dragging = false; dragTimer.Stop(); };

        animTimer.Interval = 16; animTimer.Tick += AnimTickMain;
        animTimer1.Interval = 16; animTimer1.Tick += AnimTick1;
        animTimer2.Interval = 16; animTimer2.Tick += AnimTick2;

        BuildMainMenu();
        form.Controls.Add(topBar);
        Application.Run(form);
    }

    static void BuildMainMenu()
    {
        for (int i = form.Controls.Count - 1; i >= 0; i--)
        {
            if (!(form.Controls[i] is Panel)) form.Controls.RemoveAt(i);
        }

        form.Size = new Size(550, 280);
        int btnW = 140, btnH = 45, gap = 12;
        int totalW = btnW * 3 + gap * 2;
        int startX = (form.Width - totalW) / 2;
        int startY = (form.Height - btnH) / 2;

        injectStatusLabel = new Label { Text = "Browser: Not attached", Location = new Point(form.Width / 2 - 60, startY - 30), AutoSize = true, ForeColor = Color.FromArgb(150, 150, 160), Font = new Font("Segoe UI", 8f), BackColor = Color.Transparent };

        Button aiBtn = MakeBtn("AI", startX, startY, btnW, btnH);
        Button mainBtn = MakeBtn("MAIN", startX + btnW + gap, startY, btnW, btnH);
        mainBtn.Click += (s, e) => { if (browserAttached) OpenMainForm(); else MessageBox.Show("Attach to browser first!", "Easy Mash", MessageBoxButtons.OK, MessageBoxIcon.Warning); };
        Button helpBtn = MakeBtn("HELP", startX + (btnW + gap) * 2, startY, btnW, btnH);

        Button injectBtn = new Button { Text = "Inject", Size = new Size(100, 32), Location = new Point(form.Width - 125, form.Height - 47), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        injectBtn.FlatAppearance.BorderSize = 1; injectBtn.FlatAppearance.BorderColor = Color.White; injectBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 20, 20);
        injectBtn.Click += (s, e) => AttachToBrowser();

        Label versionLabel = new Label { Text = "Version 0.05 (BETA)", Location = new Point(15, form.Height - 45), AutoSize = true, ForeColor = Color.FromArgb(100, 100, 110), Font = new Font("Segoe UI", 7f), BackColor = Color.Transparent };

        form.Controls.Add(injectStatusLabel); form.Controls.Add(aiBtn); form.Controls.Add(mainBtn); form.Controls.Add(helpBtn); form.Controls.Add(injectBtn); form.Controls.Add(versionLabel);
    }

    static void AttachToBrowser()
    {
        string[] processes = { "yandex", "browser", "chrome", "msedge", "firefox", "opera" };
        string[] names = { "Yandex", "Yandex", "Chrome", "Edge", "Firefox", "Opera" };

        for (int i = 0; i < processes.Length; i++)
        {
            Process[] procs = Process.GetProcessesByName(processes[i]);
            if (procs.Length > 0)
            {
                IntPtr hwnd = procs[0].MainWindowHandle;
                if (hwnd != IntPtr.Zero)
                {
                    browserAttached = true; attachedBrowserName = names[i];
                    ShowWindow(hwnd, 9); SetForegroundWindow(hwnd);

                    Form overlay = new Form { Text = "", Size = new Size(380, 150), StartPosition = FormStartPosition.CenterScreen, BackColor = Color.Black, FormBorderStyle = FormBorderStyle.None, TopMost = true, ShowInTaskbar = false };
                    GraphicsPath op = new GraphicsPath(); op.AddArc(0, 0, 20, 20, 180, 90); op.AddArc(overlay.Width - 20, 0, 20, 20, 270, 90); op.AddArc(overlay.Width - 20, overlay.Height - 20, 20, 20, 0, 90); op.AddArc(0, overlay.Height - 20, 20, 20, 90, 90); overlay.Region = new Region(op);
                    overlay.Controls.Add(new Label { Text = "INJECTION SUCCESSFUL", Location = new Point(65, 25), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 15f, FontStyle.Bold), BackColor = Color.Transparent });
                    overlay.Controls.Add(new Label { Text = "Attached to " + names[i], Location = new Point(120, 60), AutoSize = true, ForeColor = Color.FromArgb(150, 150, 160), Font = new Font("Segoe UI", 10f), BackColor = Color.Transparent });
                    overlay.Controls.Add(new Label { Text = "Easy Mash v0.05 (BETA)", Location = new Point(115, 85), AutoSize = true, ForeColor = Color.FromArgb(80, 80, 90), Font = new Font("Segoe UI", 8f), BackColor = Color.Transparent });
                    Button okBtn = new Button { Text = "OK", Size = new Size(100, 32), Location = new Point(140, 110), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand };
                    okBtn.FlatAppearance.BorderSize = 1; okBtn.FlatAppearance.BorderColor = Color.White; okBtn.Click += (s2, e2) => overlay.Close(); overlay.Controls.Add(okBtn);
                    overlay.Show();

                    injectStatusLabel.Text = "Browser: " + names[i] + " (attached)"; injectStatusLabel.ForeColor = Color.FromArgb(0, 200, 100);
                    return;
                }
            }
        }
        injectStatusLabel.Text = "Browser: Not found"; injectStatusLabel.ForeColor = Color.FromArgb(200, 60, 60);
    }

    static void OpenMainForm()
    {
        Form mainForm = new Form { Text = "", Size = new Size(500, 400), StartPosition = FormStartPosition.CenterScreen, BackColor = Color.Black, FormBorderStyle = FormBorderStyle.None };
        GraphicsPath mp = new GraphicsPath(); mp.AddArc(0, 0, 20, 20, 180, 90); mp.AddArc(mainForm.Width - 20, 0, 20, 20, 270, 90); mp.AddArc(mainForm.Width - 20, mainForm.Height - 20, 20, 20, 0, 90); mp.AddArc(0, mainForm.Height - 20, 20, 20, 90, 90); mainForm.Region = new Region(mp);

        Panel topBar = new Panel { Size = new Size(mainForm.Width, 30), Location = new Point(0, 0), BackColor = Color.Black };
        Button closeBtn = new Button { Text = "x", Size = new Size(30, 30), Location = new Point(mainForm.Width - 30, 0), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 10f) };
        closeBtn.FlatAppearance.BorderSize = 0; closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(200, 40, 40); closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Black; closeBtn.Click += (s, e) => mainForm.Close();
        topBar.Controls.Add(closeBtn);

        bool mDragging = false; Point mDrag = Point.Empty;
        topBar.MouseDown += (s, e) => { mDragging = true; mDrag = e.Location; };
        topBar.MouseMove += (s, e) => { if (mDragging) mainForm.Location = new Point(Cursor.Position.X - mDrag.X, Cursor.Position.Y - mDrag.Y); };
        topBar.MouseUp += (s, e) => mDragging = false;

        string currentURL = GetActiveBrowserTitle();
        Label urlLabel = new Label { Text = string.IsNullOrEmpty(currentURL) ? "No page detected" : currentURL, Location = new Point(50, 50), Size = new Size(400, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter };

        System.Windows.Forms.Timer urlTimer = new System.Windows.Forms.Timer { Interval = 500 };
        urlTimer.Tick += (s, e) => { string u = GetActiveBrowserTitle(); if (!string.IsNullOrEmpty(u)) urlLabel.Text = u; };
        urlTimer.Start(); mainForm.FormClosed += (s, e) => urlTimer.Stop();

        // Open Portal button
        Button openPortalBtn = new Button { Text = "Open Portal", Size = new Size(200, 45), Location = new Point(150, 100), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 13f, FontStyle.Bold), Cursor = Cursors.Hand };
        openPortalBtn.FlatAppearance.BorderSize = 1; openPortalBtn.FlatAppearance.BorderColor = Color.White; openPortalBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 20, 20);
        openPortalBtn.Click += (s, e) => Process.Start(new ProcessStartInfo { FileName = "https://school.mos.ru", UseShellExecute = true });

        int pX = 150, pY = 100, pW = 200, pH = 45;
        openPortalBtn.Tag = new int[] { pX, pY, pW, pH };
        openPortalBtn.MouseEnter += (s, e) => { animBtn1 = openPortalBtn; int[] t = (int[])openPortalBtn.Tag; animStartW1 = openPortalBtn.Width; animEndW1 = t[2] + 16; animStartH1 = openPortalBtn.Height; animEndH1 = t[3] + 8; animStartX1 = openPortalBtn.Location.X; animEndX1 = t[0] - 8; animStartY1 = openPortalBtn.Location.Y; animEndY1 = t[1] - 4; animStep1 = 0; animTimer1.Start(); };
        openPortalBtn.MouseLeave += (s, e) => { animBtn1 = openPortalBtn; int[] t = (int[])openPortalBtn.Tag; animStartW1 = openPortalBtn.Width; animEndW1 = t[2]; animStartH1 = openPortalBtn.Height; animEndH1 = t[3]; animStartX1 = openPortalBtn.Location.X; animEndX1 = t[0]; animStartY1 = openPortalBtn.Location.Y; animEndY1 = t[1]; animStep1 = 0; animTimer1.Start(); };

        // Generate Auth Link button
        Button genLinkBtn = new Button { Text = "Generate Auth Link", Size = new Size(200, 45), Location = new Point(150, 165), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 13f, FontStyle.Bold), Cursor = Cursors.Hand };
        genLinkBtn.FlatAppearance.BorderSize = 1; genLinkBtn.FlatAppearance.BorderColor = Color.White; genLinkBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 20, 20);
        genLinkBtn.Click += (s, e) => new LinkGeneratorForm(attachedBrowserName).Show();

        int gX = 150, gY = 165, gW = 200, gH = 45;
        genLinkBtn.Tag = new int[] { gX, gY, gW, gH };
        genLinkBtn.MouseEnter += (s, e) => { animBtn2 = genLinkBtn; int[] t = (int[])genLinkBtn.Tag; animStartW2 = genLinkBtn.Width; animEndW2 = t[2] + 16; animStartH2 = genLinkBtn.Height; animEndH2 = t[3] + 8; animStartX2 = genLinkBtn.Location.X; animEndX2 = t[0] - 8; animStartY2 = genLinkBtn.Location.Y; animEndY2 = t[1] - 4; animStep2 = 0; animTimer2.Start(); };
        genLinkBtn.MouseLeave += (s, e) => { animBtn2 = genLinkBtn; int[] t = (int[])genLinkBtn.Tag; animStartW2 = genLinkBtn.Width; animEndW2 = t[2]; animStartH2 = genLinkBtn.Height; animEndH2 = t[3]; animStartX2 = genLinkBtn.Location.X; animEndX2 = t[0]; animStartY2 = genLinkBtn.Location.Y; animEndY2 = t[1]; animStep2 = 0; animTimer2.Start(); };

        mainForm.Controls.Add(urlLabel); mainForm.Controls.Add(openPortalBtn); mainForm.Controls.Add(genLinkBtn); mainForm.Controls.Add(topBar);
        mainForm.Show();
    }

    static string GetActiveBrowserTitle()
    {
        string[] browsers = { "yandex", "browser", "chrome", "msedge", "firefox", "opera" };
        foreach (string proc in browsers)
        {
            Process[] procs = Process.GetProcessesByName(proc);
            if (procs.Length > 0)
            {
                IntPtr hwnd = procs[0].MainWindowHandle;
                StringBuilder sb = new StringBuilder(512); GetWindowText(hwnd, sb, 512);
                string title = sb.ToString();
                if (!string.IsNullOrEmpty(title))
                {
                    int idx = title.LastIndexOf(" - ");
                    if (idx > 0) title = title.Substring(0, idx);
                    return title;
                }
            }
        }
        return "";
    }

    static bool CheckDotNet8()
    {
        try { ProcessStartInfo psi = new ProcessStartInfo { FileName = "dotnet", Arguments = "--list-runtimes", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }; Process p = Process.Start(psi); string o = p.StandardOutput.ReadToEnd(); p.WaitForExit(); return o.Contains("Microsoft.NETCore.App 8.") || o.Contains("Microsoft.WindowsDesktop.App 8."); }
        catch { return false; }
    }

    static void DragTick(object sender, EventArgs e)
    {
        float cx = form.Location.X, cy = form.Location.Y;
        float nx = cx + (targetLocation.X - cx) * 0.2f, ny = cy + (targetLocation.Y - cy) * 0.2f;
        if (Math.Abs(nx - targetLocation.X) < 0.5f && Math.Abs(ny - targetLocation.Y) < 0.5f) { form.Location = targetLocation; dragTimer.Stop(); return; }
        form.Location = new Point((int)nx, (int)ny);
    }

    static Button MakeBtn(string text, int bx, int by, int bw, int bh)
    {
        Button btn = new Button { Text = text, Size = new Size(bw, bh), Location = new Point(bx, by), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 13f), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter, Tag = new int[] { bx, by, bw, bh } };
        btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = Color.White; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 20, 20);
        btn.MouseEnter += (s, e) => { animBtn1 = btn; int[] t = (int[])btn.Tag; animStartW1 = btn.Width; animEndW1 = t[2] + 14; animStartH1 = btn.Height; animEndH1 = t[3] + 6; animStartX1 = btn.Location.X; animEndX1 = t[0] - 7; animStartY1 = btn.Location.Y; animEndY1 = t[1] - 3; animStep1 = 0; animTimer.Start(); };
        btn.MouseLeave += (s, e) => { animBtn1 = btn; int[] t = (int[])btn.Tag; animStartW1 = btn.Width; animEndW1 = t[2]; animStartH1 = btn.Height; animEndH1 = t[3]; animStartX1 = btn.Location.X; animEndX1 = t[0]; animStartY1 = btn.Location.Y; animEndY1 = t[1]; animStep1 = 0; animTimer.Start(); };
        return btn;
    }

    static void AnimTickMain(object sender, EventArgs e)
    {
        if (animBtn1 == null || animStep1 >= animTotal) { animTimer.Stop(); if (animBtn1 != null) { animBtn1.Size = new Size(animEndW1, animEndH1); animBtn1.Location = new Point(animEndX1, animEndY1); } animBtn1 = null; return; }
        animStep1++; float t = (float)animStep1 / animTotal; t = t * t * (3f - 2f * t);
        animBtn1.Size = new Size(animStartW1 + (int)((animEndW1 - animStartW1) * t), animStartH1 + (int)((animEndH1 - animStartH1) * t));
        animBtn1.Location = new Point(animStartX1 + (int)((animEndX1 - animStartX1) * t), animStartY1 + (int)((animEndY1 - animStartY1) * t));
    }

    static void AnimTick1(object sender, EventArgs e)
    {
        if (animBtn1 == null || animStep1 >= animTotal) { animTimer1.Stop(); if (animBtn1 != null) { animBtn1.Size = new Size(animEndW1, animEndH1); animBtn1.Location = new Point(animEndX1, animEndY1); } animBtn1 = null; return; }
        animStep1++; float t = (float)animStep1 / animTotal; t = t * t * (3f - 2f * t);
        animBtn1.Size = new Size(animStartW1 + (int)((animEndW1 - animStartW1) * t), animStartH1 + (int)((animEndH1 - animStartH1) * t));
        animBtn1.Location = new Point(animStartX1 + (int)((animEndX1 - animStartX1) * t), animStartY1 + (int)((animEndY1 - animStartY1) * t));
    }

    static void AnimTick2(object sender, EventArgs e)
    {
        if (animBtn2 == null || animStep2 >= animTotal) { animTimer2.Stop(); if (animBtn2 != null) { animBtn2.Size = new Size(animEndW2, animEndH2); animBtn2.Location = new Point(animEndX2, animEndY2); } animBtn2 = null; return; }
        animStep2++; float t = (float)animStep2 / animTotal; t = t * t * (3f - 2f * t);
        animBtn2.Size = new Size(animStartW2 + (int)((animEndW2 - animStartW2) * t), animStartH2 + (int)((animEndH2 - animStartH2) * t));
        animBtn2.Location = new Point(animStartX2 + (int)((animEndX2 - animStartX2) * t), animStartY2 + (int)((animEndY2 - animStartY2) * t));
    }
}

public class LinkGeneratorForm : Form
{
    private TextBox txtResult, txtInputURL;
    private string browserName = "";

    public LinkGeneratorForm(string browser)
    {
        browserName = browser;
        this.Text = ""; this.Size = new Size(550, 380); this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = Color.Black; this.FormBorderStyle = FormBorderStyle.None;

        GraphicsPath path = new GraphicsPath(); path.AddArc(0, 0, 20, 20, 180, 90); path.AddArc(this.Width - 20, 0, 20, 20, 270, 90); path.AddArc(this.Width - 20, this.Height - 20, 20, 20, 0, 90); path.AddArc(0, this.Height - 20, 20, 20, 90, 90); this.Region = new Region(path);

        Panel topBar = new Panel { Size = new Size(this.Width, 30), Location = new Point(0, 0), BackColor = Color.Black };
        Button closeBtn = new Button { Text = "x", Size = new Size(30, 30), Location = new Point(this.Width - 30, 0), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 10f) };
        closeBtn.FlatAppearance.BorderSize = 0; closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(200, 40, 40); closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Black; closeBtn.Click += (s, e) => this.Close();
        topBar.Controls.Add(closeBtn);

        bool dragging = false; Point dragPoint = Point.Empty;
        topBar.MouseDown += (s, e) => { dragging = true; dragPoint = e.Location; };
        topBar.MouseMove += (s, e) => { if (dragging) this.Location = new Point(Cursor.Position.X - dragPoint.X, Cursor.Position.Y - dragPoint.Y); };
        topBar.MouseUp += (s, e) => dragging = false;

        Label lblResult = new Label { Text = "GENERATED LINK", Location = new Point(50, 45), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold), BackColor = Color.Transparent };
        txtResult = new TextBox { Location = new Point(50, 68), Size = new Size(450, 55), BackColor = Color.FromArgb(15, 15, 15), ForeColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8f), Multiline = true, ReadOnly = true };

        Button btnGenerate = MakeBtn("Generate New", 50, 135, 140, 34);
        btnGenerate.Click += (s, e) => GenerateNewLink();

        Button btnCopy = MakeBtn("Copy", 200, 135, 100, 34);
        btnCopy.Click += (s, e) => { if (!string.IsNullOrEmpty(txtResult.Text)) { Clipboard.SetText(txtResult.Text); MessageBox.Show("Link copied!", "Easy Mash", MessageBoxButtons.OK, MessageBoxIcon.Information); } };

        Button btnOpen = MakeBtn("Open in " + browserName, 310, 135, 190, 34);
        btnOpen.Click += (s, e) => { if (!string.IsNullOrEmpty(txtResult.Text)) Process.Start(new ProcessStartInfo { FileName = txtResult.Text, UseShellExecute = true }); };

        Label lblInput = new Label { Text = "RESPONSE URL", Location = new Point(50, 185), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold), BackColor = Color.Transparent };
        txtInputURL = new TextBox { Location = new Point(50, 208), Size = new Size(450, 35), BackColor = Color.FromArgb(15, 15, 15), ForeColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9f) };

        Button btnGo = MakeBtn("GO", 50, 258, 450, 40);
        btnGo.Click += (s, e) => { string u = txtInputURL.Text.Trim(); if (!string.IsNullOrEmpty(u)) { if (!u.StartsWith("http")) u = "https://" + u; Process.Start(new ProcessStartInfo { FileName = u, UseShellExecute = true }); } };

        this.Controls.Add(lblResult); this.Controls.Add(txtResult); this.Controls.Add(btnGenerate); this.Controls.Add(btnCopy); this.Controls.Add(btnOpen);
        this.Controls.Add(lblInput); this.Controls.Add(txtInputURL); this.Controls.Add(btnGo); this.Controls.Add(topBar);

        GenerateNewLink();
    }

    private Button MakeBtn(string text, int x, int y, int w, int h)
    {
        Button btn = new Button { Text = text, Size = new Size(w, h), Location = new Point(x, y), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = Color.White; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 20, 20);
        return btn;
    }

    private void GenerateNewLink()
    {
        byte[] verifierBytes = RandomNumberGenerator.GetBytes(32);
        string codeVerifier = Base64UrlEncode(verifierBytes);

        byte[] challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        string codeChallenge = Base64UrlEncode(challengeBytes);

        string boParams = "/sps/oauth/ae?" +
            "scope=" + Uri.EscapeDataString("openid profile snils birthday contacts blitz_qr_auth") +
            "&access_type=" + Uri.EscapeDataString("offline") +
            "&response_type=" + Uri.EscapeDataString("code") +
            "&client_id=" + Uri.EscapeDataString("dyn~dnevnik.mos.ru~5f644f0c-59f0-44bf-af7a-bf4ba7173909") +
            "&redirect_uri=" + Uri.EscapeDataString("https://dnevnik.mos.ru/auth/callback") +
            "&prompt=" + Uri.EscapeDataString("login") +
            "&code_challenge=" + Uri.EscapeDataString(codeChallenge) +
            "&code_challenge_method=" + Uri.EscapeDataString("S256");

        txtResult.Text = "https://login.mos.ru/sps/login/methods/password?bo=" + Uri.EscapeDataString(boParams);
    }

    private string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
