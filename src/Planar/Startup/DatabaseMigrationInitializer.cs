﻿using DbUp;
using Planar.Service;
using Planar.Service.Exceptions;
using System;
using System.Reflection;

namespace Planar.Startup
{
    public static class DatabaseMigrationInitializer
    {
        private static readonly Assembly _assembly = Assembly.Load(nameof(DatabaseMigrations));

        private static void HandleError(Exception ex)
        {
            Console.WriteLine(string.Empty.PadLeft(80, '-'));
            Console.WriteLine("Migration fail");
            Console.WriteLine(string.Empty.PadLeft(80, '-'));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();

            Console.ReadLine();
            Environment.Exit(-1);
        }

        public static void RunMigration()
        {
            if (!AppSettings.RunDatabaseMigration)
            {
                Console.WriteLine("[x] Skip database migration");
                var count = CountScriptsToExecute();
                if (count > 0)
                {
                    throw new PlanarException($"there are {count} script in database migration to run. enable database migrations in settings file or run database migrations manually");
                }

                return;
            }

            try
            {
                Console.WriteLine("[x] Run database migration");

                EnsureDatabase.For.SqlDatabase(AppSettings.DatabaseConnectionString);

                var engine =
                    DeployChanges.To
                        .SqlDatabase(AppSettings.DatabaseConnectionString)
                        .WithScriptsEmbeddedInAssembly(_assembly)
                        .WithTransaction()
                        .LogToConsole()
                        .LogScriptOutput()
                        .Build();

                var count = CountScriptsToExecute();
                Console.WriteLine($"    - Found {count} scripts to run");

                if (count == 0)
                {
                    return;
                }

                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                var result = engine.PerformUpgrade();

                if (result.Successful)
                {
                    Console.WriteLine(string.Empty.PadLeft(80, '-'));
                    Console.WriteLine("Migration success");
                    Console.WriteLine(string.Empty.PadLeft(80, '-'));
                }
                else
                {
                    HandleError(result.Error);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private static int CountScriptsToExecute()
        {
            var engine =
                    DeployChanges.To
                        .SqlDatabase(AppSettings.DatabaseConnectionString)
                        .WithScriptsEmbeddedInAssembly(_assembly)
                        .Build();

            var scripts = engine.GetScriptsToExecute();
            return scripts.Count;
        }
    }
}