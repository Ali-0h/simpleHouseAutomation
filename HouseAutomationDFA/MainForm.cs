using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HouseAutomationDFA
{
    // DFA states
    public enum DFAState
    {
        Idle,
        LightOn,
        LightOff,
        TempAdjust,
        TimerSet,
        TimerRunning,
        TimerExpired
    }

    // Temperature visual categories
    public enum TempVisualState
    {
        Cool,
        Normal,
        Warm
    }

    // DFA logic (same behavior)
    public class HouseDFA
    {
        public DFAState CurrentState { get; private set; } = DFAState.Idle;
        public bool LightOn { get; private set; } = false;
        public int Temperature { get; private set; } = 22;
        public int RemainingSeconds { get; private set; } = 0;

        private int tempAdjustCountdown = 0;
        public event Action StateChanged;

        public void ToggleLight()
        {
            LightOn = !LightOn;
            CurrentState = LightOn ? DFAState.LightOn : DFAState.LightOff;
            StateChanged?.Invoke();
        }

        public void IncreaseTemp()
        {
            Temperature++;
            EnterTempAdjust();
        }

        public void DecreaseTemp()
        {
            Temperature = Math.Max(5, Temperature - 1);
            EnterTempAdjust();
        }

        private void EnterTempAdjust()
        {
            CurrentState = DFAState.TempAdjust;
            tempAdjustCountdown = 3;
            StateChanged?.Invoke();
        }

        public void SetTimer(int seconds)
        {
            RemainingSeconds = seconds;
            CurrentState = DFAState.TimerSet;
            StateChanged?.Invoke();
        }

        public void ConfirmTimer()
        {
            if (CurrentState == DFAState.TimerSet)
            {
                CurrentState = DFAState.TimerRunning;
                StateChanged?.Invoke();
            }
        }

        public void Tick()
        {
            switch (CurrentState)
            {
                case DFAState.TempAdjust:
                    tempAdjustCountdown--;
                    if (tempAdjustCountdown <= 0)
                    {
                        CurrentState = DFAState.Idle;
                        StateChanged?.Invoke();
                    }
                    break;

                case DFAState.TimerRunning:
                    if (RemainingSeconds > 0)
                    {
                        RemainingSeconds--;
                    }
                    else
                    {
                        CurrentState = DFAState.TimerExpired;
                        OnTimerExpired();
                    }
                    StateChanged?.Invoke();
                    break;

                case DFAState.TimerExpired:
                    CurrentState = DFAState.Idle;
                    StateChanged?.Invoke();
                    break;
            }
        }

        private void OnTimerExpired()
        {
            LightOn = false;
            CurrentState = DFAState.TimerExpired;
            StateChanged?.Invoke();
        }
    }

    // Main form — full UI and integration
    public class MainForm : Form
    {
        // panels
        private Panel panelTitle, panelFooter, panelSide, panelCenter, panelDfaContainer, panelDesc;
        // status card
        private Panel panelStatusCard;
        private Label lblStatusTemp, lblStatusLight, lblStatusTimer, lblStatusState;

        // center image
        private PictureBox pictureRoom;

        // controls
        private Button btnToggleLight, btnTempUp, btnTempDown, btnTimer1, btnTimer5, btnConfirmTimer;

        // logs
        private RichTextBox rtbDesc;

        // DFA + visual
        private HouseDFA dfa;
        private DfaVisualizer dfaVisualizer;

        // timer
        private Timer uiTimer;

        // temp visual
        private TempVisualState tempVisual = TempVisualState.Normal;

        public MainForm()
        {
            Text = "House Automation - DFA";
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(1180, 720);
            Font = new Font("Segoe UI", 9f);

            InitializeUI();
            InitializeDFA();
            StartTimer();
        }

        private void InitializeUI()
        {
            // Title
            panelTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(10)
            };
            var lblTitle = new Label
            {
                Text = "House Automation Control Panel",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };
            panelTitle.Controls.Add(lblTitle);
            Controls.Add(panelTitle);

            // Footer
            panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(22, 22, 22)
            };
            var lblFooter = new Label
            {
                Text = "Made by: Ali, Muhammad Gabriel M., Pollido, Arjay A., Tan, Rowell Joseph",
                Dock = DockStyle.Fill,
                ForeColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelFooter.Controls.Add(lblFooter);
            Controls.Add(panelFooter);

            // Left side: controls + status
            panelSide = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(38, 38, 38),
                Padding = new Padding(12)
            };
            Controls.Add(panelSide);

            // Status card (rounded look will be drawn in Paint)
            panelStatusCard = new Panel
            {
                Height = 120,
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            panelStatusCard.Paint += PanelStatusCard_Paint;
            panelSide.Controls.Add(panelStatusCard);

            lblStatusTemp = CreateStatusLine("Temp: -- °C");
            lblStatusLight = CreateStatusLine("Light: OFF");
            lblStatusTimer = CreateStatusLine("Timer: 0s");
            lblStatusState = CreateStatusLine("State: Idle");

            // add labels into status card
            panelStatusCard.Controls.Add(lblStatusState);
            panelStatusCard.Controls.Add(lblStatusTimer);
            panelStatusCard.Controls.Add(lblStatusLight);
            panelStatusCard.Controls.Add(lblStatusTemp);

            // Controls label (below status)
            var lblControls = new Label
            {
                Text = "Controls",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            panelSide.Controls.Add(lblControls);
            panelSide.Controls.Add(CreateSpacer(6));

            // Buttons in logical order:
            // Toggle Light, Temp +, Temp -, Timer 1 min, Timer 5 min, Confirm Timer
            btnToggleLight = CreateShadowButton("Toggle Light");
            btnToggleLight.Click += (s, e) => { dfa?.ToggleLight(); };

            btnTempUp = CreateShadowButton("Temp +");
            btnTempUp.Click += (s, e) => { dfa?.IncreaseTemp(); };

            btnTempDown = CreateShadowButton("Temp -");
            btnTempDown.Click += (s, e) => { dfa?.DecreaseTemp(); };

            btnTimer1 = CreateShadowButton("Timer 1 min");
            btnTimer1.Click += (s, e) => { dfa?.SetTimer(60); };

            btnTimer5 = CreateShadowButton("Timer 5 min");
            btnTimer5.Click += (s, e) => { dfa?.SetTimer(300); };

            // Confirm Timer button with blue style (A)
            btnConfirmTimer = CreateShadowButton("Confirm Timer");
            btnConfirmTimer.Click += (s, e) => { dfa?.ConfirmTimer(); };
            // style as blue 'primary'
            btnConfirmTimer.BackColor = Color.FromArgb(0, 122, 204);
            btnConfirmTimer.ForeColor = Color.White;
            btnConfirmTimer.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);

            // add buttons in order to side
            panelSide.Controls.Add(WrapWithShadow(btnToggleLight));
            panelSide.Controls.Add(CreateSpacer(6));
            panelSide.Controls.Add(WrapWithShadow(btnTempUp));
            panelSide.Controls.Add(CreateSpacer(6));
            panelSide.Controls.Add(WrapWithShadow(btnTempDown));
            panelSide.Controls.Add(CreateSpacer(10));
            panelSide.Controls.Add(WrapWithShadow(btnTimer1));
            panelSide.Controls.Add(CreateSpacer(6));
            panelSide.Controls.Add(WrapWithShadow(btnTimer5));
            panelSide.Controls.Add(CreateSpacer(6));
            panelSide.Controls.Add(WrapWithShadow(btnConfirmTimer));
            panelSide.Controls.Add(CreateSpacer(12));

            // Center: image area
            panelCenter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Padding = new Padding(12)
            };
            Controls.Add(panelCenter);

            pictureRoom = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None
            };
            panelCenter.Controls.Add(pictureRoom);

            // Right column: DFA visual + logs
            var rightColumn = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                BackColor = Color.FromArgb(36, 36, 36),
                Padding = new Padding(10)
            };
            Controls.Add(rightColumn);

            // container for DFA visual (we will dock the visualizer into this)
            panelDfaContainer = new Panel
            {
                Height = 340,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(8)
            };
            rightColumn.Controls.Add(panelDfaContainer);

            // header label inside DFA
            var lblDfaHeader = new Label
            {
                Text = "DFA Visualizer (Real-time)",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.FromArgb(0, 174, 239),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            panelDfaContainer.Controls.Add(lblDfaHeader);

            // logs / description below
            panelDesc = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(8)
            };
            rightColumn.Controls.Add(panelDesc);

            var lblLogHeader = new Label
            {
                Text = "Project Description",
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(0, 174, 239),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            panelDesc.Controls.Add(lblLogHeader);

            rtbDesc = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 28, 28),
                ForeColor = Color.White,
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            panelDesc.Controls.Add(rtbDesc);

            // Set initial description / project text
            rtbDesc.Text =
