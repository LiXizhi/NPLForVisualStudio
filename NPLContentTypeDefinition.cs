using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace NPLForVisualStudio
{
    /// <summary>
    /// You can define your own content type and link a file name extension to it by using the editor Managed Extensibility Framework (MEF) extensions. 
    /// https://learn.microsoft.com/en-us/visualstudio/extensibility/walkthrough-linking-a-content-type-to-a-file-name-extension?view=vs-2022
    /// </summary>
    public class NPLContentDefinition
    {
        [Export]
        [Name("npl")]
        [BaseDefinition("text")]
        internal static ContentTypeDefinition NPLContentTypeDefinition;

        [Export]
        [FileExtension(".npl")]
        [ContentType("npl")]
        internal static FileExtensionToContentTypeDefinition NPLFileExtensionDefinition;

        //[Export]
        //[FileExtension(".lua")]
        //[ContentType("npl")]
        //internal static FileExtensionToContentTypeDefinition NPLFileExtensionDefinition2;
    }
}
