﻿namespace XamlColorSchemeGenerator
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // TODO: Add flags for some parameters.
                if (args.Any())
                {
                    var inputFile = args[0];

                    using (var mutex = Lock(inputFile))
                    {
                        try
                        {
                            var generator = new ColorSchemeGenerator();

                            generator.GenerateColorSchemeFiles(inputFile);
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("You have pass the generator input file as a commandline parameter.");
                    return 1;
                }

                // TODO: Add help output.

                stopwatch.Stop();
                Trace.WriteLine($"Generation time: {stopwatch.Elapsed}");

                return 0;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);

                if (Debugger.IsAttached)
                {
                    Console.ReadLine();
                }

                return 1;
            }
        }

        private static Mutex Lock(string file)
        {           
            var mutexName = $"Local\\XamlColorSchemeGenerator_{file.GetHashCode()}";

            var mutex = new Mutex(false, mutexName);

            if (mutex.WaitOne(TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException("Another instance of this application blocked the concurrent execution.");
            }
            
            return mutex;
        }
    }
}