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

            await Login(link);

            //var chargeCommandResponse = await link.ChargeCommand(new ChargeTask(ChemistryType.LiPo, 4.2m, 12, 16000, 20, ChargingBalanceMode.Unbalanced));
            //Console.WriteLine(chargeCommandResponse);

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var response = await link.StatusQuery();
                    var mainDevStatus = response.DeviceStatus.Single(x => x.Slave == "SL0");
                    Console.WriteLine(
                        $"In: {mainDevStatus.InputVoltage,4:0.0}V {response.ChargeStatus?.InputPower ?? 0,4:0}W  " +
                        $"Out: {mainDevStatus.OutputVoltage,4:0.0}V {response.ChargeStatus?.OutputCurrent ?? 0,4:0.0}A  " +
                        $"State: {response.ChargeStatus?.TimeCharged ?? TimeSpan.Zero,8} {response.ChargeStatus?.CapacityChargedMah ?? 0,6:0}mAh {mainDevStatus.BatteryPercent,3:0}% {mainDevStatus.Temperature,2:0}°C {response.ChargingStage,14} Err:{mainDevStatus.ErrorCode}");
                }
                catch (Cm1620ConfusedException)
                {
                    await Login(link);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                await Task.Delay(1000);
            }

            await link.StopCommand();
        }

        private static async Task<LoginCmdResponse> Login(Cm1620Link link)
        {
            var loginResponse = await link.Login().SingleAsync(x => x.Slave == "SL0");
            if (loginResponse.Result != LoginResult.Ok)
            {
                Console.WriteLine("Login failed");
                throw new Exception("Login failed");
            }
            return loginResponse;
        }
    }
}
