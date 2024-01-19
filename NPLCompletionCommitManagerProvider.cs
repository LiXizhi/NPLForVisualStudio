using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPLForVisualStudio
{
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("NPL/Lua language commit manager provider")]
    [ContentType("text")]
    class NPLCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
    {
        IDictionary<ITextView, IAsyncCompletionCommitManager> cache = new Dictionary<ITextView, IAsyncCompletionCommitManager>();

        public IAsyncCompletionCommitManager GetOrCreate(ITextView textView)
        {
            if (cache.TryGetValue(textView, out var itemSource))
                return itemSource;

            if (textView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
            {
                string extension = Path.GetExtension(document.FilePath);
                if (extension != ".lua" && extension != ".npl" && extension != ".html")
                {
                    cache.Add(textView, null);
                    return null;
                }
            }

            var manager = new NPLCompletionCommitManager();
            textView.Closed += (o, e) => cache.Remove(textView); // clean up memory as files are closed
            cache.Add(textView, manager);
            return manager;
        }
    }
}
