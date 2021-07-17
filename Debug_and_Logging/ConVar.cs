using System;
using System.Collections.Generic;

namespace MinecraftClone.Debug_and_Logging
{
    public class Convar
    {
        public string Description; 
        public string HelpMessage;
        readonly bool _canEdit;
        readonly List<Action<string>> _changeNotifications;
        string _variable;
        
        Convar(string Name, string Desc, string Help, string Thing, bool ReadOnly)
        {
            SetVariable(Thing);
            _changeNotifications = new List<Action<string>>();
            Description = Desc;
            HelpMessage = Help;
            _canEdit = ReadOnly;
            
            ConsoleLibrary.BindConvar(Name ,this);
        }
        
        public string GetVariable()
        {
            return _variable;
        }
        
        public void SetVariable(string Var)
        {
            if (_canEdit)
            {
                _variable = Var;
                        
                // Callbacks to run when variable changed.
                foreach (Action<string> Action in _changeNotifications)
                {
                    Action(_variable);
                }   
            }
        }

        public void AddListener(Action<string> Listener)
        {
            _changeNotifications.Add(Listener);
        }
    }
}