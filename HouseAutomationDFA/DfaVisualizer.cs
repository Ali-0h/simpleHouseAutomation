using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HouseAutomationDFA
{
    public class DfaVisualizer : Panel
    {
        private readonly HouseDFA _dfa;

        private readonly Font nodeFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        private readonly Font arrowFont = new Font("Segoe UI", 8f);

        private float r = 34f;

        // Node positions
        private PointF pIdle, pTimerSet, pTimerRunning, pLightsOn;
        private PointF pTempAdjust, pTimerExpired, pLightsOff;

        public DfaVisualizer(HouseDFA dfa)
        {
            _dfa = dfa;
            DoubleBuffered = true;
            _dfa.StateChanged += () => Invalidate();
            Resize += (s, e) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            ComputeLayout();
            DrawArrows(e.Graphics);
            DrawNodes(e.Graphics);
        }

        // =========================
        // LAYOUT — HORIZONTAL DFA
        // =========================
        private void ComputeLayout()
        {
            float w = Math.Max(ClientSize.Width, 300);   // enforce minimum width
            float h = Math.Max(ClientSize.Height, 200);  // enforce minimum height

            float topY = h * 0.28f;
            float bottomY = h * 0.70f;

            float left = 35f;
            float step = 100f; // FIXED spacing (key change)

            // Top row
            pIdle = new PointF(left + step * 0, topY);
            pTimerSet = new PointF(left + step * 1, topY);
            pTimerRunning = new PointF(left + step * 2, topY);
            pLightsOn = new PointF(left + step * 3, topY);

            // Bottom row
            pTempAdjust = new PointF(pIdle.X, bottomY);
            pTimerExpired = new PointF(pTimerRunning.X, bottomY);
            pLightsOff = new PointF(pLightsOn.X, bottomY);
        }

        // =========================
        // NODES
        // =========================
        private void DrawNodes(Graphics g)
        {
            DrawNode(g, pIdle, "IDLE", _dfa.CurrentState == DFAState.Idle);
            DrawNode(g, pTimerSet, "Timer\nSET", _dfa.CurrentState == DFAState.TimerSet);
            DrawNode(g, pTimerRunning, "TIMER\nRUNNING", _dfa.CurrentState == DFAState.TimerRunning);
            DrawNode(g, pLightsOn, "LIGHTS\nON", _dfa.LightOn);

            DrawNode(g, pTempAdjust, "Temp\nAdjust", _dfa.CurrentState == DFAState.TempAdjust);
            DrawNode(g, pTimerExpired, "Timer\nExpired", _dfa.CurrentState == DFAState.TimerExpired);
            DrawNode(g, pLightsOff, "LIGHTS\nOFF", !_dfa.LightOn);
        }

        private void DrawNode(Graphics g, PointF center, string text, bool active)
        {
            RectangleF rect = new RectangleF(center.X - r, center.Y - r, r * 2, r * 2);

            Color fill = active ? Color.DodgerBlue : Color.FromArgb(50, 50, 50);

            using (Brush b = new SolidBrush(fill))
            using (Pen p = new Pen(Color.White, active ? 2.5f : 1.5f))
            {
                g.FillEllipse(b, rect);
                g.DrawEllipse(p, rect);
            }

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(text, nodeFont, Brushes.White, rect, sf);
        }

        // =========================
        // ARROWS — MATCH REFERENCE
        // =========================
        private void DrawArrows(Graphics g)
        {
            // Main flow
            Arrow(g, pIdle, pTimerSet, "set time");
            Arrow(g, pTimerSet, pTimerRunning, "confirm");
            Arrow(g, pTimerRunning, pLightsOn, "toggle");

            // Temperature
            Arrow(g, pIdle, pTempAdjust, "temp");

            // Timer expiration
            Arrow(g, pTimerRunning, pTimerExpired, "timeout");

            // Light control
            Arrow(g, pTimerExpired, pLightsOff, "toggle");

            // User toggle
            Arrow(g, pLightsOn, pLightsOff, "usertoggle");
            Arrow(g, pLightsOff, pLightsOn, "usertoggle");

            // Reset
            Arrow(g, pTimerExpired, pIdle, "reset");
        }

        private void Arrow(Graphics g, PointF from, PointF to, string label)
        {
            using (Pen pen = new Pen(Color.LightGray, 2f))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5, 5);
                g.DrawLine(pen, from, to);
            }

            PointF mid = new PointF(
                (from.X + to.X) / 2f,
                (from.Y + to.Y) / 2f - 10
            );

            g.DrawString(label, arrowFont, Brushes.LightGray, mid);
        }
    }
}
