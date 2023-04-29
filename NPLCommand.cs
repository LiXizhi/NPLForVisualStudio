using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using static Microsoft.VisualStudio.Threading.AsyncReaderWriterLock;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;

namespace NPLForVisualStudio
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class NPLCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d6a84f34-ea32-4607-b494-0090c1faa774");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="NPLCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private NPLCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static NPLCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in NPLCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new NPLCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            EnvDTE.DTE dte = await this.ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            EnvDTE.TextSelection ts = dte.ActiveWindow.Selection as EnvDTE.TextSelection;
            if (ts == null)
                return;
            try
            {
                string sFileName = dte.ActiveWindow.Document.FullName;
                int lineNumber = ts.CurrentLine;
                // System.Windows.MessageBox.Show("Set breakpoint here..." + sFileName + lineNumber.ToString());
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:8099/");
                    var content = new FormUrlEncodedContent(new[]
                    {
                            new KeyValuePair<string, string>("action", "addbreakpoint"),
                            new KeyValuePair<string, string>("filename", sFileName),
                            new KeyValuePair<string, string>("line", lineNumber.ToString()),
                        });
                    var result = client.PostAsync("/ajax/debugger", content).Result;
                    var task = result.Content.ReadAsStringAsync();
                    task.ContinueWith((t) => {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            if (System.Windows.MessageBox.Show("Please start your NPL process first \nand start NPL Code Wiki at: \n http://127.0.0.1:8099/ \nDo you want to see help page?", "NPL HTTP Debugger", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                            {
                                System.Diagnostics.Process.Start("https://github.com/LiXizhi/NPLRuntime/wiki/NPLCodeWiki");
                            }
                        }
                        else
                        {
                            // completed successfully
                            string url = "http://127.0.0.1:8099/debugger";
                            // url += string.Format("?filename={0}&line={1}", sFileName, lineNumber);
                            System.Diagnostics.Process.Start(url);
                        }
                    });
                }
            }
            catch (Exception exp)
            {
                if (System.Windows.MessageBox.Show("Please start your NPL process first \nand start NPL Code Wiki at: \n http://127.0.0.1:8099/ \nDo you want to see help page?", "NPL HTTP Debugger", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://github.com/LiXizhi/NPLRuntime/wiki/NPLCodeWiki");
                }
            }
        }
    }
}
