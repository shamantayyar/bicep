// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.IO.Pipes;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core.Utils;

namespace Bicep.LanguageServer
{
    public class Program
    {
        public static async Task Main(string[] args)
            => await RunWithCancellationAsync(async cancellationToken =>
            {
                string profilePath = DirHelper.GetTempPath();
                ProfileOptimization.SetProfileRoot(profilePath);
                ProfileOptimization.StartProfile("bicepserver.profile");

                // the server uses JSON-RPC over stdin & stdout to communicate,
                // so be careful not to use console for logging!

                var pipeArg = args.FirstOrDefault(x => x.StartsWith(@"--pipe=\\.\pipe\"));

                Server server;
                if (pipeArg is not null)
                {
                    var pipeName = pipeArg.Substring(@"--pipe=\\.\pipe\".Length);
                    var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                    await clientPipe.ConnectAsync(cancellationToken);

                    server = new Server(
                        clientPipe,
                        clientPipe,
                        new Server.CreationOptions());
                }
                else
                {
                    server = new Server(
                        Console.OpenStandardInput(),
                        Console.OpenStandardOutput(),
                        new Server.CreationOptions());

                    throw new InvalidOperationException("ASFASDFASF");
                }

                await server.RunAsync(cancellationToken);
            });

        private static async Task RunWithCancellationAsync(Func<CancellationToken, Task> runFunc)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                cancellationTokenSource.Cancel();
            };

            try
            {
                await runFunc(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationTokenSource.Token)
            {
                // this is expected - no need to rethrow
            }
        }
    }
}
