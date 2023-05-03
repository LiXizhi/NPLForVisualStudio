using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NPLForVisualStudio
{
    internal sealed class NPLAsyncQuickInfoSource : IAsyncQuickInfoSource
    {
        private static readonly ImageId _icon = KnownMonikers.Method.ToImageId();
        
        private ITextBuffer _textBuffer;

        public NPLAsyncQuickInfoSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        // This is called on a background thread.
        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
            
            if (triggerPoint != null)
            {
                var line = triggerPoint.Value.GetContainingLine();
                var lineNumber = triggerPoint.Value.GetContainingLine().LineNumber;
                var lineSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeInclusive);
                string sLine = line.GetText();
                var listMethods = NPLDocs.Instance.FindQuickInfo(sLine, Math.Min(triggerPoint.Value.Position - line.Start.Position, sLine.Length));

                if(listMethods.Count > 0)
                {
                    List<ContainerElement> childElements = new List<ContainerElement>();
                    foreach (var method in listMethods)
                    {
                        childElements.Add(new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new ImageElement(_icon),
                            new ClassifiedTextElement(
                                new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, method.GetMethodDeclarationString())
                        )));
                        if (!string.IsNullOrEmpty(method.Description))
                        {
                            childElements.Add(new ContainerElement(
                                ContainerElementStyle.Wrapped,
                                new ClassifiedTextElement(
                                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, method.Description)
                                )));
                        }
                        if (!string.IsNullOrEmpty(method.FilenameDefinedIn))
                        {
                            childElements.Add(new ContainerElement(
                                ContainerElementStyle.Wrapped,
                                new ClassifiedTextElement(
                                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, $"in {method.FilenameDefinedIn}:{method.LineDefined}")
                                ))); ;
                        }
                    }

                    var parentElm = new ContainerElement(ContainerElementStyle.Stacked, childElements);
                    return Task.FromResult(new QuickInfoItem(lineSpan, parentElm));
                }
            }

            return Task.FromResult<QuickInfoItem>(null);
        }

        public void Dispose()
        {
            // This provider does not perform any cleanup.
        }
    }
}
