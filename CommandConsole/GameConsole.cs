using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HenriHuh.Commands
{

    public class GameConsole : MonoBehaviour, ICommandLogger
    {
        public KeyCode consoleToggleKey = KeyCode.BackQuote;
        public char consoleToggleChar = '§';
        [Tooltip("By default the console only reflects fields with the Command attribute")]
        public bool requireAttribute = true;
        public bool raycast3DObjects = true;
        //public bool raycast2DObjects = false; //TODO
        public List<MonoBehaviour> defaultTargets = new List<MonoBehaviour>();

        private bool consoleOpen;
        private CommandConsole console;

        private Queue<LogMessage> logQueue = new Queue<LogMessage>();
        private string consoleLog = "";
        private string userInput = "";
        private int currentPreviewIndex = 0;
        private bool focusControlOnInput = false;

        private bool initializedGUI = false;
        private GUIStyle style_LogWindow;
        private GUIStyle style_CommandPreview;

        private object[] RaycastObjectOnCursor()
        {
            List<object> monoBehaviourTargets = new List<object>();
            RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
            for (int i = 0; i < hits.Length; i++)
            {
                MonoBehaviour[] monoBehaviours = hits[i].collider.GetComponents<MonoBehaviour>();
                monoBehaviourTargets.AddRange(monoBehaviours);
            }

            return monoBehaviourTargets.ToArray();
        }

        [Command]
        public void Help()
        {
            console.Help();
        }

        private void InitConsole()
        {
            CommandConsoleSettings settings = new CommandConsoleSettings()
            {
                requireAttribute = requireAttribute,
                logger = this
            };

            object[] targets = RaycastObjectOnCursor();
            
            List<object> allTargets = new List<object>();
            allTargets.AddRange(defaultTargets);
            for (int i = 0; i < targets.Length; i++)
            {
                if (!allTargets.Contains(targets[i]))
                {
                    allTargets.Add(targets[i]);
                }
            }

            console = new CommandConsole(allTargets.ToArray(), settings);
        }

        private void OpenConsole()
        {
            consoleOpen = true;
            focusControlOnInput = true;
            InitConsole();
        }

        private void CloseConsole()
        {
            console = null;
            consoleOpen = false;
        }

        private void InitGUIStyles()
        {
            style_LogWindow = new GUIStyle(GUI.skin.box);
            style_LogWindow.alignment = TextAnchor.LowerLeft;
            style_LogWindow.normal.textColor = Color.white;
            style_LogWindow.richText = true;

            Texture2D previewTex = new Texture2D(1,1);
            previewTex.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
            previewTex.Apply();
            style_CommandPreview = new GUIStyle(GUI.skin.box);
            style_CommandPreview.normal.background = previewTex;
            style_CommandPreview.normal.textColor = Color.white;
            style_CommandPreview.alignment = TextAnchor.LowerLeft;
            style_CommandPreview.richText = true;

            initializedGUI = true;
        }

        private void OnGUI()
        {
            if (!consoleOpen)
            {
                if(Event.current.type == EventType.KeyDown && (Event.current.keyCode == consoleToggleKey || Event.current.character == consoleToggleChar))
                {
                    OpenConsole();
                }
                return;
            }

            if (!initializedGUI)
            {
                InitGUIStyles();
            }

            float logHeight = Screen.height / 2;
            float inputHeight = 20;

            Rect windowRect = new Rect(0, 0, Screen.width, logHeight);

            // Log
            consoleLog += GetLogsFromQueue();
            GUI.Box(windowRect, consoleLog, style_LogWindow);
            
            string previewStr = "";
            Rect previewRect = default;

            // Commands Preview
            if (userInput.Length > 0)
            {
                List<string> methodsAndParams = console.GetMethodNamesAndParams(userInput);
                if (methodsAndParams.Count > 0)
                {
                    const int MAX_PREVIEW_COUNT = 5;
                    int previewCount = methodsAndParams.Count > MAX_PREVIEW_COUNT ? MAX_PREVIEW_COUNT : methodsAndParams.Count;
                    float commandPreviewHeight = previewCount * inputHeight;
                    previewRect = new Rect(0, logHeight - commandPreviewHeight, Screen.width, commandPreviewHeight);
                    for (int i = 0; i < previewCount; i++)
                    {
                        string currentPreview = methodsAndParams[Tools.RepeatInt(currentPreviewIndex + i, methodsAndParams.Count)];
                        previewStr += (i > 0 ? "\n" : "") + currentPreview;
                    }

                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab && methodsAndParams.Count > 0)
                {
                    userInput = console.GetMethodNames(userInput)[Tools.RepeatInt(currentPreviewIndex, methodsAndParams.Count)];
                    focusControlOnInput = true;
                }
            }
            else
            {
                currentPreviewIndex = 0;
            }

            bool closeConsole = false;
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return)
                {
                    object ret = console.InvokeOrAssign(userInput);
                    if (ret != null)
                    {
                        LogResult(ret.ToString());
                    }
                    userInput = "";
                }
                else if (Event.current.keyCode == consoleToggleKey || Event.current.character == consoleToggleChar)
                {
                    closeConsole = true;
                }
            }
            else if (Event.current.isKey)
            {
                if(Event.current.keyCode == KeyCode.UpArrow)
                {
                    currentPreviewIndex++;
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    currentPreviewIndex--;
                }
            }

            // Input
            Rect inputRect = new Rect(0, logHeight, Screen.width, inputHeight);
            GUI.SetNextControlName("InputField");
            userInput = GUI.TextField(inputRect, userInput);
            if (closeConsole)
            {
                userInput = userInput.Trim('§');
                CloseConsole();
            }
            else if (focusControlOnInput)
            {
                GUI.FocusControl("InputField");
                focusControlOnInput = false;
                TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                textEditor.MoveCursorToPosition(new Vector2(1000, 1000));
            }

            if (previewStr.Length > 0)
            {
                GUI.Box(previewRect, previewStr, style_CommandPreview);
            }
            

        }

        private string GetLogsFromQueue()
        {
            string logsString = "";
            for (int i = 0; i < logQueue.Count; i++)
            {
                LogMessage message = logQueue.Dequeue();
                string msg = message.Message;
                switch (message.LogType)
                {
                    case LogType.Log:
                        msg = "<color=#FFFFFF>>" + msg + "</color>";
                        break;
                    case LogType.Error:
                        msg = "<color=#FFC100>" + msg + "</color>";
                        break;
                    case LogType.Result:
                        msg = "<color=#53ff38>" + msg + "</color>";
                        break;
                    default:
                        break;
                }
                logsString += "\n" + msg;
            }

            return logsString;
        }

        public void Log(string message)
        {
            logQueue.Enqueue(new LogMessage(message, LogType.Log));
        }

        public void LogError(string message)
        {
            logQueue.Enqueue(new LogMessage(message, LogType.Error));
        }

        private void LogResult(string message)
        {
            logQueue.Enqueue(new LogMessage(message, LogType.Result));
        }


        private struct LogMessage
        {
            public string Message { get; private set; }
            public LogType LogType { get; private set; }
            public LogMessage(string message, LogType type)
            {
                this.Message = message;
                this.LogType = type;
            }
        }

        public enum LogType
        {
            Log,
            Error,
            Result,
        }
    }
}
