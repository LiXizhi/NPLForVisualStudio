﻿using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace NPLForVisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(NPLForVisualStudioPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class NPLForVisualStudioPackage : AsyncPackage
    {
        /// <summary>
        /// NPLForVisualStudioPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "9d0ece69-b599-4f43-834a-f969a4fb147a";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            NPLDocs.Instance.Dte = await GetServiceAsync(typeof(DTE)) as DTE;

            var events = NPLDocs.Instance.Dte.Events;
            var solutionEvents = events.SolutionEvents;
            solutionEvents.Opened += HandleOpenSolution;

            if(NPLDocs.Instance.Dte.Solution.IsOpen)
            {
                HandleOpenSolution();
            }

            await NPLCommandSetBreakpoint.InitializeAsync(this);
            await NPLCommandGotoDefinition.InitializeAsync(this);
        }

        #endregion

        private void HandleOpenSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            NPLDocs.Instance.LoadDocumentationInSolution();
        }

        /// <summary>
        /// write a line of text to NPL output panel
        /// </summary>
        /// <param name="text"></param>
        public void WriteOutput(String text)
        {
            Console.WriteLine(text);
        }
    }
}
