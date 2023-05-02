using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPLForVisualStudio
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("NPL/Lua language completion provider")]
    [ContentType("text")]
    class NPLCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        IDictionary<ITextView, IAsyncCompletionSource> cache = new Dictionary<ITextView, IAsyncCompletionSource>();

        [Import]
        ITextStructureNavigatorSelectorService StructureNavigatorSelector;

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (cache.TryGetValue(textView, out var itemSource))
                return itemSource;

            var source = new NPLCompletionSource(NPLDocs.Instance, StructureNavigatorSelector); // opportunity to pass in MEF parts
            textView.Closed += (o, e) => cache.Remove(textView); // clean up memory as files are closed
            cache.Add(textView, source);
            return source;
        }
    }
}
