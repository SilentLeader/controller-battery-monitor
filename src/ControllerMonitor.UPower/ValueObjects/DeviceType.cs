namespace ControllerMonitor.UPower.ValueObjects;

/// <summary>
/// UPower device types
/// </summary>
public enum DeviceType
{
    Unknown = 0,
    LinePower = 1,
    Battery = 2,
    Ups = 3,
    Monitor = 4,
    Mouse = 5,
    Keyboard = 6,
    Pda = 7,
    Phone = 8,
    MediaPlayer = 9,
    Tablet = 10,
    Computer = 11,
    GamingInput = 12,
    Pen = 13,
    Touchpad = 14,
    ModemDevice = 15,
    Network = 16,
    Headset = 17,
    Speakers = 18,
    Headphones = 19,
    Other = 20
}