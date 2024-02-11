# ISDT CM1620
Documentation and code for the ISDT CM1620 charging module

## General

The ISDT CM1620 is a charging module, intended for industrial use. In contrast to the "hobby-grade" chargers it does not have buttons to use the device, instead you are expected to remote control it, (either with a display/controller that can be bought separately or via a PC) which makes sense for industrial use.  
It can also be controlled via a smartphone app for a more stand-alone usage.

Official specifications:
- 11-70V / 1100W input
- 8-70.4V 20A / 1000W output
- LiHv, LiPo, LiFe 2-16S balance charging
- Micro USB, Bluetooth, RS485, CAN

Despite its drawbacks (see Reverse Engineering Discoveries), the cheap price might still make this device a useful tool for battery charging.

## Documentation
There is some official documentation and C# example code on the ISDT website:
https://www.isdt.co/down/openSource/CM1620.zip

While the documentation and code are very helpful, they still aren't great.
The documentation is incomplete and the code quality isn't great. For example, the code breaks depending on the system language/culture, the request/response messages in the code comments often don't match what the device actually sends, etc.

## Reverse Engineering Discoveries

### Battery Chemistries
In addition to the chemistries defined in the spec, there seems to be an extra chemistry called `ULiHv`.

Also, there is an artificial cell voltage limitation, which makes this device unsuitable to charge LiIon batteries (with the stock firmware), as 4.1V is just outside the LiPo voltage range.

- `LiFe`: 3.555V to 3.755V
- `LiPo`: 4.105V to 4.305V
- `LiHv`: 4.255V to 4.455V
- `ULiHv` (undocumented): 4.350V to 4.550V

Possibly, the `LiFe` mode could be used to storage+balance charge LiIon/LiPo batteries, even if it's not intended to be used like that.

In theory, that limit could easily be patched out by modifying two bytes in the firmware, but this hasn't been tested.

### Communication Protocols

- Micro USB
    - USB serial
    - documented ASCII serial protocol with example code
- Bluetooth
    - For use with "ISD Go" app
    - Completely different binary-based BLE protocol (could likely be reverse engineered from the app if needed)
- RS485
    - used for the optional controller and communication between multiple chargers (for parallel use)
    - uses the USB-C D+ and D- pins (D+ is A, D- is B) at 250000 baud 8N1
    - has something connected to the CC pins, however it's unclear what it's used for
    - "Host" port can be used to communicate in the same way as Micro USB
    - "Ext" port doesn't respond to commands
    - "Slave" port repeatedly sends `#hello SL1` to find the next charger in the chain
- CAN
    - as far as we can tell, the CAN support is just a plain lie. There does not seem to be CAN support in the firmware and a CAN transceiver is nowhere to be found on the PCB. 
    - But there is an unpopulated SOIC-8 footprint besides the RS485 transceiver for the "EXT" port, that matches the generic CAN-transceiver pinout. So they at least seem to have planned for CAN support.

In addition to the documented default password `null` used in the serial communication, there seem to be additional passwords like `tdsi` (ISDT backwards) and `_monitor_` likely used by the communication between chargers and between charger+display.

Additional, less relevant, reverse-engineering notes can be found in the [README inside the RevEng/ folder](RevEng/).

## Serial Protocol

The serial protocol is based on simple request-response communication.
It uses `#` as a sign for the controller and `@` as a sign for the responder. Commands and their responses may span multiple lines, each (including the last one) ending in `\n`. The end of the command/response is always marked with `\r`.

For most commands, the user has to be "logged in" (otherwise the device will respond with `@confused`). Logging in just means the `#login` command has to be sent with a password. The password (under normal circumstances) always seems to be "null", however there are some internal passwords (see Reverse Engineering Discoveries).
The login times out after ~3 seconds after the last command has been sent, so either commands have to be sent continuously, or the `@confused` message should be handled to relogin on the device.

In most commands, the response of all devices in the chain is returned. The first line usually contains the number of responses, followed by one line per device.

Example:

(`> ` is controller->device, `< ` is device->controller, `\n` is shown as a line break): 
```
> #login null
> \r
< @login 2
< SL0 ok
< SL1 ok
< \r
```
`SL0` and `SL1` refer to the two connected chargers ("slaves").

### Example code
Based on the serial protocol documentation, the example code and some reverse engineering, I've written some example code in C# that implements most of the protocol commands as straight-forward as I could (arguably, RegEx might not be the most readable way to parse these messages, but it's one of the easier ways to do it).

It can be found under `code/`.

## Hardware
### Teardown
- Remove the 4 screws from the front plate (display side)
- Losen the two grub screws on the side of the aluminum housing
- Carefully remove the front plate by unclipping the two clips on the top and bottom
    - :warning: Caution! There is a short, fragile ribbon cable between the display and the main PCB
- Slide out the whole PCB

Some pictures of the insides can be found in the [RevEng/](RevEng/) folder.