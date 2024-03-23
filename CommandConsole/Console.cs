using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace HenriHuh.Commands
{
    public class CommandConsoleSettings
    {
        /// <summary>
        /// Only show methods that use <see cref="CommandAttribute"/>
        /// </summary>
        public bool requireAttribute = true;

        public ICommandLogger logger;
    }

    public interface ICommandLogger
    {
        public void Log(string message);
        public void LogError(string message);

    }

    public class UnityDebugLogger : ICommandLogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }

    /// <summary>
    /// Command console for invoking methods of target class using reflection.
    /// </summary>
    public class CommandConsole
    {

        private List<System.Reflection.MethodInfo> commands = new List<System.Reflection.MethodInfo>();
        public object[] targets { get; private set; }
        public CommandConsoleSettings Settings { get; private set; }
        public Dictionary<string, object> variables { get; private set; } = new Dictionary<string, object>();
        public ICommandLogger CommandLogger => Settings.logger;


        public CommandConsole(object target, CommandConsoleSettings Settings = null)
        {
            if (Settings == null)
            {
                this.Settings = new CommandConsoleSettings();
            }
            else
            {
                this.Settings = Settings;
            }

            System.Type targetType = target.GetType();
            targets = new object[] { target };
            System.Reflection.MethodInfo[] mInfo = targetType.GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static);

            for (int i = 0; i < mInfo.Length; i++)
            {
                if (!Settings.requireAttribute || mInfo[i].GetCustomAttributes(typeof(CommandAttribute), true).Length > 0)
                {
                    commands.Add(mInfo[i]);
                }
            }
        }


        public CommandConsole(object[] targets, CommandConsoleSettings Settings = null)
        {
            if (Settings == null)
            {
                this.Settings = new CommandConsoleSettings();
            }
            else
            {
                this.Settings = Settings;
            }

            this.targets = targets;
            List<Type> types = new List<Type>();
            for (int i = 0; i < this.targets.Length; i++)
            {
                System.Type targetType = targets[i].GetType();
                if (types.Contains(targetType))
                {
                    Debug.LogWarning("Multiple objects of syme type is not allowed!");
                    continue;
                }
                types.Add(targetType);
                System.Reflection.MethodInfo[] mInfo = targetType.GetMethods(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static);

                for (int j = 0; j < mInfo.Length; j++)
                {
                    if (!Settings.requireAttribute || mInfo[j].GetCustomAttributes(typeof(CommandAttribute), true).Length > 0)
                    {
                        commands.Add(mInfo[j]);
                    }
                }
            }
        }

        public static CommandConsole OpenConsoleOnObject(UnityEngine.Object obj, CommandConsoleSettings settings = null)
        {
            if (obj != null && obj is GameObject gameObject)
            {
                MonoBehaviour[] monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
                CommandConsole console = new CommandConsole(monoBehaviours, settings);
                return console;
            }
            else
            {
                Debug.LogError("Object is null or not GameObject: " + obj);
                return null;
            }

        }

        [Command]
        public void Help()
        {
            CommandLogger?.Log("List of commands:");
            List<string> cmds = GetMethodNamesAndParams();
            for (int i = 0; i < cmds.Count; i++)
            {
                CommandLogger?.Log(cmds[i]);
            }
        }

        public object InvokeOrAssign(string operation)
        {
            if (operation.Contains("=") && (operation.IndexOf('=') < operation.IndexOf('(') || !operation.Contains("(")))
            {
                if (operation.IndexOf('=') == 0)
                {
                    CommandLogger?.LogError("Invalid variable");
                    return null;
                }
                string variableName = operation.Substring(0, operation.IndexOf('=') - 1);
                variableName = variableName.Trim();

                string method = operation.Substring(operation.IndexOf('=') + 1);
                method = method.TrimStart();

                object retObject = Invoke(method);
                if (retObject != null)
                {
                    variables[variableName] = retObject;
                }

                return retObject;
            }
            else
            {
                // Convert: "Command arg" -> "Command(arg)"
                if(!operation.Contains('(') && operation.Contains(' '))
                {
                    operation = operation.Replace(' ', '(') + ')';
                }

                object retObject = Invoke(operation);
                return retObject;

            }

        }

        /// <summary>
        /// Invoke method by name from assigned target.
        /// </summary>
        public object Invoke(string methodName)
        {

            // Check assigned variables
            if (variables.TryGetValue(methodName.Trim(), out object variableObject))
            {
                Settings.logger.Log(methodName.Trim() + ": " + variableObject.ToString());
                return variableObject;
            }

            List<object> args = new List<object>();
            int argPos = methodName.IndexOf("(");
            if (argPos > 0 && methodName[argPos + 1] != ')')
            {
                // Remove method name from string. Example: "Method(Other(arg))" -> "Other(arg))"
                string stringArgs = methodName.Remove(methodName.Length - 1).Substring(argPos + 1);

                if (stringArgs.Contains('('))
                {
                    int leftbrackets = 0;
                    int rightBrackets = 0;
                    int splitIndex = 0;
                    for (int i = 0; i < stringArgs.Length; i++)
                    {
                        if (stringArgs[i] == '(')
                        {
                            leftbrackets++;
                        }
                        else if (stringArgs[i] == ')')
                        {
                            rightBrackets++;
                        }
                        else if (stringArgs[i] == ',' && leftbrackets == rightBrackets)
                        {
                            if (leftbrackets > 0)
                            {
                                string otherMethod = stringArgs.Substring(splitIndex, i - splitIndex);
                                otherMethod = otherMethod.TrimStart().TrimEnd();
                                args.Add(Invoke(otherMethod));
                                leftbrackets = 0;
                                rightBrackets = 0;
                            }
                            else
                            {
                                args.Add(stringArgs.Substring(splitIndex, i - splitIndex));
                            }
                            splitIndex = i + 1;
                        }
                    }
                    if (stringArgs.Substring(splitIndex + 1).Contains('('))
                    {
                        string otherMethod = stringArgs.Substring(splitIndex);
                        otherMethod = otherMethod.TrimStart().TrimEnd();
                        args.Add(Invoke(otherMethod));
                    }
                    else
                    {
                        args.Add(stringArgs.Substring(splitIndex));
                    }
                }
                else
                {
                    string[] splittedArgs = stringArgs.Split(',');
                    string sss = "Args: ";
                    for (int i = 0; i < splittedArgs.Length; i++)
                    {
                        sss += splittedArgs[i] + ", ";
                    }
                    args.AddRange(splittedArgs);
                }
            }

            if (argPos > 0)
            {
                // Remove arguments from str
                methodName = methodName.Substring(0, argPos);
            }

            for (int i = 0; i < commands.Count; i++)
            {
                if (methodName.Equals(commands[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    System.Reflection.ParameterInfo[] paramInfos = commands[i].GetParameters();
                    if (args.Count != paramInfos.Length || (args.Count > 0 && args[0].GetType() == typeof(string) && (string)args[0] == ""))
                    {
                        string cmdFullName = commands[i].Name + "(";
                        for (int j = 0; j < paramInfos.Length; j++)
                        {
                            cmdFullName += paramInfos[j].Name + " : " + paramInfos[j].ParameterType;
                            cmdFullName += j == paramInfos.Length - 1 ? "" : ", ";
                        }

                        string GetArgs()
                        {
                            string rtrnargs = "";
                            for (int k = 0; k < args.Count; k++)
                            {
                                rtrnargs += args[k].ToString() + ", ";
                            }
                            return rtrnargs;
                        }

                        CommandLogger?.LogError("Argument count is invalid for: " + cmdFullName + ") Input method: " + methodName + ", Args: " + GetArgs());
                        return null;
                    }

                    if (args.Count > 0)
                    {
                        bool parseSucceeded = TryParseArgs(args, paramInfos);

                        if (!parseSucceeded)
                        {
                            return null;
                        }
                    }

                    object returnValue = commands[i].Invoke(targets.First(n => n.GetType() == commands[i].ReflectedType), args.ToArray());
                    CommandLogger?.Log("Method invoked: " + commands[i].Name + "(" + string.Join(", ", args) + ")");
                    return returnValue;
                }
            }

            CommandLogger?.LogError("No method found with given name: " + methodName);
            return null;
        }

        private bool TryParseArgs(List<object> args, System.Reflection.ParameterInfo[] paramInfos)
        {
            for (int j = 0; j < paramInfos.Length; j++)
            {
                if (args[j] == null)
                {
                    CommandLogger?.LogError("Argument " + j + " is null. Expected: " + paramInfos[j].ParameterType);
                    return false;
                }

                bool isString = args[j].GetType() == typeof(string);

                if (isString)
                {
                    string argString = args[j].ToString().Trim();
                    if (variables.TryGetValue(argString, out object variableObject))
                    {
                        args[j] = variableObject;
                        continue;
                    }
                }
                if (paramInfos[j].ParameterType == typeof(object) || paramInfos[j].ParameterType == args[j].GetType())
                {
                    continue;
                }


                try
                {

                    if (paramInfos[j].ParameterType == typeof(int))
                    {
                        args[j] = int.Parse((string)args[j]);
                    }
                    else if (paramInfos[j].ParameterType == typeof(float))
                    {
                        args[j] = float.Parse((string)args[j]);
                    }
                    else if (paramInfos[j].ParameterType == typeof(double))
                    {
                        args[j] = double.Parse((string)args[j]);
                    }
                    else if (paramInfos[j].ParameterType == typeof(bool))
                    {
                        args[j] = bool.Parse((string)args[j]);
                    }
                    else if (paramInfos[j].ParameterType.IsEnum)
                    {
                        args[j] = Enum.Parse(paramInfos[j].ParameterType, (string)args[j], true);
                    }
                    else if (paramInfos[j].ParameterType != typeof(string))
                    {
                        CommandLogger?.LogError("Method param type not implemented: " + args[j] + " to " + paramInfos[j].ParameterType);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    CommandLogger?.LogError("Could not parse argument " + args[j] + " to " + paramInfos[j].ParameterType + ". " + e.Message);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all target method names with autocomplete.
        /// </summary>
        public List<string> GetMethodNames(string startWith = "")
        {
            List<string> names = new List<string>();
            for (int i = 0; i < commands.Count; i++)
            {

                if (!commands[i].Name.StartsWith(startWith, StringComparison.OrdinalIgnoreCase)) continue;

                string n = commands[i].Name;
                names.Add(n);
            }
            return names;
        }

        /// <summary>
        /// Get all target method names and parameters with autocomplete.
        /// </summary>
        public List<string> GetMethodNamesAndParams(string startWith = "")
        {

            List<string> names = new List<string>();
            for (int i = 0; i < commands.Count; i++)
            {

                if (!commands[i].Name.StartsWith(startWith, StringComparison.OrdinalIgnoreCase)) continue;

                string nameAndParams = commands[i].Name;
                System.Reflection.ParameterInfo[] paramInfos = commands[i].GetParameters();
                if (paramInfos.Length > 0)
                {
                    nameAndParams += "(";
                    for (int j = 0; j < paramInfos.Length; j++)
                    {
                        nameAndParams += j == 0 ? "" : ", ";
                        nameAndParams += paramInfos[j].Name + " : " + paramInfos[j].ParameterType;
                    }
                    nameAndParams += ")";
                }
                names.Add(nameAndParams);
            }
            return names;
        }

    }
    public class CommandAttribute : Attribute
    {

    }
}



