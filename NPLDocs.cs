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
    sealed internal class NPLDocs
    {
        public readonly XmlDocumentationLoader xmlDocumentationLoader = new XmlDocumentationLoader();

        public TableDeclarationProvider xmlDeclarationProvider = new TableDeclarationProvider();

        public readonly Dictionary<string, TableDeclarationProvider> frameXmlDeclarationProviders =
            new Dictionary<string, TableDeclarationProvider>();

        private EnvDTE.DTE dte = null;
        public EnvDTE.DTE Dte { get => dte; set => dte = value; }

        private DeclarationAuthoringScope authoringScope;

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

        private void BuildQuickInfoString(TableDeclarationProvider declarations, string sWord, StringBuilder info, string sPrefix = null)
        {
            if (declarations != null)
            {
                foreach (var method in declarations.FindMethods(sWord))
                {
                    info.AppendFormat("{0}{1}",
                        info.Length == 0 ? "" : "\n-------------------\n",
                        method.Value.GetQuickInfo(!String.IsNullOrEmpty(sPrefix) ? sPrefix : (String.IsNullOrEmpty(method.Key) ? "" : method.Key + ".")));
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
        public string FindQuickInfo(string sLine, in int nCursor)
        {
            int nColFrom, nColTo;
            string sInfo = "";
            string sWord = GetWordFromLineAndCursor(sLine, nCursor, out nColFrom, out nColTo);
            if (sWord != null && sWord.Length > 0)
            {
                StringBuilder info = new StringBuilder();

                BuildQuickInfoString(xmlDeclarationProvider, sWord, info);

                if (info.Length > 0)
                {
                    sInfo = info.ToString();
                }
            }
            return sInfo;
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
        private string GetAbsolutionFilePath(string filename)
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
                        return true;
                    }
                }
            }
            return false;
        }

        private bool FindGotoDefinition(string sLine, in int nCursor)
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
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Data from https://en.wikipedia.org/wiki/List_of_chemical_elements
        public List<Element> Elements { get; } = new List<Element>
        {
            new Element(1,"H","Hydrogen",1.008d, Element.Categories.NonMetal),
            new Element(2,"He","Helium",4.002602d, Element.Categories.NonMetal),
            new Element(3,"Li","Lithium",6.94d, Element.Categories.Metal),
            new Element(4,"Be","Beryllium",9.0121831d, Element.Categories.Metal),
            new Element(5,"B","Boron",10.81d, Element.Categories.Metalloid),
            new Element(6,"C","Carbon",12.011d, Element.Categories.NonMetal),
            new Element(7,"N","Nitrogen",14.007d, Element.Categories.NonMetal),
            new Element(8,"O","Oxygen",15.999d, Element.Categories.NonMetal),
            new Element(9,"F","Fluorine",18.998403163d, Element.Categories.NonMetal),
            new Element(10,"Ne","Neon",20.1797d, Element.Categories.NonMetal),
            new Element(11,"Na","Sodium",22.98976928d, Element.Categories.Metal),
            new Element(12,"Mg","Magnesium",24.305d, Element.Categories.Metal),
            new Element(13,"Al","Aluminium",26.9815385d, Element.Categories.Metal),
            new Element(14,"Si","Silicon",28.085d, Element.Categories.Metalloid),
            new Element(15,"P","Phosphorus",30.973761998d, Element.Categories.NonMetal),
            new Element(16,"S","Sulfur",32.06d, Element.Categories.NonMetal),
            new Element(17,"Cl","Chlorine",35.45d, Element.Categories.NonMetal),
            new Element(18,"Ar","Argon",39.948d, Element.Categories.NonMetal),
            new Element(19,"K","Potassium",39.0983d, Element.Categories.Metal),
            new Element(20,"Ca","Calcium",40.078d, Element.Categories.Metal),
            new Element(21,"Sc","Scandium",44.955908d, Element.Categories.Metal),
            new Element(22,"Ti","Titanium",47.867d, Element.Categories.Metal),
            new Element(23,"V","Vanadium",50.9415d, Element.Categories.Metal),
            new Element(24,"Cr","Chromium",51.9961d, Element.Categories.Metal),
            new Element(25,"Mn","Manganese",54.938044d, Element.Categories.Metal),
            new Element(26,"Fe","Iron",55.845d, Element.Categories.Metal),
            new Element(27,"Co","Cobalt",58.933194d, Element.Categories.Metal),
            new Element(28,"Ni","Nickel",58.6934d, Element.Categories.Metal),
            new Element(29,"Cu","Copper",63.546d, Element.Categories.Metal),
            new Element(30,"Zn","Zinc",65.38d, Element.Categories.Metal),
            new Element(31,"Ga","Gallium",69.723d, Element.Categories.Metal),
            new Element(32,"Ge","Germanium",72.630d, Element.Categories.Metalloid),
            new Element(33,"As","Arsenic",74.921595d, Element.Categories.Metalloid),
            new Element(34,"Se","Selenium",78.971d, Element.Categories.NonMetal),
            new Element(35,"Br","Bromine",79.904d, Element.Categories.NonMetal),
            new Element(36,"Kr","Krypton",83.798d, Element.Categories.NonMetal),
            new Element(37,"Rb","Rubidium",85.4678d, Element.Categories.Metal),
            new Element(38,"Sr","Strontium",87.62d, Element.Categories.Metal),
            new Element(39,"Y","Yttrium",88.90584d, Element.Categories.Metal),
            new Element(40,"Zr","Zirconium",91.224d, Element.Categories.Metal),
            new Element(41,"Nb","Niobium",92.90637d, Element.Categories.Metal),
            new Element(42,"Mo","Molybdenum",95.95d, Element.Categories.Metal),
            new Element(43,"Tc","Technetium",98d, Element.Categories.Metal),
            new Element(44,"Ru","Ruthenium",101.07d, Element.Categories.Metal),
            new Element(45,"Rh","Rhodium",102.90550d, Element.Categories.Metal),
            new Element(46,"Pd","Palladium",106.42d, Element.Categories.Metal),
            new Element(47,"Ag","Silver",107.8682d, Element.Categories.Metal),
            new Element(48,"Cd","Cadmium",112.414d, Element.Categories.Metal),
            new Element(49,"In","Indium",114.818d, Element.Categories.Metal),
            new Element(50,"Sn","Tin",118.710d, Element.Categories.Metal),
            new Element(51,"Sb","Antimony",121.760d, Element.Categories.Metalloid),
            new Element(52,"Te","Tellurium",127.60d, Element.Categories.Metalloid),
            new Element(53,"I","Iodine",126.90447d, Element.Categories.NonMetal),
            new Element(54,"Xe","Xenon",131.293d, Element.Categories.NonMetal),
            new Element(55,"Cs","Caesium",132.90545196d, Element.Categories.Metal),
            new Element(56,"Ba","Barium",137.327d, Element.Categories.Metal),
            new Element(57,"La","Lanthanum",138.90547d, Element.Categories.Metal),
            new Element(58,"Ce","Cerium",140.116d, Element.Categories.Metal),
            new Element(59,"Pr","Praseodymium",140.90766d, Element.Categories.Metal),
            new Element(60,"Nd","Neodymium",144.242d, Element.Categories.Metal),
            new Element(61,"Pm","Promethium",145d, Element.Categories.Metal),
            new Element(62,"Sm","Samarium",150.36d, Element.Categories.Metal),
            new Element(63,"Eu","Europium",151.964d, Element.Categories.Metal),
            new Element(64,"Gd","Gadolinium",157.25d, Element.Categories.Metal),
            new Element(65,"Tb","Terbium",158.92535d, Element.Categories.Metal),
            new Element(66,"Dy","Dysprosium",162.500d, Element.Categories.Metal),
            new Element(67,"Ho","Holmium",164.93033d, Element.Categories.Metal),
            new Element(68,"Er","Erbium",167.259d, Element.Categories.Metal),
            new Element(69,"Tm","Thulium",168.93422d, Element.Categories.Metal),
            new Element(70,"Yb","Ytterbium",173.045d, Element.Categories.Metal),
            new Element(71,"Lu","Lutetium",174.9668d, Element.Categories.Metal),
            new Element(72,"Hf","Hafnium",178.49d, Element.Categories.Metal),
            new Element(73,"Ta","Tantalum",180.94788d, Element.Categories.Metal),
            new Element(74,"W","Tungsten",183.84d, Element.Categories.Metal),
            new Element(75,"Re","Rhenium",186.207d, Element.Categories.Metal),
            new Element(76,"Os","Osmium",190.23d, Element.Categories.Metal),
            new Element(77,"Ir","Iridium",192.217d, Element.Categories.Metal),
            new Element(78,"Pt","Platinum",195.084d, Element.Categories.Metal),
            new Element(79,"Au","Gold",196.966569d, Element.Categories.Metal),
            new Element(80,"Hg","Mercury",200.592d, Element.Categories.Metal),
            new Element(81,"Tl","Thallium",204.38d, Element.Categories.Metal),
            new Element(82,"Pb","Lead",207.2d, Element.Categories.Metal),
            new Element(83,"Bi","Bismuth",208.98040d, Element.Categories.Metal),
            new Element(84,"Po","Polonium",209d, Element.Categories.Metal),
            new Element(85,"At","Astatine",210d, Element.Categories.Metalloid),
            new Element(86,"Rn","Radon",222d, Element.Categories.NonMetal),
            new Element(87,"Fr","Francium",223d, Element.Categories.Metal),
            new Element(88,"Ra","Radium",226d, Element.Categories.Metal),
            new Element(89,"Ac","Actinium",227d, Element.Categories.Metal),
            new Element(90,"Th","Thorium",232.0377d, Element.Categories.Metal),
            new Element(91,"Pa","Protactinium",231.03588d, Element.Categories.Metal),
            new Element(92,"U","Uranium",238.02891d, Element.Categories.Metal),
            new Element(93,"Np","Neptunium",237d, Element.Categories.Metal),
            new Element(94,"Pu","Plutonium",244d, Element.Categories.Metal),
            new Element(95,"Am","Americium",243d, Element.Categories.Metal),
            new Element(96,"Cm","Curium",247d, Element.Categories.Metal),
            new Element(97,"Bk","Berkelium",247d, Element.Categories.Metal),
            new Element(98,"Cf","Californium",251d, Element.Categories.Metal),
            new Element(99,"Es","Einsteinium",252d, Element.Categories.Metal),
            new Element(100,"Fm","Fermium",257d, Element.Categories.Metal),
            new Element(101,"Md","Mendelevium",258d, Element.Categories.Metal),
            new Element(102,"No","Nobelium",259d, Element.Categories.Metal),
            new Element(103,"Lr","Lawrencium",266d, Element.Categories.Metal),
            new Element(104,"Rf","Rutherfordium",267d, Element.Categories.Metal),
            new Element(105,"Db","Dubnium",268d, Element.Categories.Metal),
            new Element(106,"Sg","Seaborgium",269d, Element.Categories.Metal),
            new Element(107,"Bh","Bohrium",270d, Element.Categories.Metal),
            new Element(108,"Hs","Hassium",277d, Element.Categories.Metal),
            new Element(109,"Mt","Meitnerium",278d, Element.Categories.Uncategorized),
            new Element(110,"Ds","Darmstadtium",281d, Element.Categories.Uncategorized),
            new Element(111,"Rg","Roentgenium",282d, Element.Categories.Uncategorized),
            new Element(112,"Cn","Copernicium",285d, Element.Categories.Uncategorized),
            new Element(113,"Nh","Nihonium",286d, Element.Categories.Uncategorized),
            new Element(114,"Fl","Flerovium",289d, Element.Categories.Uncategorized),
            new Element(115,"Mc","Moscovium",290d, Element.Categories.Uncategorized),
            new Element(116,"Lv","Livermorium",293d, Element.Categories.Uncategorized),
            new Element(117,"Ts","Tennessine",294d, Element.Categories.Uncategorized),
            new Element(118,"Og","Oganesson",294d, Element.Categories.Uncategorized),
        };
        

        public class Element
        {
            public int AtomicNumber { get; }
            public string Symbol { get; }
            public string Name { get; }
            public double AtomicWeight { get; }
            public Categories Category { get; }

            public enum Categories
            {
                NonMetal,
                Metal,
                Metalloid,
                Uncategorized
            }

            internal Element(int atomicNumber, string symbol, string name, double atomicWeight, Categories category)
            {
                AtomicNumber = atomicNumber;
                Symbol = symbol;
                Name = name;
                AtomicWeight = atomicWeight;
                Category = category;
            }
        }
    }
}
