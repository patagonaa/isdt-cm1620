namespace CM1620.Models
{
    public record ChargeStatus(decimal TaskCurrent, decimal InputPower, decimal OutputCurrent, decimal CapacityChargedMah, TimeSpan TimeCharged);
}