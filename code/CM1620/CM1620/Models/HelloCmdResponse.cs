namespace CM1620.Models
{
    public record HelloCmdResponse(string Slave, string Model, Version SoftwareVersion, Version BootloaderVersion, Version HardwareVersion);
}