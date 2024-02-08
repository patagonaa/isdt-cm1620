using System.Globalization;
using System.Text.RegularExpressions;
using CM1620.Models;

namespace CM1620
{
    public partial class Cm1620Link
    {
        private readonly ICommunication _communication;

        public Cm1620Link(ICommunication communication)
        {
            _communication = communication;
        }

        public async IAsyncEnumerable<HelloCmdResponse> Hello()
        {
            var response = await _communication.SendCommand("hello");
            var expectedLines = int.Parse(response.Status ?? throw new Cm1620Exception("missing hello status"), CultureInfo.InvariantCulture);
            if (expectedLines != response.AdditionalLines.Length)
                throw new InvalidOperationException($"expected {response.Status}, got {response.AdditionalLines.Length}");
            foreach (var line in response.AdditionalLines)
            {
                var match = HelloResponseRegex().Match(line);
                if (!match.Success)
                    throw new InvalidOperationException($"Invalid hello line {line}");
                yield return new HelloCmdResponse(
                    match.Groups["slave"].Value,
                    match.Groups["model"].Value,
                    Version.Parse(match.Groups["sl"].Value),
                    Version.Parse(match.Groups["bt"].Value),
                    Version.Parse(match.Groups["hw"].Value));
            }
        }

        public async IAsyncEnumerable<LoginCmdResponse> Login(string password = "null")
        {
            var response = await _communication.SendCommand("login", password);
            var expectedLines = int.Parse(response.Status ?? throw new Cm1620Exception("missing login status"), CultureInfo.InvariantCulture);

            if (expectedLines != response.AdditionalLines.Length)
                throw new InvalidOperationException($"expected {response.Status}, got {response.AdditionalLines.Length}");
            foreach (var line in response.AdditionalLines)
            {
                var match = LoginResponseRegex().Match(line);
                if (!match.Success)
                    throw new InvalidOperationException($"Invalid login line {line}");

                var status = match.Groups["result"].Value switch
                {
                    "ok" => LoginResult.Ok,
                    "error" => LoginResult.PasswordError,
                    _ => throw new Cm1620Exception($"Invalid Login status {response.Status}")
                };
                yield return new LoginCmdResponse(match.Groups["slave"].Value, status);
            }
        }

        public async IAsyncEnumerable<TaskQueryResponse> TaskQuery()
        {
            var response = await _communication.SendCommand("task");
            var expectedLines = int.Parse(response.Status ?? throw new Cm1620Exception("missing task status"), CultureInfo.InvariantCulture);
            if (expectedLines != response.AdditionalLines.Length)
                throw new InvalidOperationException($"expected {response.Status}, got {response.AdditionalLines.Length}");
            foreach (var line in response.AdditionalLines)
            {
                var match = TaskResponseRegex().Match(line);
                if (!match.Success)
                    throw new InvalidOperationException($"Invalid task query line {line}");
                yield return new TaskQueryResponse(
                    match.Groups["slave"].Value,
                    new ChargeTask(
                        match.Groups["chemistry"].Value switch
                        {
                            "lipo" => ChemistryType.LiPo,
                            "lihv" => ChemistryType.LiHv,
                            "life" => ChemistryType.LiFe,
                            _ => throw new Cm1620Exception($"invalid chemistry {match.Groups["chemistry"].Value}")
                        },
                        decimal.Parse(match.Groups["cellv"].Value, CultureInfo.InvariantCulture),
                        match.Groups["cellseries"].Value.Equals("auto", StringComparison.OrdinalIgnoreCase) ? null : int.Parse(match.Groups["cellseries"].Value[..^1], CultureInfo.InvariantCulture),
                        int.Parse(match.Groups["capacity"].Value, CultureInfo.InvariantCulture),
                        decimal.Parse(match.Groups["current"].Value, CultureInfo.InvariantCulture),
                        match.Groups["chargemode"].Value switch
                        {
                            "BLN" => ChargingBalanceMode.Balanced,
                            "UBL" => ChargingBalanceMode.Unbalanced,
                            _ => throw new Cm1620Exception($"invalid charging mode {match.Groups["chargemode"].Value}")
                        }
                    )
                );
            }
        }

