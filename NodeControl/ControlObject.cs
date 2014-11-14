using BinarySerialization;

namespace NodeControl
{
    public class ControlObject
    {
        public ObjectType ObjectType { get; set; }

        public ushort ObjectLength { get; set; }

        [FieldLength("ObjectLength")]
        [Subtype("ObjectType", ObjectType.RequestStatus, typeof(RequestStatusCommand))]
        [Subtype("ObjectType", ObjectType.Arm, typeof(ArmCommand))]
        [Subtype("ObjectType", ObjectType.Disarm, typeof(DisarmCommand))]
        [Subtype("ObjectType", ObjectType.FireControl, typeof(FireControlCommand))]
        public ControlObjectPayload Payload { get; set; }
    }
}
