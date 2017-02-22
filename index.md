# Testing

This is a paragraph.

```powershell
# Parameters
$orionServer = "10.110.46.121"
$orionUsername = "admin"
$orionPassword = ""

# Connect to SWIS
$swis = Connect-Swis -Hostname $orionServer -UserName $orionUsername -Password $orionPassword

if (0) {

Invoke-SwisVerb $swis Orion.NodesCustomProperties CreateCustomPropertyWithValues @(
    "TestProp",         # Name
    "",                 # Description
    "System.String",    # ValueType
    $null,              # Size
    $null,              # ValidRange
    $null,              # Parser
    $null,              # Header
    $null,              # Alignment
    $null,              # Format
    $null,              # Units
    @("a", "b", "c"),              # Values
    $null,              # Usages
    $null,              # Mandatory
    $null               # Default
)


}

Invoke-SwisVerb $swis Orion.NodesCustomProperties ModifyCustomProperty @(
    "TestProp",         # Name
    "",                 # Description
    4000,              # Size
    @("a", "b")              # Values
)
```