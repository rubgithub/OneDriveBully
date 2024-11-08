using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace OneDriveBully
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        // A lot of help came from here: https://www.codeproject.com/articles/290013/formless-system-tray-application

        static void Main()
        {
            var args = Environment.GetCommandLineArgs(); 
            if (args.Length > 1)
            {
                if (args[1] == "bully")
                {
                    var fn = new MyFunctions();
                    if (!fn.checkUserSettings())
                    {
                        Console.Out.WriteLine("Settings are missing.");
                        return;
                    }
                    fn.bullyNowSyncFiles(); 
                    Console.Out.WriteLine("BullyNow Executed.");
                } else
                {
                    Console.Out.WriteLine("Invalid command line argument, argument valid: 'bully'");
                }
                Application.Exit();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Version 1.4 - Check if the application is already running -
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            //Version 1.4 - Check if the application is already running +


            // Show the system tray icon.					
            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();

                // Make sure the application runs!
                Application.Run();               
            }
        }
    }
}
