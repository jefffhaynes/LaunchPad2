using System.Collections.Generic;

namespace NodeControl
{
    public class FireControlCommand : ControlObjectPayload
    {
        public FireControlCommand()
        {
            PortStates = new List<PortStatus>();
        }

        public List<PortStatus> PortStates { get; set; } 
    }
}
