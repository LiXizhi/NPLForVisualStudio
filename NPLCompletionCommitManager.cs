using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace NPLForVisualStudio
{
    /// <summary>
    /// The simplest implementation of IAsyncCompletionCommitManager that provides Commit Characters and uses default behavior otherwise
    /// When we first create the completion session, we access the PotentialCommitCharacters property that returns characters that potentially commit completion 
    /// when user types them. We access this property, therefore it should return a preallocated array.
    /// Typically, the commit characters include space and other token delimeters such as ., (, ). Don't worry about Tab and Enter, as they are handled separately. 
    /// If a character is a commit character in some, but not all situations, you must add it to this collection. 
    /// Characters from available IAsyncCompletionCommitManagers are combined into a single collection for the duration of the completion session.
    /// 
    /// We maintain this list so that editor's completion feature can quickly ignore characters that are not commit characters. 
    /// If user types a character found in the provided array, Editor will call ShouldCommitCompletion on the UI thread. 
    /// This is an opportunity to tell whether certain character is indeed a commit character in the given location. 
    /// In most cases, simply return true, which means that every character in PotentialCommitCharacters will trigger the commit behavior.
    /// 
    /// see also: https://github.com/microsoft/vs-editor-api/issues/9
    /// </summary>
    internal class NPLCompletionCommitManager : IAsyncCompletionCommitManager
    {
        public NPLCompletionCommitManager()
        {
        }

        ImmutableArray<char> commitChars = new char[] { ' ', '\'', '"', ',', '.', ';', ':' }.ToImmutableArray();

        public IEnumerable<char> PotentialCommitCharacters => commitChars;

        public bool ShouldCommitCompletion(IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
        {
            // This method runs synchronously, potentially before CompletionItem has been computed.
            // The purpose of this method is to filter out characters not applicable at given location.

            // This method is called only when typedChar is among the PotentialCommitCharacters
            // in this simple example, all PotentialCommitCharacters do commit, so we always return true
            return true;
        }

        public CommitResult TryCommit(IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
        {
            // Objects of interest here are session.TextView and session.TextView.Caret.
            // This method runs synchronously

            return CommitResult.Unhandled; // use default commit mechanism.
        }
    }
}