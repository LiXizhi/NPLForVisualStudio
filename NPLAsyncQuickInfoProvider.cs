using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace NPLForVisualStudio
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("NPL Async Quick Info Provider")]
    [ContentType("text")]
    [Order]
    internal sealed class NPLAsyncQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            // This ensures only one instance per textbuffer is created
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new NPLAsyncQuickInfoSource(textBuffer));
        }
    }
}
