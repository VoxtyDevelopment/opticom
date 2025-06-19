using System.Collections.Generic;

public class Config
{
    public bool UseWhitelist { get; set; }
    public List<string> WhitelistedVehicles { get; set; }
    public bool Debug { get; set; }
}