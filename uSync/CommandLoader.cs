//
//     All of this is a very simplified version of Chauffeur 
//     all credit to Aaron Powell
//     https://github.com/aaronpowell/Chauffeur
// 
//     When 7.3 is settled a bit, and Chauffeur works, we will
//     build uSync command on top of Chauffeur as it does this
//     in a much better and flexible way,
// 
//     this is a scaffold so we can test our ideas around the
//     command line, and see what does and doesn't work
//

namespace uSync
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Umbraco.Core;

    using Jumoo.uSync.Migrations.Commands;

    public class CommandLoader
    {
        private static CommandLoader _instance;
        public static CommandLoader Instance
        {
            get { return _instance ?? (_instance = new CommandLoader()); }
        }

        private Dictionary<string, Command> commands;

        public void Init()
        {

            object[] parms = new object[]
                {  Console.In, Console.Out };

            commands = new Dictionary<string, Command>();

            var cmdTypes = TypeFinder.FindClassesOfType<Command>();
            foreach(var command in cmdTypes)
            {
                var cmdInstance = Activator.CreateInstance(command, parms) as Command;
                if (cmdInstance != null)
                {
                    commands.Add(cmdInstance.Name(), cmdInstance);
                }
            }
        }

        public Command GetCommandItem(string name)
        {
            if (commands.ContainsKey(name))
                return commands[name];

            return null;
        }
    }
}
