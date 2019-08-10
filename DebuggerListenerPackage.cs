using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Task = System.Threading.Tasks.Task;

namespace DebuggerListener
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(DebuggerListenerPackage.PackageGuidString)]
    public sealed class DebuggerListenerPackage : AsyncPackage
    {
        public const string PackageGuidString = "19365f10-3b44-450c-96f1-7e0d2ad8fdf8";

        private DTE dte;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as DTE;
            if (dte == null) return;
            ListenerAsync();
        }

        private async Task ListenerAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    var server = new NamedPipeServerStream($"DebuggerPipe.{System.Diagnostics.Process.GetCurrentProcess().Id}");
                    await server.WaitForConnectionAsync();
                    try
                    {
                        StreamReader reader = new StreamReader(server);
                        StreamWriter writer = new StreamWriter(server);
                        string line;
                        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                        {
                            string result = await ExecuteCommandAsync(line);
                            await writer.WriteLineAsync(result);
                            await writer.FlushAsync();
                        }
                    }
                    catch (IOException) { }
                    server.Close();
                }
            });
        }

        private async Task<string> ExecuteCommandAsync(string command)
        {
            var args = command.Split(':');
            var task = args?[0];
            switch (task)
            {
                case "Debug" when args.Length == 3:
                    if (int.TryParse(args[1], out int pid))
                    {
                        return (await AttachDebuggerAsync(pid)).ToString();
                    }
                    return "";
                case "Project" when args.Length == 2:
                    if (await IsProjectOpenedAsync(args[1]))
                    {
                        return "true";
                    }
                    return "false";
            }
            return "command not supported";
        }

        private async Task<bool> AttachDebuggerAsync(int pid)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                foreach (Process process in dte.Debugger.LocalProcesses)
                {
                    if (process.ProcessID == pid)
                    {
                        process.Attach();
                        return true;
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        private async Task<bool> IsProjectOpenedAsync(string projectName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var projects = dte.Solution?.Projects;
            foreach (Project project in projects)
            {
                if (project.FullName.Contains(projectName))
                {
                    return true;
                }
            }
            return false;
        }

        //protected override void Initialize()
        //{
        //    base.Initialize();
        //    //ExternalSettingsManager ext = ExternalSettingsManager.CreateForApplication(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe");
        //    //SettingsStore store = ext.GetReadOnlySettingsStore(SettingsScope.UserSettings);
        //    //foreach (string name in store.GetPropertyNames(@"MRUItems\{a9c4a31f-f9cb-47a9-abc0-49ce82d0b3ac}\Items"))
        //    //{
        //    //    string value = store.GetString(@"MRUItems\{a9c4a31f-f9cb-47a9-abc0-49ce82d0b3ac}\Items", name);
        //    //    Console.WriteLine("Property name: {0}, value: {1}", name, value.Split('|')[0]);
        //    //}
    }
}