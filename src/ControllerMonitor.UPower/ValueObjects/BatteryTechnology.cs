namespace ControllerMonitor.UPower.ValueObjects;

/// <summary>
/// Battery technology types
/// </summary>
public enum BatteryTechnology
{
    Unknown = 0,
    LithiumIon = 1,
    LithiumPolymer = 2,
    LithiumIronPhosphate = 3,
    LeadAcid = 4,
    NickelCadmium = 5,
    NickelMetalHydride = 6
}