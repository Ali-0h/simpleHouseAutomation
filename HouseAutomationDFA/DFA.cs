using System;
using System.Drawing;
using System.Windows.Forms;

namespace HouseAutomationDFA
{
    public enum RoomState
    {
        LightsOff_Cool,
        LightsOff_Warm,
        LightsOn_Cool,
        LightsOn_Warm
    }

    public class DFAController
    {
        public RoomState CurrentState { get; private set; }

        public event Action StateChanged;

        public DFAController()
        {
            CurrentState = RoomState.LightsOff_Cool;
        }

        public void SetLights(bool on)
        {
            switch (CurrentState)
            {
                case RoomState.LightsOff_Cool: CurrentState = on ? RoomState.LightsOn_Cool : CurrentState; break;
                case RoomState.LightsOff_Warm: CurrentState = on ? RoomState.LightsOn_Warm : CurrentState; break;
                case RoomState.LightsOn_Cool: CurrentState = on ? CurrentState : RoomState.LightsOff_Cool; break;
                case RoomState.LightsOn_Warm: CurrentState = on ? CurrentState : RoomState.LightsOff_Warm; break;
            }
            StateChanged?.Invoke();
        }

        public void SetTemp(bool warm)
        {
            switch (CurrentState)
            {
                case RoomState.LightsOff_Cool: CurrentState = warm ? RoomState.LightsOff_Warm : CurrentState; break;
                case RoomState.LightsOff_Warm: CurrentState = warm ? CurrentState : RoomState.LightsOff_Cool; break;
                case RoomState.LightsOn_Cool: CurrentState = warm ? RoomState.LightsOn_Warm : CurrentState; break;
                case RoomState.LightsOn_Warm: CurrentState = warm ? CurrentState : RoomState.LightsOn_Cool; break;
            }
            StateChanged?.Invoke();
        }
    }
}
