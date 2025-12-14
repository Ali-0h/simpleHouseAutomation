using System;

namespace HouseAutomationDFA
{
    public enum RoomState
    {
        Idle,
        L_OFF_T_COOL,
        L_OFF_T_NEUTRAL,
        L_OFF_T_WARM,
        L_ON_T_COOL,
        L_ON_T_NEUTRAL,
        L_ON_T_WARM
    }

    public class DFAController
    {
        private bool _timerRunning;

        public bool IsTimerRunning => _timerRunning;

        public RoomState CurrentState { get; private set; }

        public event Action StateChanged;

        public DFAController()
        {
            _timerRunning = false;
            CurrentState = RoomState.Idle;
        }

        // =========================
        // TIMER
        // =========================

        public void TimerStart()
        {
            if (_timerRunning)
                return;

            _timerRunning = true;

            // ✅ FORCE LIGHT ON + NEUTRAL when timer starts
            TransitionTo(RoomState.L_ON_T_NEUTRAL);
        }

        public void TimerEnd()
        {
            if (!_timerRunning)
                return;

            _timerRunning = false;
            TransitionTo(RoomState.Idle);
        }

        // =========================
        // LIGHT CONTROL
        // =========================

        public void LightOn()
        {
            if (!_timerRunning)
                return;

            switch (CurrentState)
            {
                case RoomState.L_OFF_T_COOL:
                    TransitionTo(RoomState.L_ON_T_COOL);
                    break;

                case RoomState.L_OFF_T_NEUTRAL:
                    TransitionTo(RoomState.L_ON_T_NEUTRAL);
                    break;

                case RoomState.L_OFF_T_WARM:
                    TransitionTo(RoomState.L_ON_T_WARM);
                    break;
            }
        }

        public void LightOff()
        {
            if (!_timerRunning)
                return;

            switch (CurrentState)
            {
                case RoomState.L_ON_T_COOL:
                    TransitionTo(RoomState.L_OFF_T_COOL);
                    break;

                case RoomState.L_ON_T_NEUTRAL:
                    TransitionTo(RoomState.L_OFF_T_NEUTRAL);
                    break;

                case RoomState.L_ON_T_WARM:
                    TransitionTo(RoomState.L_OFF_T_WARM);
                    break;
            }
        }

        // =========================
        // TEMPERATURE
        // =========================

        public void TempCool()
        {
            if (!_timerRunning)
                return;

            if (IsLightOn())
                TransitionTo(RoomState.L_ON_T_COOL);
            else
                TransitionTo(RoomState.L_OFF_T_COOL);
        }

        public void TempNeutral()
        {
            if (!_timerRunning)
                return;

            if (IsLightOn())
                TransitionTo(RoomState.L_ON_T_NEUTRAL);
            else
                TransitionTo(RoomState.L_OFF_T_NEUTRAL);
        }

        public void TempWarm()
        {
            if (!_timerRunning)
                return;

            if (IsLightOn())
                TransitionTo(RoomState.L_ON_T_WARM);
            else
                TransitionTo(RoomState.L_OFF_T_WARM);
        }

        // =========================
        // HELPERS
        // =========================

        private bool IsLightOn()
        {
            return CurrentState == RoomState.L_ON_T_COOL
                || CurrentState == RoomState.L_ON_T_NEUTRAL
                || CurrentState == RoomState.L_ON_T_WARM;
        }

        private void TransitionTo(RoomState newState)
        {
            if (newState == CurrentState)
                return;

            CurrentState = newState;
            StateChanged?.Invoke();
        }
    }
}
