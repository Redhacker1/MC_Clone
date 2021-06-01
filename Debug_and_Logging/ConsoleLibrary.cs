using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace MinecraftClone.Debug_and_Logging
{
    public static class ConsoleLibrary
    {
        static StringBuilder _scrollback = new StringBuilder();
        
        /// <summary>
        /// This enum specifies the run setting you would like to use
        /// MainThread - The process command is called when you enter the command via the "SendCommand" function
        /// OwnThread - A thread is automatically spawned by the constructor, it just works, however you must ensure that all commands are thread-safe
        /// UserDefinedThread - A thread that the implementor decides where to place, This runs in much the same mode as OwnThread
        /// however just like MainThread you are responsible for calling the process_command.
        /// This can be useful if you want this to share a thread with another item (such as a logging thread) much the same as the previous two apply
        /// The Final 2 are not implemented currently and are essentially versions that will run two different pathways depending on if the command is thread safe.
        /// The idea being you can call the MainThread process_command and either use OwnThread or UserDefinedThread. The commands that support multithreading will be threaded
        /// the ones that dont will be run on the main thread.
        /// </summary>

        public delegate string CommandFunc(params string[] Arguments);

        //Only used with threading enabled
        static readonly List<string> MtCommandQueue = new List<string>();
        
        //Only used with special threadmodes enabled
        static readonly List<string> StCommandQueue = new List<string>();

        static readonly object Printlock = new object();
        
        //


        internal struct CommandStruct
        {
            public CommandFunc Method;
            public string Description;
            public string HelpMessage;
            public bool ThreadSafe;
        }

        static readonly Dictionary<string, Convar> GVars = new Dictionary<string, Convar>();
        static readonly Dictionary<string, CommandStruct> BoundCommands = new Dictionary<string, CommandStruct>();
        
        
        static string _consoleData = string.Empty;
        
        // Function called when wanting to print out text data to a console.
        static Action<string> _printDelegate;



        public static void BindConvar(string Name, Convar Var)
        {
            GVars.Add(Name ,Var);
        }

        public static void InitConsole(Action<string> WriteOutputDelegate)
        {


            _printDelegate = WriteOutputDelegate;
            BindCommand("help", "Gives context as to what command does, usage: \"help <CommandName>\"", "Usage: \n \"help <CommandName>\". additional modifiers:\n -more - pulls up helptext", Help, true);
            BindCommand("list_commands", "Lists all bound commands, Usage: \"list_commands\"", "Usage: \"list_commands\"\n additional modifiers:\n -detailed, pulls up description", ListCommands, true);
            BindCommand("clear", "clears Console data", "Usage: clear", ClearScrollback, true);
            BindCommand("say", "prints text to console, does not ignore ';'", "Usage say \"Text\"", Say, true);
        }
        
        /// <summary>
        /// This is the always safe way of passing in a command. It will choose the method most approperate
        /// </summary>
        /// <param name="Text"></param>
        public  static void SendCommand(string Text)
        {
            process_command(Text,true);
        }

        static IEnumerable<string> SplitCommands(string ConsoleText)
        {
            List<string> FirstPass = ConsoleText.Split(';').ToList();
            if (ConsoleText.Contains('"') == false)
            {
                return FirstPass;
            }

            int EnteringLiteral = 0;
            for (int Text = 0; Text < FirstPass.Count; Text++)
            {
                string TextString = FirstPass[Text];
                if (TextString.Contains('"'))
                {
                    switch (EnteringLiteral)
                    {
                        case 0:
                            EnteringLiteral = 1;
                            break;
                        case 1:
                            EnteringLiteral = 2;
                            break;
                    }
                }

                if (EnteringLiteral != 0)
                {
                    if (Text >= 1)
                    {
                        FirstPass[Text - 1] =  FirstPass[Text - 1] + ';'+ TextString;
                        FirstPass.RemoveAt(Text);
                    }
                }

                if (EnteringLiteral == 2)
                {
                    EnteringLiteral = 0;
                }
            }

            return FirstPass;
        }

        static IEnumerable<List<string>> TokenizeCommands(IEnumerable<string> Commands)
        {
            List<List<string>> OutputList = new List<List<string>>();
            foreach (string Command in Commands)
            {
                List<string> TokenizedCommand = Command.Split(' ').ToList();
                TokenizedCommand.RemoveAll(Is_Separator);
                OutputList.Add(TokenizedCommand);
            }

            return OutputList;
        }

        /// <summary>
        ///  This is what runs in the background when running the command in the threaded mode.
        ///  I am exposing this so that it can be run manually if you want to run it in your own existing thread.
        ///  Another benefit is that it can run only when it was called on main thread, which also allows me to make seperate queues
        /// </summary>
        /// <param name="Text">This is the raw unparsed command</param>
        /// <param name="InvokedOnMain">Use when invoking on main thread so it will check main thread command queue (only useful for last two experimental modes)</param>
        /// <param name="DecideThreadingMode">Usually not necissary, really only used internally</param>
        public static void process_command(string Text,bool InvokedOnMain, bool DecideThreadingMode = false)
        {
            //Sanity check
            if(string.IsNullOrEmpty(Text))
                return;
            
            
            IEnumerable<string> TotalCommands = SplitCommands(Text);
            IEnumerable<List<string>> TokenizedCommands = TokenizeCommands(TotalCommands);

            // Runs each command
            foreach (List<string> Command in TokenizedCommands)
            {
                string CmdName = Command[0];
                _scrollback.Append(string.Join(" ", Command) + '\n');
                Command.RemoveAt(0);
                
                
                if (BoundCommands.ContainsKey(CmdName))
                {
                    string Output = BoundCommands[CmdName].Method(Command.ToArray());

                    if (Output != string.Empty)
                    {
                        DebugPrint(Output + '\n');
                    }
                }
                else if (GVars.ContainsKey(CmdName))
                {
                    if (Command.Count == 0)
                    {
                        DebugPrint(GVars[CmdName]);
                    }
                    else
                    {
                        GVars[CmdName].SetVariable(string.Join(",", Command));
                    }
                }
                else
                {
                    DebugPrint($"Command/GVar \"{CmdName}\" Not found!\n");
                }
            }
            
        }

        public static void DebugPrint(object Text)
        {
            lock (Printlock)
            {
                _scrollback.Append(Text.ToString()  + '\n');
                _printDelegate(_scrollback.ToString());    
            }
        }

        static bool Is_Separator(string Input)
        {
            return Input.Trim() == string.Empty;
        }

        //TODO: Use Attribute to automatically generate this, making it clean
        public static void BindCommand(string CommandName,string Description, string HelpText, CommandFunc Method, bool ThreadSafe)
        {
            CommandStruct Command = new CommandStruct
            {
                Method = Method, Description = Description, HelpMessage = HelpText, ThreadSafe = ThreadSafe
            };

            BoundCommands[CommandName.Trim()] = Command;
        }
        
        //Default Commands bound by constructor
        static string  Help(params string[] Args)
        {
            List<string> ArgsList = Args.ToList();
            ArgsList.Remove("help");
            if (ArgsList.Count >= 2)
            {
                bool Detailed = Args.Contains("-more");
                
                if (Detailed)
                {
                    ArgsList.Remove("more");
                    if (BoundCommands[ArgsList[0]].Description.EndsWith("\n") || BoundCommands[ArgsList[0]].HelpMessage.StartsWith("\n"))
                    {
                        return BoundCommands[ArgsList[0]].Description + BoundCommands[ArgsList[0]].HelpMessage;
                    }
                    
                    return BoundCommands[ArgsList[0]].Description +'\n'+BoundCommands[ArgsList[0]].HelpMessage;
                }
            }
            else if (ArgsList.Count == 1)
            {
                return BoundCommands[ArgsList[0]].HelpMessage;
                
            }
            return string.Empty;
        }

        static string ListCommands(params string[] Args)
        {
            StringBuilder Output = new StringBuilder();
            bool AddDescription = Args.Contains("-detailed");
            foreach (KeyValuePair<string, CommandStruct> Command in BoundCommands)
            {
                if (Command.Key != "list_commands")
                {
                    Output.Append(Command.Key);
                    if (AddDescription)
                    {
                        if (Command.Value.Description.EndsWith("\n"))
                        {
                            Output.Append(Command.Value.Description);
                        }
                        else
                        {
                            Output.Append('\n'+ Command.Value.Description);
                        }
                    }
                    Output.Append('\n');   
                }
            }
            return Output.ToString();
        }

        static string Say(params string[] Args)
        {
            StringBuilder StringBuilder = new StringBuilder();
            bool Quotes = false;
            foreach (string Thing in Args)
            {
                if (Quotes == false)
                {
                    Quotes = Thing.Contains('"');
                    StringBuilder = new StringBuilder();
                }
                StringBuilder.Append(Thing.Trim('"') + " ");
            }

            return !Quotes ? string.Empty : StringBuilder.ToString();
        }

        static string ClearScrollback(params string[] Args)
        {
            lock (Printlock)
            {
                _scrollback = new StringBuilder();
                _printDelegate(string.Empty);
                return string.Empty;   
            }
        }
    }
}