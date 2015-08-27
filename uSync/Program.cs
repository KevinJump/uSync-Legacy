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
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            if (ConfigurationManager.ConnectionStrings["umbracoDbDSN"] == null)
            {
                var path = new FileInfo(Assembly.GetExecutingAssembly().Location)
                    .Directory.FullName;

                var configPath = Path.Combine(path, "..", "web.config");

                var domain = AppDomain.CreateDomain(
                    "umbraco-domain",
                    AppDomain.CurrentDomain.Evidence,
                    new AppDomainSetup
                    {
                        ConfigurationFile = configPath
                    });

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach(var assembly in assemblies)
                {
                    try
                    {
                        domain.Load(assembly.FullName);
                    }
                    catch(FileNotFoundException)
                    {
                        Console.WriteLine("Failed to load");
                    }
                }

                domain.SetData("DataDirectory", Path.Combine(path, "..", "App_Data"));
                var thisAssembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
                domain.ExecuteAssembly(thisAssembly.Name, args);
            }
            else
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Console.Write("Starting up Umbraco Instance ....");

                var app = new ConsoleApplicationBase();
                app.Start(app, new EventArgs());

                sw.Stop();
                Console.WriteLine(" Loaded ({0}ms)", sw.ElapsedMilliseconds);

                Console.WriteLine("Umbraco: {0}.{1}",
                    Umbraco.Core.Configuration.UmbracoVersion.Current.Major,
                    Umbraco.Core.Configuration.UmbracoVersion.Current.Minor);

                var host = new UmbracoHost(Console.In, Console.Out);

                if (args.Any())
                    host.Run(args).Wait();
                else
                    host.Run().Wait();
                   
            }
        }
    }
}
