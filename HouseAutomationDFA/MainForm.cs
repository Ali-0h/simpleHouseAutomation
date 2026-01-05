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

        // -----------------------
        // LIGHT
        // -----------------------
        public void ToggleLight()
        {
            LightOn = !LightOn;
            CurrentState = LightOn ? DFAState.LightOn : DFAState.LightOff;
            StateChanged?.Invoke();
        }

        // -----------------------
        // TEMPERATURE
        // -----------------------
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
            // 🚨 DO NOT interrupt TimerRunning
            if (CurrentState == DFAState.TimerRunning)
            {
                StateChanged?.Invoke();
                return;
            }

            CurrentState = DFAState.TempAdjust;
            tempAdjustCountdown = 3;
            StateChanged?.Invoke();
        }

        // -----------------------
        // TIMER
        // -----------------------
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
                // ✅ FIX #1: Timer turns the light ON
                LightOn = true;

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
            // ✅ FIX #2: Timer controls light lifecycle
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
            ClientSize = new Size(1000, 720);
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
                Width = 200,
                BackColor = Color.FromArgb(38, 38, 38),
                Padding = new Padding(1)
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
                Width = 400,
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

            panelDesc = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 32),
                Padding = new Padding(6)
            };

            rightColumn.Controls.Add(panelDesc);


            // header label inside DFA
            var dfaHeader = new Label
            {
                Text = "DFA Visualizer (Real-time)",
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Color.FromArgb(0, 174, 239),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            panelDfaContainer.Controls.Add(dfaHeader);

         
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

            // canvas panel (sits BELOW the header)
            var dfaCanvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            panelDfaContainer.Controls.Add(dfaCanvas);

            // DFA visualizer goes INSIDE the canvas
            dfaVisualizer = new DfaVisualizer(dfa)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            dfaCanvas.Controls.Add(dfaVisualizer);

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
        
    }
}