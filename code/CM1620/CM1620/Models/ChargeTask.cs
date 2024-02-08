namespace CM1620.Models
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Chemistry"></param>
    /// <param name="CellChargeVoltage"></param>
    /// <param name="CellStringNumber">null = auto</param>
    /// <param name="BatteryCapacitymAh"></param>
    /// <param name="ChargeCurrentA"></param>
    /// <param name="ChargingMode"></param>
    public record ChargeTask(ChemistryType Chemistry, decimal CellChargeVoltage, int? CellStringNumber, int BatteryCapacitymAh, decimal ChargeCurrentA, ChargingBalanceMode ChargingMode);
}