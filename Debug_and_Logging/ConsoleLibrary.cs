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

        public delegate string CommandFunc(params string[] arguments);

        //Only used with threading enabled
        static readonly List<string> MtCommandQueue = new List<string>();
        
        //Only used with special threadmodes enabled
        static readonly List<string> StCommandQueue = new List<string>();

        static readonly object Printlock = new object();
        
        //Threading Mode

        internal struct CommandStruct
        {
            public CommandFunc Method;
            public string Description;
            public string HelpMessage;
            public bool ThreadSafe;
        }
        
        static readonly Dictionary<string, CommandStruct> BoundCommands = new Dictionary<string, CommandStruct>();
        
        
        static string _consoleData = string.Empty;
        
        // Function called when wanting to print out text data to a console.
        static Action<string> _printDelegate;

        public static void InitConsole(Action<string> writeOutputDelegate)
        {
            _printDelegate = writeOutputDelegate;
            BindCommand("help", "Gives context as to what command does, usage: \"help <CommandName>\"", "Usage: \n \"help <CommandName>\". additional modifiers:\n -more - pulls up helptext", Help, true);
            BindCommand("list_commands", "Lists all bound commands, Usage: \"list_commands\"", "Usage: \"list_commands\"\n additional modifiers:\n -detailed, pulls up description", ListCommands, true);
            BindCommand("clear", "clears Console data", "Usage: clear", ClearScrollback, true);
            BindCommand("say", "prints text to console, does not ignore ';'", "Usage say \"Text\"", Say, true);
        }
        
        /// <summary>
        /// This is the always safe way of passing in a command. It will choose the method most approperate
        /// </summary>
        /// <param name="text"></param>
        public  static void SendCommand(string text)
        {
            process_command(text,true);
        }

        static IEnumerable<string> SplitCommands(string consoleText)
        {
            List<string> firstPass = consoleText.Split(';').ToList();
            if (consoleText.Contains('"') == false)
            {
                return firstPass;
            }

            int enteringLiteral = 0;
            for (int text = 0; text < firstPass.Count; text++)
            {
                string textString = firstPass[text];
                if (textString.Contains('"'))
                {
                    switch (enteringLiteral)
                    {
                        case 0:
                            enteringLiteral = 1;
                            break;
                        case 1:
                            enteringLiteral = 2;
                            break;
                    }
                }

                if (enteringLiteral != 0)
                {
                    if (text >= 1)
                    {
                        firstPass[text - 1] =  firstPass[text - 1] + ';'+ textString;
                        firstPass.RemoveAt(text);
                    }
                }

                if (enteringLiteral == 2)
                {
                    enteringLiteral = 0;
                }
            }

            return firstPass;
        }

        static IEnumerable<List<string>> TokenizeCommands(IEnumerable<string> commands)
        {
            List<List<string>> outputList = new List<List<string>>();
            foreach (string command in commands)
            {
                List<string> tokenizedCommand = command.Split(' ').ToList();
                tokenizedCommand.RemoveAll(Is_Separator);
                outputList.Add(tokenizedCommand);
            }

            return outputList;
        }

        /// <summary>
        ///  This is what runs in the background when running the command in the threaded mode.
        ///  I am exposing this so that it can be run manually if you want to run it in your own existing thread.
        ///  Another benefit is that it can run only when it was called on main thread, which also allows me to make seperate queues
        /// </summary>
        /// <param name="text">This is the raw unparsed command</param>
        /// <param name="invokedOnMain">Use when invoking on main thread so it will check main thread command queue (only useful for last two experimental modes)</param>
        /// <param name="decideThreadingMode">Usually not necissary, really only used internally</param>
        public static void process_command(string text,bool invokedOnMain, bool decideThreadingMode = false)
        {
            GD.Print(text);
            if(string.IsNullOrEmpty(text))
                return;
            IEnumerable<string> totalCommands = SplitCommands(text);
            IEnumerable<List<string>> tokenizedCommands = TokenizeCommands(totalCommands);

            // Runs each command
            foreach (List<string> command in tokenizedCommands)
            {
                if (command.Count > 0)
                {
                    string cmdName = command[0];
                    _scrollback.Append(string.Join(" ", command) + '\n');
                    command.RemoveAt(0);
                    if (BoundCommands.ContainsKey(cmdName))
                    {
                        string output = BoundCommands[cmdName].Method(command.ToArray());

                        if (output != string.Empty)
                        {
                            DebugPrint(output + '\n');
                        }
                    }
                    else
                    {
                        DebugPrint($"Command \"{cmdName}\" Not found!\n");
                    }
                }
            }
            
        }

        public static void DebugPrint(object text)
        {
            lock (Printlock)
            {
                _scrollback.Append(text.ToString()  + '\n');
                _printDelegate(_scrollback.ToString());    
            }
        }

        static bool Is_Separator(string input)
        {
            return input.Trim() == string.Empty;
        }

        public static void BindCommand(string commandName,string description, string helpText, CommandFunc method, bool threadSafe)
        {
            CommandStruct command = new CommandStruct
            {
                Method = method, Description = description, HelpMessage = helpText, ThreadSafe = threadSafe
            };

            BoundCommands[commandName.Trim()] = command;
        }
        
        //Default Commands bound by constructor
        static string  Help(params string[] args)
        {
            List<string> argsList = args.ToList();
            argsList.Remove("help");
            if (argsList.Count >= 2)
            {
                bool detailed = args.Contains("-more");
                
                if (detailed)
                {
                    argsList.Remove("more");
                    if (BoundCommands[argsList[0]].Description.EndsWith("\n") || BoundCommands[argsList[0]].HelpMessage.StartsWith("\n"))
                    {
                        return BoundCommands[argsList[0]].Description + BoundCommands[argsList[0]].HelpMessage;
                    }
                    
                    return BoundCommands[argsList[0]].Description +'\n'+BoundCommands[argsList[0]].HelpMessage;
                }
            }
            else if (argsList.Count == 1)
            {
                return BoundCommands[argsList[0]].HelpMessage;
                
            }
            return string.Empty;
        }

        static string ListCommands(params string[] args)
        {
            StringBuilder output = new StringBuilder();
            bool addDescription = args.Contains("-detailed");
            foreach (KeyValuePair<string, CommandStruct> command in BoundCommands)
            {
                if (command.Key != "list_commands")
                {
                    output.Append(command.Key);
                    if (addDescription)
                    {
                        if (command.Value.Description.EndsWith("\n"))
                        {
                            output.Append(command.Value.Description);
                        }
                        else
                        {
                            output.Append('\n'+ command.Value.Description);
                        }
                    }
                    output.Append('\n');   
                }
            }
            return output.ToString();
        }

        static string Say(params string[] args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool quotes = false;
            foreach (string thing in args)
            {
                if (quotes == false)
                {
                    quotes = thing.Contains('"');
                    stringBuilder = new StringBuilder();
                }
                stringBuilder.Append(thing.Trim('"') + " ");
            }

            return !quotes ? string.Empty : stringBuilder.ToString();
        }

        static string ClearScrollback(params string[] args)
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