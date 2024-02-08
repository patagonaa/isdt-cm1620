namespace CM1620.Models
{
    public enum ChargeErrorCode
    {
        None = 0,
        LoginErrorPassword = 1,
        LoginErrorTimeout = 2,

        ParameterErrorBatteryType = 101,
        ParameterErrorTask = 102,
        ParameterErrorCurrent = 103,
        ParameterErrorVoltage = 104,
        ParameterErrorCapacity = 105,
        ParameterErrorBalance = 106,
        ParameterErrorStringNumber = 107,

        ParallelChargeErrorSlaveNumber = 151,
        ParallelChargeErrorBalance = 152,
        ParallelChargeErrorOutputVoltage = 153,
        ParallelChargeErrorTaskCurrent = 154,
        ParallelChargeErrorStartError = 155,
        ParallelChargeErrorConnectionError = 156,

        StartupErrorBalancePortNotConnected = 201,
        StartupErrorAbnormalBatteryConnection = 202,
        StartupErrorCellOvervoltage = 203,
        StartupErrorCellUndervoltage = 204,
        StartupErrorReverseBatteryConnection = 205,
        StartupErrorUnbalancedCharging = 206,
        StartupErrorBalanceChargeNotSupported = 207,
        StartupErrorOutputOvervoltage = 208,
        StartupErrorInputUndervoltage = 209,
        StartupErrorInputOvervoltage = 210,
        StartupErrorTaskNotSupported = 211,
        StartupErrorBattGoOverTemperature = 212,
        StartupErrorInputOutputDifferenceTooHigh = 213,

        OperationErrorOutputOvercurrent = 301,
        OperationErrorOutputOvervoltage = 302,
        OperationErrorInputOvervoltage = 303,
        OperationErrorInputUndervoltage = 304,
        OperationErrorUnstableInputVoltage = 305,
        OperationErrorOverTemperature = 306,
        OperationErrorTimeout = 307,
        OperationErrorBatteryConnectionError = 308,
        OperationErrorCellOvervoltage = 309,
        OperationErrorAbnormalBatteryConnection = 310,
        OperationErrorAbnormalCellVoltage = 311,
        OperationErrorNotSupported = 312,
        OperationErrorSuperCapacity = 313,

        // Test Error = 4xx

        SlaveErrorCommunicationTimeout = 501,
        SlaveErrorStartTimeout = 502,

    }
}
