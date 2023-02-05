using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joystick_Reaction_Timer
{
    internal class ButtonPress
    {
        public DateTime time { get; set; }
        public String buttonName { get; set; }

        public ButtonPress(DateTime time, String buttonName)
        {
            this.time=time;
            this.buttonName=buttonName;
        }
    }
}
