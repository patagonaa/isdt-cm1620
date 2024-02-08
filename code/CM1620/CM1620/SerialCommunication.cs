using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using CM1620.Models;

namespace CM1620
{
    public class SerialCommunication : ICommunication
    {
        private readonly SerialPort _serialPort;

        public SerialCommunication(string serialPortName)
        {
            _serialPort = new SerialPort(serialPortName)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                BaudRate = 250000
            };
            _serialPort.Open();
        }

        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }

        public Task<IsdtResponse> SendCommand(string command, string? args = null)
        {
            var commandSb = new StringBuilder();
            commandSb.Append('#');
            commandSb.Append(command);
            if (args != null)
            {
                commandSb.Append(' ');
                commandSb.Append(args);
            }
            commandSb.Append("\n\r");
            var commandStr = commandSb.ToString();
            Debug.WriteLine($">{commandStr.Replace("\r", "\\r").Replace("\n", "\\n")}");
            _serialPort.Write(commandStr);

            var sb = new StringBuilder();
            var lines = new List<string>();

            while (true)
            {
                var chr = (char)_serialPort.ReadChar();
                if (chr == '\n')
                {
                    var line = sb.ToString();
                    sb.Clear();
                    lines.Add(line);
                }
                else if (chr == '\r')
                {
                    break;
                }
                else
                {
                    sb.Append(chr);
                }
            }

            Debug.WriteLine($"<{string.Join("\\n", lines)}\\r");

            if (lines.Count == 0)
                throw new Cm1620Exception("empty response");

            var splitResponseLine = lines[0].Split(' ', 2);
            var responseCmd = splitResponseLine[0];

            if (responseCmd.Equals("@confused"))
                throw new Cm1620ConfusedException($"request: {commandStr}{Environment.NewLine}response: {responseCmd}{Environment.NewLine}expected: @{command}");
            if (!splitResponseLine[0].Equals($"@{command}", StringComparison.OrdinalIgnoreCase))
                throw new Cm1620Exception($"request: {commandStr}{Environment.NewLine}response: {responseCmd}{Environment.NewLine}expected: @{command}");

            var status = splitResponseLine.Length > 1 ? splitResponseLine[1] : null;

            return Task.FromResult(new IsdtResponse(status, lines.Skip(1).ToArray()));
        }
    }
}