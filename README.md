# Command Console for Unity

Command console for Unity runtime and editor using C# reflection.

## Features
- Call methods and fields from the runtime or editor console
- Assign variables and use them in the console

## Demo video
[![Video](https://img.youtube.com/vi/HbKuvySszTE/0.jpg)](https://www.youtube.com/watch?v=HbKuvySszTE)

## Usage

### Runtime

1. Add the `GameConsole.cs` script to an empty GameObject.
2. Use the `CommandAttribute` or uncheck `require attribute` (this will reflect all methods and fields).
3. Add default targets for the game console. The game console will also raycast and check any 3D objects at cursor position.

```c#
    [Command]
    public float Pow(int val)
    {
        return Mathf.Pow(val, 2);
    }
```

### Editor

1. Right-click on any object in the hierarchy.
2. Select `Console` from the context menu.

### Syntax for Invoking Commands
#### Methods without parameters
- _MethodName_
- Example: GetPosition
#### Methods with single parameters
- _MethodName_ _Parameter_
- Example: Pow 2
#### Methods with single or multiple parameteres
- _MethodName_(_Parameter1, Parameter2, ..._)
- Example: Multiply(2, 4)
#### Assigning Variables
- _VariableName_ = _MethodName_(_Parameter1, Parameter2, ..._)
- Example: position = GetPosition
- Example: InstantiateItem(Box, position)

### Code

To create a new CommandConsole directly from code:

```c#
    void InitConsole()
    {
        CommandConsoleSettings settings = new CommandConsoleSettings()
        {
            requireAttribute = true,
            logger = new UnityDebugLogger()
        };
        CommandConsole console = new CommandConsole(this, settings);
    }
```
