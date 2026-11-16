using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IPTray {
    public class HistoryRecord {
        public DateTime Timestamp;
        public string IP;
        public string CountryCode;
        public string Country;
        public string City;
        public string ISP;

        public string LocationString {
            get {
                if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country)) {
                    return City + ", " + Country;
                }
                if (!string.IsNullOrEmpty(Country)) {
                    return Country;
                }
                return CountryCode;
            }
        }
    }

    public static class HistoryManager {
        private static string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IPTray");
        private static string historyFile = Path.Combine(dataDir, "history.csv");
        private static List<HistoryRecord> records = new List<HistoryRecord>();
        private static object syncRoot = new object();

        static HistoryManager() {
            try {
                if (!Directory.Exists(dataDir)) {
                    Directory.CreateDirectory(dataDir);
                }
                Load();
            } catch { }
        }

        public static List<HistoryRecord> GetRecords() {
            lock (syncRoot) {
                return new List<HistoryRecord>(records);
            }
        }

        public static void AddRecord(string ip, string countryCode, string country, string city, string isp) {
            lock (syncRoot) {
                // Avoid logging identical consecutive entries
                if (records.Count > 0) {
                    var last = records[0];
                    if (last.IP == ip && last.CountryCode == countryCode && last.City == city) {
                        return;
                    }
                }

                var item = new HistoryRecord {
                    Timestamp = DateTime.Now,
                    IP = ip,
                    CountryCode = countryCode,
                    Country = country,
                    City = city,
                    ISP = isp
                };

                records.Insert(0, item);
                if (records.Count > 500) {
                    records.RemoveAt(records.Count - 1);
                }
                Save();
            }
        }

        public static void Clear() {
            lock (syncRoot) {
                records.Clear();
                try {
                    if (File.Exists(historyFile)) {
                        File.Delete(historyFile);
                    }
                } catch { }
            }
        }

        private static void Load() {
            if (!File.Exists(historyFile)) return;
            try {
                var lines = File.ReadAllLines(historyFile);
                records.Clear();
                foreach (var line in lines) {
                    if (string.IsNullOrEmpty(line)) continue;
                    var parts = line.Split('\t');
                    if (parts.Length >= 6) {
                        DateTime dt;
                        DateTime.TryParse(parts[0], out dt);
                        records.Add(new HistoryRecord {
                            Timestamp = dt,
                            IP = parts[1],
                            CountryCode = parts[2],
                            Country = parts[3],
                            City = parts[4],
                            ISP = parts[5]
                        });
                    }
                }
            } catch { }
        }

        private static void Save() {
            try {
                var lines = new List<string>();
                foreach (var r in records) {
                    lines.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                        r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        r.IP, r.CountryCode, r.Country, r.City, r.ISP));
                }
                File.WriteAllLines(historyFile, lines.ToArray());
            } catch { }
        }
    }

    public class HistoryForm : Form {
        private ListView listView;
        private Label lblCurrentIP;
        private Label lblCurrentLocation;
        private Button btnCopy;
        private Button btnClear;
        private Button btnClose;
        private Button btnRefresh;
        private TaskbarWidget ownerWidget;

        public HistoryForm(TaskbarWidget owner) {
            this.ownerWidget = owner;
            this.Text = "IPTray - تاریخچه اتصالات و لوکیشن‌ها";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(580, 480);
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            InitializeUI();
            RefreshData();
        }

        private void InitializeUI() {
            // Header card
            Panel card = new Panel {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(36, 36, 36),
                Padding = new Padding(15)
            };

            lblCurrentIP = new Label {
                Text = "IP: ...",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };

            lblCurrentLocation = new Label {
                Text = "موقعیت: ...",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(160, 210, 255),
                Location = new Point(16, 45),
                AutoSize = true
            };

            btnCopy = new Button {
                Text = "📋 کپی IP فعلی",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 36),
                Location = new Point(415, 22),
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += delegate {
                if (!string.IsNullOrEmpty(ownerWidget.CurrentIP)) {
                    Clipboard.SetText(ownerWidget.CurrentIP);
                    btnCopy.Text = "✔ کپی شد!";
                    var t = new System.Windows.Forms.Timer { Interval = 1500 };
                    t.Tick += delegate {
                        btnCopy.Text = "📋 کپی IP فعلی";
                        t.Stop();
                        t.Dispose();
                    };
                    t.Start();
                }
            };

            card.Controls.Add(lblCurrentIP);
            card.Controls.Add(lblCurrentLocation);
            card.Controls.Add(btnCopy);

            // History ListView
            listView = new ListView {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(22, 22, 22),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f)
            };

            listView.Columns.Add("زمان تغییر", 145);
            listView.Columns.Add("آی‌پی (IP)", 125);
            listView.Columns.Add("منطقه و کشور", 155);
            listView.Columns.Add("ارائه‌دهنده (ISP)", 135);

            listView.DoubleClick += delegate {
                if (listView.SelectedItems.Count > 0) {
                    string ip = listView.SelectedItems[0].SubItems[1].Text;
                    Clipboard.SetText(ip);
                    MessageBox.Show(this, "آی‌پی " + ip + " در کلیپ‌بورد کپی شد.", "کپی شد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // Bottom bar
            Panel bottom = new Panel {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(32, 32, 32),
                Padding = new Padding(10)
            };

            btnClear = new Button {
                Text = "🗑 پاک کردن تاریخچه",
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(240, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(135, 30),
                Location = new Point(12, 10),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += delegate {
                if (MessageBox.Show(this, "آیا از پاک کردن کامل تاریخچه اتصالات اطمینان دارید؟", "تأیید", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    HistoryManager.Clear();
                    RefreshData();
                }
            };

            btnRefresh = new Button {
                Text = "🔄 بروزرسانی",
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 30),
                Location = new Point(155, 10),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += delegate {
                ownerWidget.TriggerRefresh();
                Thread.Sleep(800);
                RefreshData();
            };

            btnClose = new Button {
                Text = "بستن",
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(470, 10),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += delegate { this.Close(); };

            bottom.Controls.Add(btnClear);
            bottom.Controls.Add(btnRefresh);
            bottom.Controls.Add(btnClose);

            this.Controls.Add(listView);
            this.Controls.Add(bottom);
            this.Controls.Add(card);

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) this.Close();
            };
        }

        public void RefreshData() {
            lblCurrentIP.Text = "IP: " + ownerWidget.CurrentIP + " [" + ownerWidget.CurrentCountryCode + "]";
            lblCurrentLocation.Text = "موقعیت: " + ownerWidget.CurrentLocationString + " | ارائه‌دهنده: " + ownerWidget.CurrentISP;

            listView.Items.Clear();
            var list = HistoryManager.GetRecords();
            foreach (var item in list) {
                var lvi = new ListViewItem(item.Timestamp.ToString("yyyy/MM/dd HH:mm:ss"));
                lvi.SubItems.Add(item.IP);
                lvi.SubItems.Add(item.LocationString + " (" + item.CountryCode + ")");
                lvi.SubItems.Add(item.ISP ?? "");
                listView.Items.Add(lvi);
            }
        }
    }

    public class TaskbarWidget : Form {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
                return cp;
            }
        }

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem itemStatus;
        private ToolStripMenuItem itemIP;
        private ToolStripMenuItem itemLocation;
        private ToolStripMenuItem itemISP;
        private ToolStripMenuItem itemStartup;

        private System.Windows.Forms.Timer dockTimer;
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.Timer copyFeedbackTimer;

        public string CurrentIP { get; private set; }
        public string CurrentCountry { get; private set; }
        public string CurrentCity { get; private set; }
        public string CurrentISP { get; private set; }
        public string CurrentCountryCode { get; private set; }
        public string CurrentLocationString { get; private set; }

        private bool isCopyFeedback = false;
        private bool isHovered = false;
        private bool isDragging = false;
        private Point dragStartPoint;
        private int customX = -1;

        private HistoryForm activeHistoryForm = null;

        private const string AppName = "IPTray";
        private const string RegKeyPath = @"Software\IPTray";
        private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public TaskbarWidget() {
            CurrentIP = "در حال بررسی...";
            CurrentLocationString = "در حال دریافت منطقه...";
            CurrentCountryCode = "..";

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(26, 26, 26);
            this.Height = 38;
            this.Width = 180;

            LoadSettings();

            contextMenu = new ContextMenuStrip();
            contextMenu.Font = new Font("Segoe UI", 9.5f);

            itemStatus = new ToolStripMenuItem("وضعیت: در حال بررسی...") { Enabled = false };
            itemIP = new ToolStripMenuItem("IP: Loading...") { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            itemLocation = new ToolStripMenuItem("Location: -") { Enabled = false };
            itemISP = new ToolStripMenuItem("ISP: -") { Enabled = false };

            var itemHistory = new ToolStripMenuItem("📜 مشاهده تاریخچه اتصالات", null, delegate { ShowHistory(); });
            var itemCopy = new ToolStripMenuItem("📋 کپی IP", null, delegate { CopyIP(); });
            var itemRefresh = new ToolStripMenuItem("🔄 بروزرسانی الان", null, delegate { RefreshIPAsync(); });
            var itemResetPos = new ToolStripMenuItem("🎯 بازنشانی موقعیت روی نوار", null, delegate { ResetPosition(); });

            itemStartup = new ToolStripMenuItem("🚀 اجرای خودکار در استارت‌آپ ویندوز") {
                Checked = IsStartupEnabled(),
                CheckOnClick = true
            };
            itemStartup.Click += delegate { ToggleStartup(itemStartup.Checked); };

            var itemExit = new ToolStripMenuItem("❌ خروج", null, delegate {
                Application.Exit();
            });

            contextMenu.Items.Add(itemStatus);
            contextMenu.Items.Add(itemIP);
            contextMenu.Items.Add(itemLocation);
            contextMenu.Items.Add(itemISP);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(itemHistory);
            contextMenu.Items.Add(itemCopy);
            contextMenu.Items.Add(itemRefresh);
            contextMenu.Items.Add(itemResetPos);
            contextMenu.Items.Add(itemStartup);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(itemExit);

            this.ContextMenuStrip = contextMenu;

            // Timer to keep widget docked on taskbar
            dockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            dockTimer.Tick += delegate { AlignToTaskbar(); };
            dockTimer.Start();

            // Periodic IP check every 60 seconds
            refreshTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            refreshTimer.Tick += delegate { RefreshIPAsync(); };
            refreshTimer.Start();

            // Copy feedback timer
            copyFeedbackTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            copyFeedbackTimer.Tick += delegate {
                isCopyFeedback = false;
                copyFeedbackTimer.Stop();
                this.Invalidate();
            };

            // Network change listener
            NetworkChange.NetworkAddressChanged += delegate {
                Thread.Sleep(1200);
                RefreshIPAsync();
            };

            AlignToTaskbar();
            RefreshIPAsync();
        }

        public void TriggerRefresh() {
            RefreshIPAsync();
        }

        public void ShowHistory() {
            if (activeHistoryForm != null && !activeHistoryForm.IsDisposed) {
                activeHistoryForm.BringToFront();
                activeHistoryForm.Focus();
                return;
            }

            activeHistoryForm = new HistoryForm(this);

            // Position right above widget
            int x = this.Left + this.Width / 2 - activeHistoryForm.Width / 2;
            int y = this.Top - activeHistoryForm.Height - 8;

            // Ensure on screen
            var screen = Screen.FromControl(this);
            if (x < screen.WorkingArea.Left + 10) x = screen.WorkingArea.Left + 10;
            if (x + activeHistoryForm.Width > screen.WorkingArea.Right - 10) x = screen.WorkingArea.Right - activeHistoryForm.Width - 10;
            if (y < screen.WorkingArea.Top + 10) y = this.Bottom + 8;

            activeHistoryForm.Location = new Point(x, y);
            activeHistoryForm.Show();
        }

        private void AlignToTaskbar() {
            if (isDragging) return;

            IntPtr trayHwnd = FindWindow("Shell_TrayWnd", null);
            if (trayHwnd == IntPtr.Zero) return;

            RECT rect;
            if (!GetWindowRect(trayHwnd, out rect)) return;

            int taskbarHeight = rect.Bottom - rect.Top;
            int widgetHeight = Math.Min(38, taskbarHeight - 6);
            int widgetY = rect.Top + (taskbarHeight - widgetHeight) / 2;

            int widgetX;
            if (customX >= 0) {
                widgetX = customX;
            } else {
                widgetX = rect.Right - this.Width - 230;
            }

            if (widgetX < rect.Left + 50) widgetX = rect.Left + 50;
            if (widgetX + this.Width > rect.Right - 50) widgetX = rect.Right - this.Width - 50;

            SetWindowPos(this.Handle, HWND_TOPMOST, widgetX, widgetY, this.Width, widgetHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                isDragging = false;
                dragStartPoint = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left) {
                int dx = Math.Abs(e.X - dragStartPoint.X);
                int dy = Math.Abs(e.Y - dragStartPoint.Y);
                if (dx > 4 || dy > 4) {
                    isDragging = true;
                    int newX = this.Left + (e.X - dragStartPoint.X);
                    this.Left = newX;
                    customX = newX;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) {
                if (isDragging) {
                    isDragging = false;
                    SaveSettings();
                } else {
                    // Click opens History Flyout!
                    ShowHistory();
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            isHovered = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            isHovered = false;
            this.Invalidate();
        }

        private void CopyIP() {
            if (!string.IsNullOrEmpty(CurrentIP) && CurrentIP != "در حال بررسی..." && CurrentIP != "آفلاین") {
                try {
                    Clipboard.SetText(CurrentIP);
                    isCopyFeedback = true;
                    copyFeedbackTimer.Stop();
                    copyFeedbackTimer.Start();
                    this.Invalidate();
                } catch { }
            }
        }

        private void ResetPosition() {
            customX = -1;
            SaveSettings();
            AlignToTaskbar();
        }

        private void RefreshIPAsync() {
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    FetchGeoIP();
                } catch {
                    SafeUpdateUIError();
                }
            });
        }

        private void FetchGeoIP() {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            string ip = "", country = "", countryCode = "", city = "", isp = "";

            // Provider 1: ip-api.com
            try {
                var req = (HttpWebRequest)WebRequest.Create("http://ip-api.com/line/?fields=query,country,countryCode,city,isp,status");
                req.Timeout = 4000;
                using (var resp = req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream())) {
                    var status = sr.ReadLine();
                    if (status == "success") {
                        country = sr.ReadLine();
                        countryCode = sr.ReadLine();
                        city = sr.ReadLine();
                        isp = sr.ReadLine();
                        ip = sr.ReadLine();
                    }
                }
            } catch { }

            // Provider 2: Cloudflare trace
            if (string.IsNullOrEmpty(ip)) {
                try {
                    var req = (HttpWebRequest)WebRequest.Create("https://1.1.1.1/cdn-cgi/trace");
                    req.Timeout = 4000;
                    using (var resp = req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream())) {
                        string line;
                        while ((line = sr.ReadLine()) != null) {
                            if (line.StartsWith("ip=")) ip = line.Substring(3).Trim();
                            if (line.StartsWith("loc=")) countryCode = line.Substring(4).Trim();
                        }
                        country = countryCode;
                    }
                } catch { }
            }

            // Provider 3: ifconfig.me
            if (string.IsNullOrEmpty(ip)) {
                try {
                    var req = (HttpWebRequest)WebRequest.Create("https://ifconfig.me/ip");
                    req.Timeout = 4000;
                    using (var resp = req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream())) {
                        ip = sr.ReadToEnd().Trim();
                        country = "Unknown";
                        countryCode = "??";
                    }
                } catch { }
            }

            if (string.IsNullOrEmpty(ip)) {
                SafeUpdateUIError();
                return;
            }

            CurrentIP = ip;
            CurrentCountry = country;
            CurrentCountryCode = countryCode;
            CurrentCity = city;
            CurrentISP = isp;

            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country)) {
                CurrentLocationString = city + ", " + country;
            } else if (!string.IsNullOrEmpty(country)) {
                CurrentLocationString = country;
            } else if (!string.IsNullOrEmpty(countryCode)) {
                CurrentLocationString = "Country: " + countryCode;
            } else {
                CurrentLocationString = "موقعیت نامشخص";
            }

            // Log change to persistent history
            HistoryManager.AddRecord(CurrentIP, CurrentCountryCode, CurrentCountry, CurrentCity, CurrentISP);

            SafeUpdateUI();
        }

        private void SafeUpdateUI() {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke((Action)(delegate {
                try {
                    itemStatus.Text = "وضعیت: متصل (" + CurrentCountryCode + ")";
                    itemIP.Text = "IP: " + CurrentIP;
                    itemLocation.Text = "لوکیشن: " + CurrentLocationString;
                    itemISP.Text = "ارائه‌دهنده: " + (string.IsNullOrEmpty(CurrentISP) ? "N/A" : CurrentISP);

                    using (Graphics g = this.CreateGraphics())
                    using (Font fontTop = new Font("Segoe UI", 8.8f, FontStyle.Bold))
                    using (Font fontBottom = new Font("Segoe UI", 7.8f, FontStyle.Regular)) {
                        string sampleTop = CurrentIP + "  [" + CurrentCountryCode + "]";
                        SizeF sizeTop = g.MeasureString(sampleTop, fontTop);
                        SizeF sizeBottom = g.MeasureString(CurrentLocationString, fontBottom);

                        int maxTextWidth = (int)Math.Max(sizeTop.Width, sizeBottom.Width);
                        int targetWidth = Math.Max(165, maxTextWidth + 34);

                        if (Math.Abs(this.Width - targetWidth) > 4) {
                            this.Width = targetWidth;
                            AlignToTaskbar();
                        }
                    }

                    if (activeHistoryForm != null && !activeHistoryForm.IsDisposed) {
                        activeHistoryForm.RefreshData();
                    }

                    this.Invalidate();
                } catch { }
            }));
        }

        private void SafeUpdateUIError() {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke((Action)(delegate {
                try {
                    CurrentIP = "آفلاین";
                    CurrentCountryCode = "!";
                    CurrentLocationString = "اتصال قطع است";
                    itemStatus.Text = "وضعیت: عدم دسترسی به اینترنت";
                    this.Invalidate();
                } catch { }
            }));
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            int radius = 8;
            using (GraphicsPath path = GetRoundedPath(rect, radius)) {
                Color bgColor;
                if (isCopyFeedback) {
                    bgColor = Color.FromArgb(24, 80, 40);
                } else if (isHovered) {
                    bgColor = Color.FromArgb(44, 44, 44);
                } else {
                    bgColor = Color.FromArgb(28, 28, 28);
                }

                using (SolidBrush brush = new SolidBrush(bgColor)) {
                    g.FillPath(brush, path);
                }

                Color borderColor = isCopyFeedback ? Color.FromArgb(70, 200, 110) : Color.FromArgb(50, 255, 255, 255);
                using (Pen pen = new Pen(borderColor, 1f)) {
                    g.DrawPath(pen, path);
                }
            }

            if (isCopyFeedback) {
                using (Font fontTop = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (Font fontBottom = new Font("Segoe UI", 7.8f, FontStyle.Regular))
                using (SolidBrush brushTop = new SolidBrush(Color.FromArgb(150, 255, 180)))
                using (SolidBrush brushBottom = new SolidBrush(Color.FromArgb(210, 240, 220))) {
                    StringFormat sf = new StringFormat {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("✔ آی‌پی کپی شد", fontTop, brushTop, new RectangleF(0, 3, this.Width, 18), sf);
                    g.DrawString(CurrentIP, fontBottom, brushBottom, new RectangleF(0, 20, this.Width, 15), sf);
                }
                return;
            }

            Color statusColor;
            if (CurrentCountryCode == "IR") {
                statusColor = Color.FromArgb(235, 75, 75);
            } else if (!string.IsNullOrEmpty(CurrentCountryCode) && CurrentCountryCode != "!" && CurrentCountryCode != "??") {
                statusColor = Color.FromArgb(46, 204, 113);
            } else {
                statusColor = Color.FromArgb(150, 150, 150);
            }

            // Top line: Dot + IP + [Code]
            using (SolidBrush dotBrush = new SolidBrush(statusColor)) {
                g.FillEllipse(dotBrush, 10, 8, 7, 7);
            }

            using (Font ipFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White)) {
                StringFormat sf = new StringFormat {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near
                };
                g.DrawString(CurrentIP, ipFont, textBrush, new RectangleF(23, 2, this.Width - 65, 18), sf);
            }

            if (!string.IsNullOrEmpty(CurrentCountryCode)) {
                using (Font codeFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                using (SolidBrush codeBrush = new SolidBrush(statusColor)) {
                    StringFormat sf = new StringFormat {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Far
                    };
                    g.DrawString("[" + CurrentCountryCode + "]", codeFont, codeBrush, new RectangleF(0, 2, this.Width - 10, 18), sf);
                }
            }

            // Bottom line: Location
            using (Font locFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))
            using (SolidBrush locBrush = new SolidBrush(Color.FromArgb(175, 185, 195))) {
                StringFormat sf = new StringFormat {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(CurrentLocationString, locFont, locBrush, new RectangleF(23, 20, this.Width - 30, 15), sf);
            }
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius) {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void LoadSettings() {
            try {
                using (var key = Registry.CurrentUser.OpenSubKey(RegKeyPath, false)) {
                    if (key != null) {
                        object val = key.GetValue("CustomX");
                        if (val != null) {
                            customX = (int)val;
                        }
                    }
                }
            } catch { }
        }

        private void SaveSettings() {
            try {
                using (var key = Registry.CurrentUser.CreateSubKey(RegKeyPath)) {
                    if (key != null) {
                        key.SetValue("CustomX", customX);
                    }
                }
            } catch { }
        }

        private static bool IsStartupEnabled() {
            try {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, false)) {
                    if (key != null) {
                        return key.GetValue(AppName) != null;
                    }
                }
            } catch { }
            return false;
        }

        private static void ToggleStartup(bool enable) {
            try {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, true)) {
                    if (key != null) {
                        if (enable) {
                            key.SetValue(AppName, "\"" + Application.ExecutablePath + "\"");
                        } else {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show("خطا در تنظیم استارت‌آپ: " + ex.Message);
            }
        }
    }

    public static class Program {
        [STAThread]
        public static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TaskbarWidget());
        }
    }
}
