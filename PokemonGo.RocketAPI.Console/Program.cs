﻿#region

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Logging;
using System.Windows.Forms;
#endregion


namespace PokemonGo.RocketAPI.Console
{
    internal class Program
    {
        static int exitCode = 0;

        public static void ExitApplication(int exitCode)
        {
            Program.exitCode = exitCode;
            Application.Exit();
        }

        private static Logic.Logic CreateLogic(ISettings settings)
        {
            var logic = new Logic.Logic(settings);
            logic.ShowEditCredentials += settings.PromptForCredentials;
            return logic;
        }

        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException
                += delegate (object sender, UnhandledExceptionEventArgs eargs)
                {
                    Exception exception = (Exception)eargs.ExceptionObject;
                    System.Console.WriteLine("Unhandled exception: " + exception);
                    Environment.Exit(1);
                };

            ServicePointManager.ServerCertificateValidationCallback = Validator;
            Logger.SetLogger();

            Task.Run(() =>
            {
                bool settingsLoaded = false;
                Settings settings = null;
                try
                {
                    settings = new Settings();
                    settings.Load();
                    settingsLoaded = true;
                }
                catch (Exception e)
                {
                    Logger.Write("Could not load settings from configuration from file. Continuing with default settings. Error: " + e.ToString(), LogLevel.Error);
                }
                if (settingsLoaded)
                {
                    try
                    {
                        CreateLogic(settings).Execute().Wait();
                    }
                    catch (PtcOfflineException)
                    {
                        Logger.Write("PTC Servers are probably down OR your credentials are wrong. Try google", LogLevel.Error);
                        Logger.Write("Trying again in 60 seconds...");
                        Thread.Sleep(60000);
                        CreateLogic(settings).Execute().Wait();
                    }
                    catch (AccountNotVerifiedException)
                    {
                        Logger.Write("Account not verified. - Exiting");
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Logger.Write($"Unhandled exception: {ex}", LogLevel.Error);
                        CreateLogic(settings).Execute().Wait();
                    }
                }
            });
            System.Console.ReadLine();
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
    }
}