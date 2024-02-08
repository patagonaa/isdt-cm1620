using CM1620;
using CM1620.Models;
using Moq;
using NUnit.Framework;

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

            var results = await sut.StatusQuery().ToListAsync();

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.Multiple(() =>
            {
                var response = results[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));
                Assert.That(response.ChargingStage, Is.EqualTo(ChargingStage.ConstCurChging));

                Assert.That(response.ChargeStatus, Is.Not.Null);
                var chargeStatus = response.ChargeStatus!;
                Assert.That(chargeStatus.TaskCurrent, Is.EqualTo(40));
                Assert.That(chargeStatus.InputPower, Is.EqualTo(1000));
                Assert.That(chargeStatus.OutputCurrent, Is.EqualTo(39.9));
                Assert.That(chargeStatus.CapacityChargedMah, Is.EqualTo(15200));
                Assert.That(chargeStatus.TimeCharged, Is.EqualTo(TimeSpan.FromSeconds(30)));

                Assert.That(response.CellVoltages, Is.Null);
                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });

            Assert.Multiple(() =>
            {
                var response = results[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));
                Assert.That(response.ChargingStage, Is.EqualTo(ChargingStage.ParallelChging));
            });

            Assert.Multiple(() =>
            {
                var response = results[2];
                Assert.That(response.Slave, Is.EqualTo("SL2"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));
                Assert.That(response.ChargingStage, Is.EqualTo(ChargingStage.ParallelChging));
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

                "SL1 32.0V 24.0V 56C N 100% BV 000 ConstCurChging",
                "10.0A 400W 9.9A 15200mAh 00:00:30",
                "3.785 3.785 3.785 3.785 3.785 3.785 3.785 3.785 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0"
            };

            comm.Setup(x => x.SendCommand("status", null)).ReturnsAsync(new IsdtResponse("2", lines));

            var sut = new Cm1620Link(comm.Object);

            var results = await sut.StatusQuery().ToListAsync();

            Assert.That(results, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                var response = results[0];
                Assert.That(response.Slave, Is.EqualTo("SL0"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(85));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));
                Assert.That(response.ChargingStage, Is.EqualTo(ChargingStage.ConstCurChging));

                Assert.That(response.ChargeStatus, Is.Not.Null);
                var chargeStatus = response.ChargeStatus!;
                Assert.That(chargeStatus.TaskCurrent, Is.EqualTo(20));
                Assert.That(chargeStatus.InputPower, Is.EqualTo(400));
                Assert.That(chargeStatus.OutputCurrent, Is.EqualTo(19.8m));
                Assert.That(chargeStatus.CapacityChargedMah, Is.EqualTo(15200));
                Assert.That(chargeStatus.TimeCharged, Is.EqualTo(TimeSpan.FromSeconds(30)));

                Assert.That(response.CellVoltages, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellVoltages, new[] { 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m });

                Assert.That(response.CellResistancesMilliOhm, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellResistancesMilliOhm, new[] { 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 3.6m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m });
            });

            Assert.Multiple(() =>
            {
                var response = results[1];
                Assert.That(response.Slave, Is.EqualTo("SL1"));
                Assert.That(response.InputVoltage, Is.EqualTo(32m));
                Assert.That(response.OutputVoltage, Is.EqualTo(24m));
                Assert.That(response.Temperature, Is.EqualTo(56));
                Assert.That(response.BattGo, Is.False);
                Assert.That(response.BatteryPercent, Is.EqualTo(100));
                Assert.That(response.ErrorCode, Is.EqualTo(ChargeErrorCode.None));
                Assert.That(response.ChargingStage, Is.EqualTo(ChargingStage.ConstCurChging));

                Assert.That(response.ChargeStatus, Is.Not.Null);
                var chargeStatus = response.ChargeStatus!;
                Assert.That(chargeStatus.TaskCurrent, Is.EqualTo(10));
                Assert.That(chargeStatus.InputPower, Is.EqualTo(400));
                Assert.That(chargeStatus.OutputCurrent, Is.EqualTo(9.9m));
                Assert.That(chargeStatus.CapacityChargedMah, Is.EqualTo(15200));
                Assert.That(chargeStatus.TimeCharged, Is.EqualTo(TimeSpan.FromSeconds(30)));

                Assert.That(response.CellVoltages, Is.Not.Null);
                CollectionAssert.AreEqual(response.CellVoltages, new[] { 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 3.785m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m });

                Assert.That(response.CellResistancesMilliOhm, Is.Null);
            });
        }
    }
}