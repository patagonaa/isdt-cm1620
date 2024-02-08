# Reverse Engineering Notes

## Firmware
### Rough Timeline
1. Get firmware update file from [the official API](https://www.isdt.co/down/firmwares/firmwareDownloadList.json) (used by the ISDGo flasher)
1. Decrypt firmware with [isdttool](https://github.com/quentinmit/isdttool)
1. Find out it uses the Huada Semiconductor `HC32F460` MCU via debug strings inside the binary
1. Setup Ghidra project [roughly following this guide](https://www.weigu.lu/microcontroller/ghidra_nsa/index.html)
    - Firmware offset is `0x6000`. This is probably preceded by the bootloader, but I couldn't easily find that binary.
    - [The HC32F460 SVD file](https://github.com/shadow578/platform-hc32f46x/tree/main/misc/svd) had to be modified until the script didn't throw an error anymore (didn't like overlapping sectors)
    - Include some structs and enum definitions from the [HC32F460 SDK](https://github.com/SourceWolf/HC32F460_FreeRTOS/tree/master/HC32F460Temp_FreeRTOS/driver/inc) (make sure to replace `__IO` with `volatile`, otherwise Ghidra throws an error)
1. Loose yourself for some hours in Ghidra until most of the communication protocol is understood

### Discoveries
Most discoveries are already contained in the main README, but here are some more:
- The serial protocol supports sub-nodes up until `SL7`
- It appears that the first CM1620 in the chain (`SL0`) will act as the main controller and keep track of all sub-nodes in the chain.
- Roughly around the offset `0x2B500` (`0x25500` in the binary file) is a big chunk of bitmap / font data.
- There is no access to the CAN peripheral at all and the big chunk of unanalyzed code is mostly bitmap / font data, so as of right now, there appears to be no CAN support whatsoever.
- Very rough correlations:
    - USART3/4 have something to do with task_host
    - PA9, PA11, PA12 have something to do with USBFS
    - PB7 has something to do with USART4

## Hardware
- The used Bluetooth chip is a `CH579`
- The marking of the main CPU is "DT520100 DD02", but it's actually a HC32F460.

### Connector Board
![Picture of the conenctor board with some traces marked](Hardware/ISDT%20CM1620%20Connector%20Board.png)
A very rough trace-out of the connector board.  
There is a footprint for a CAN transceiver beside the RS485 transceiver for the EXT "USB-C" port.  

As this board is replacable, they definitely planned for a CAN variant, but the firmware contians no code that even hints about any implemented CAN functionality at this point.

More pictures of the insides are in the [Hardware/img/](Hardware/img/) folder.