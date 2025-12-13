using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HouseAutomationDFA
{
    public class DfaVisualizer : Panel
    {
        private DFAController _dfa;

        public DfaVisualizer(DFAController dfa)
        {
            _dfa = dfa;
            DoubleBuffered = true;

            _dfa.StateChanged += () => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DrawStates(e.Graphics);
            DrawArrows(e.Graphics);
        }

        private void DrawState(Graphics g, string text, Rectangle rect, bool active)
        {
            Color fill = active ? Color.FromArgb(80, 150, 255, 150) : Color.FromArgb(40, 200, 200, 200);
            using (Brush b = new SolidBrush(fill))
            using (Pen p = new Pen(Color.White, active ? 3 : 2))
            {
                g.FillEllipse(b, rect);
                g.DrawEllipse(p, rect);
            }

            var format = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, new Font("Segoe UI", 10, FontStyle.Bold), Brushes.White, rect, format);
        }

        private void DrawCurveArrow(Graphics g, Point from, Point to)
        {
            using (Pen pen = new Pen(Color.LightGray, 2))
            {
                pen.CustomEndCap = new AdjustableArrowCap(6, 6);

                var path = new GraphicsPath();
                var mid = new Point((from.X + to.X) / 2, from.Y - 50);

                path.AddBezier(from, mid, mid, to);

                g.DrawPath(pen, path);
            }
        }

        private void DrawArrows(Graphics g)
        {
            // Hardcoded state positions (clean layout)
            var pOffCool = new Point(100, 80);
            var pOffWarm = new Point(300, 80);
            var pOnCool = new Point(100, 250);
            var pOnWarm = new Point(300, 250);

            DrawCurveArrow(g, pOffCool, pOnCool);
            DrawCurveArrow(g, pOffWarm, pOnWarm);
            DrawCurveArrow(g, pOnCool, pOffCool);
            DrawCurveArrow(g, pOnWarm, pOffWarm);

            DrawCurveArrow(g, pOffCool, pOffWarm);
            DrawCurveArrow(g, pOffWarm, pOffCool);
            DrawCurveArrow(g, pOnCool, pOnWarm);
            DrawCurveArrow(g, pOnWarm, pOnCool);
        }

        private void DrawStates(Graphics g)
        {
            DrawState(g, "Lights Off\nCool", new Rectangle(50, 40, 120, 80), _dfa.CurrentState == RoomState.LightsOff_Cool);
            DrawState(g, "Lights Off\nWarm", new Rectangle(250, 40, 120, 80), _dfa.CurrentState == RoomState.LightsOff_Warm);
            DrawState(g, "Lights On\nCool", new Rectangle(50, 210, 120, 80), _dfa.CurrentState == RoomState.LightsOn_Cool);
            DrawState(g, "Lights On\nWarm", new Rectangle(250, 210, 120, 80), _dfa.CurrentState == RoomState.LightsOn_Warm);
        }
    }
}
