namespace CM1620.Models
{
    public record StatusQueryResponse(string Slave, decimal InputVoltage, decimal OutputVoltage, decimal Temperature, bool BattGo, decimal BatteryPercent, ChargeErrorCode ErrorCode, ChargingStage ChargingStage, ChargeStatus? ChargeStatus, decimal[]? CellVoltages, decimal[]? CellResistancesMilliOhm);
}