﻿using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NPLForVisualStudio
{
    internal sealed class NPLAsyncQuickInfoSource : IAsyncQuickInfoSource
    {
        private static readonly ImageId _icon = KnownMonikers.AbstractCube.ToImageId();
        
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
                var sCurrentWord = NPLDocs.Instance.FindQuickInfo(sLine, Math.Min(triggerPoint.Value.Position - line.Start.Position, sLine.Length));

                var lineNumberElm = new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_icon),
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, $"{sCurrentWord} Line number: "),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, $"{lineNumber + 1}")
                    ));

                var dateElm = new ContainerElement(
                    ContainerElementStyle.Stacked,
                    lineNumberElm,
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.SymbolDefinition, "The current date: "),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, DateTime.Now.ToShortDateString())
                    ));

                return Task.FromResult(new QuickInfoItem(lineSpan, dateElm));
            }

            return Task.FromResult<QuickInfoItem>(null);
        }

        public void Dispose()
        {
            // This provider does not perform any cleanup.
        }
    }
}
