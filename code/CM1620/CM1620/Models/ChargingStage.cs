namespace CM1620.Models
{
    public enum ChargingStage
    {
        Standby = 1,
        Abnormal,
        ParallelChging,
        Activate,
        CurrentClimb,
        ConstCurChging,
        ConstVolChging,
        NormalEnd,
        Trickling
    }
}