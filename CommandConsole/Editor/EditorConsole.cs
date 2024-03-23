using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HenriHuh.Commands
{

    public class CommandConsoleWindow : EditorWindow, ICommandLogger
    {
        private CommandConsole console;
        private string commandString = "";
        private string commandAutoFilled;
        private List<string> consoleOutput = new List<string>();
        private int autoFillIndex = 0;
        private string targets;
        Vector2 scrollPos;


        const int METHOD_DISPLAY_COUNT = 3;

        [MenuItem("GameObject/Console")]
        public static void OpenConsoleOnSelectedWithAttribute()
        {
            CommandConsoleSettings settings = new CommandConsoleSettings()
            {
                requireAttribute = true,
                logger = new UnityDebugLogger(),
            };

            CommandConsole console = CommandConsole.OpenConsoleOnObject(Selection.activeGameObject, settings);
            OpenConsole(console);
        }


        /// <summary>
        /// Open a generic console window in editor. Returns null in built player!
        /// </summary>
        public static void OpenConsole(CommandConsole console)
        {

            CommandConsoleWindow window = EditorWindow.CreateInstance<CommandConsoleWindow>();
            window.Init(console);
            window.Show();
            window.Focus();
            console.Settings.logger = window;
        }

        public void Init(CommandConsole console)
        {
            targets = console.targets[0].ToString();
            for (int i = 1; i < console.targets.Length; i++)
            {
                targets += ", " + console.targets[i];
            }

            this.titleContent.text = "Console: " + targets;
            this.Show();
            this.console = console;
            for (int i = 0; i < 40; i++)
            {
                consoleOutput.Add("");
            }
            scrollPos.y = consoleOutput.Count * 15;
            minSize = new Vector2(360, 240);
        }

        private void OnGUI()
        {

            GUILayout.Label("Command Window : " + targets, EditorStyles.largeLabel);
            GUILayout.BeginVertical();

            GUIStyle richStyle = new GUIStyle(GUI.skin.textArea);
            richStyle.richText = true;

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(160));

            GUILayout.TextArea(string.Join("\n", consoleOutput), richStyle, GUILayout.Height(consoleOutput.Count * 15 + 5));

            GUILayout.EndScrollView();


            GUI.SetNextControlName("command");
            commandString = GUILayout.TextField(commandString);

            List<string> methods = console.GetMethodNamesAndParams(commandString);
            autoFillIndex = autoFillIndex >= methods.Count ? 0 : autoFillIndex;
            commandAutoFilled = "";
            for (int i = 0; i < Mathf.Min(methods.Count, METHOD_DISPLAY_COUNT); i++)
            {
                string methodString = methods[Tools.RepeatInt(autoFillIndex + i, methods.Count)];
                commandAutoFilled += i > 0 ? "\n" : "";
                commandAutoFilled += commandString + "<color=#646464>" + methodString.Substring(commandString.Length) + "</color>";
            }

            GUIStyle colorLabel = new GUIStyle(GUI.skin.label);
            colorLabel.richText = true;
            GUILayout.TextField(commandAutoFilled, colorLabel);
            Rect autoFillRect = GUILayoutUtility.GetLastRect();

            if (Event.current.type == EventType.KeyUp)
            {
                if (Event.current.keyCode == KeyCode.DownArrow && methods.Count > 0)
                {
                    autoFillIndex = Tools.RepeatInt(autoFillIndex + 1, methods.Count);
                }

                if (Event.current.keyCode == KeyCode.UpArrow && methods.Count > 0)
                {
                    autoFillIndex = Tools.RepeatInt(autoFillIndex - 1, methods.Count);
                }

                if (Event.current.keyCode == KeyCode.Tab)
                {
                    List<string> auto = console.GetMethodNames(commandString);
                    if (auto.Count > 0)
                    {
                        commandString = auto[autoFillIndex];
                    }
                    Event.current.Use();
                    EditorGUI.FocusTextInControl("command");
                    this.SendEvent(Event.KeyboardEvent("right"));
                }

                if (Event.current.keyCode == KeyCode.Return)
                {
                    ConsoleOutput(commandString);
                    object returnValue = console.InvokeOrAssign(commandString);
                    if (returnValue != null)
                    {
                        LogResult(returnValue.ToString());
                    }

                    commandString = "";
                    scrollPos.y = consoleOutput.Count * 15;
                }
                Repaint();
            }
            else if (Event.current.type == EventType.ScrollWheel && autoFillRect.Contains(Event.current.mousePosition))
            {
                Repaint();
                autoFillIndex = Tools.RepeatInt(autoFillIndex - (int)Mathf.Sign(Event.current.delta.y), methods.Count);
            }

            GUILayout.EndVertical();

        }



        public void ConsoleOutput(string output)
        {
            consoleOutput.RemoveAt(0);
            consoleOutput.Add(output);
        }

        public void Log(string message)
        {
            ConsoleOutput("<color=#FFFFFF>" + message + "</color>");
        }
        public void LogResult(string message)
        {
            ConsoleOutput("<color=#53ff38>" + message + "</color>");
        }

        public void LogError(string message)
        {
            ConsoleOutput("<color=#FFC100>" + message + "</color>");
        }

    }

}