namespace CM1620.Models.StatusQuery
{
    public record ChargeStatus(decimal TaskCurrent, decimal InputPower, decimal OutputCurrent, int CapacityChargedMah, TimeSpan TimeCharged);
}