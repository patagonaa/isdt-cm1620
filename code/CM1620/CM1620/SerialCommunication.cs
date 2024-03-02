﻿using System.Diagnostics;
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

            _serialPort.DiscardInBuffer();
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

            Debug.WriteLine($"<{string.Join("\\n", lines).Replace("\0", "\\0")}\\r");

            if (lines.Count == 0)
                throw new Cm1620Exception("empty response");

            var splitResponseLine = lines[0].Split(' ', 2);

            var responseCmdLine = splitResponseLine[0];

            // In some cases, via RS485 we get garbage ("?" or "\0") before the response.
            // That might be due to interference (usually, when the bus pullup/pulldown is missing).
            // Either way, skipping until the "@" makes it more reliable.
            var atPosition = responseCmdLine.IndexOf('@');
            if (atPosition == -1)
                throw new Cm1620Exception($"invalid response cmd line {responseCmdLine}");
            var responseCmd = responseCmdLine[atPosition..];

            if (responseCmd.Equals("@confused", StringComparison.OrdinalIgnoreCase))
                throw new Cm1620ConfusedException($"Got @confused response. Probably Login required. {Environment.NewLine}request: {commandStr}{Environment.NewLine}expected: @{command}");
            if (!responseCmd.Equals($"@{command}", StringComparison.OrdinalIgnoreCase))
                throw new Cm1620Exception($"request: {commandStr}{Environment.NewLine}response: {responseCmd}{Environment.NewLine}expected: @{command}");

            var status = splitResponseLine.Length > 1 ? splitResponseLine[1] : null;

            return Task.FromResult(new IsdtResponse(status, lines.Skip(1).ToArray()));
        }
    }
}