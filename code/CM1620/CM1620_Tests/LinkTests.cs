using CM1620;
using CM1620.Models;
using Moq;
using NUnit.Framework;
using System.Globalization;

namespace CM1620_Tests
{
    public class LinkTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task HelloCmd()
        {
            var comm = new Mock<ICommunication>();
            comm.Setup(x => x.SendCommand("hello", null)).ReturnsAsync(new IsdtResponse("2", ["SL0 CM1620 AP1.0.0.0 BT1.0.0.0 HW1.0.0.0", "SL1 CM1620 AP1.0.0.0 BT1.0.0.0 HW1.0.0.0"]));

            var sut = new Cm1620Link(comm.Object);

            var response = await sut.Hello().ToListAsync();

            Assert.That(response, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(response[0].Slave, Is.EqualTo("SL0"));
                Assert.That(response[0].Model, Is.EqualTo("CM1620"));
                Assert.That(response[0].SoftwareVersion, Is.EqualTo(Version.Parse("1.0.0.0")));
                Assert.That(response[0].BootloaderVersion, Is.EqualTo(Version.Parse("1.0.0.0")));
                Assert.That(response[0].HardwareVersion, Is.EqualTo(Version.Parse("1.0.0.0")));
            });
            Assert.Multiple(() =>
            {
                Assert.That(response[1].Slave, Is.EqualTo("SL1"));
                Assert.That(response[1].Model, Is.EqualTo("CM1620"));
                Assert.That(response[1].SoftwareVersion, Is.EqualTo(Version.Parse("1.0.0.0")));
                Assert.That(response[1].BootloaderVersion, Is.EqualTo(Version.Parse("1.0.0.0")));
                Assert.That(response[1].HardwareVersion, Is.EqualTo(Version.Parse("1.0.0.0")));
            });
        }

        [Test]
        public async Task StatusCmd_1()
        {
            var comm = new Mock<ICommunication>();
            var lines = new[]
            {
                "SL0 32.0V 24.0V 56C N 85% UBL 000 ConstCurChging",
                "40.0A 1000W 39.9A 15200mAh 00:00:30",
                "SL1 32.0V 24.0V 56C N 85% UBL 000 ParallelChging",
                "SL2 32.0V 24.0V 56C N 85% UBL 000 ParallelChging"
            };

            comm.Setup(x => x.SendCommand("status", null)).ReturnsAsync(new IsdtResponse("3", lines));

            var sut = new Cm1620Link(comm.Object);

            var result = await sut.StatusQuery();

            Assert.Multiple(() =>
            {
                Assert.That(result.ChargingStage, Is.EqualTo(ChargingStage.ConstCurChging));

                Assert.That(result.ChargeStatus, Is.Not.Null);
                var chargeStatus = result.ChargeStatus!;
                Assert.That(chargeStatus.TaskCurrent, Is.EqualTo(40));
                Assert.That(chargeStatus.InputPower, Is.EqualTo(1000));
                Assert.That(chargeStatus.OutputCurrent, Is.EqualTo(39.9m));
                Assert.That(chargeStatus.CapacityChargedMah, Is.EqualTo(15200));
                Assert.That(chargeStatus.TimeCharged, Is.EqualTo(TimeSpan.FromSeconds(30)));
            });

            Assert.That(result.DeviceStatus, Has.Length.EqualTo(3));

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[2];
                Assert.That(response.Slave, Is.EqualTo("SL2"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });
        }

        [Test]
        public async Task StatusCmd_2()
        {
            var comm = new Mock<ICommunication>();
            var lines = new[]
            {
                "SL0 32.0V 24.0V 56C N 85% BVR 000 ConstCurChging",
                "20.0A 400W 19.8A 15200mAh 00:00:30",
                "3.785 3.785 3.785 3.785 3.785 3.785 3.785 3.785 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0",
                "3.6 3.6 3.6 3.6 3.6 3.6 3.6 3.6 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0",

                "SL1 32.0V 24.0V 56C N 100% BV 000 ParallelChging",
                "3.785 3.785 3.785 3.785 3.785 3.785 3.785 3.785 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0"
            };

            comm.Setup(x => x.SendCommand("status", null)).ReturnsAsync(new IsdtResponse("2", lines));

            var sut = new Cm1620Link(comm.Object);

            var result = await sut.StatusQuery();

            Assert.Multiple(() =>
            {
                Assert.That(result.ChargingStage, Is.EqualTo(ChargingStage.ConstCurChging));

                Assert.That(result.ChargeStatus, Is.Not.Null);
                var chargeStatus = result.ChargeStatus!;
                Assert.That(chargeStatus.TaskCurrent, Is.EqualTo(20));
                Assert.That(chargeStatus.InputPower, Is.EqualTo(400));
                Assert.That(chargeStatus.OutputCurrent, Is.EqualTo(19.8));
                Assert.That(chargeStatus.CapacityChargedMah, Is.EqualTo(15200));
                Assert.That(chargeStatus.TimeCharged, Is.EqualTo(TimeSpan.FromSeconds(30)));
            });

            Assert.That(result.DeviceStatus, Has.Length.EqualTo(2));
            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellVoltages, new[] { 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m });

                Assert.That(response.CellResistancesMilliOhm, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellResistancesMilliOhm, new[] { 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m });
            });

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(100));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellVoltages, new[] { 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m });

                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });
        }

        [Test]
        public async Task StatusCmd_RealDevice_1()
        {
            var comm = new Mock<ICommunication>();
            var lines = new[] // from real device
            {
                "SL0 52.3V 27.6V 37C N 92% UBL 000 ConstCurChging",
                "33.0A 978W 32.6A 873mAh 00:01:48",
                "SL1 52.8V 27.6V 37C N 93% UBL 000 ParallelChging",
            };

            comm.Setup(x => x.SendCommand("status", null)).ReturnsAsync(new IsdtResponse("2", lines));

            var sut = new Cm1620Link(comm.Object);

            var result = await sut.StatusQuery();

            Assert.Multiple(() =>
            {
                Assert.That(result.ChargingStage, Is.EqualTo(ChargingStage.ConstCurChging));

                Assert.That(result.ChargeStatus, Is.Not.Null);
                var chargeStatus = result.ChargeStatus!;
                Assert.That(chargeStatus.TaskCurrent, Is.EqualTo(33));
                Assert.That(chargeStatus.InputPower, Is.EqualTo(978));
                Assert.That(chargeStatus.OutputCurrent, Is.EqualTo(32.6));
                Assert.That(chargeStatus.CapacityChargedMah, Is.EqualTo(873));
                Assert.That(chargeStatus.TimeCharged, Is.EqualTo(TimeSpan.Parse("00:01:48", CultureInfo.InvariantCulture)));
            });

            Assert.That(result.DeviceStatus, Has.Length.EqualTo(2));
            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(52.3m));
                Assert.That(response.OutputVoltage, Is.EqualTo(27.6m));
                Assert.That(response.Temperature, Is.EqualTo(37));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(92));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(52.8m));
                Assert.That(response.OutputVoltage, Is.EqualTo(27.6m));
                Assert.That(response.Temperature, Is.EqualTo(37));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(93));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });
        }

        [Test]
        public async Task StatusCmd_RealDevice_2()
        {
            var comm = new Mock<ICommunication>();
            var lines = new[] // from real device
            {
                "SL0 53.5V 26.5V 35C N 62% BV 000 standby",
                "3.330 3.331 3.336 3.329 3.335 3.331 3.333 3.331 0.000 0.000 0.000 0.000 0.000 0.000 0.000 0.000",
                "SL1 53.8V 26.8V 34C N 80% UBL 000 standby",
            };

            comm.Setup(x => x.SendCommand("status", null)).ReturnsAsync(new IsdtResponse("2", lines));

            var sut = new Cm1620Link(comm.Object);

            var result = await sut.StatusQuery();

            Assert.Multiple(() =>
            {
                Assert.That(result.ChargingStage, Is.EqualTo(ChargingStage.Standby));
                Assert.That(result.ChargeStatus, Is.Null);
            });

            Assert.That(result.DeviceStatus, Has.Length.EqualTo(2));
            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(53.5m));
                Assert.That(response.OutputVoltage, Is.EqualTo(26.5m));
                Assert.That(response.Temperature, Is.EqualTo(35));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(62));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellVoltages, new[] { 3.330m, 3.331m, 3.336m, 3.329m, 3.335m, 3.331m, 3.333m, 3.331m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m });

                Assert.That(response.CellResistancesMilliOhm, Is.Null);

            });

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(53.8m));
                Assert.That(response.OutputVoltage, Is.EqualTo(26.8m));
                Assert.That(response.Temperature, Is.EqualTo(34));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(80));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });
        }

        [Test]
        public async Task StatusCmd_RealDevice_3()
        {
            var comm = new Mock<ICommunication>();
            var lines = new[] // from real device
            {
                "SL0 53.8V 26.7V 28C N 0% UBL 000 standby",
                "SL1 53.5V 26.4V 28C N 2% BV 000 standby",
                "3.322 3.323 3.326 3.319 3.324 3.322 3.325 3.322 0.000 0.000 0.000 0.000 0.000 0.000 0.000 0.000",
            };

            comm.Setup(x => x.SendCommand("status", null)).ReturnsAsync(new IsdtResponse("2", lines));

            var sut = new Cm1620Link(comm.Object);

            var result = await sut.StatusQuery();

            Assert.Multiple(() =>
            {
                Assert.That(result.ChargingStage, Is.EqualTo(ChargingStage.Standby));
                Assert.That(result.ChargeStatus, Is.Null);
            });

            Assert.That(result.DeviceStatus, Has.Length.EqualTo(2));
            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(53.8m));
                Assert.That(response.OutputVoltage, Is.EqualTo(26.7m));
                Assert.That(response.Temperature, Is.EqualTo(28));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(0));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);

            });

            Assert.Multiple(() =>
            {
                var response = result.DeviceStatus[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(53.5m));
                Assert.That(response.OutputVoltage, Is.EqualTo(26.4m));
                Assert.That(response.Temperature, Is.EqualTo(28));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(2));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));

                Assert.That(response.CellVoltages, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellVoltages, new[] { 3.322m, 3.323m, 3.326m, 3.319m, 3.324m, 3.322m, 3.325m, 3.322m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m, 0.000m });

                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });
        }
    }
}