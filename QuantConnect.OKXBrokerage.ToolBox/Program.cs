/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Linq;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.ToolBox;

namespace QuantConnect.OKXBrokerage.ToolBox
{
    /// <summary>
    /// OKX ToolBox entry point
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            // Parse command-line arguments
            var optionsObject = ToolboxArgumentParser.ParseArguments(args);

            if (optionsObject.Count == 0)
            {
                PrintMessageAndExit();
                return;
            }

            // Check for required --app parameter
            if (!optionsObject.TryGetValue("app", out var targetApp))
            {
                PrintMessageAndExit(1, "ERROR: --app value is required");
                return;
            }

            var targetAppName = targetApp?.ToString().ToLowerInvariant();

            // Check for symbol properties updater variants
            if (targetAppName.Contains("updater") ||
                targetAppName.EndsWith("spu") ||
                targetAppName == "okxsymbolpropertiesupdater" ||
                targetAppName == "okxspu")
            {
                Log.LogHandler = new CompositeLogHandler();

                try
                {
                    // Always use production API for symbol properties updates
                    Log.Trace($"Program.Main(): Environment: Production (hardcoded)");
                    Log.Trace($"Program.Main(): API URL: {OKXEnvironment.ProductionApiUrl}");

                    var downloader = new OKXExchangeInfoDownloader();
                    var updater = new ExchangeInfoUpdater(downloader);

                    Log.Trace("Program.Main(): Starting OKX symbol properties update...");
                    updater.Run();
                    Log.Trace("Program.Main(): Symbol properties update completed successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Program.Main(): Failed to update symbol properties");
                    PrintMessageAndExit(1, $"ERROR: {ex.Message}");
                }
            }
            else
            {
                PrintMessageAndExit(1, $"ERROR: Unrecognized --app value: {targetAppName}");
            }
        }

        /// <summary>
        /// Prints message and exits
        /// </summary>
        private static void PrintMessageAndExit(int exitCode = 0, params string[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                Console.WriteLine("OKX ToolBox - Symbol Properties Updater");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run --project QuantConnect.OKXBrokerage.ToolBox --app okxspu");
                Console.WriteLine("  dotnet run --project QuantConnect.OKXBrokerage.ToolBox --app OKXSymbolPropertiesUpdater");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  --app <app-name>        Required. Application to run (okxspu, OKXSymbolPropertiesUpdater)");
                Console.WriteLine();
                Console.WriteLine("Note:");
                Console.WriteLine("  This tool always uses the Production API (https://api.okxio.ws/api/v4)");
                Console.WriteLine("  to download symbol properties for live trading.");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  dotnet run --project QuantConnect.OKXBrokerage.ToolBox --app okxspu");
            }
            else
            {
                foreach (var message in messages.Where(m => !string.IsNullOrWhiteSpace(m)))
                {
                    if (exitCode > 0)
                    {
                        Log.Error(message);
                    }
                    else
                    {
                        Log.Trace(message);
                    }
                }
            }

            Environment.Exit(exitCode);
        }
    }
}
