namespace CM1620.Models.StatusQuery
{
    public record StatusQueryDeviceStatus(string Slave, decimal InputVoltage, decimal OutputVoltage, int Temperature, bool BattGo, int BatteryPercent, ChargeErrorCode ErrorCode, decimal[]? CellVoltages, decimal[]? CellResistancesMilliOhm);
}