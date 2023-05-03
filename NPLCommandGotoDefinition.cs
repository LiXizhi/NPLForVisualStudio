using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Task = System.Threading.Tasks.Task;

namespace NPLForVisualStudio
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class NPLCommandGotoDefinition
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d6a84f34-ea32-4607-b494-0090c1faa774");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="NPLCommandGotoDefinition"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private NPLCommandGotoDefinition(AsyncPackage package, OleMenuCommandService commandService)
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
        public static NPLCommandGotoDefinition Instance
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
            // Switch to the main thread - the call to AddCommand in NPLCommandGotoDefinition's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new NPLCommandGotoDefinition(package, commandService);
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
            try
            {
                EnvDTE.DTE dte = NPLDocs.Instance.Dte;
                EnvDTE.TextSelection ts = dte.ActiveWindow.Selection as EnvDTE.TextSelection;
                if (ts == null)
                    return;
                var activePoint = ts.ActivePoint;
                int nLine = ts.ActivePoint.Line;
                int nLineCharOffset = ts.ActivePoint.LineCharOffset;
                string sLine = activePoint.CreateEditPoint().GetLines(activePoint.Line, activePoint.Line + 1);

                // Get current line text and line cursor position
                if(!String.IsNullOrEmpty(sLine) && NPLDocs.Instance.FindGotoDefinition(sLine, nLineCharOffset))
                {
                    var authoringScope = NPLDocs.Instance.authoringScope;
                    dte.Documents.Open(authoringScope.m_goto_filename);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
