namespace CM1620.Models.StatusQuery
{
    public record StatusQueryResponse(ChargingStage ChargingStage, ChargeStatus? ChargeStatus, StatusQueryDeviceStatus[] DeviceStatus);
}