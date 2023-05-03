using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NPLForVisualStudio
{
    /// <summary>
    /// This is the main class for code completion and intellisense. 
    /// dte property is set when visual studio package starts. 
    /// </summary>
    sealed internal class NPLDocs
    {
        public readonly XmlDocumentationLoader xmlDocumentationLoader = new XmlDocumentationLoader();

        public TableDeclarationProvider xmlDeclarationProvider = new TableDeclarationProvider();

        public readonly Dictionary<string, TableDeclarationProvider> frameXmlDeclarationProviders =
            new Dictionary<string, TableDeclarationProvider>();

        private EnvDTE.DTE dte = null;
        public EnvDTE.DTE Dte { get => dte; set => dte = value; }

        public DeclarationAuthoringScope authoringScope;

        public NPLDocs()
        {
            authoringScope = new DeclarationAuthoringScope();

            // By Xizhi: we will also load ${SolutionDir}/Documentation/*.xml when opening a new solution file.  
            LoadXmlDocumentation();
        }

        private static readonly object lock_ = new object ();  
        private static NPLDocs instance = null;
        public static NPLDocs Instance
        {
            get
            {
                lock (lock_)
                {
                    if (instance == null)
                    {
                        instance = new NPLDocs();
                    }
                    return instance;
                }
            }
        }

        public void LoadDocumentationInSolution()
        {
            try
            {
                string solutionDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName);
                if (solutionDir != null)
                {
                    // Load the documentation
                    LoadXmlDocumentation(solutionDir + "\\");

                    foreach (Project proj in Dte.Solution.Projects)
                    {
                        string projDir = System.IO.Path.GetDirectoryName(proj.FullName);
                        if (projDir != null && projDir != solutionDir)
                        {
                            // Load the documentation
                            LoadXmlDocumentation(projDir + "\\");
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Loads the XML documentation.
        /// </summary>
        public void LoadXmlDocumentation(string documentationRootPath = null)
        {
            if (xmlDeclarationProvider == null)
            {
                xmlDeclarationProvider = new TableDeclarationProvider();
            }
            // Retrieve install directory
            if (documentationRootPath == null)
            {
                // documentationRootPath = ParaEngine.NPLLanguageService.NPLLanguageServicePackage.PackageRootPath;
                documentationRootPath = ObtainInstallationFolder() + "\\";
            }

            if (documentationRootPath != null)
            {
                try
                {
                    documentationRootPath += "Documentation";
                    int nFileCount = 0;
                    //Look for XML files and load them using the XML documentation loader
                    foreach (string path in Directory.GetFiles(documentationRootPath, "*.xml"))
                    {
                        nFileCount++;
                        xmlDocumentationLoader.LoadXml(path);
                    }
                    // WriteOutput(String.Format("Load {0} NPL doc file(s) in folder: {1}", nFileCount, documentationRootPath));
                    xmlDocumentationLoader.AddDeclarations(xmlDeclarationProvider);
                }
                catch (Exception)
                {

                }
            }
        }

        public static string ObtainInstallationFolder()
        {
            Type packageType = typeof(NPLForVisualStudioPackage);
            Uri uri = new Uri(packageType.Assembly.CodeBase);
            var assemblyFileInfo = new FileInfo(uri.LocalPath);
            return assemblyFileInfo.Directory.FullName;
        }

        private void BuildQuickInfoString(TableDeclarationProvider declarations, string sWord, in List<Method> listMethods, string sPrefix = null)
        {
            if (declarations != null)
            {
                foreach (var method in declarations.FindMethods(sWord))
                {
                    //listMethods.Add(method.Value.GetQuickInfo(!String.IsNullOrEmpty(sPrefix) ? sPrefix : (String.IsNullOrEmpty(method.Key) ? "" : method.Key + ".")));
                    listMethods.Add(method.Value);
                }
            }
        }

        static bool IsFunctionWordChar(char cChar)
        {
            return Char.IsLetterOrDigit(cChar) || cChar == '_';
        }

        public static string GetWordFromLineAndCursor(string sLine, in int nCursor, out int nColFrom, out int nColTo)
        {
            nColFrom = nCursor;
            nColTo = nColFrom;

            if (sLine != null && nColFrom >= 0 && nColFrom < sLine.Length)
            {
                char cChar = sLine[nColFrom];

                if (IsFunctionWordChar(cChar))
                {
                    for (int i = nColFrom - 1; i >= 0; i--)
                    {
                        if (IsFunctionWordChar(sLine[i]))
                            nColFrom = i;
                        else
                            break;
                    }
                    for (int i = nColTo + 1; i < sLine.Length; i++)
                    {
                        if (IsFunctionWordChar(sLine[i]))
                            nColTo = i;
                        else
                            break;
                    }
                    string sWord = sLine.Substring(nColFrom, nColTo - nColFrom + 1);
                    if (sWord.Length > 0 && Char.IsLetter(sWord[0]))
                    {
                        return sWord;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// mouse over a text to display some info. 
        /// called from parser thread
        /// </summary>
        /// <param name="request"></param>
        public List<Method> FindQuickInfo(string sLine, in int nCursor)
        {
            int nColFrom, nColTo;
            List<Method> listMethods = new List<Method>();
            string sWord = GetWordFromLineAndCursor(sLine, nCursor, out nColFrom, out nColTo);
            if (sWord != null && sWord.Length > 0)
            {
                StringBuilder info = new StringBuilder();

                BuildQuickInfoString(xmlDeclarationProvider, sWord, listMethods);
            }
            return listMethods;
        }

        private string FindDocFileInDir(string filename, string solutionDir)
        {
            string fullpath = solutionDir + "\\" + filename;
            if (File.Exists(fullpath))
                return fullpath;
            else
            {
                fullpath = solutionDir + "\\src\\" + filename;
                if (File.Exists(fullpath))
                    return fullpath;
                else
                {
                    fullpath = solutionDir + "\\Documentation\\" + filename;
                    if (File.Exists(fullpath))
                        return fullpath;
                }
            }
            return null;
        }

        /// <summary>
        /// we will search in current solution directory, and `./src` and `./Documentation` directory. 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string GetAbsolutionFilePath(string filename)
        {
            if (!File.Exists(filename) && !filename.Contains(":"))
            {
                DTE2 dte = Dte as DTE2;
                string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);

                string fullname = FindDocFileInDir(filename, solutionDir);
                if (fullname == null)
                {
                    foreach (Project proj in dte.Solution.Projects)
                    {
                        string projDir = System.IO.Path.GetDirectoryName(proj.FullName);
                        if (projDir != null && projDir != solutionDir)
                        {
                            fullname = FindDocFileInDir(filename, projDir);
                            if (fullname != null)
                            {
                                return fullname;
                            }
                        }
                    }
                }
            }
            return filename;
        }
        private bool BuildGotoDefinitionUri(TableDeclarationProvider declarations, DeclarationAuthoringScope authoringScope, string sWord)
        {
            if (declarations != null)
            {
                foreach (var method in declarations.FindDeclarations(sWord, true))
                {
                    var func = method.Value;
                    if (func.FilenameDefinedIn != null)
                    {
                        authoringScope.m_goto_filename = GetAbsolutionFilePath(func.FilenameDefinedIn);
                        authoringScope.m_goto_textspan.iStartLine = Int32.Parse(func.LineDefined);
                        authoringScope.m_goto_textspan.iEndLine = authoringScope.m_goto_textspan.iStartLine;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool FindGotoDefinition(string sLine, in int nCursor)
        {
            authoringScope.m_goto_filename = null;
            
            int nColFrom, nColTo;
            string sWord = GetWordFromLineAndCursor(sLine, nCursor, out nColFrom, out nColTo);
            if (sWord != null && sWord.Length > 0)
            {
                if (BuildGotoDefinitionUri(xmlDeclarationProvider, authoringScope, sWord))
                    return true;
            }

            // look for NPL.load and implement open file
            if (sLine != null)
            {
                if (sLine.Contains("NPL.load("))
                {
                    // we will goto the file specified in NPL.load
                    Regex reg = new Regex("NPL\\.load\\(\"([^\"]+)\"\\)");
                    Match m = reg.Match(sLine);
                    if (m.Success && m.Groups.Count >= 2)
                    {
                        string sFilename = m.Groups[1].Value;
                        if (sFilename.StartsWith("("))
                        {
                            int nIndex = sFilename.IndexOf(')');
                            if (nIndex > 0)
                            {
                                sFilename = sFilename.Substring(nIndex + 1);
                            }
                        }
                        sFilename = GetAbsolutionFilePath(sFilename);
                        if (sFilename != null)
                        {
                            authoringScope.m_goto_filename = sFilename;
                            authoringScope.m_goto_textspan.iStartLine = 0;
                            authoringScope.m_goto_textspan.iEndLine = 0;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