@"HOUSE AUTOMATION SYSTEM USING DFA

This application demonstrates a smart house automation system
modeled using a Deterministic Finite Automaton (DFA).

Each system behavior is represented as a distinct state,
ensuring predictable and controlled transitions.

────────────────────────────
DFA STATES
────────────────────────────
Idle
• Default state waiting for user input.

Light On / Light Off
• Controls room lighting using a toggle action.

Temp Adjust
• Activated when temperature is increased or decreased.
• Automatically returns to Idle after adjustment.

Timer Set
• User selects a duration for automation.

Timer Running
• Countdown executes automatically without user input.

Timer Expired
• Light is turned OFF and the system resets to Idle.

────────────────────────────
TIMER PURPOSE
────────────────────────────
The timer demonstrates time-based DFA transitions,
showing how automation can operate independently
after initial user input.

────────────────────────────
KEY FEATURES
────────────────────────────
✔ Real-time DFA visualization  
✔ Curved-arrow state transitions  
✔ Temperature-based room visuals  
✔ Clean modern UI (Flat Dark theme)  
✔ Educational DFA-based design";
        }

        // draw rounded background for status card
        private void PanelStatusCard_Paint(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            using (GraphicsPath path = RoundedRect(rect, 12))
            using (Brush b = new SolidBrush(Color.FromArgb(42, 42, 42)))
            using (Pen pen = new Pen(Color.FromArgb(70, 70, 70), 1f))
            {
                g.FillPath(b, path);
                g.DrawPath(pen, path);
            }
        }

        private void InitializeDFA()
        {
            dfa = new HouseDFA();
            dfa.StateChanged += () => { RefreshStatus(); };

            // create visualizer and dock to panelDfaContainer (under the header we already added)
            dfaVisualizer = new DfaVisualizer(dfa)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            // insert after header label (header already added as first control)
            panelDfaContainer.Controls.Add(dfaVisualizer);
            panelDfaContainer.Controls.SetChildIndex(dfaVisualizer, 0);

            // initial UI sync
            RefreshStatus();
            UpdateImage();
        }

        private void StartTimer()
        {
            uiTimer = new Timer();
            uiTimer.Interval = 1000;
            uiTimer.Tick += (s, e) =>
            {
                dfa.Tick();
                RefreshStatus();
                UpdateImage();
            };
            uiTimer.Start();
        }

        // update status labels
        private void RefreshStatus()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshStatus);
                return;
            }

            lblStatusTemp.Text = $"Temp: {dfa.Temperature} °C";
            lblStatusLight.Text = $"Light: {(dfa.LightOn ? "ON" : "OFF")}";
            lblStatusTimer.Text = $"Timer: {dfa.RemainingSeconds}s";
            lblStatusState.Text = $"State: {dfa.CurrentState}";

            // redraw DFA visual highlight
            dfaVisualizer.Invalidate();
        }

        // update center picture based on temp & light
        private void UpdateImage()
        {
            // classify temperature
            if (dfa.Temperature <= 18) tempVisual = TempVisualState.Cool;
            else if (dfa.Temperature >= 27) tempVisual = TempVisualState.Warm;
            else tempVisual = TempVisualState.Normal;

            try
            {
                if (dfa.LightOn)
                {
                    switch (tempVisual)
                    {
                        case TempVisualState.Cool:
                            pictureRoom.Image = Properties.Resources.LightsOnCool;
                            break;
                        case TempVisualState.Warm:
                            pictureRoom.Image = Properties.Resources.LightsOnWarm;
                            break;
                        default:
                            pictureRoom.Image = Properties.Resources.LightsOn;
                            break;
                    }
                }
                else
                {
                    switch (tempVisual)
                    {
                        case TempVisualState.Cool:
                            pictureRoom.Image = Properties.Resources.LightsOffCool;
                            break;
                        case TempVisualState.Warm:
                            pictureRoom.Image = Properties.Resources.LightsOffWarm;
                            break;
                        default:
                            pictureRoom.Image = Properties.Resources.LightsOff;
                            break;
                    }
                }
            }
            catch
            {
                pictureRoom.Image = null;
            }
        }

        // ===== helpers & UI small functions =====
        private Label CreateStatusLine(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 3, 3, 3),
                Font = new Font("Segoe UI", 9f)
            };
        }

        private Control WrapWithShadow(Control inner)
        {
            var container = new Panel
            {
                Height = inner.Height + 8,
                Dock = DockStyle.Top,
                Padding = new Padding(3),
                BackColor = Color.FromArgb(18, 18, 18)
            };
            inner.Dock = DockStyle.Fill;
            container.Controls.Add(inner);
            return container;
        }

        private Button CreateShadowButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Height = 40,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(58, 58, 58),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => b.BackColor = Color.FromArgb(80, 80, 80);
            b.MouseLeave += (s, e) => b.BackColor = Color.FromArgb(58, 58, 58);
            return b;
        }

        private Panel CreateSpacer(int h)
        {
            var p = new Panel { Height = h, Dock = DockStyle.Top, BackColor = Color.Transparent };
            return p;
        }

        // rounded rectangle helper
        private GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ======================================
        //           DFA VISUALIZER CLASS
        // ======================================
        // Circle-node visualizer (curved arrows, labels, highlight)
        private class DfaVisualizer : Panel
        {
            private readonly HouseDFA _dfa;
            private readonly Font labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            private readonly Font arrowFont = new Font("Segoe UI", 8f, FontStyle.Regular);

            // circle node positions (will be recomputed on resize)
            private PointF pIdle, pLightOn, pLightOff, pTempAdjust, pTimerSet, pTimerRunning, pTimerExpired;
            private float nodeRadius = 36f;

            public DfaVisualizer(HouseDFA dfa)
            {
                _dfa = dfa ?? throw new ArgumentNullException(nameof(dfa));
                DoubleBuffered = true;
                _dfa.StateChanged += () => Invalidate();
                this.Resize += (s, e) => Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ComputeLayout();
                DrawArrows(e.Graphics);
                DrawNodes(e.Graphics);
            }

            private void ComputeLayout()
            {
                // compute positions in a compact layout that fits the panel
                float w = ClientSize.Width;
                float h = ClientSize.Height;

                // center top: Idle
                pIdle = new PointF(w * 0.5f, h * 0.12f);

                // middle row: LightOn (left), TempAdjust (center), LightOff (right)
                pLightOn = new PointF(w * 0.20f, h * 0.38f);
                pTempAdjust = new PointF(w * 0.50f, h * 0.38f);
                pLightOff = new PointF(w * 0.80f, h * 0.38f);

                // bottom row: TimerSet (left), TimerRunning (center), TimerExpired (right)
                pTimerSet = new PointF(w * 0.20f, h * 0.72f);
                pTimerRunning = new PointF(w * 0.50f, h * 0.72f);
                pTimerExpired = new PointF(w * 0.80f, h * 0.72f);

                // radius adaptively
                nodeRadius = Math.Min(48f, Math.Min(w, h) / 12f);
            }

            private void DrawNodes(Graphics g)
            {
                DrawCircleNode(g, pIdle, "Idle", _dfa.CurrentState == DFAState.Idle);
                DrawCircleNode(g, pLightOn, "Light On", _dfa.CurrentState == DFAState.LightOn);
                DrawCircleNode(g, pLightOff, "Light Off", _dfa.CurrentState == DFAState.LightOff);
                DrawCircleNode(g, pTempAdjust, "Temp Adjust", _dfa.CurrentState == DFAState.TempAdjust);
                DrawCircleNode(g, pTimerSet, "Timer Set", _dfa.CurrentState == DFAState.TimerSet);
                DrawCircleNode(g, pTimerRunning, "Timer Running", _dfa.CurrentState == DFAState.TimerRunning);
                DrawCircleNode(g, pTimerExpired, "Timer Expired", _dfa.CurrentState == DFAState.TimerExpired);
            }

            private void DrawCircleNode(Graphics g, PointF center, string text, bool active)
            {
                float r = nodeRadius;
                var rect = new RectangleF(center.X - r, center.Y - r, r * 2, r * 2);
                Color fill = active ? Color.FromArgb(255, 30, 144, 255) : Color.FromArgb(60, 60, 60);
                using (Brush b = new SolidBrush(fill))
                using (Pen p = new Pen(active ? Color.Cyan : Color.LightGray, active ? 2.8f : 1.6f))
                {
                    g.FillEllipse(b, rect);
                    g.DrawEllipse(p, rect);
                }

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (Brush txt = new SolidBrush(Color.White))
                {
                    g.DrawString(text, labelFont, txt, rect, sf);
                }
            }

            private void DrawArrows(Graphics g)
            {
                // helper draws a curved arrow with a label on the curve
                DrawCurvedArrow(g, pIdle, pLightOn, -40, "toggle");
                DrawCurvedArrow(g, pIdle, pLightOff, -40, "toggle");
                DrawCurvedArrow(g, pIdle, pTempAdjust, -10, "temp mode");
                DrawCurvedArrow(g, pIdle, pTimerSet, 10, "set timer");

                DrawCurvedArrow(g, pLightOn, pLightOff, 0, "toggle");
                DrawCurvedArrow(g, pLightOff, pLightOn, 0, "toggle");

                DrawCurvedArrow(g, pTempAdjust, pIdle, 60, "apply");

                DrawCurvedArrow(g, pTimerSet, pTimerRunning, 18, "confirm");
                DrawCurvedArrow(g, pTimerRunning, pTimerExpired, 18, "timeout");
                DrawCurvedArrow(g, pTimerExpired, pIdle, 30, "reset");
            }

            private void DrawCurvedArrow(Graphics g, PointF a, PointF b, float controlYOffset, string label)
            {
                using (Pen pen = new Pen(Color.LightGray, 2f))
                {
                    pen.CustomEndCap = new AdjustableArrowCap(6, 6);
                    // control point is between a and b, shifted by controlYOffset vertically
                    var mid = new PointF((a.X + b.X) / 2f, (a.Y + b.Y) / 2f + controlYOffset);

                    // create path and draw
                    var path = new GraphicsPath();
                    path.AddBezier(a, mid, mid, b);
                    g.DrawPath(pen, path);

                    // draw label at near-midpoint of path
                    var labelPos = new PointF((a.X + b.X) / 2f, (a.Y + b.Y) / 2f + controlYOffset / 2f - 6);
                    var txtSize = g.MeasureString(label, arrowFont);
                    var labelRect = new RectangleF(labelPos.X - txtSize.Width / 2f - 6, labelPos.Y - txtSize.Height / 2f - 4, txtSize.Width + 12, txtSize.Height + 6);

                    // draw dark pill behind label
                    using (Brush pill = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                    using (Pen pillPen = new Pen(Color.FromArgb(100, 100, 100)))
                    {
                        var pillPath = RoundedRect(labelRect, 6);
                        g.FillPath(pill, pillPath);
                        g.DrawPath(pillPen, pillPath);
                    }
                    // draw label text
                    using (Brush txt = new SolidBrush(Color.LightGray))
                    {
                        g.DrawString(label, arrowFont, txt, labelRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    }
                }
            }

            private GraphicsPath RoundedRect(RectangleF r, float radius)
            {
                var path = new GraphicsPath();
                float d = radius * 2f;
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}