        public async IAsyncEnumerable<StatusQueryResponse> StatusQuery()
        {
            var response = await _communication.SendCommand("status");

            for (int i = 0; i < response.AdditionalLines.Length; i++)
            {
                var statusLineMatch = StatusResponseRegex().Match(response.AdditionalLines[i]);
                if (!statusLineMatch.Success)
                    throw new Cm1620Exception($"Invalid status line {response.AdditionalLines[i]}");
                var balMode = statusLineMatch.Groups["balmode"].Value;
                var chargeStage = Enum.Parse<ChargingStage>(statusLineMatch.Groups["chargemode"].Value, true);

                ChargeStatus? chargeStatus = null;

                switch (chargeStage)
                {
                    case ChargingStage.Standby:
                    case ChargingStage.Abnormal:
                    case ChargingStage.ParallelChging:
                        break;

                    case ChargingStage.Activate:
                    case ChargingStage.CurrentClimb:
                    case ChargingStage.ConstCurChging:
                    case ChargingStage.ConstVolChging:
                    case ChargingStage.NormalEnd:
                    case ChargingStage.Trickling:
                        i++;
                        var chargeStatusMatch = StatusResponseChargeRegex().Match(response.AdditionalLines[i]);
                        if (!chargeStatusMatch.Success)
                            throw new Cm1620Exception($"Invalid charge status line {response.AdditionalLines[i]}");
                        chargeStatus = new ChargeStatus(
                            decimal.Parse(chargeStatusMatch.Groups["taskcurrent"].Value, CultureInfo.InvariantCulture),
                            decimal.Parse(chargeStatusMatch.Groups["inputpower"].Value, CultureInfo.InvariantCulture),
                            decimal.Parse(chargeStatusMatch.Groups["outputcurrent"].Value, CultureInfo.InvariantCulture),
                            decimal.Parse(chargeStatusMatch.Groups["chargecapacity"].Value, CultureInfo.InvariantCulture),
                            TimeSpan.Parse(chargeStatusMatch.Groups["time"].Value, CultureInfo.InvariantCulture)
                            );
                        break;
                    default:
                        throw new InvalidOperationException("Invalid charge mode");
                }

                decimal[]? cellVoltages = null;
                decimal[]? cellResistancesMilliOhm = null;

                switch (balMode)
                {
                    case "UBL":
                        break;
                    case "BV":
                        i++;
                        cellVoltages = response.AdditionalLines[i].Split(' ').Select(x => decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case "BVR":
                        i++;
                        cellVoltages = response.AdditionalLines[i].Split(' ').Select(x => decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                        i++;
                        cellResistancesMilliOhm = response.AdditionalLines[i].Split(' ').Select(x => decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid balance mode");
                }

                yield return new StatusQueryResponse(
                    statusLineMatch.Groups["slave"].Value,
                    decimal.Parse(statusLineMatch.Groups["inputv"].Value, CultureInfo.InvariantCulture),
                    decimal.Parse(statusLineMatch.Groups["outputv"].Value, CultureInfo.InvariantCulture),
                    decimal.Parse(statusLineMatch.Groups["temp"].Value, CultureInfo.InvariantCulture),
                    statusLineMatch.Groups["battgo"].Value.Equals("Y", StringComparison.OrdinalIgnoreCase),
                    decimal.Parse(statusLineMatch.Groups["percent"].Value, CultureInfo.InvariantCulture),
                    (ChargeErrorCode)int.Parse(statusLineMatch.Groups["error"].Value, CultureInfo.InvariantCulture),
                    chargeStage,
                    chargeStatus,
                    cellVoltages,
                    cellResistancesMilliOhm
                    );
            }
        }

        public async Task<ChargeCommandResponse> ChargeCommand(ChargeTask task)
        {
            var batteryType = task.Chemistry switch
            {
                ChemistryType.LiPo => "lipo",
                ChemistryType.LiHv => "lihv",
                ChemistryType.LiFe => "life",
                _ => throw new ArgumentException("Invalid chemistry type"),
            };
            var cellStringNumber = task.CellStringNumber == null ? "auto" : (task.CellStringNumber.Value.ToString(CultureInfo.InvariantCulture) + "S");

            var chargeMode = task.ChargingMode switch
            {
                ChargingBalanceMode.Balanced => "BLN",
                ChargingBalanceMode.Unbalanced => "UBL",
                _ => throw new ArgumentException("Invalid charging mode"),
            };

            var response = await _communication.SendCommand("charge", $"{batteryType} {task.CellChargeVoltage.ToString("0.00", CultureInfo.InvariantCulture)}V {cellStringNumber} {task.BatteryCapacitymAh}mAh {task.ChargeCurrentA.ToString(CultureInfo.InvariantCulture)}A {chargeMode}");

            var status = response.Status ?? throw new Cm1620Exception("missing charge status");
            var splitStatus = status.Split(' ');

            return new ChargeCommandResponse(Enum.Parse<ChargeResponseStatus>(splitStatus[0], true), splitStatus.Length > 1 ? (ChargeErrorCode)int.Parse(splitStatus[1]) : ChargeErrorCode.None);
        }

        public async Task<decimal> AdjustCurrentCommand(decimal newCurrent)
        {
            var response = await _communication.SendCommand("adjust", newCurrent.ToString("0.00", CultureInfo.InvariantCulture) + "A");
            if (response.Status == null)
                throw new Cm1620Exception("missing adjust status");
            return decimal.Parse(response.Status[0..^1], CultureInfo.InvariantCulture);
        }

        public async Task StopCommand()
        {
            await _communication.SendCommand("stop");
        }

        public async Task<RecoverResponse> RecoverCommand()
        {
            var response = await _communication.SendCommand("recover");
            return Enum.Parse<RecoverResponse>(response.Status ?? throw new Cm1620Exception("missing recover status"), true);
        }

        public async Task LogoutCommand()
        {
            await _communication.SendCommand("logout");
        }

        public async Task RebootCommand()
        {
            await _communication.SendCommand("reboot");
        }

        [GeneratedRegex(@"^(?<slave>SL\d+) (?<result>[\S]+)$")]
        private static partial Regex LoginResponseRegex();

        [GeneratedRegex(@"^(?<slave>SL\d+) (?<model>[\S]+) AP(?<sl>[\d\.]+) BT(?<bt>[\d\.]+) HW(?<hw>[\d\.]+)$")]
        private static partial Regex HelloResponseRegex();
        [GeneratedRegex(@"^(?<slave>SL\d+) charge (?<chemistry>[\S]+) (?<cellv>[\d\.]+)V (?<cellseries>(?:[\d]+S|auto)) (?<capacity>[\d\.]+)mAh (?<current>[\d\.]+)A (?<chargemode>[\S]+)$")]
        private static partial Regex TaskResponseRegex();
        [GeneratedRegex(@"^(?<slave>SL\d+) (?<inputv>[\d\.]+)V (?<outputv>[\d\.]+)V (?<temp>[\d]+)C (?<battgo>[YN]+) (?<percent>[\d\.]+)% (?<balmode>[\S]+) (?<error>[\d]+) (?<chargemode>[\S]+)$")]
        private static partial Regex StatusResponseRegex();
        [GeneratedRegex(@"^(?<taskcurrent>[\d\.]+)A (?<inputpower>[\d\.]+)W (?<outputcurrent>[\d\.]+)A (?<chargecapacity>[\d\.]+)mAh (?<time>[\d\:]+)$")]
        private static partial Regex StatusResponseChargeRegex();
    }
}
