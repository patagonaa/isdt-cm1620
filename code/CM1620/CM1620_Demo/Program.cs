using CM1620;
using CM1620.Models;
using System.Globalization;

namespace CM1620_Demo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (o, args) => { cts.Cancel(); args.Cancel = true; };

            using var communication = new SerialCommunication("COM4");
            var link = new Cm1620Link(communication);

            var loginResponse = await link.Login().SingleAsync(x => x.Slave == "SL0");
            if (loginResponse.Result != LoginResult.Ok)
                throw new Exception("Login failed");

            //var chargeCommandResponse = await link.ChargeCommand(new ChargeTask(ChemistryType.LiPo, 4.105m, 16, 35000, 10, ChargingMode.Unbalanced));
            //Console.WriteLine(chargeCommandResponse);

            while (!cts.IsCancellationRequested)
            {
                var response = await link.StatusQuery().SingleAsync(x => x.Slave == "SL0");
                Console.WriteLine(
                    $"In: {response.InputVoltage,4:0.0}V {response.ChargeStatus?.InputPower ?? 0,4:0}W  " +
                    $"Out: {response.OutputVoltage,4:0.0}V {response.ChargeStatus?.OutputCurrent ?? 0,4:0.0}A  " +
                    $"State: {response.ChargeStatus?.TimeCharged ?? TimeSpan.Zero,8} {response.ChargeStatus?.CapacityChargedMah??0,6:0}mAh {response.BatteryPercent,3:0}% {response.Temperature,2:0}°C {response.ChargingStage,14} Err:{response.ErrorCode}");
                await Task.Delay(1000);
            }

            //await link.StopCommand();
        }
    }
}
