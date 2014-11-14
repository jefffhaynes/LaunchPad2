namespace NodeControl
{
    public enum ObjectType : byte
    {
        RequestStatus = 0x0,
        Arm = 0x1,
        Disarm = 0x2,
        FireControl = 0x3,
        Status = 0x4
    }
}
