using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Reflection;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Runtime.InteropServices;

namespace QZ.UserControl
{
    public class HtmlUpdater
    {
        TextBlock textBlock;
        CurrentStateType currentState = new CurrentStateType();

        private void UpdateStyle(HtmlTag aTag)
        {
            currentState.UpdateStyle(aTag);
        }

        private Inline UpdateElement(HtmlTag aTag)
        {
            Inline retVal = null;

            switch (aTag.Name)
            {
                case "binding":
                case "text":
                    if (aTag.Name == "binding")
                    {
                        retVal = new Bold(new Run("{Binding}"));
                        if (aTag.Contains("path") && (textBlock.DataContext != null))
                        {
                            object obj = textBlock.DataContext;
                            PropertyInfo pi = obj.GetType().GetProperty(aTag["path"]);
                            if (pi != null && pi.CanRead)
                                retVal = new Run(pi.GetValue(obj, null).ToString());
                        }
                    }
                    else
                        retVal = new Run(aTag["value"]);

                    if (currentState.SubScript) retVal.SetValue(Typography.VariantsProperty, FontVariants.Subscript);
                    if (currentState.SuperScript) retVal.SetValue(Typography.VariantsProperty, FontVariants.Superscript);
                    if (currentState.Bold) retVal = new Bold(retVal);
                    if (currentState.Italic) retVal = new Italic(retVal);
                    if (currentState.Underline) retVal = new Underline(retVal);

                    if (currentState.Foreground.HasValue)
                        retVal.Foreground = new SolidColorBrush(currentState.Foreground.Value);

                    if (currentState.Font != null)
                        try { retVal.FontFamily = new System.Windows.Media.FontFamily(currentState.Font); }
                        catch { } //Font name not found...

                    if (currentState.FontSize.HasValue)
                        retVal.FontSize = currentState.FontSize.Value;

                    break;
                case "br":
                    retVal = new LineBreak();
                    break;
                default:
                    Debug.WriteLine("UpdateElement - " + aTag.Name + " not handled.");
                    retVal = new Run();
                    //Image img = new Image();
                    //BitmapImage bi = new BitmapImage(new Uri(@"c:\temp\1148706365-1.png"));
                    //img.Source = bi;
                    //retVal = new Figure(new BlockUIContainer(img));
                    break;
            }


            if (currentState.HyperLink != null && currentState.HyperLink != "")
            {
                Hyperlink link = new Hyperlink(retVal);
                try
                {
                    link.NavigateUri = new Uri(currentState.HyperLink);
                }
                catch
                {
                    link.NavigateUri = null;
                }
                retVal = link;
            }

            return retVal;
        }

        public HtmlUpdater(TextBlock aBlock)
        {
            textBlock = aBlock;
        }

        public void Update(HtmlTagTree tagTree)
        {
            List<HtmlTag> tagList = tagTree.ToHtmlTagList();

            foreach (HtmlTag tag in tagList)
            {
                switch (Defines.BuiltinTags[tag.ID].flags)
                {
                    case HTMLFlag.TextFormat: UpdateStyle(tag); break;
                    case HTMLFlag.Element: textBlock.Inlines.Add(UpdateElement(tag)); break;
                }

            }
        }
    }

    /// <summary>
	/// Represent owner of all HtmlTag.
	/// </summary>
	public class HtmlTagTree : HtmlTagNode
    {
        public HtmlTagTree() : base(null, null)
        {
            isRoot = true;
            tag = new HtmlTag("master", "");
        }

        public override bool CanAdd(HtmlTag aTag)
        {
            return true;
        }

        static string printNode(Int32 level, HtmlTagNode node)
        {
            string spacing = " ";
            string retVal = "";
            for (int i = 0; i < level; i++)
                spacing += "  ";

            retVal += spacing + node.ToString() + '\r' + '\n';

            foreach (HtmlTagNode subnode in node)
                retVal += HtmlTagTree.printNode(level + 1, subnode);

            return retVal;
        }

        public override string ToString()
        {
            string retVal = "";

            foreach (HtmlTagNode subnode in this)
                retVal += HtmlTagTree.printNode(0, subnode);

            return retVal;
        }
    }

    public class HtmlTagNode : IEnumerable
    {
        protected bool isRoot;
        protected HtmlTag tag;
        protected List<HtmlTagNode> childTags;
        protected HtmlTagNode parentNode;

        ///<summary> Gets the embedded HtmlTag of this node. <> </summary>
        public HtmlTag Tag { get { return tag; } }
        ///<summary> Gets parent node of this node. <> </summary>
        public HtmlTagNode Parent { get { return parentNode; } }
        ///<summary> Gets subnodes of this node. </summary>
        public List<HtmlTagNode> Items { get { return childTags; } }
        ///<summary> Gets subnodes emuerator. </summary>
        public IEnumerator GetEnumerator() { return childTags.GetEnumerator(); }
        ///<summary> Gets whether this node is root of all nodes. </summary>
        public bool IsRoot { get { return isRoot; } }
        ///<summary> Gets whether this node contain other nodes. </summary>
        public bool isContainer { get { return childTags.Count > 0; } }

        public bool isBlock { get { return Defines.BuiltinTags[tag.ID].flags == HTMLFlag.Region; } }

        public virtual bool CanAdd(HtmlTag aTag)
        {
            if (tag.IsEndTag)
                return false;

            if ((aTag.Name == '/' + tag.Name) ||
                (aTag.Level < tag.Level))
                return true;
            return false;
        }

        public HtmlTagNode Add(HtmlTag aTag)
        {
            if (!CanAdd(aTag))
                throw new Exception("Cannot add here, check your coding.");

            HtmlTagNode retVal = new HtmlTagNode(this, aTag);
            Items.Add(retVal);

            if (aTag.Name == '/' + tag.Name)
                return Parent;
            else return retVal;
        }

        ///<summary> Constructor, hide from user view. </summary>
        private HtmlTagNode(HtmlTag aTag) {; }
        ///<summary> Constructor. </summary>
        public HtmlTagNode(HtmlTagNode aParentNode, HtmlTag aTag) : base()
        {
            isRoot = false;
            parentNode = aParentNode;
            tag = aTag;
            childTags = new List<HtmlTagNode>();
        }

        public List<HtmlTag> ToHtmlTagList()
        {
            List<HtmlTag> retVal = new List<HtmlTag>();
            retVal.Add(Tag);

            foreach (HtmlTagNode subnode in this)
                retVal.AddRange(subnode.ToHtmlTagList());

            return retVal;
        }

        public override string ToString()
        {
            return tag.ToString();
        }

        ///<summary> Debug this component. </summary>
        public void PrintItems()
        {
            foreach (HtmlTag t in this)
                Console.WriteLine(t);
        }
    }

    public class HtmlTag
    {
        private static IParamParser HtmlAttributeParser = new ParamParser(new HtmlAttributeStringSerializer());

        private string name;                                     //HtmlTag name without <>        
        private Dictionary<string, string> variables = new Dictionary<string, string>();     //Variable List and values

        ///<summary> Gets HtmlTag ID in BuiltInTags. (without <>) </summary>
        internal int ID { get { return Defines.BuiltinTags.ToList().FindIndex(tagInfo => tagInfo.Html.Equals(name.TrimStart('/'))); } }
        ///<summary> Gets HtmlTag Level in BuiltInTags. (without <>) </summary>
        internal Int32 Level { get { if (ID == -1) return 0; else return Defines.BuiltinTags[ID].tagLevel; } }

        internal bool IsEndTag { get { return ((name.IndexOf('/') == 0) || (variables.ContainsKey("/"))); } }

        ///<summary> Gets HtmlTag name. (without <>) </summary>
        public string Name { get { return name; } }
        ///<summary> Gets variable value. </summary>
        public string this[string key] { get { return variables[key]; } }
        ///<summary> Gets whether variable list contains the specified key. </summary>
        public bool Contains(string key) { return variables.ContainsKey(key); }
        ///<summary> Returns the string representation of the value of this instance.  </summary>
		public override string ToString()
        {
            return String.Format("<{0}> : {1}", name, variables.ToString());
        }

        /// <summary>
        /// Initialite procedure, can be used by child tags.
        /// </summary>
        protected void init(string aName, Dictionary<string, string> aVariables)
        {
            name = aName.ToLower();
            if (aVariables == null)
                variables = new Dictionary<string, string>();
            else
                variables = aVariables;
        }

        ///<summary> Constructor. </summary>
        public HtmlTag(string aName, string aVarString)
        {
            init(aName, HtmlAttributeParser.StringToDictionary(aVarString));
        }

        public HtmlTag(string aText)
        {
            Dictionary<string, string> aList = new Dictionary<string, string>();
            aList.Add("value", aText);
            init("text", aList);
        }
    }

    public class HtmlParser1
    {
        private HtmlTagTree tree;
        internal HtmlTagNode previousNode = null;

        /// <summary>
        /// Constructor
        /// </summary>        
        public HtmlParser1(HtmlTagTree aTree)
        {
            tree = aTree;
        }

        /// <summary> Return true if both < and > found in input. </summary>        
        private bool haveClosingTag(string input)
        {
            if ((input.IndexOf('[') != -1) && (input.IndexOf(']') != -1))
                return false;
            return true;
        }
        /// <summary> Add a Non TextTag to Tag List </summary>        
        internal void addTag(HtmlTag aTag)
        {
            //            HtmlTagNode newNode = new HtmlTagNode(
            if (previousNode == null) { previousNode = tree; }

            while (!previousNode.CanAdd(aTag))
                previousNode = previousNode.Parent;

            previousNode = previousNode.Add(aTag);
        }
        /// <summary>
        /// Parse a string and return text before a tag, the tag and it's variables, and the string after that tag.
        /// </summary>
        private static void readNextTag(string s, ref string beforeTag, ref string afterTag, ref string tagName,
                                          ref string tagVars, char startBracket, char endBracket)
        {
            Int32 pos1 = s.IndexOf(startBracket);
            Int32 pos2 = s.IndexOf(endBracket);

            if ((pos1 == -1) || (pos2 == -1) || (pos2 < pos1))
            {
                tagVars = "";
                beforeTag = s;
                afterTag = "";
            }
            else
            {
                String tagStr = s.Substring(pos1 + 1, pos2 - pos1 - 1);
                beforeTag = s.Substring(0, pos1);
                afterTag = s.Substring(pos2 + 1, s.Length - pos2 - 1);

                Int32 pos3 = tagStr.IndexOf(' ');
                if ((pos3 != -1) && (tagStr != ""))
                {
                    tagName = tagStr.Substring(0, pos3);
                    tagVars = tagStr.Substring(pos3 + 1, tagStr.Length - pos3 - 1);
                }
                else
                {
                    tagName = tagStr;
                    tagVars = "";
                }

                if (tagName.StartsWith("!--"))
                {
                    if ((tagName.Length < 6) || (!(tagName.EndsWith("--"))))
                    {
                        Int32 pos4 = afterTag.IndexOf("-->");
                        if (pos4 != -1)
                            afterTag = afterTag.Substring(pos4 + 2, afterTag.Length - pos4 - 1);
                    }
                    tagName = "";
                    tagVars = "";
                }

            }
        }
        /// <summary>
        /// Parse a string and return text before a tag, the tag and it's variables, and the string after that tag.
        /// </summary>
        private static void readNextTag(string s, ref string beforeTag, ref string afterTag, ref string tagName, ref string tagVars)
        {
            HtmlParser1.readNextTag(s, ref beforeTag, ref afterTag, ref tagName, ref tagVars, '[', ']');
        }
        /// <summary>
        /// Recrusive paraser.
        /// </summary>        
        private void parseHtml(ref string s)
        {
            string beforeTag = "", afterTag = "", tagName = "", tagVar = "";
            readNextTag(s, ref beforeTag, ref afterTag, ref tagName, ref tagVar);

            if (beforeTag != "")
                addTag(new HtmlTag(beforeTag));   		//Text
            if (tagName != "")
                addTag(new HtmlTag(tagName, tagVar));

            s = afterTag;
        }
        /// <summary>
        /// Parse Html
        /// </summary>        
        public void Parse(TextReader reader)
        {
            previousNode = null;

            string input = reader.ReadToEnd();

            while (input != "")
                parseHtml(ref input);
        }

        public static void DebugUnit()
        {
            //string beforeTag="", afterTag="", tagName="", tagVar="";
            //readNextTag("<!-- xyz --><a href=\"xyz\"><b>", ref beforeTag, ref afterTag, ref tagName, ref tagVar);
            //readNextTag(afterTag, ref beforeTag, ref afterTag, ref tagName, ref tagVar);
            //Console.WriteLine(beforeTag);
            //Console.WriteLine(afterTag);
            //Console.WriteLine(tagName);
            //Console.WriteLine(tagVar);
            //string Html = "<b>test</b>";
            //            
            //            mh.parser.Parse((new StringReader(Html)));
            //            mh.masterTag.childTags.PrintItems();
        }
    }

    /// <summary>
    /// Enums types
    /// </summary>
    #region Enums Types
    public enum loadType { ltString, ltFile, ltWeb, ltWebNoCache }                  //Define where to load from.
    //public enum threeSide { _default=0, _left, _top, _right }
    public enum fourSide { _default = 0, _left, _top, _right, _bottom }               //Define Left, Top, Right and Bottom
    public enum hAlignType { Unknown, Left, Right, Centre }                         //Define visible object horizontal hAlign
    public enum vAlignType { Unknown, Top, Bottom }                                 //Define visible object verticial hAlign
    public enum formMethodType { Default, Get, Post }                               //Define form action
    public enum tagStatusType { Normal, Focused, Active }                           //Define state of a visile tag
    public enum selectInfoPairs { sStart, sEnd }                                    //Define Start and End of SelectInfo
    public enum parseMode { Text = 0, Html, BBCode }                          		//Parse html or bbcode

    public enum HTMLFlag
    {
        TextFormat, Element, Dynamic, Table,
        Controls, Search, Xml, Region, Variable, None
    }                                                         //Define tag type in BuiltInTags
    public enum aTextStyle { isNormal, isSubScript, isSuperScript }                 //Define text style
    public enum loadStatus { Idle, Load, Update, Draw, Overlay }                    //Define what is miniHtml doing
    public enum elementType { eSpace, eText, eSymbol, eId, eClass, eStyle, eDash }  //Define type of a char
    public enum symbolType { Reserved, European, Symbol, Scientific, Shape }        //Define symbol type in BuiltInSymbols
    public enum textTransformType { None, Uppercase, Lowercase, Capitalize }        //Define how to transofm a text
    public enum positionStyleType { Static, Relative, Absolute, Fixed, Inherited }  //Define how to allocate a tag
    public enum borderStyleType                                                     //Define a list of border style 
    {
        None, Dotted, Dashed, Solid, Double, Groove, Ridge,
        Inset, Outset, Inherit
    }
    public enum bulletStyleType                                                     //Define a list of bullet style 
    {
        None, Circle, Square, Decimal, UpperAlpha, LowerAlpha,
        UpperRoman, LowerRoman
    }
    public enum variableType { Number, Alpha, String, Formated, Paragraph }         //Define variableType of variableTag (for search text)
    #endregion

    /// <summary>
    /// Records Types
    /// </summary>  
    #region Records Types

    public class ColorSettings
    { public System.Windows.Media.Color fontColor, urlColor, activeColor, visitedColor, backGroundColor; }
    public class KeyValuePair { public String key, value; }
    public class FontStyleSet { public bool bold, italic, regular, strikeout, underline; }
    public class HTMLTagInfo
    {
        public string Html;
        public HTMLFlag flags;
        public Int16 tagLevel;
        public HTMLTagInfo(string aHtml, HTMLFlag aFlags, Int16 aTagLevel)
        {
            this.Html = aHtml;
            this.flags = aFlags;
            this.tagLevel = aTagLevel;
        }
    }
    public class SymbolInfo
    {
        public string symbol;
        public Int32 code;
        public symbolType type;
        public SymbolInfo(string aSymbol, Int32 aCode, symbolType aType)
        {
            this.symbol = aSymbol;
            this.code = aCode;
            this.type = aType;
        }
    }
    public class loadOptionsType
    {
        public bool updateHtml, drawHtml, loadImage, alignImage;
        public Int32 maxWidth, maxHeight;
    }
    public class RomanDigits
    {
        public UInt32 value;
        public string rep;
        public RomanDigits(UInt32 aValue, string aRep)
        {
            this.value = aValue;
            this.rep = aRep;
        }
    }
    #endregion



    public class Defines
    {
        /// <summary>
        /// Constants
        /// </summary>
        #region Constants
        public static Int32 border = 5;
        public static string symbolList = @" !@#$%^&*()[]\,./{}:|?";
        public static string picMask = ".jpg .gif .png .bmp";
        public static Int32 defaultListIndent = 40;
        public static Int32 defaultBlockQuoteIndent = 10;
        public static Int32 defaultHRuleHeight = 10;
        public static Int32 defaultHRuleMargin = 5;
        public static Int32 defaultWidth = 200;
        public static string defaultFntName = "Courier";
        public static Int32 defaultFntSize = 12;
        public static string lineBreak = "\r\n";
        public static string formattedSpacing = "     ";
        #endregion

        /// <summary>
        /// Array Consts
        /// </summary>      
        #region Array Consts
        public static HTMLTagInfo[] BuiltinTags = new HTMLTagInfo[51] {   
          #region Built in tag list
           //HtmlTag Level guide
           // 50 Master
           // 40 Xml
           // 30 var(Variables tag for search)
           // 15 Html
           // 14 Title, Head, Body
           // 13 selection, hi, DIV, SPAN
           // 12 Table, centre, Form
           // 11 Tr (Table Row)
           // 10 Td, Th (Table Cell, Header)
           // 09 ul, ol (Numbering)
           // 08 li (List), Indent, blockquote
           // 07 P (Paragraph),  H1-H6
           // 06
           // 05 A HtmlTag, Input
           // 04 Text formating tags (B, strong, U, S, I, FONT, sub, sup), 
           // 03
           // 02
           // 01 Unknown Tags, script
           // 00 Text, hr, user, Img, Dynamic, BR, Meta, Binding
           new HTMLTagInfo ("master",       HTMLFlag.Region,        50),
           new HTMLTagInfo ("xml",          HTMLFlag.Xml,           40),
           new HTMLTagInfo ("var",          HTMLFlag.Search,        30),
           new HTMLTagInfo ("html",         HTMLFlag.Region,        15),
           new HTMLTagInfo ("head",         HTMLFlag.Region,        14),
           new HTMLTagInfo ("body",         HTMLFlag.Region,        14),
           new HTMLTagInfo ("title",        HTMLFlag.Region,        14),
           new HTMLTagInfo ("div",          HTMLFlag.Region,        13),
           new HTMLTagInfo ("selection",    HTMLFlag.TextFormat,    13),
           new HTMLTagInfo ("hi",           HTMLFlag.TextFormat,    13),
           new HTMLTagInfo ("table",        HTMLFlag.Table,         13),
           new HTMLTagInfo ("centre",       HTMLFlag.Region,        13),
           new HTMLTagInfo ("form",         HTMLFlag.Controls,      12),
           new HTMLTagInfo ("tr",           HTMLFlag.Table,         11),
           new HTMLTagInfo ("td",           HTMLFlag.Table,         10),
           new HTMLTagInfo ("th",           HTMLFlag.Table,         10),
           new HTMLTagInfo ("ul",           HTMLFlag.Region,        09),
           new HTMLTagInfo ("ol",           HTMLFlag.Region,        09),
           new HTMLTagInfo ("li",           HTMLFlag.Region,        08),
           new HTMLTagInfo ("blockquote",   HTMLFlag.TextFormat,    08),
           new HTMLTagInfo ("indent",       HTMLFlag.Region,        08),
           new HTMLTagInfo ("p",            HTMLFlag.Region,        07),
           new HTMLTagInfo ("h1",           HTMLFlag.Region,        07),
           new HTMLTagInfo ("h2",           HTMLFlag.Region,        07),
           new HTMLTagInfo ("h3",           HTMLFlag.Region,        07),
           new HTMLTagInfo ("h4",           HTMLFlag.Region,        07),
           new HTMLTagInfo ("h5",           HTMLFlag.Region,        07),
           new HTMLTagInfo ("h6",           HTMLFlag.Region,        07),
           new HTMLTagInfo ("span",         HTMLFlag.Region,        07),
           new HTMLTagInfo ("font",         HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("u",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("b",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("s",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("i",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("a",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("sup",          HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("sub",          HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("strong",       HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("color",        HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("input",        HTMLFlag.Controls,      02),
           new HTMLTagInfo ("select",       HTMLFlag.Controls,      02),
           new HTMLTagInfo ("option",       HTMLFlag.Controls,      02),
           new HTMLTagInfo ("script",       HTMLFlag.None,          01),
           new HTMLTagInfo ("meta",         HTMLFlag.Variable,      00),
           new HTMLTagInfo ("br",           HTMLFlag.Element,       00),
           new HTMLTagInfo ("hr",           HTMLFlag.Element,       00),
           new HTMLTagInfo ("img",          HTMLFlag.Element,       00),
           new HTMLTagInfo ("text",         HTMLFlag.Element,       00),
           new HTMLTagInfo ("binding",      HTMLFlag.Element,       00),
           new HTMLTagInfo ("dynamic",      HTMLFlag.Dynamic,       00),
           new HTMLTagInfo ("user",         HTMLFlag.Dynamic,       00),                  
           #endregion
       };

        public static SymbolInfo[] BuiltinSymbols = new SymbolInfo[252] {
          #region Built in Symbol list
            new SymbolInfo("amp"     ,0038,symbolType.Reserved), //01
            new SymbolInfo("gt"      ,0062,symbolType.Reserved), //02
            new SymbolInfo("lt"      ,0060,symbolType.Reserved), //03
            new SymbolInfo("quot"    ,0034,symbolType.Reserved), //04
            new SymbolInfo("acute"   ,0180,symbolType.European), //05
            new SymbolInfo("cedil"   ,0184,symbolType.European), //06
            new SymbolInfo("circ"    ,0710,symbolType.European), //07
            new SymbolInfo("macr"    ,0175,symbolType.European), //08
            new SymbolInfo("middot"  ,0183,symbolType.European), //09
            new SymbolInfo("tilde"   ,0732,symbolType.European), //10
            new SymbolInfo("urnl"    ,0168,symbolType.European), //11            
            new SymbolInfo("Aacute"  ,0193,symbolType.European), //12
            new SymbolInfo("aacute"  ,0225,symbolType.European), //13
            new SymbolInfo("Acirc"   ,0194,symbolType.European), //14
            new SymbolInfo("acirc"   ,0226,symbolType.European), //15
            new SymbolInfo("AElig"   ,0198,symbolType.European), //16
            new SymbolInfo("aelig"   ,0230,symbolType.European), //17
            new SymbolInfo("Agrave"  ,0192,symbolType.European), //18
            new SymbolInfo("agrave"  ,0224,symbolType.European), //19
            new SymbolInfo("Aring"   ,0197,symbolType.European), //20
            new SymbolInfo("aring"   ,0229,symbolType.European), //21
            new SymbolInfo("Atilde"  ,0195,symbolType.European), //22
            new SymbolInfo("atilde"  ,0227,symbolType.European), //23
            new SymbolInfo("Auml"    ,0196,symbolType.European), //24
            new SymbolInfo("auml"    ,0228,symbolType.European), //25
            new SymbolInfo("Ccedil"  ,0199,symbolType.European), //26
            new SymbolInfo("ccedil"  ,0231,symbolType.European), //27
            new SymbolInfo("Eacute"  ,0201,symbolType.European), //28
            new SymbolInfo("eacute"  ,0233,symbolType.European), //29
            new SymbolInfo("Ecirc"   ,0202,symbolType.European), //30
            new SymbolInfo("ecirc"   ,0234,symbolType.European), //31
            new SymbolInfo("Egrave"  ,0200,symbolType.European), //32
            new SymbolInfo("egrave"  ,0232,symbolType.European), //33
            new SymbolInfo("ETH"     ,0208,symbolType.European), //34
            new SymbolInfo("eth"     ,0240,symbolType.European), //35
            new SymbolInfo("Euml"    ,0203,symbolType.European), //36
            new SymbolInfo("euml"    ,0235,symbolType.European), //37
            new SymbolInfo("Iacute"  ,0205,symbolType.European), //38
            new SymbolInfo("iacute"  ,0237,symbolType.European), //39
            new SymbolInfo("Icirc"   ,0206,symbolType.European), //40
            new SymbolInfo("icirc"   ,0238,symbolType.European), //41
            new SymbolInfo("Igrave"  ,0204,symbolType.European), //42
            new SymbolInfo("igrave"  ,0236,symbolType.European), //43
            new SymbolInfo("Iuml"    ,0207,symbolType.European), //44
            new SymbolInfo("iuml"    ,0239,symbolType.European), //45
            new SymbolInfo("Ntide"   ,0209,symbolType.European), //46
            new SymbolInfo("Ntide"   ,0241,symbolType.European), //47
            new SymbolInfo("Oacute"  ,0211,symbolType.European), //48
            new SymbolInfo("oacute"  ,0243,symbolType.European), //49
            new SymbolInfo("Ocirc"   ,0212,symbolType.European), //50
            new SymbolInfo("Ocirc"   ,0244,symbolType.European), //51
            new SymbolInfo("OElig"   ,0338,symbolType.European), //52
            new SymbolInfo("oelig"   ,0339,symbolType.European), //53
            new SymbolInfo("Ograve"  ,0210,symbolType.European), //54
            new SymbolInfo("ograve"  ,0242,symbolType.European), //55
            new SymbolInfo("Oslash"  ,0216,symbolType.European), //56
            new SymbolInfo("oslash"  ,0248,symbolType.European), //57
            new SymbolInfo("Otilde"  ,0213,symbolType.European), //58
            new SymbolInfo("otilde"  ,0245,symbolType.European), //59
            new SymbolInfo("Ouml"    ,0214,symbolType.European), //60
            new SymbolInfo("ouml"    ,0246,symbolType.European), //61
            new SymbolInfo("Scaron"  ,0352,symbolType.European), //62
            new SymbolInfo("scaron"  ,0353,symbolType.European), //63
            new SymbolInfo("szlig"   ,0223,symbolType.European), //64
            new SymbolInfo("THORN"   ,0222,symbolType.European), //65
            new SymbolInfo("thorn"   ,0254,symbolType.European), //66
            new SymbolInfo("Uacute"  ,0218,symbolType.European), //67
            new SymbolInfo("uacute"  ,0250,symbolType.European), //68
            new SymbolInfo("Ucirc"   ,0219,symbolType.European), //69
            new SymbolInfo("ucirc"   ,0251,symbolType.European), //70
            new SymbolInfo("Ugrave"  ,0217,symbolType.European), //71
            new SymbolInfo("ugrave"  ,0249,symbolType.European), //72
            new SymbolInfo("Uuml"    ,0220,symbolType.European), //73
            new SymbolInfo("uuml"    ,0252,symbolType.European), //74
            new SymbolInfo("Yacute"  ,0221,symbolType.European), //75
            new SymbolInfo("yacute"  ,0253,symbolType.European), //76
            new SymbolInfo("Yuml"    ,0255,symbolType.European), //77
            new SymbolInfo("yuml"    ,0376,symbolType.European), //78
            new SymbolInfo("cent"    ,0162,symbolType.Symbol), //79
            new SymbolInfo("curren"  ,0164,symbolType.Symbol), //80
            new SymbolInfo("euro"    ,8364,symbolType.Symbol), //81
            new SymbolInfo("pound"   ,0163,symbolType.Symbol), //82
            new SymbolInfo("yen"     ,0165,symbolType.Symbol), //83            
            new SymbolInfo("brvbar"  ,0166,symbolType.Symbol), //84
            new SymbolInfo("bull"    ,8226,symbolType.Symbol), //85
            new SymbolInfo("copy"    ,0169,symbolType.Symbol), //86
            new SymbolInfo("dagger"  ,8224,symbolType.Symbol), //87
            new SymbolInfo("Dagger"  ,8225,symbolType.Symbol), //88
            new SymbolInfo("frasl"   ,8260,symbolType.Symbol), //89
            new SymbolInfo("hellip"  ,8230,symbolType.Symbol), //90
            new SymbolInfo("iexcl"   ,0161,symbolType.Symbol), //91
            new SymbolInfo("image"   ,8465,symbolType.Symbol), //92
            new SymbolInfo("iquest"  ,0191,symbolType.Symbol), //93
            new SymbolInfo("lrm"     ,8206,symbolType.Symbol), //94
            new SymbolInfo("mdash"   ,8212,symbolType.Symbol), //95
            new SymbolInfo("ndash"   ,8211,symbolType.Symbol), //96
            new SymbolInfo("not"     ,0172,symbolType.Symbol), //97
            new SymbolInfo("oline"   ,8254,symbolType.Symbol), //98
            new SymbolInfo("ordf"    ,0170,symbolType.Symbol), //99
            new SymbolInfo("ordm"    ,0186,symbolType.Symbol), //100
            new SymbolInfo("para"    ,0182,symbolType.Symbol), //101
            new SymbolInfo("permil"  ,8240,symbolType.Symbol), //102
            new SymbolInfo("prime"   ,8242,symbolType.Symbol), //103
            new SymbolInfo("Prime"   ,8243,symbolType.Symbol), //104
            new SymbolInfo("real"    ,8476,symbolType.Symbol), //105
            new SymbolInfo("reg"     ,0714,symbolType.Symbol), //106
            new SymbolInfo("rlm"     ,8207,symbolType.Symbol), //107
            new SymbolInfo("sect"    ,0167,symbolType.Symbol), //108
            new SymbolInfo("shy"     ,0173,symbolType.Symbol), //109
            new SymbolInfo("sup1"    ,0185,symbolType.Symbol), //110
            new SymbolInfo("trade"   ,8482,symbolType.Symbol), //111
            new SymbolInfo("weierp"  ,8472,symbolType.Symbol), //112            
            new SymbolInfo("bdquo"   ,8222,symbolType.Symbol), //113
            new SymbolInfo("laquo"   ,0171,symbolType.Symbol), //114
            new SymbolInfo("ldquo"   ,8220,symbolType.Symbol), //115
            new SymbolInfo("lsaquo"  ,8249,symbolType.Symbol), //116
            new SymbolInfo("lsquo"   ,8216,symbolType.Symbol), //117
            new SymbolInfo("raquo"   ,0187,symbolType.Symbol), //118
            new SymbolInfo("rdquo"   ,8221,symbolType.Symbol), //119
            new SymbolInfo("rsaquo"  ,8250,symbolType.Symbol), //120
            new SymbolInfo("rsquo"   ,8217,symbolType.Symbol), //121
            new SymbolInfo("sbquo"   ,8218,symbolType.Symbol), //122            
            new SymbolInfo("emsp"    ,8195,symbolType.Symbol), //123
            new SymbolInfo("ensp"    ,8194,symbolType.Symbol), //124
            new SymbolInfo("nbsp"    ,0160,symbolType.Symbol), //125
            new SymbolInfo("thinsp"  ,8201,symbolType.Symbol), //126
            new SymbolInfo("zwj"     ,8205,symbolType.Symbol), //127
            new SymbolInfo("zwnj"    ,8204,symbolType.Symbol), //128
            new SymbolInfo("deg"    ,0176,symbolType.Scientific), //129
            new SymbolInfo("divide" ,0247,symbolType.Scientific), //130
            new SymbolInfo("frac12" ,0189,symbolType.Scientific), //131
            new SymbolInfo("frac14" ,0188,symbolType.Scientific), //132
            new SymbolInfo("frac34" ,0190,symbolType.Scientific), //133
            new SymbolInfo("ge"     ,8805,symbolType.Scientific), //134
            new SymbolInfo("le"     ,8804,symbolType.Scientific), //135
            new SymbolInfo("minus"  ,8722,symbolType.Scientific), //136
            new SymbolInfo("sup2"   ,0178,symbolType.Scientific), //137
            new SymbolInfo("sup3"   ,0179,symbolType.Scientific), //138
            new SymbolInfo("times"  ,0215,symbolType.Scientific), //139
            new SymbolInfo("alefsym",8501,symbolType.Scientific), //140
            new SymbolInfo("and"    ,8743,symbolType.Scientific), //141
            new SymbolInfo("ang"    ,8736,symbolType.Scientific), //142
            new SymbolInfo("asymp"  ,8776,symbolType.Scientific), //143
            new SymbolInfo("cap"    ,8745,symbolType.Scientific), //144
            new SymbolInfo("cong"   ,8773,symbolType.Scientific), //145
            new SymbolInfo("cup"    ,8746,symbolType.Scientific), //146
            new SymbolInfo("empty"  ,8709,symbolType.Scientific), //147
            new SymbolInfo("equiv"  ,8801,symbolType.Scientific), //148
            new SymbolInfo("exist"  ,8707,symbolType.Scientific), //149
            new SymbolInfo("fnof"   ,0402,symbolType.Scientific), //150
            new SymbolInfo("forall" ,8704,symbolType.Scientific), //151
            new SymbolInfo("infin"  ,8734,symbolType.Scientific), //152
            new SymbolInfo("int"    ,8747,symbolType.Scientific), //153
            new SymbolInfo("isin"   ,8712,symbolType.Scientific), //154
            new SymbolInfo("lang"   ,9001,symbolType.Scientific), //155
            new SymbolInfo("lceil"  ,8968,symbolType.Scientific), //156
            new SymbolInfo("lfloor" ,8970,symbolType.Scientific), //157
            new SymbolInfo("lowast" ,8727,symbolType.Scientific), //158
            new SymbolInfo("micro"  ,0181,symbolType.Scientific), //159
            new SymbolInfo("nabla"  ,8711,symbolType.Scientific), //160
            new SymbolInfo("ne"     ,8800,symbolType.Scientific), //161
            new SymbolInfo("ni"     ,8715,symbolType.Scientific), //162
            new SymbolInfo("notin"  ,8713,symbolType.Scientific), //163
            new SymbolInfo("nsub"   ,8836,symbolType.Scientific), //164
            new SymbolInfo("cplus"  ,8853,symbolType.Scientific), //165
            new SymbolInfo("or"     ,8744,symbolType.Scientific), //166
            new SymbolInfo("otimes" ,8855,symbolType.Scientific), //167
            new SymbolInfo("part"   ,8706,symbolType.Scientific), //168
            new SymbolInfo("perp"   ,8869,symbolType.Scientific), //169
            new SymbolInfo("plusmn" ,0177,symbolType.Scientific), //170
            new SymbolInfo("prod"   ,8719,symbolType.Scientific), //171
            new SymbolInfo("prop"   ,8733,symbolType.Scientific), //172
            new SymbolInfo("radic"  ,8730,symbolType.Scientific), //173
            new SymbolInfo("rang"   ,9002,symbolType.Scientific), //174
            new SymbolInfo("rceil"  ,8969,symbolType.Scientific), //175
            new SymbolInfo("rfloor" ,8971,symbolType.Scientific), //176
            new SymbolInfo("sdot"   ,8901,symbolType.Scientific), //177
            new SymbolInfo("sim"    ,8764,symbolType.Scientific), //178
            new SymbolInfo("sub"    ,8834,symbolType.Scientific), //179
            new SymbolInfo("sube"   ,8838,symbolType.Scientific), //180
            new SymbolInfo("sum"    ,8721,symbolType.Scientific), //181
            new SymbolInfo("sup"    ,8835,symbolType.Scientific), //182
            new SymbolInfo("supe"   ,8839,symbolType.Scientific), //183
            new SymbolInfo("there4" ,8756,symbolType.Scientific), //184
            new SymbolInfo("Alpha"  ,0913,symbolType.Scientific), //185
            new SymbolInfo("alpha"  ,0945,symbolType.Scientific), //186
            new SymbolInfo("Beta"   ,0914,symbolType.Scientific), //187
            new SymbolInfo("beta"   ,0946,symbolType.Scientific), //188
            new SymbolInfo("Chi"    ,0935,symbolType.Scientific), //189
            new SymbolInfo("chi"    ,0967,symbolType.Scientific), //190
            new SymbolInfo("Delta"  ,0916,symbolType.Scientific), //191
            new SymbolInfo("delta"  ,0948,symbolType.Scientific), //192
            new SymbolInfo("Epsilon",0917,symbolType.Scientific), //193
            new SymbolInfo("epsilon",0949,symbolType.Scientific), //194
            new SymbolInfo("Eta"    ,0919,symbolType.Scientific), //195
            new SymbolInfo("eta"    ,0951,symbolType.Scientific), //196
            new SymbolInfo("Gamma"  ,0915,symbolType.Scientific), //197
            new SymbolInfo("gamma"  ,0947,symbolType.Scientific), //198
            new SymbolInfo("Iota"   ,0921,symbolType.Scientific), //199
            new SymbolInfo("iota"   ,0953,symbolType.Scientific), //200
            new SymbolInfo("Kappa"  ,0922,symbolType.Scientific), //201
            new SymbolInfo("kappa"  ,0954,symbolType.Scientific), //202
            new SymbolInfo("Lambda" ,0923,symbolType.Scientific), //203
            new SymbolInfo("lambda" ,0955,symbolType.Scientific), //204
            new SymbolInfo("Mu"     ,0924,symbolType.Scientific), //205
            new SymbolInfo("mu"     ,0956,symbolType.Scientific), //206
            new SymbolInfo("Nu"     ,0925,symbolType.Scientific), //207
            new SymbolInfo("nu"     ,0957,symbolType.Scientific), //208
            new SymbolInfo("Omega"  ,0937,symbolType.Scientific), //209
            new SymbolInfo("omega"  ,0969,symbolType.Scientific), //210
            new SymbolInfo("Omicron",0927,symbolType.Scientific), //211
            new SymbolInfo("omicron",0959,symbolType.Scientific), //212
            new SymbolInfo("Phi"    ,0934,symbolType.Scientific), //213
            new SymbolInfo("phi"    ,0966,symbolType.Scientific), //214
            new SymbolInfo("Pi"     ,0928,symbolType.Scientific), //215
            new SymbolInfo("pi"     ,0960,symbolType.Scientific), //216
            new SymbolInfo("piv"    ,0982,symbolType.Scientific), //217
            new SymbolInfo("Psi"    ,0936,symbolType.Scientific), //218
            new SymbolInfo("psi"    ,0968,symbolType.Scientific), //219
            new SymbolInfo("Rho"    ,0929,symbolType.Scientific), //220
            new SymbolInfo("rho"    ,0961,symbolType.Scientific), //221
            new SymbolInfo("Sigma"  ,0931,symbolType.Scientific), //222
            new SymbolInfo("sigma"  ,0963,symbolType.Scientific), //223
            new SymbolInfo("sigmaf" ,0962,symbolType.Scientific), //224
            new SymbolInfo("Tau"    ,0932,symbolType.Scientific), //225
            new SymbolInfo("tau"    ,0964,symbolType.Scientific), //226
            new SymbolInfo("Theta"  ,0920,symbolType.Scientific), //227
            new SymbolInfo("theta"  ,0952,symbolType.Scientific), //228
            new SymbolInfo("thetasym",0977,symbolType.Scientific),//229
            new SymbolInfo("upsih"  ,0978,symbolType.Scientific), //230
            new SymbolInfo("Upsilon",0933,symbolType.Scientific), //231
            new SymbolInfo("upsilon",0965,symbolType.Scientific), //232
            new SymbolInfo("Xi"     ,0926,symbolType.Scientific), //233
            new SymbolInfo("xi"     ,0958,symbolType.Scientific), //234
            new SymbolInfo("Zeta"   ,0918,symbolType.Scientific), //235
            new SymbolInfo("zeta"   ,0950,symbolType.Scientific), //236
            new SymbolInfo("crarr"  ,8629,symbolType.Shape), //237
            new SymbolInfo("darr"   ,8595,symbolType.Shape), //238
            new SymbolInfo("dArr"   ,8659,symbolType.Shape), //239
            new SymbolInfo("harr"   ,8596,symbolType.Shape), //240
            new SymbolInfo("hArr"   ,8660,symbolType.Shape), //241
            new SymbolInfo("larr"   ,8592,symbolType.Shape), //242
            new SymbolInfo("lArr"   ,8656,symbolType.Shape), //243
            new SymbolInfo("rarr"   ,8594,symbolType.Shape), //244
            new SymbolInfo("rArr"   ,8658,symbolType.Shape), //245
            new SymbolInfo("uarr"   ,8593,symbolType.Shape), //246
            new SymbolInfo("uArr"   ,8657,symbolType.Shape), //247            
            new SymbolInfo("clubs"  ,9827,symbolType.Shape), //248
            new SymbolInfo("diams"  ,9830,symbolType.Shape), //249
            new SymbolInfo("hearts" ,9829,symbolType.Shape), //250
            new SymbolInfo("spades" ,9824,symbolType.Shape), //251
            new SymbolInfo("loz"    ,9674,symbolType.Shape)  //252
            #endregion
       };

        public static String[] BuiltinStyles = new String[83]
       {
          #region Built in Style list        
        "background-attachment",        //00
		"background-color",             //01
		"backgroundimage",              //02
		"background-repeat",            //03
		"background-position",          //04
		"border",                       //05
		"border-color",                 //06
		"border-spacing",               //07
		"border-style",                 //08
		"border-top",                   //09
		"border-right",                 //10
		"border-bottom",                //11
		"border-left",                  //12
		"border-top-color",             //13
		"border-right-color",           //14
		"border-bottom-color",          //15
		"border-left-color",            //16
		"border-top-style",             //17
		"border-right-style",           //18
		"border-bottom-style",          //19
		"border-left-style",            //20
		"border-top-width",             //21
		"border-right-width",           //22
		"border-bottom-width",          //23
		"border-left-width",            //24
		"border-width",                 //25
		"clear",                        //26
		"bottom",                       //27
		"color",                        //28
		"cursor",                       //29
		"display",                      //30
		"float",                        //31
		"font",                         //32
		"font-family",                  //33
		"font-size",                    //34
		"font-style",                   //35
		"font-variant",                 //36
		"font-weight",                  //37
		"height",                       //38
		"left",                         //39
		"letter-spacing",               //40
		"line-height",                  //41
		"list-style",                   //42
		"list-style-image",             //43
		"list-style-position",          //44
		"list-style-type",              //45
		"margin",                       //46
		"margin-top",                   //47
		"margin-right",                 //48
		"margin-bottom",                //49
		"margin-left",                  //50
		"marks",                        //51
		"max-height",                   //52
		"max-width",                    //53
		"min-height",                   //54
		"min-width",                    //55
		"orphans",                      //56
		"overflow",                     //57
		"padding",                      //58
		"padding-top",                  //59
		"padding-right",                //60
		"padding-bottom",               //61
		"padding-left",                 //62
		"page",                         //63
		"page-break-after",             //64
		"page-break-before",            //65
		"page-break-inside",            //66
		"position",                     //67
		"right",                        //68
		"size",                         //69
		"table-display",                //70
		"text-align",                   //71
		"text-decoration",              //72
		"text-indent",                  //73
		"text-transform",               //74
		"top",                          //75
		"vertical-align",               //76
		"visibility",                   //77
		"white-space",                  //78
		"windows",                      //79
		"width",                        //80
		"word-spacing",                 //81
		"z-index"                       //82
        #endregion
       };

        public static RomanDigits[] BuiltinRomans = new RomanDigits[13]
       {
       	  #region Built in Romans list       	  
       	  new RomanDigits(1000, "M"),
          new RomanDigits(900, "CM"),
          new RomanDigits(500, "D"),
          new RomanDigits(400, "CD"),
          new RomanDigits(100, "C"),
          new RomanDigits(90, "XC"),
          new RomanDigits(50, "L"),
          new RomanDigits(40, "XL"),
          new RomanDigits(10, "X"),
          new RomanDigits(9, "IX"),
          new RomanDigits(5, "V"),
          new RomanDigits(4, "IV"),
          new RomanDigits(1, "I"),
	      #endregion
       };

        public static HTMLTagInfo[] BuiltinBBCodes = new HTMLTagInfo[14] {   
          #region Built in BBCode list           
           new HTMLTagInfo ("ul",           HTMLFlag.Region,        09),
           new HTMLTagInfo ("ol",           HTMLFlag.Region,        09),
           new HTMLTagInfo ("*",            HTMLFlag.Region,        08),
           new HTMLTagInfo ("quote",        HTMLFlag.TextFormat,    08),
           new HTMLTagInfo ("centre",       HTMLFlag.Region,        07),
           new HTMLTagInfo ("size",         HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("color",        HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("u",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("b",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("s",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("i",            HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("url",          HTMLFlag.TextFormat,    04),
           new HTMLTagInfo ("img",          HTMLFlag.Element,       00),
           new HTMLTagInfo ("br",           HTMLFlag.Region,        00),
           #endregion
       };
        #endregion
        /// <summary>
        /// Event Handlers
        /// </summary>
        #region Event Handlers
        public class LinkClickEventArgs : EventArgs
        {
            private Object cTag;
            private String cURL;
            public Object currentTag { get { return cTag; } }
            public String targetURL { get { return cURL; } }
            public LinkClickEventArgs(Object aTag, String aURL)
            {
                this.cTag = aTag;
                this.cURL = aURL;
            }
        }

        public class mhWorkEventArgs : EventArgs
        {
            public enum WorkType { wUpdate, wLoad, wDraw }
            public WorkType work;
            public mhWorkEventArgs(WorkType aWorkType)
            {
                this.work = aWorkType;
            }
        }

        public class FormElementEventArgs : EventArgs
        {
            public Object ElementTag;
            public FormElementEventArgs(Object aTag)
            {
                this.ElementTag = aTag;
            }
        }

        public delegate void LinkClickEventHandler(
            Object sender,
            LinkClickEventArgs e);

        public delegate void mhWorkEventHandler(
            Object sender,
            mhWorkEventArgs e);

        public delegate void FormEventHandler(
            Object sender,
            FormElementEventArgs e);
        #endregion






        /// <summary>
        /// Test current unit.
        /// </summary>       
        public static void DebugUnit()
        {


        }
    }

    public class CurrentStateType
    {
        private List<HtmlTag> activeStyle = new List<HtmlTag>();
        private bool bold;
        private bool italic;
        private bool underline;
        private bool subscript;
        private bool superscript;
        private string hyperlink = null;
        private System.Windows.Media.Color? foreground;
        private string font = null;
        private double? fontSize;


        public bool Bold { get { return bold; } }
        public bool Italic { get { return italic; } }
        public bool Underline { get { return underline; } }
        public bool SubScript { get { return subscript; } }
        public bool SuperScript { get { return superscript; } }
        public string HyperLink { get { return hyperlink; } }
        public System.Windows.Media.Color? Foreground { get { return foreground; } }
        public string Font { get { return font; } }
        public double? FontSize { get { return fontSize; } }

        public void UpdateStyle(HtmlTag aTag)
        {
            if (!aTag.IsEndTag)
                activeStyle.Add(aTag);
            else
                for (int i = activeStyle.Count - 1; i >= 0; i--)
                    if ('/' + activeStyle[i].Name == aTag.Name)
                    {
                        activeStyle.RemoveAt(i);
                        break;
                    }
            updateStyle();
        }


        private void updateStyle()
        {
            bold = false;
            italic = false;
            underline = false;
            subscript = false;
            superscript = false;
            foreground = null;
            font = null;
            hyperlink = "";
            fontSize = null;

            foreach (HtmlTag aTag in activeStyle)
                switch (aTag.Name)
                {
                    case "b": bold = true; break;
                    case "i": italic = true; break;
                    case "u": underline = true; break;
                    case "sub": subscript = true; break;
                    case "sup": superscript = true; break;
                    case "a": if (aTag.Contains("href")) hyperlink = aTag["href"]; break;
                    case "font":
                        if (aTag.Contains("color"))
                            try { foreground = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(aTag["color"]); }
                            catch { foreground = Colors.Black; }
                        if (aTag.Contains("face"))
                            font = aTag["face"];
                        if (aTag.Contains("size"))
                            try { fontSize = Double.Parse(aTag["size"]); }
                            catch { };
                        break;
                }
        }

        public CurrentStateType()
        {

        }



    }

    /// <summary>
	/// Description of DependencyProterty.
	/// </summary>
	public class DependencyProperty<AnyType>
    {
        private static List<DependencyProperty<AnyType>> dependencyPropertyList = new List<DependencyProperty<AnyType>>();
        private string name;
        private object propertyValue;
        private Type propertyType;
        private Type ownerType;
        private PropertyMetadata typeMetadata;
        private ValidateValueCallback validateValueCallback;

        public Type PropertyType { get { return propertyType; } }
        internal object PropertyValue
        {
            get { return propertyValue; }
            set { propertyValue = value; CallValidateValueCallback(); }
        }




        private DependencyProperty(string aName, Type aPropertyType,
                                   Type anOwnerType, PropertyMetadata aTypeMetadata,
                                   ValidateValueCallback aValidateValueCallback)
        {
            name = aName;
            propertyType = aPropertyType;
            ownerType = anOwnerType;
            typeMetadata = aTypeMetadata;
            validateValueCallback = aValidateValueCallback;
        }

        public static DependencyProperty<AnyType> Register(string name, Type propertyType,
                                                  Type ownerType, PropertyMetadata typeMetadata,
                                                  ValidateValueCallback validateValueCallback)
        {
            DependencyProperty<AnyType> retVal = new DependencyProperty<AnyType>(name, propertyType, ownerType,
                                                               typeMetadata, validateValueCallback);




            dependencyPropertyList.Add(retVal);
            return retVal;
        }

        public static void SetValue(DependencyProperty<AnyType> property, object propertyValue)
        {
            property.PropertyValue = propertyValue;
        }

        public static object GetValue(DependencyProperty<AnyType> property)
        {
            return property.PropertyValue;
        }


        private void CallValidateValueCallback()
        {
            if (validateValueCallback != null)
                validateValueCallback(this, new ValidateValueEventArgs());
        }
    }


    internal class DependencyPropertyList
    {

    }

    public class PropertyMetadata
    {

    }

    public class ValidateValueEventArgs : EventArgs
    {

    }

    public delegate void ValidateValueCallback(Object sender, ValidateValueEventArgs e);


    public class ProcessInfo
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public Int32 ProcessID;
        public Int32 ThreadID;
    }

    public class Header
    {
#if CF
        const string user32 = "coredll.dll";
        const string kernel32 = "coredll.dll";
#else
        const string user32 = "user32.dll";
        const string kernel32 = "kernel32.dll";
#endif

        [DllImport(kernel32)]
        public static extern Int32 CreateProcess(string appName,
            string cmdLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
            Int32 boolInheritHandles, Int32 dwCreationFlags, IntPtr lpEnvironment,
            IntPtr lpszCurrentDir, Byte[] si, ProcessInfo pi);

        [DllImport(kernel32)]
        public static extern Int32 WaitForSingleObject(IntPtr handle, Int32 wait);

        [DllImport(kernel32)]
        public static extern Int32 GetLastError();

        [DllImport(kernel32)]
        public static extern Int32 CloseHandle(IntPtr handle);


    }

    /// <summary>
    /// An Item entry, can store a string and an object (Pie)
    /// </summary>
    public class PropertyItemType
    {
        public string key;
        public string value;
        public object attachment;
        public PropertyItemType(string aKey, string aValue)
        {
            key = aKey;
            value = aValue;
        }
        public PropertyItemType(string aKey, object anAttachment)
        {
            key = aKey;
            attachment = anAttachment;
        }
        public PropertyItemType(string aKey, string aValue, object anAttachment)
        {
            key = aKey;
            value = aValue;
            attachment = anAttachment;
        }
    }
    /// <summary>
    /// Variable List that use "Key" to store PropertyItemType
    /// </summary>
    public class PropertyList : ListDictionary
    {
        public bool createIfNotExist = true;

        /// <summary>
        /// Get an item from the list
        /// </summary>
        private PropertyItemType getPropertyInfo(string aKey)
        {
            if (Contains(aKey))
            {
                foreach (DictionaryEntry de in this)
                {
                    if ((string)de.Key == aKey)
                    {
                        return (PropertyItemType)de.Value;
                    }
                }
            }

            PropertyItemType retVal = new PropertyItemType(aKey, "");
            if (createIfNotExist) { Add(aKey, retVal); }
            return retVal;
        }
        /// <summary>
        /// Get an item from the list
        /// </summary>
        private PropertyItemType getPropertyInfo(Int32 anId)
        {
            IDictionaryEnumerator Enum = GetEnumerator();
            if (Count >= anId)
            {
                for (int i = 0; i <= anId; i++) { Enum.MoveNext(); }
                return (PropertyItemType)Enum.Value;
            }
            return new PropertyItemType(anId.ToString(), "");
        }
        /// <summary>
        /// Set or Add an item to the list
        /// </summary>
        private void setPropertyInfo(string aKey, PropertyItemType aValue)
        {
            VerifyType(aValue);
            if (Contains(aKey))
            {
                this.Remove(aKey);
            }
            Add(aKey, aValue);
        }
        /// <summary>
        /// Check item before add.
        /// </summary>        
        private void VerifyType(object value)
        {
            if (!(value is PropertyItemType))
            { throw new ArgumentException("Invalid Type."); }
        }
        /// <summary>
        /// Add a new PropertyInfo
        /// </summary>
        public void Add(string aKey, string aValue)
        {
            Add(aKey, new PropertyItemType(aKey, aValue));
        }
        /// <summary>
        /// Add a new PropertyInfo
        /// </summary>
        public void Add(string aKey, string aValue, object anAttachment)
        {
            Add(aKey, new PropertyItemType(aKey, aValue, anAttachment));
        }
        /// <summary>
        /// Retrieve a PropertyItem using a key
        /// </summary>
        public PropertyItemType this[String aKey]
        {
            get
            {
                return getPropertyInfo(aKey);
            }
            set
            {
                setPropertyInfo(aKey, value);
            }
        }
        /// <summary>
        /// Retrieve a PropertyItem using an id
        /// </summary>
        public PropertyItemType this[Int32 anId]
        {
            get
            {
                return getPropertyInfo(anId);
            }

        }

        public string Html()
        {
            string retVal = "";
            for (Int32 i = 0; i < this.Count; i++)
            {
                PropertyItemType item = this[i];
                retVal += " " + item.key + "=\"" + item.value + "\"";
            }

            return retVal;
        }


        public PropertyList Clone()
        {
            PropertyList retVal = new PropertyList();
            foreach (PropertyItemType item in this)
                retVal.Add(item.key, item.value);
            return retVal;
        }

        public override string ToString()
        {
            string retVal = "";
            for (int i = 0; i < Count; i++)
                retVal += String.Format(" {0}=\"{1}\"; ", this[i].key, this[i].value);
            if (retVal == "")
                return " ";
            else return retVal;
        }


        public static void DebugUnit()
        {
            PropertyList list = new PropertyList();
            list["abc"].value = "abcd";
            list["bcd"].value = "bcde";
            list["cde"].value = "cdef";
            list.Remove("abc");

            Debug.Assert((list["bcd"].value == "bcde"), "PropertyList Failed.");
            Debug.Assert((list["abc"].value == ""), "PropertyList Failed.");
        }


        public static PropertyList FromString(string s)
        {
            return Utils.ExtravtVariables(s);
        }



    }

    /// <summary>
	/// Convert Header string to useable type.
	/// </summary>
	public class CssHeaderStyleType
    {
        public static string UnspecifiedTagName = "n0ne";
        public enum ElementType
        {
            Unknown, FirstChar, FirstLine, FirstLetter,
            Link, Visited, Focused, Hover, Active
        };
        public ElementType elements;
        public string tagName;
        public string tagClass;
        public string tagID;
        public bool familyTag;
        public bool noOtherClassID;

        public static ElementType ElementsToCssElementType(string input)
        {
            switch (input)
            {
                case ("first-char"):
                    return ElementType.FirstChar;
                case ("first-line"):
                    return ElementType.FirstLine;
                case ("first-letter"):
                    return ElementType.FirstLetter;
                case ("link"):
                    return ElementType.Link;
                case ("visited"):
                    return ElementType.Visited;
                case ("focused"):
                    return ElementType.Focused;
                case ("hover"):
                    return ElementType.Hover;
                default:
                    return ElementType.Active;
            }
        }
        public void PrintItems()
        {
            string fmt = "Name:{0}, ID:{1}, Cls:{2}, EleIdx:{3}, Flags:{4}{5}";
            string f = "_"; if (familyTag) f = "F";
            string n = "_"; if (noOtherClassID) n = "N";
            Console.WriteLine(String.Format(fmt, tagName, tagID, tagClass,
                                          (Int32)elements, f, n));
        }

        public string Css()
        {
            string idStr = "";
            string classStr = "";
            if (tagID != "") idStr = "#" + tagID;
            if (tagClass != "") classStr = "." + tagClass;
            return tagName + idStr + classStr + " ";
        }

        public CssHeaderStyleType(string header)
        {
            elements = ElementType.Unknown;
            familyTag = false;
            noOtherClassID = false;
            tagID = "";
            tagClass = "";

            string k = header;

            char lastChar = k[k.Length - 1];
            switch (lastChar)
            {
                case '+':
                    familyTag = true;
                    k = k.Substring(0, k.Length - 1);
                    break;
                case '>':
                    noOtherClassID = true;
                    k = k.Substring(0, k.Length - 1);
                    break;
            }

            if (k.IndexOf('#') > -1)
                tagID = Utils.ExtractAfter(ref k, '#');
            if (k.IndexOf('.') > -1)
                tagClass = Utils.ExtractAfter(ref k, '.');
            if (k.IndexOf(':') > -1)
                elements = CssHeaderStyleType.ElementsToCssElementType(
                                   Utils.ExtractAfter(ref k, ':'));
            tagName = k;
            if (tagName.Trim() == "")
                tagName = UnspecifiedTagName;

        }
    }

    /// <summary>
    /// Store one record of a full css style (header and styles)
    /// </summary>
    public class CssStyleType
    {
        public string tagName;
        public string styleTagName;
        public ArrayList parentTagName;
        public string styleClass;
        public string styleID;
        public PropertyList styles;

        public CssStyleType()
        {
            parentTagName = new ArrayList();
            styles = new PropertyList();
        }
        ~CssStyleType()
        {
            styles = null;
            parentTagName = null;
        }
        public string printParentTagName()
        {
            string retVal = "";
            foreach (object o in parentTagName)
            {
                retVal += ',' + (string)o;
            }
            return retVal.Trim(',');
        }
    }

    public class CssStyleGroupType
    {
        public string styleTagName;
        public ArrayList parentTagName;

        public CssStyleGroupType()
        {
            parentTagName = new ArrayList();
        }
        ~CssStyleGroupType()
        {
            parentTagName = null;
        }
    }

    /// <summary>
    /// A List of CssStyleType
    /// </summary>
    public class CssStyleList : CollectionBase
    {
        private CssStyleType getCssStyle(Int32 index)
        {
            return (CssStyleType)List[index];
        }

        private void setCssStyle(Int32 index, CssStyleType value)
        {
            List[index] = value;
        }

        private void verifyType(object value)
        {
            if (value == null)
                throw new ArgumentException("Nil exception");
            if (!(value is CssStyleType))
                throw new ArgumentException("Invalid Type - " + value.ToString());
        }

        protected override void OnInsert(int index, object value)
        {
            verifyType(value);
            base.OnInsert(index, value);
        }

        protected override void OnSet(int index, object oldValue, object newValue)
        {
            verifyType(newValue);
            base.OnSet(index, oldValue, newValue);
        }

        protected override void OnValidate(object value)
        {
            verifyType(value);
            base.OnValidate(value);
        }

        public CssStyleList() : base()
        {

        }

        ~CssStyleList()
        {
            List.Clear();
        }

        public Int32 Add(CssStyleType value)
        {
            return List.Add(value);
        }

        public void Insert(Int32 index, CssStyleType value)
        {
            List.Insert(index, value);
        }

        public void Remove(CssStyleType value)
        {
            List.Remove(value);
        }

        public bool Contains(CssStyleType value)
        {
            return List.Contains(value);
        }

        public void PrintItems()
        {
            for (Int32 i = 0; i < Count; i++)
            {
                CssStyleType c = this[i];
                CssHeaderStyleType style = new CssHeaderStyleType(c.styleTagName);
                style.PrintItems();

                for (Int32 j = 0; j < c.styles.Count; j++)
                {
                    string output = String.Format("[key: {0} = {1}]",
                                                  c.styles[j].key, c.styles[j].value);
                    Console.WriteLine(output);
                }
                Console.WriteLine("");
            }
        }

        public string Css()
        {
            string retVal = "";
            for (Int32 i = 0; i < Count; i++)
            {
                CssStyleType c = this[i];
                CssHeaderStyleType style = new CssHeaderStyleType(c.styleTagName);
                retVal += style.Css() + "{ ";

                for (Int32 j = 0; j < c.styles.Count; j++)
                    retVal += c.styles[j].key + ":" + c.styles[j].value + ";";

                retVal += " }" + Defines.lineBreak;
            }
            return retVal;
        }

        public CssStyleType this[Int32 index]
        {
            get
            {
                return getCssStyle(index);
            }
            set
            {
                setCssStyle(index, value);
            }
        }
    }

    /// <summary>
    /// A List of CssStyleType for specified tag
    /// </summary>
    public class TagCssStyleType
    {
        public string tagName;
        public CssStyleList cssStyles;
        public TagCssStyleType(string aTagName) : base()
        {
            tagName = aTagName;
            cssStyles = new CssStyleList();
        }
        ~TagCssStyleType()
        {
            cssStyles = null;
        }
        public void AddCssStyle(CssStyleType aStyle)
        {
            cssStyles.Add(aStyle);
        }

    }

    /// <summary>
    /// Container of all TagCssStyleType (All cssStyles for all tags)
    /// </summary>
    public class TagCssStyleDictionary : ListDictionary
    {
        private TagCssStyleType getCssTagStyle(string key)
        {
            if (Contains(key))
            {
                foreach (DictionaryEntry de in this)
                    if ((string)de.Key == key)
                        return (TagCssStyleType)(de.Value);
            }

            return new TagCssStyleType(key);
        }
        private TagCssStyleType getCssTagStyle(Int32 id)
        {
            IDictionaryEnumerator em = this.GetEnumerator();
            if (Count >= id)
            {
                for (Int32 i = 0; i <= id; i++)
                    em.MoveNext();
                return (TagCssStyleType)(em.Value);
            }
            return new TagCssStyleType(id.ToString());
        }
        private void setCssTagStyle(string key, TagCssStyleType value)
        {
            if (Contains(key))
            {
                foreach (DictionaryEntry de in this)
                    if ((string)de.Key == key)
                    {
                        DictionaryEntry te = de;
                        te.Value = value;
                        return;
                    }
            }
            else
                Add(key, value);
        }
        private void verifyType(object value)
        {
            if (!(value is TagCssStyleType))
                throw new ArgumentException("Invalid Type");
        }

        public TagCssStyleDictionary() : base()
        {

        }

        ~TagCssStyleDictionary()
        {

        }

        public void PrintItems()
        {
            IDictionaryEnumerator em = this.GetEnumerator();
            while (em.MoveNext())
                ((TagCssStyleType)(em.Value)).cssStyles.PrintItems();
        }

        public string Css()
        {
            string retVal = "";
            IDictionaryEnumerator em = this.GetEnumerator();
            while (em.MoveNext())
                retVal += ((TagCssStyleType)(em.Value)).cssStyles.Css();
            return retVal;
        }

        public void AddCssStyle(string input)
        {
            ArrayList css = Utils.DecodeCssStyle(input);
            for (Int32 i = 0; i < css.Count; i++)
            {
                CssStyleType cssStyle = (CssStyleType)(css[i]);
                if (cssStyle.styleTagName != "")
                {
                    if (!(Contains(cssStyle.tagName)))
                        Add(cssStyle.tagName, new TagCssStyleType(cssStyle.tagName));
                    this[cssStyle.tagName].AddCssStyle(cssStyle);
                }
            }
        }

        public TagCssStyleType this[string input]
        {
            get
            {
                return getCssTagStyle(input);
            }
            set
            {
                setCssTagStyle(input, value);
            }
        }

        public TagCssStyleType this[Int32 id]
        {
            get
            {
                return getCssTagStyle(id);
            }
        }

        public CssStyleList ListAllCssStyle(string tagName)
        {
            CssStyleList retVal = new CssStyleList();

            for (Int32 i = 0; i < this[tagName].cssStyles.Count; i++)
                retVal.Add(this[tagName].cssStyles[i]);
            for (Int32 i = 0; i < this[CssHeaderStyleType.UnspecifiedTagName].cssStyles.Count; i++)
                retVal.Add(this[CssHeaderStyleType.UnspecifiedTagName].cssStyles[i]);

            return retVal;
        }

    }

    public class Utils
    {
        // Text based conversion and replacement routines //
        #region Char Type detection
        public static bool isNumber(char c) { return Char.IsNumber(c); }
        public static bool isAlpha(char c) { return Char.IsLetter(c); }
        public static bool iaSymbol(char c) { return !((isNumber(c)) && (isAlpha(c))); }
        public static elementType charType(char c)
        {
            if ((Utils.isNumber(c)) || (Utils.isAlpha(c))) { return elementType.eText; }
            else
                if (c == ' ') { return elementType.eSpace; }
            else
                    if (c == '#') { return elementType.eClass; }
            else
                        if (c == '.') { return elementType.eId; }
            else
                            if (c == ':') { return elementType.eStyle; }
            else
                                if (c == '-') { return elementType.eDash; }
            else return elementType.eSymbol;
        }
        #endregion
        #region Replacement routines
        /// <summary>
        /// Replace a char in text
        /// </summary>
        public static String Replace(string input, char fromChar, char toChar)
        {
            return input.Replace(fromChar, toChar);
        }
        /// <summary>
        /// Pop out string before the char looking for.
        /// </summary>
        public static String ExtractBefore(ref string input, char lookFor)
        {
            Int32 pos = input.IndexOf(lookFor);
            String retVal = "";
            if (pos == -1)
            { retVal = input; input = ""; }
            else
            {
                retVal = input.Substring(0, pos);
                input = input.Substring(pos + 1, input.Length - pos - 1);
            }

            return retVal;
        }
        /// <summary>
        /// Pop out string after the char looking for.
        /// </summary>
        public static String ExtractAfter(ref string input, char lookFor)
        {
            Int32 pos = input.IndexOf(lookFor);
            String retVal = "";
            if (pos == -1)
            {
                retVal = input;
                input = "";
            }
            else
            {
                retVal = input.Substring(pos + 1, input.Length - pos - 1);
                input = input.Substring(0, pos);
            }

            return retVal;
        }
        /// <summary>
        /// Pop out string between the startChar and endChar.
        /// </summary>        
        public static String ExtractBetween(string input, char startChar, char endChar)
        {
            string retVal = ExtractAfter(ref input, startChar);
            return ExtractBefore(ref retVal, endChar);
        }
        /// <summary>
        /// Pop out string after the char looking for(Seperator).
        /// </summary>
        public static String ExtractNextItem(ref string input, char seperator)
        {
            string retVal = ExtractBefore(ref input, seperator);
            if (retVal == "")
            {
                retVal = input;
                input = "";
            }
            input = input.Trim(seperator);
            return retVal;
        }
        /// <summary>
        /// Turn a string (e.g. Commatext) to a List.
        /// </summary>        
        public static ArrayList ExtractList(string input, char seperator)
        {
            ArrayList retVal = new ArrayList();
            string k = input;
            string newItem = "";
            while (k != "")
            {
                newItem = ExtractNextItem(ref k, seperator);
                if (newItem.IndexOf("'") != -1) { newItem = ExtractBetween(newItem, "'"[0], "'"[0]); }
                if (newItem.IndexOf('"') != -1) { newItem = ExtractBetween(newItem, '"', '"'); }
                if (newItem != "") { retVal.Add(newItem); }
            }
            return retVal;
        }
        /// <summary>
        /// Remove slash (\) in front of a strinig.
        /// </summary>        
        public static string RemoveFrontSlash(string input)
        {
            if ((input.Length > 0) && ((input[0] == '/') || (input[0] == '\\')))
            { return input.Substring(1); }
            else
            { return input; }
        }
        /// <summary>
        /// First letter to uppercase, the rest lowercase
        /// </summary>
        public static string Capitalize(string input)
        {
            string retVal = input;
            if (retVal.Length > 1)
            { return Char.ToUpper(retVal[0]) + retVal.Substring(1); }
            else
                if (retVal.Length == 1) { return retVal.ToUpper(); }
            else
            { return ""; }
        }
        /// <summary>
        /// Create hash string from input
        /// </summary>        
        public static string SimpleHash(string input)
        {
            Int32 a = (Int32)('a');
            string retVal = "";
            foreach (char c in input)
                retVal += (char)(((Int32)(c) % 25) + a);
            return retVal;
        }
        /// <summary>
        /// Add a slash "\" to end of input if not exists
        /// </summary>        
        public static string AppendSlash(string input)
        {
            if (input.EndsWith(@"\")) { return input; }
            else
            { return input + @"\"; }
        }
        /// <summary>
        /// Remove slash end of input
        /// </summary>        
        public static string RemoveSlash(string input)
        {
            if (input.EndsWith(@"\")) { return input.Substring(0, input.Length - 1); }
            else
            { return input; }
        }
        /// <summary>
        /// Transfer text case based on transformType
        /// </summary>
        public static string TransformText(string input, textTransformType transformType)
        {
            switch (transformType)
            {
                case textTransformType.Capitalize:
                    return input.Substring(0, 1).ToUpper() + input.Substring(1);
                case textTransformType.Lowercase:
                    return input.ToLower();
                case textTransformType.None:
                    return input;
                case textTransformType.Uppercase:
                    return input.ToUpper();
            }
            return input;
        }

        static char quote = '\'';
        private static void locateNextVariable(ref string working, ref string varName, ref string varValue)
        {
            working = working.Trim();

            Int32 pos1 = working.IndexOf('=');
            if (pos1 != -1)
            {
                varName = working.Substring(0, pos1);
                Int32 j = working.IndexOf(quote);
                Int32 f1 = working.IndexOf(' ');
                Int32 f2 = working.IndexOf('=');
                if (f1 == -1) { f1 = f2 + 1; }

                if ((j == -1) || (j > f1))
                {
                    varValue = working.Substring(f2 + 1, working.Length - f2 - 1);
                    f1 = working.IndexOf(' ');
                    if (f1 == -1)
                    {
                        working = "";
                    }
                    else
                    {
                        working = working.Substring(f1 + 1, working.Length - f1 - 1);
                    }

                }
                else
                {
                    working = working.Substring(j + 1, working.Length - j - 1);
                    j = working.IndexOf(quote);
                    if (j != -1)
                    {
                        varValue = working.Substring(0, j);
                        working = working.Substring(j + 1, working.Length - j - 1);
                    }
                }
            }
            else
            {
                varName = working;
                varValue = "TRUE";
                working = "";
            }

        }
        /// <summary>
        /// Extract html tag variables (e.g. href="xyz" name="abc")
        /// </summary>
        public static PropertyList ExtravtVariables(string input)
        {
            PropertyList retVal = new PropertyList();
            string working = input;
            string varName = "", varValue = "";
            while (working != "")
            {
                locateNextVariable(ref working, ref varName, ref varValue);
                retVal.Add(varName, varValue);
            }
            return retVal;
        }
        /// <summary>
        /// Extract CSS tag id (e.g. hl em {})
        /// </summary>       
        public static String ExtractNextElement(ref string input, ref elementType element)
        {
            input = input.Trim();
            if (input == "") { return ""; }
            string retVal = "";

            elementType e = charType(input[0]);
            element = e;
            char nextChar = input[0];


            while (input != "")
            {
                retVal += nextChar;
                input = input.Substring(1);
                string temp = input.Trim();
                if ((temp != "") && (charType(temp[0]) == elementType.eSymbol))
                {
                    input = temp.Substring(1);
                    return retVal + temp[0];
                }
                switch (e)
                {
                    case elementType.eSymbol:
                        return retVal;
                    case elementType.eDash:
                        retVal += ExtractNextElement(ref input, ref element);
                        break;
                    case elementType.eStyle:
                        retVal += ExtractNextElement(ref input, ref element);
                        break;
                    case elementType.eClass:
                        retVal += ExtractNextElement(ref input, ref element);
                        break;
                    case elementType.eId:
                        retVal += ExtractNextElement(ref input, ref element);
                        break;
                }

                if (input == "") { return retVal; }
                else
                {
                    e = charType(input[0]);
                    nextChar = input[0];
                    if (e == elementType.eSpace) { return retVal; }
                }

            }
            return retVal;

        }
        #endregion
        #region Symbol related routines
        /// <summary>
        /// Return a html that list all symbol.
        /// </summary>
        public static String SymbolHtml()
        {
            String retValue = "";
            foreach (SymbolInfo si in Defines.BuiltinSymbols)
            {
                retValue += '&' + si.symbol + ';';
            }
            return retValue;
        }
        /// <summary>
        /// Decode a html symbol 
        /// </summary>        
        public static Int32 LocateSymbol(string input)
        {
            string k = ExtractBetween(input, '&', ';');
            if (k.Length > 1)
            {
                foreach (SymbolInfo symb in Defines.BuiltinSymbols)
                {
                    Int32 symbolNumber = -1;

                    try
                    { symbolNumber = Convert.ToInt32(k); }
                    catch
                    {
                        symbolNumber = -1;
                    }

                    if ((symb.symbol.Equals(k)) || (symb.code == symbolNumber))
                    {
                        return symb.code;
                    }
                }
            }
            return -1;
        }
        /// <summary>
        /// Decode all html symbol in text (e.g. &amp;) to actual text.
        /// </summary>
        public static string DecodeSymbol(string input)
        {
            Int32 Idx1 = input.IndexOf('&');
            Int32 Idx2 = 0;
            string retVal = input;
            if (input != "") { Idx2 = input.IndexOf(';', Idx1 + 1); }
            if ((Idx1 != -1) && (Idx2 > Idx1))
            {
                string text = ExtractBefore(ref input, '&');
                string symb = ExtractBefore(ref input, ';');

                if ((symb.ToLower() == "amp") || (symb == "0038"))
                { text = text + "&"; }
                else
                {
                    Int32 symbIndex = LocateSymbol('&' + symb + ';');
                    if (symbIndex != -1)
                    { text = text + (char)(symbIndex); }
                }
                retVal = text + input;
            }

            if (retVal == input) { return input; }
            else
            { return DecodeSymbol(retVal); }

        }

        #endregion        
        #region Record Type Conversion routines

        /// <summary>
        /// Convert Text Color(Blue) to System.Drawing.Color
        /// </summary>
        /// <param name="colorString"></param>
        /// <returns></returns>
        public static System.Drawing.Color String2Color(string colorString)
        {
            #region String2ColorList
            if (colorString.ToLower().Equals("aliceblue")) { return System.Drawing.Color.AliceBlue; }
            else
            if (colorString.ToLower().Equals("antiquewhite")) { return System.Drawing.Color.AntiqueWhite; }
            else
            if (colorString.ToLower().Equals("aqua")) { return System.Drawing.Color.Aqua; }
            else
            if (colorString.ToLower().Equals("aquamarine")) { return System.Drawing.Color.Aquamarine; }
            else
            if (colorString.ToLower().Equals("azure")) { return System.Drawing.Color.Azure; }
            else
            if (colorString.ToLower().Equals("beige")) { return System.Drawing.Color.Beige; }
            else
            if (colorString.ToLower().Equals("bisque")) { return System.Drawing.Color.Bisque; }
            else
            if (colorString.ToLower().Equals("black")) { return System.Drawing.Color.Black; }
            else
            if (colorString.ToLower().Equals("blanchedalmond")) { return System.Drawing.Color.BlanchedAlmond; }
            else
            if (colorString.ToLower().Equals("blue")) { return System.Drawing.Color.Blue; }
            else
            if (colorString.ToLower().Equals("blueviolet")) { return System.Drawing.Color.BlueViolet; }
            else
            if (colorString.ToLower().Equals("brown")) { return System.Drawing.Color.Brown; }
            else
            if (colorString.ToLower().Equals("burlywood")) { return System.Drawing.Color.BurlyWood; }
            else
            if (colorString.ToLower().Equals("cadetblue")) { return System.Drawing.Color.CadetBlue; }
            else
            if (colorString.ToLower().Equals("chartreuse")) { return System.Drawing.Color.Chartreuse; }
            else
            if (colorString.ToLower().Equals("chocolate")) { return System.Drawing.Color.Chocolate; }
            else
            if (colorString.ToLower().Equals("coral")) { return System.Drawing.Color.Coral; }
            else
            if (colorString.ToLower().Equals("cornflowerblue")) { return System.Drawing.Color.CornflowerBlue; }
            else
            if (colorString.ToLower().Equals("cornsilk")) { return System.Drawing.Color.Cornsilk; }
            else
            if (colorString.ToLower().Equals("crimson")) { return System.Drawing.Color.Crimson; }
            else
            if (colorString.ToLower().Equals("cyan")) { return System.Drawing.Color.Cyan; }
            else
            if (colorString.ToLower().Equals("darkblue")) { return System.Drawing.Color.DarkBlue; }
            else
            if (colorString.ToLower().Equals("darkcyan")) { return System.Drawing.Color.DarkCyan; }
            else
            if (colorString.ToLower().Equals("darkgoldenrod")) { return System.Drawing.Color.DarkGoldenrod; }
            else
            if (colorString.ToLower().Equals("darkgray")) { return System.Drawing.Color.DarkGray; }
            else
            if (colorString.ToLower().Equals("darkgreen")) { return System.Drawing.Color.DarkGreen; }
            else
            if (colorString.ToLower().Equals("darkkhaki")) { return System.Drawing.Color.DarkKhaki; }
            else
            if (colorString.ToLower().Equals("darkmagenta")) { return System.Drawing.Color.DarkMagenta; }
            else
            if (colorString.ToLower().Equals("darkolivegreen")) { return System.Drawing.Color.DarkOliveGreen; }
            else
            if (colorString.ToLower().Equals("darkorange")) { return System.Drawing.Color.DarkOrange; }
            else
            if (colorString.ToLower().Equals("darkorchid")) { return System.Drawing.Color.DarkOrchid; }
            else
            if (colorString.ToLower().Equals("darkred")) { return System.Drawing.Color.DarkRed; }
            else
            if (colorString.ToLower().Equals("darksalmon")) { return System.Drawing.Color.DarkSalmon; }
            else
            if (colorString.ToLower().Equals("darkseagreen")) { return System.Drawing.Color.DarkSeaGreen; }
            else
            if (colorString.ToLower().Equals("darkslateblue")) { return System.Drawing.Color.DarkSlateBlue; }
            else
            if (colorString.ToLower().Equals("darkslategray")) { return System.Drawing.Color.DarkSlateGray; }
            else
            if (colorString.ToLower().Equals("darkturquoise")) { return System.Drawing.Color.DarkTurquoise; }
            else
            if (colorString.ToLower().Equals("darkviolet")) { return System.Drawing.Color.DarkViolet; }
            else
            if (colorString.ToLower().Equals("deeppink")) { return System.Drawing.Color.DeepPink; }
            else
            if (colorString.ToLower().Equals("deepskyblue")) { return System.Drawing.Color.DeepSkyBlue; }
            else
            if (colorString.ToLower().Equals("dimgray")) { return System.Drawing.Color.DimGray; }
            else
            if (colorString.ToLower().Equals("dodgerblue")) { return System.Drawing.Color.DodgerBlue; }
            else
            if (colorString.ToLower().Equals("firebrick")) { return System.Drawing.Color.Firebrick; }
            else
            if (colorString.ToLower().Equals("floralwhite")) { return System.Drawing.Color.FloralWhite; }
            else
            if (colorString.ToLower().Equals("forestgreen")) { return System.Drawing.Color.ForestGreen; }
            else
            if (colorString.ToLower().Equals("fuchsia")) { return System.Drawing.Color.Fuchsia; }
            else
            if (colorString.ToLower().Equals("gainsboro")) { return System.Drawing.Color.Gainsboro; }
            else
            if (colorString.ToLower().Equals("ghostwhite")) { return System.Drawing.Color.GhostWhite; }
            else
            if (colorString.ToLower().Equals("gold")) { return System.Drawing.Color.Gold; }
            else
            if (colorString.ToLower().Equals("goldenrod")) { return System.Drawing.Color.Goldenrod; }
            else
            if (colorString.ToLower().Equals("gray")) { return System.Drawing.Color.Gray; }
            else
            if (colorString.ToLower().Equals("green")) { return System.Drawing.Color.Green; }
            else
            if (colorString.ToLower().Equals("greenyellow")) { return System.Drawing.Color.GreenYellow; }
            else
            if (colorString.ToLower().Equals("honeydew")) { return System.Drawing.Color.Honeydew; }
            else
            if (colorString.ToLower().Equals("hotpink")) { return System.Drawing.Color.HotPink; }
            else
            if (colorString.ToLower().Equals("indianred")) { return System.Drawing.Color.IndianRed; }
            else
            if (colorString.ToLower().Equals("indigo")) { return System.Drawing.Color.Indigo; }
            else
            if (colorString.ToLower().Equals("ivory")) { return System.Drawing.Color.Ivory; }
            else
            if (colorString.ToLower().Equals("khaki")) { return System.Drawing.Color.Khaki; }
            else
            if (colorString.ToLower().Equals("lavender")) { return System.Drawing.Color.Lavender; }
            else
            if (colorString.ToLower().Equals("lavenderblush")) { return System.Drawing.Color.LavenderBlush; }
            else
            if (colorString.ToLower().Equals("lawngreen")) { return System.Drawing.Color.LawnGreen; }
            else
            if (colorString.ToLower().Equals("lemonchiffon")) { return System.Drawing.Color.LemonChiffon; }
            else
            if (colorString.ToLower().Equals("lightblue")) { return System.Drawing.Color.LightBlue; }
            else
            if (colorString.ToLower().Equals("lightcoral")) { return System.Drawing.Color.LightCoral; }
            else
            if (colorString.ToLower().Equals("lightcyan")) { return System.Drawing.Color.LightCyan; }
            else
            if (colorString.ToLower().Equals("lightgoldenrodyellow")) { return System.Drawing.Color.LightGoldenrodYellow; }
            else
            if (colorString.ToLower().Equals("lightgray")) { return System.Drawing.Color.LightGray; }
            else
            if (colorString.ToLower().Equals("lightgreen")) { return System.Drawing.Color.LightGreen; }
            else
            if (colorString.ToLower().Equals("lightpink")) { return System.Drawing.Color.LightPink; }
            else
            if (colorString.ToLower().Equals("lightsalmon")) { return System.Drawing.Color.LightSalmon; }
            else
            if (colorString.ToLower().Equals("lightseagreen")) { return System.Drawing.Color.LightSeaGreen; }
            else
            if (colorString.ToLower().Equals("lightskyblue")) { return System.Drawing.Color.LightSkyBlue; }
            else
            if (colorString.ToLower().Equals("lightslategray")) { return System.Drawing.Color.LightSlateGray; }
            else
            if (colorString.ToLower().Equals("lightsteelblue")) { return System.Drawing.Color.LightSteelBlue; }
            else
            if (colorString.ToLower().Equals("lightyellow")) { return System.Drawing.Color.LightYellow; }
            else
            if (colorString.ToLower().Equals("lime")) { return System.Drawing.Color.Lime; }
            else
            if (colorString.ToLower().Equals("limegreen")) { return System.Drawing.Color.LimeGreen; }
            else
            if (colorString.ToLower().Equals("linen")) { return System.Drawing.Color.Linen; }
            else
            if (colorString.ToLower().Equals("magenta")) { return System.Drawing.Color.Magenta; }
            else
            if (colorString.ToLower().Equals("maroon")) { return System.Drawing.Color.Maroon; }
            else
            if (colorString.ToLower().Equals("mediumaquamarine")) { return System.Drawing.Color.MediumAquamarine; }
            else
            if (colorString.ToLower().Equals("mediumblue")) { return System.Drawing.Color.MediumBlue; }
            else
            if (colorString.ToLower().Equals("mediumorchid")) { return System.Drawing.Color.MediumOrchid; }
            else
            if (colorString.ToLower().Equals("mediumpurple")) { return System.Drawing.Color.MediumPurple; }
            else
            if (colorString.ToLower().Equals("mediumseagreen")) { return System.Drawing.Color.MediumSeaGreen; }
            else
            if (colorString.ToLower().Equals("mediumslateblue")) { return System.Drawing.Color.MediumSlateBlue; }
            else
            if (colorString.ToLower().Equals("mediumspringgreen")) { return System.Drawing.Color.MediumSpringGreen; }
            else
            if (colorString.ToLower().Equals("mediumturquoise")) { return System.Drawing.Color.MediumTurquoise; }
            else
            if (colorString.ToLower().Equals("mediumvioletred")) { return System.Drawing.Color.MediumVioletRed; }
            else
            if (colorString.ToLower().Equals("midnightblue")) { return System.Drawing.Color.MidnightBlue; }
            else
            if (colorString.ToLower().Equals("mintcream")) { return System.Drawing.Color.MintCream; }
            else
            if (colorString.ToLower().Equals("mistyrose")) { return System.Drawing.Color.MistyRose; }
            else
            if (colorString.ToLower().Equals("moccasin")) { return System.Drawing.Color.Moccasin; }
            else
            if (colorString.ToLower().Equals("navajowhite")) { return System.Drawing.Color.NavajoWhite; }
            else
            if (colorString.ToLower().Equals("navy")) { return System.Drawing.Color.Navy; }
            else
            if (colorString.ToLower().Equals("oldlace")) { return System.Drawing.Color.OldLace; }
            else
            if (colorString.ToLower().Equals("olive")) { return System.Drawing.Color.Olive; }
            else
            if (colorString.ToLower().Equals("olivedrab")) { return System.Drawing.Color.OliveDrab; }
            else
            if (colorString.ToLower().Equals("orange")) { return System.Drawing.Color.Orange; }
            else
            if (colorString.ToLower().Equals("orangered")) { return System.Drawing.Color.OrangeRed; }
            else
            if (colorString.ToLower().Equals("orchid")) { return System.Drawing.Color.Orchid; }
            else
            if (colorString.ToLower().Equals("palegoldenrod")) { return System.Drawing.Color.PaleGoldenrod; }
            else
            if (colorString.ToLower().Equals("palegreen")) { return System.Drawing.Color.PaleGreen; }
            else
            if (colorString.ToLower().Equals("paleturquoise")) { return System.Drawing.Color.PaleTurquoise; }
            else
            if (colorString.ToLower().Equals("palevioletred")) { return System.Drawing.Color.PaleVioletRed; }
            else
            if (colorString.ToLower().Equals("papayawhip")) { return System.Drawing.Color.PapayaWhip; }
            else
            if (colorString.ToLower().Equals("peachpuff")) { return System.Drawing.Color.PeachPuff; }
            else
            if (colorString.ToLower().Equals("peru")) { return System.Drawing.Color.Peru; }
            else
            if (colorString.ToLower().Equals("pink")) { return System.Drawing.Color.Pink; }
            else
            if (colorString.ToLower().Equals("plum")) { return System.Drawing.Color.Plum; }
            else
            if (colorString.ToLower().Equals("powderblue")) { return System.Drawing.Color.PowderBlue; }
            else
            if (colorString.ToLower().Equals("purple")) { return System.Drawing.Color.Purple; }
            else
            if (colorString.ToLower().Equals("red")) { return System.Drawing.Color.Red; }
            else
            if (colorString.ToLower().Equals("rosybrown")) { return System.Drawing.Color.RosyBrown; }
            else
            if (colorString.ToLower().Equals("royalblue")) { return System.Drawing.Color.RoyalBlue; }
            else
            if (colorString.ToLower().Equals("saddlebrown")) { return System.Drawing.Color.SaddleBrown; }
            else
            if (colorString.ToLower().Equals("salmon")) { return System.Drawing.Color.Salmon; }
            else
            if (colorString.ToLower().Equals("sandybrown")) { return System.Drawing.Color.SandyBrown; }
            else
            if (colorString.ToLower().Equals("seagreen")) { return System.Drawing.Color.SeaGreen; }
            else
            if (colorString.ToLower().Equals("seashell")) { return System.Drawing.Color.SeaShell; }
            else
            if (colorString.ToLower().Equals("sienna")) { return System.Drawing.Color.Sienna; }
            else
            if (colorString.ToLower().Equals("silver")) { return System.Drawing.Color.Silver; }
            else
            if (colorString.ToLower().Equals("skyblue")) { return System.Drawing.Color.SkyBlue; }
            else
            if (colorString.ToLower().Equals("slateblue")) { return System.Drawing.Color.SlateBlue; }
            else
            if (colorString.ToLower().Equals("slategray")) { return System.Drawing.Color.SlateGray; }
            else
            if (colorString.ToLower().Equals("snow")) { return System.Drawing.Color.Snow; }
            else
            if (colorString.ToLower().Equals("springgreen")) { return System.Drawing.Color.SpringGreen; }
            else
            if (colorString.ToLower().Equals("steelblue")) { return System.Drawing.Color.SteelBlue; }
            else
            if (colorString.ToLower().Equals("tan")) { return System.Drawing.Color.Tan; }
            else
            if (colorString.ToLower().Equals("teal")) { return System.Drawing.Color.Teal; }
            else
            if (colorString.ToLower().Equals("thistle")) { return System.Drawing.Color.Thistle; }
            else
            if (colorString.ToLower().Equals("tomato")) { return System.Drawing.Color.Tomato; }
            else
            if (colorString.ToLower().Equals("transparent")) { return System.Drawing.Color.Transparent; }
            else
            if (colorString.ToLower().Equals("turquoise")) { return System.Drawing.Color.Turquoise; }
            else
            if (colorString.ToLower().Equals("violet")) { return System.Drawing.Color.Violet; }
            else
            if (colorString.ToLower().Equals("wheat")) { return System.Drawing.Color.Wheat; }
            else
            if (colorString.ToLower().Equals("white")) { return System.Drawing.Color.White; }
            else
            if (colorString.ToLower().Equals("whitesmoke")) { return System.Drawing.Color.WhiteSmoke; }
            else
            if (colorString.ToLower().Equals("yellow")) { return System.Drawing.Color.Yellow; }
            else
            if (colorString.ToLower().Equals("yellowgreen")) { return System.Drawing.Color.YellowGreen; }
            else
                return System.Drawing.Color.Black;
            #endregion
        }
        /// <summary>
        /// Convert Html System.Drawing.Color(#FFFFFF) or Text System.Drawing.Color(Blue) to System.Drawing.System.Drawing.Color
        /// </summary>
        /// <param name="colorString"></param>
        /// <returns></returns>
        public static System.Drawing.Color WebColor2Color(string colorString)
        {
            try
            {
                if ((colorString == "") ||
                    (colorString.IndexOf("<") > -1) ||
                    (colorString.IndexOf(">") > -1))
                    return System.Drawing.Color.Black;
                if (colorString[0] == '#')
                {
                    Int32 red = Convert.ToInt32(colorString.Substring(1, 2), 16);
                    Int32 green = Convert.ToInt32(colorString.Substring(3, 2), 16);
                    Int32 blue = Convert.ToInt32(colorString.Substring(5, 2), 16);
                    return System.Drawing.Color.FromArgb(red, green, blue);
                }
                return String2Color(colorString);
            }
            catch
            {
                return System.Drawing.Color.Transparent;
            }

        }
        /// <summary>
        /// Convert Html Align (left, right, centre) to AlignType
        /// </summary>
        /// <param name="alignString"></param>
        /// <returns></returns>
        public static hAlignType StrAlign2Align(string alignString)
        {
            if (alignString.ToLower().Equals("left")) { return hAlignType.Left; }
            else
                if (alignString.ToLower().Equals("right")) { return hAlignType.Right; }
            else
                    if (alignString.ToLower().Equals("centre")) { return hAlignType.Centre; }
            else
                return hAlignType.Left;

        }
        /// <summary>
        /// Multipurpose pixel converter to integer, support (%, em, px),
        /// not support (points,cm,mm,picas) yet.
        /// </summary>
        /// <param name="sizeString"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static Int32 StrSize2PixelSize(string sizeString, Int32 def)
        {
            bool plusSign = false;
            bool negativeSign = false;
            string sizeStr = sizeString.Trim();
            if (sizeStr.Length == 0) { return 0; }
            if ((sizeStr[0] == '+') || (sizeStr[0] == '-'))
            {
                if (sizeStr[0] == '+') { plusSign = true; } else { negativeSign = true; }
                sizeStr = sizeStr.Substring(1);
            }
            Int32 intPos = 0;
            while ((sizeStr.Length - 1 > intPos) && ((Utils.isNumber(sizeStr[intPos + 1])) ||
                    (sizeStr[intPos + 1] == '.'))) { intPos++; }

            Single number = Convert.ToSingle(sizeStr.Substring(0, intPos + 1));
            string symbol = sizeStr.Substring(intPos + 1, sizeStr.Length - intPos - 1);
            if (Utils.isNumber(sizeStr[intPos]))
            {
                if (symbol.ToLower().Equals("%")) { number = def * number / 100; }
                else
                    if (symbol.ToLower().Equals("em")) { number = def * number / 100; }
                else
                        if (symbol.ToLower().Equals("px")) { }
                else
                            //Below not supported
                            if (symbol.ToLower().Equals("points")) { number = def; }
                else
                                if (symbol.ToLower().Equals("cm")) { number = def; }
                else
                                    if (symbol.ToLower().Equals("mm")) { number = def; }
                else
                                        if (symbol.ToLower().Equals("picas")) { number = def; }
            }
            if (plusSign) { return Convert.ToInt32(def + number); }
            else
                if (negativeSign) { return Convert.ToInt32(def - number); }
            else
                return Convert.ToInt32(number);

        }
        /// <summary>
        /// Convert Css position type (relative,absolute,fixed,inherited to record type.
        /// </summary>
        /// <param name="positionString"></param>
        /// <returns></returns>
        public static positionStyleType StrPosition2PositionType(string positionString)
        {
            if (positionString.ToLower().Equals("fixed")) { return positionStyleType.Fixed; }
            else
                if (positionString.ToLower().Equals("inherited")) { return positionStyleType.Inherited; }
            else
                    if (positionString.ToLower().Equals("relative")) { return positionStyleType.Relative; }
            else
                        if (positionString.ToLower().Equals("static")) { return positionStyleType.Static; }
            else
                return positionStyleType.Absolute;
        }
        /// <summary>
        /// Convert Css Border type(dotted,dashed,solid,double,groove,ridge,
        /// </summary>
        /// <param name="borderString"></param>
        /// <returns></returns>
        public static borderStyleType StrBorder2BorderType(string borderString)
        {
            if (borderString.ToLower().Equals("fixed")) { return borderStyleType.Dashed; }
            else
                if (borderString.ToLower().Equals("dotted")) { return borderStyleType.Dotted; }
            else
                    if (borderString.ToLower().Equals("double")) { return borderStyleType.Double; }
            else
                        if (borderString.ToLower().Equals("groove")) { return borderStyleType.Groove; }
            else
                            if (borderString.ToLower().Equals("inherit")) { return borderStyleType.Inherit; }
            else
                                if (borderString.ToLower().Equals("inset")) { return borderStyleType.Inset; }
            else
                                    if (borderString.ToLower().Equals("none")) { return borderStyleType.None; }
            else
                                        if (borderString.ToLower().Equals("outset")) { return borderStyleType.Outset; }
            else
                                            if (borderString.ToLower().Equals("ridge")) { return borderStyleType.Ridge; }
            else
                                                if (borderString.ToLower().Equals("solid")) { return borderStyleType.Solid; }
            else
                return borderStyleType.None;
        }
        /// <summary>
        /// Convert Css Bullet type(circle,square,decimal,upper-alpha,lower-alpha,
        ///     upper-roman,lower-roman) to record type.
        /// </summary>
        /// <param name="bulletString"></param>
        /// <returns></returns>
        public static bulletStyleType StrBullet2BulletType(string bulletString)
        {
            bulletString = bulletString.Replace("-", "");
            if (bulletString.ToLower().Equals("circle")) { return bulletStyleType.Circle; }
            else
                if (bulletString.ToLower().Equals("decimal")) { return bulletStyleType.Decimal; }
            else
                    if (bulletString.ToLower().Equals("loweralpha")) { return bulletStyleType.LowerAlpha; }
            else
                        if (bulletString.ToLower().Equals("lowerroman")) { return bulletStyleType.LowerRoman; }
            else
                            if (bulletString.ToLower().Equals("none")) { return bulletStyleType.None; }
            else
                                if (bulletString.ToLower().Equals("square")) { return bulletStyleType.Square; }
            else
                                    if (bulletString.ToLower().Equals("upperalpha")) { return bulletStyleType.UpperAlpha; }
            else
                                        if (bulletString.ToLower().Equals("upperroman")) { return bulletStyleType.UpperRoman; }
            else
                return bulletStyleType.Circle;
        }
        /// <summary>
        /// Convert Css Cursor type (default,pointer,crosshair,move,wait,help,text) to
        ///     record type.
        /// </summary>
        /// <param name="cursorString"></param>
        /// <returns></returns>
        public static Cursor StrCursor2CursorType(string cursorString)
        {
            if (cursorString.ToLower().Equals("default")) { return Cursors.Default; }
            else
                if (cursorString.ToLower().Equals("pointer")) { return Cursors.Hand; }
            else
                    if (cursorString.ToLower().Equals("crosshair")) { return Cursors.Cross; }
            else
                        if (cursorString.ToLower().Equals("move")) { return Cursors.SizeNWSE; }
            else
                            if (cursorString.ToLower().Equals("wait")) { return Cursors.WaitCursor; }
            else
                                if (cursorString.ToLower().Equals("help")) { return Cursors.Help; }
            else
                                    if (cursorString.ToLower().Equals("test")) { return Cursors.IBeam; }
            else
                return Cursors.Default;
        }
        /// <summary>
        /// Convert Form Method type to record type.
        /// </summary>
        /// <param name="methodString"></param>
        /// <returns></returns>
        public static formMethodType StrMethod2FormMethodType(string methodString)
        {
            if (methodString.ToLower().Equals("get")) { return formMethodType.Get; }
            else
                if (methodString.ToLower().Equals("post")) { return formMethodType.Post; }
            else
                return formMethodType.Default;
        }
        /// <summary>
        /// Convert Variable Type to record type (for search)
        /// </summary>
        /// <param name="typeString"></param>
        /// <returns></returns>
        public static variableType StrType2VariableType(string typeString)
        {
            if (typeString.ToLower().Equals("alpha")) { return variableType.Alpha; }
            else
                if (typeString.ToLower().Equals("formated")) { return variableType.Formated; }
            else
                    if (typeString.ToLower().Equals("number")) { return variableType.Number; }
            else
                        if (typeString.ToLower().Equals("paragraph")) { return variableType.Paragraph; }
            else
                return variableType.String;

        }
        public static textTransformType StrTransform2TextTransformType(string transformString)
        {
            if (transformString.ToLower().Equals("lowercase")) { return textTransformType.Lowercase; }
            else
                if (transformString.ToLower().Equals("uppercase")) { return textTransformType.Uppercase; }
            else
                    if (transformString.ToLower().Equals("capitalize")) { return textTransformType.Capitalize; }
            else
                        if (transformString.ToLower().Equals("none")) { return textTransformType.None; }
            else
                return textTransformType.None;
        }
        public static textTransformType StrFontVariant2TextTransformType(string variantString)
        {
            if (variantString.ToLower().Equals("small-caps")) { return textTransformType.Lowercase; }
            else
                return textTransformType.None;
        }
        public static string Number2Romans(UInt32 value)
        {
            StringBuilder retVal = new StringBuilder();
            Int32 romIndex = 0;
            while (value > 0)
            {
                UInt32 romVal = Defines.BuiltinRomans[romIndex].value;
                if (value >= romVal)
                {
                    value -= romVal;
                    retVal.Append(Defines.BuiltinRomans[romIndex].rep);
                }
                else { romIndex += 1; }
            }

            return retVal.ToString();
        }

        public static string Number2BulletValue(UInt32 value, bulletStyleType styleType)
        {
            switch (styleType)
            {
                case (bulletStyleType.Decimal):
                    return value.ToString() + ".";
                case (bulletStyleType.LowerAlpha):
                    Char a = 'a';
                    return (Char)((Int32)(a) + value - 1) + ".";
                case (bulletStyleType.UpperAlpha):
                    Char A = 'A';
                    return (Char)((Int32)(A) + value - 1) + ".";
                case (bulletStyleType.LowerRoman):
                    return Number2Romans(value).ToLower() + ".";
                case (bulletStyleType.UpperRoman):
                    return Number2Romans(value) + ".";
            }
            return "";
        }
        #endregion
        #region Record Type Locating routines
        public static Int32 LocateTag(string tagName)
        {
            tagName = tagName.ToLower();
            for (int i = 0; i < Defines.BuiltinTags.Length; i++)
            {
                HTMLTagInfo tagInfo = Defines.BuiltinTags[i];
                if (tagInfo.Html == tagName)
                {
                    return i;
                }
            }
            return -1;
        }
        public static Int32 LocateBBCode(string tagName)
        {
            tagName = tagName.ToLower();
            for (int i = 0; i < Defines.BuiltinBBCodes.Length; i++)
            {
                HTMLTagInfo tagInfo = Defines.BuiltinBBCodes[i];
                if (tagInfo.Html == tagName)
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// Return hash code for specified styleName
        /// </summary>
        public static Int32 LocateStyle(string styleName)
        {
            styleName = styleName.ToLower();
            for (Int32 i = 0; i < Defines.BuiltinStyles.Length; i++)
                if (Defines.BuiltinStyles[i] == styleName)
                    return i;
            return -1;
        }
        #endregion
        // Drawing based routines //
        #region Font and Pen related.
        /// <summary>
        /// Create a new Font Object
        /// </summary>
        public static Font CreateFont(string aFontName, Int32 aFontSize, bool isBold, bool isItalic,
            bool isUnderline, bool isStrikeout, bool isURL)
        {
            System.Drawing.FontStyle fs = new System.Drawing.FontStyle();
            if (isBold) { fs |= System.Drawing.FontStyle.Bold; }
            if (isItalic) { fs |= System.Drawing.FontStyle.Italic; }
            if ((isUnderline) || (isURL)) { fs |= System.Drawing.FontStyle.Underline; }
            if (isStrikeout) { fs |= System.Drawing.FontStyle.Strikeout; }

            return new Font(aFontName, aFontSize, fs);
        }
        /// <summary>
        /// Create Pen object based on color and size.
        /// </summary>
        public static System.Drawing.Pen CreatePen(System.Drawing.Color aColor, Int32 aSize)
        {
#if CF
            return new Pen(aColor);
#else
            return new System.Drawing.Pen(aColor, aSize);
#endif
        }
        /// <summary>
        /// Create Pen object based on brush and size.
        /// </summary>
        public static System.Drawing.Pen CreatePen(System.Drawing.Brush aBrush, Int32 aSize)
        {
#if CF
            return new Pen(Color.Black);
#else
            return new System.Drawing.Pen(aBrush, aSize);
#endif
        }
        /// <summary>
        /// Check if Font exist in system.
        /// </summary>        
        public static bool FontExists(string fontName, Graphics g)
        {
#if CF
            return true;
#else
            if (fontName == "") { return false; }
            System.Drawing.FontFamily[] families = System.Drawing.FontFamily.GetFamilies(g);
            String fontname = fontName.ToLower();
            foreach (System.Drawing.FontFamily family in families)
            {
                if (family.GetName(0).ToLower().Equals(fontname))
                {
                    return true;
                }
            }
            return false;
#endif
        }
        /// <summary>
        /// Check if Font exist in the specified PrivateFontCollection. 
        /// *CF Not supported*
        /// </summary>
#if !CF
        public static bool UserFontExists(string fontName, PrivateFontCollection pfc)
        {
            if (fontName == "") { return false; }
            if (pfc == null) { return false; }

            String fontname = fontName.ToLower();
            foreach (System.Drawing.FontFamily family in pfc.Families)
            {
                if (family.GetName(0).ToLower().Equals(fontname))
                {
                    return true;
                }
            }
            return false;
        }
#endif

        /// <summary>
        /// Load font resource to PrivateFontCollection.
        /// *CF Not supported*
        /// </summary>
#if !CF
        public static bool LoadFont(Stream resourceStream, ref PrivateFontCollection pfc)
        {
            try
            {
                int len = (int)resourceStream.Length;
                IntPtr data = Marshal.AllocCoTaskMem(len);
                Byte[] fontData = new Byte[len];
                resourceStream.Read(fontData, 0, len);
                Marshal.Copy(fontData, 0, data, len);
                if (pfc == null) { pfc = new PrivateFontCollection(); }
                pfc.AddMemoryFont(data, len);
                Marshal.FreeCoTaskMem(data);
                return true;
            }
            catch
            {
                return false;
            }
        }
#endif
        /// <summary>
        /// Load font resource to PrivateFontCollection.
        /// </summary>
#if !CF
        public static bool LoadFont(string filename, ref PrivateFontCollection pfc)
        {
            Stream resourceStream = new FileStream(filename, FileMode.Open);
            return LoadFont(resourceStream, ref pfc);
        }
#endif
        /// <summary>
        /// Load font resource to PrivateFontCollection.
        /// Note that you have to embeded a font resource first (e.g. {$R Yourfont.tif})
        /// then Lowd the font (LoadFontFromResource('Yourfont.tif',yourform,miniHtml.userFontCollection)
        /// then you can use the font as usual.
        /// </summary>
#if !CF
        public static bool LoadFont(string resourceName, Form f, ref PrivateFontCollection pfc)
        {
            try
            {
                Stream fontStream = f.GetType().Assembly.GetManifestResourceStream(resourceName);
                int len = (int)fontStream.Length;
                IntPtr data = Marshal.AllocCoTaskMem(len);
                Byte[] fontData = new Byte[len];
                fontStream.Read(fontData, 0, len);
                Marshal.Copy(fontData, 0, data, len);
                if (pfc == null) { pfc = new PrivateFontCollection(); }
                pfc.AddMemoryFont(data, len);
                fontStream.Close();
                Marshal.FreeCoTaskMem(data);
                return true;
            }
            catch
            {
                return false;
            }
        }
#endif

        #endregion
        #region Measure routines
        /// <summary>
        /// Calculate widthLimit/height of a text (used by CF), 
        /// Less precision compared with TextSize.
        /// Better than none....
        /// </summary>
        public static SizeF TextSize2(Graphics g, string aText, Font aFont)
        {
            if (aText.Trim() == "") return new SizeF(0, 0);
            if (aText[aText.Length - 1] == ' ') { aText = aText.Substring(0, aText.Length - 1) + '/'; }
            SizeF defSize = g.MeasureString(aText, aFont);
            Int32 precision = (Int32)(defSize.Width / 50);
            if (precision == 0) { precision = 1; }

            Bitmap b = new Bitmap((int)defSize.Width + 1, (int)defSize.Height + 1);
            Graphics tempGraphics = Graphics.FromImage(b);
            try
            {
                tempGraphics.FillRectangle(new SolidBrush(System.Drawing.Color.White), new Rectangle(0, 0, (int)defSize.Width + 1, (int)defSize.Height + 1));
                tempGraphics.DrawString(aText, aFont, new SolidBrush(System.Drawing.Color.Black), new PointF(0, 0));
                for (int x = (int)defSize.Width - 1; x > 0; x = x - precision)
                {
                    for (int y = (int)defSize.Height - 1; y > 0; y--)
                    {
                        if ((b.GetPixel(x, y).R < 200))
                        {
                            return new SizeF((float)x, defSize.Height);
                        }
                    }
                }
            }
            catch
            {
                return defSize;
            }
            finally
            {
                tempGraphics.Dispose();
                b.Dispose();
            }
            return defSize;
        }
        /// <summary>
        /// Calculate widthLimit/height of a text (
        /// </summary>
        public static SizeF TextSize(Graphics g, string aText, Font aFont)
        {
            if (aText.Trim() == "") return new SizeF(0, 0);
            if (aText[aText.Length - 1] == ' ') { aText = aText.Substring(0, aText.Length - 1) + "/"; }
#if CF
            return TextSize2(g, aText, aFont);
#else
            try
            {
                if (aFont.Style == System.Drawing.FontStyle.Bold)
                { return TextSize2(g, aText, aFont); } //MeasureCharacterRanges return wrong value if bold.

                StringFormat aFormat = StringFormat.GenericTypographic;
                //aFormat.Trimming = System.Drawing.StringTrimming.Character;
                //aFormat.FormatFlags += 16384;
                //aFormat.FormatFlags = aFormat.FormatFlags & StringFormatFlags.NoClip; 

                CharacterRange[] cr = new CharacterRange[1];
                cr[0] = new CharacterRange(0, aText.Length);

                aFormat.SetMeasurableCharacterRanges(cr);

                RectangleF aRect = Screen.PrimaryScreen.Bounds;
                aRect = g.MeasureCharacterRanges(aText, aFont, aRect, aFormat)[0].GetBounds(g);
                return new SizeF(aRect.Right - aRect.Left, aRect.Height);
            }
            catch
            {
                return TextSize2(g, aText, aFont);
            }

#endif
        }
        /// <summary>
        /// Calculate widthLimit/height of a text (
        /// </summary>
        public static SizeF TextSize(string aText, Font aFont)
        {
            Bitmap b = new Bitmap(10, 10);
            Graphics g = Graphics.FromImage(b);
            try
            {
                return TextSize(g, aText, aFont);
            }
            finally
            {
                g.Dispose();
                b.Dispose();
            }
        }
        /// <summary>
        /// Calculate the text position based on x axis.
        /// </summary>
        public static Int32 TextPosition(Graphics g, string aText, Font aFont, float x)
        {
            float fullWidth = TextSize(g, aText, aFont).Width;
            Int32 precision = aText.Length / 5;
            Int32 start = (Int32)(Math.Round(x / fullWidth * aText.Length)) - precision + 1;
            if (start < 1) { start = 1; }

            for (int i = start; i <= aText.Length; i++)
            {
                float currentWidth = TextSize(g, aText.Substring(0, i), aFont).Width;
                if (currentWidth > x)
                {
                    if (i == 1) { return 0; }

                    float lastWidth = TextSize(g, aText.Substring(0, i - 1), aFont).Width;

                    if ((x - lastWidth) < (currentWidth - x))
                    {
                        return i - 1;
                    }
                    else
                    {
                        return i;
                    }
                }
            }
            return aText.Length;
        }
        /// <summary>
        /// Exact copy of TextPosition except force to use TextSize2, for debug.        
        /// </summary>
        public static Int32 TextPosition2(Graphics g, string aText, Font aFont, float x)
        {
            if (aText == "")
                return 0;

            float fullWidth = TextSize2(g, aText, aFont).Width;
            Int32 precision = aText.Length / 5;
            Int32 start = (Int32)(Math.Round(x / fullWidth * aText.Length)) - precision + 1;
            if (start < 1) { start = 1; }

            for (int i = start; i <= aText.Length; i++)
            {
                float currentWidth = TextSize(g, aText.Substring(0, i), aFont).Width;
                if (currentWidth > x)
                {
                    if (i == 1) { return 0; }

                    float lastWidth = TextSize2(g, aText.Substring(0, i - 1), aFont).Width;

                    if ((x - lastWidth) < (currentWidth - x))
                    {
                        return i - 1;
                    }
                    else
                    {
                        return i;
                    }
                }
            }
            return aText.Length;
        }
        #endregion
        #region Drawing routines
        /// <summary>
        /// Output a circle.
        /// </summary>        
        public static void DrawCircle(Graphics g, Int32 x, Int32 y, Int32 size)
        {
            System.Drawing.Point[] pts = new System.Drawing.Point[3];
            pts[0] = new System.Drawing.Point(x + (size / 2), y + (size / 2));
            pts[1] = new System.Drawing.Point(x + size, y);
            pts[2] = new System.Drawing.Point(x + size, y + size);
            g.FillEllipse(System.Drawing.Brushes.Lime, x, y, (float)size, (float)size);

        }
        /// <summary>
        /// Output a triangle.
        /// </summary>            
        public static void DrawTriangle(Graphics g, Int32 x, Int32 y, Int32 width, Int32 height, bool down)
        {
            SolidBrush sBrush, bBrush;
            if (down)
            {
                sBrush = new SolidBrush(System.Drawing.Color.Black);
                bBrush = new SolidBrush(System.Drawing.Color.White);
            }
            else
            {
                bBrush = new SolidBrush(System.Drawing.Color.Black);
                sBrush = new SolidBrush(System.Drawing.Color.White);
            }
            System.Drawing.Point[] pts = new System.Drawing.Point[3];
            pts[0] = new System.Drawing.Point(x + width - 11, y + Convert.ToInt32(((height + 4) / 2)) - 1);
            pts[1] = new System.Drawing.Point(x + width - 05, y + Convert.ToInt32(((height + 4) / 2)) - 1);
            pts[2] = new System.Drawing.Point(x + width - 08, y + Convert.ToInt32(((height + 4) / 2)) + 2);
            g.DrawPolygon(new System.Drawing.Pen(System.Drawing.Color.Black), pts);
            g.FillPolygon(sBrush, pts);
        }
        /// <summary>
        /// Graphics.DrawString add some space in front, use Utils.DrawString will offset this problem.
        /// </summary>
        public static void DrawString(Graphics g, String s, Font font, System.Drawing.Brush brush, PointF point)
        {
            float AlterX = font.Size / 4;
            if (font.Style == System.Drawing.FontStyle.Bold)
                if (point.X - 1 > 0)
                {
                    AlterX = 1;
                }
            g.DrawString(s, font, brush, new PointF(point.X - AlterX, point.Y));
        }
        /// <summary>
        /// Call Graphics.DrawImage
        /// </summary>
        public static void DrawImage(Graphics g, System.Drawing.Image img, PointF point)
        {
            g.DrawImage(img, point);
        }
        #endregion
        #region Conversion routines
        /// <summary>
        /// Return negative Image
        /// </summary>
        /// <url>http://www.bobpowell.net/negativeimage.htm</url>
        public static System.Drawing.Image NegativeImage(System.Drawing.Image img)
        {
            ImageAttributes ia = new ImageAttributes();
            ColorMatrix cm = new ColorMatrix();
            cm.Matrix00 = -1;
            cm.Matrix11 = -1;
            cm.Matrix22 = -1;
            //			cm.Matrix00=cm.Matrix11=cm.Matrix22=0.99f;
            //      		cm.Matrix33=cm.Matrix44=1;
            //      		cm.Matrix40=cm.Matrix41=cm.Matrix42=.04f;
            //      		cm.Matrix33=cm.Matrix44=1;
            //      		cm.Matrix40=cm.Matrix41=cm.Matrix42=.04f;
            ia.SetColorMatrix(cm);

            Bitmap output = new Bitmap(img.Width, img.Height);
            Graphics g = Graphics.FromImage(output);
            g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
            g.Dispose();

            return output;
        }
        /// <summary>
        /// Resize a Image and return it
        /// </summary>
        /// <urlhttp://www.bobpowell.net/highqualitythumb.htm</url>
        public static System.Drawing.Image ResizeImage(System.Drawing.Image input, float percentage)
        {
            if (percentage < 1)
                throw new Exception("Thumbnail size must be at least 1% of the original size");

            Bitmap tn = new Bitmap((Int32)(input.Width * 0.01f * percentage), (Int32)(input.Height * 0.01f * percentage));
            Graphics g = Graphics.FromImage(tn);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImage(input, new Rectangle(0, 0, tn.Width, tn.Height), 0, 0, input.Width, input.Height, GraphicsUnit.Pixel);
            g.Dispose();
            return (System.Drawing.Image)tn;
        }
        /// <summary>
        /// Resize a Image and return it
        /// </summary>
        public static System.Drawing.Image ResizeImage(System.Drawing.Image input, Int32 maxWidth, Int32 maxHeight)
        {
            float percentageW = (float)maxWidth / (float)input.Width * 100;
            float percentageH = (float)maxHeight / (float)input.Height * 100;
            if (percentageW > percentageH)
                return ResizeImage(input, percentageH);
            else
                return ResizeImage(input, percentageW);
        }
        /// <summary>
        /// Resize a Image and return it
        /// </summary>
        public static System.Drawing.Image ResizeImageW(System.Drawing.Image input, Int32 width)
        {
            float percentage = (float)width / (float)input.Width * 100;
            return ResizeImage(input, percentage);
        }
        /// <summary>
        /// Resize a Image and return it
        /// </summary>
        public static System.Drawing.Image ResizeImageH(System.Drawing.Image input, Int32 height)
        {
            float percentage = (float)height / (float)input.Height * 100;
            return ResizeImage(input, percentage);
        }

        #endregion
        // System utils routines //
        #region System routines
        /// <summary>
        /// Run an external application, Run2 make use of external dll (for use in CE)
        /// </summary>
        public static bool Run2(string prog, string param, ref ProcessInfo pi)
        {
            //Int32 Infinite = Int32.MaxValue;
            //Int32 WAIT_OBJECT_0 = 0;

            Byte[] si = new Byte[128];
            if (pi == null) { pi = new ProcessInfo(); }

            Int32 returnCode = Header.CreateProcess(prog, param, IntPtr.Zero,
                IntPtr.Zero, 0, 0, IntPtr.Zero, IntPtr.Zero, si, pi);

            return (returnCode != 0);
        }
        /// <summary>
        /// Run an external application.
        /// </summary>
        public static void Run(string prog, string param)
        {
#if CF
            ProcessInfo pi = new ProcessInfo();
            Run2(prog, param, ref pi); 
#else
            ProcessStartInfo p = new ProcessStartInfo(prog, param);
            Process.Start(p);
#endif
        }
        /// <summary>
        /// Write a file from a stream 
        /// Can be used to copy files.
        /// </summary>
        public static void Stream2File(Stream inputStream, string outputFilename)
        {
            if (inputStream == null) { return; }
            FileStream fs = new FileStream(outputFilename, FileMode.Create, FileAccess.Write, FileShare.Read);
            try
            {
                Byte[] buffer = new Byte[1024 * 1024];
                Int32 readCnt = inputStream.Read(buffer, 0, buffer.Length);
                while (readCnt > 0)
                {
                    fs.Write(buffer, 0, readCnt);
                    readCnt = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
            finally
            {
                fs.Close();
            }
        }
        /// <summary>
        /// Get a list of filename
        /// </summary>        
        public static ArrayList GetFileList(string path, string mask)
        {
            ArrayList retVal = new ArrayList();
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            FileInfo[] fInfoList = dirInfo.GetFiles(mask);
            foreach (FileInfo fi in fInfoList)
            {
                retVal.Add(AppendSlash(path) + fi.Name);
            }
            return retVal;
        }
        #endregion
        #region Filename related routines
        /// <summary>
        /// Return filename from a path
        /// </summary>
        public static string ExtractFileName(string input)
        {
            string fn = RemoveSlash(input);
            Int32 idx = fn.LastIndexOf('\\');
            if (idx == -1)
            { return fn; }
            else
            { return fn.Substring(idx + 1); }
        }
        /// <summary>
        ///  Return file path from a path (with filename)
        /// </summary>
        public static string ExtractFilePath(string input)
        {
            string retVal = RemoveSlash(input);
            Int32 ind = retVal.LastIndexOf('\\');
            if (ind > 0) { retVal = input.Substring(0, ind); }
            return retVal;
        }
        /// <summary>
        /// Return file extension of a path
        /// </summary>
        public static string ExtractFileExt(string input)
        {
            return Path.GetExtension(input);
        }

        #endregion
        // Css related routines //
        #region DecodeCssStyle() routines
        private static string getHeaderSection(string input)
        {
            return (Utils.ExtractBefore(ref input, '{')).Trim();
        }
        private static string getDeclareSection(string input)
        {
            return (Utils.ExtractBetween(input, '{', '}')).Trim();
        }
        private static bool readTagName(string input, ref ArrayList cssStyleGroups)
        {
            string k = Utils.getHeaderSection(input);

            ArrayList cssTagNameList = new ArrayList();
            string newTagName = Utils.ExtractBefore(ref k, ',');
            while (newTagName != "")
            {
                cssTagNameList.Add(newTagName);
                newTagName = Utils.ExtractBefore(ref k, ',');
            }
            if (k.Trim() != "") cssTagNameList.Add(k);

            cssStyleGroups = new ArrayList();

            for (Int32 i = 0; i < cssTagNameList.Count; i++)
            {
                CssStyleGroupType newGroup = new CssStyleGroupType();
                k = (string)cssTagNameList[i];

                elementType e = elementType.eClass;
                while (k.Trim() != "")
                    newGroup.parentTagName.Add(Utils.ExtractNextElement(ref k, ref e));

                if (newGroup.parentTagName.Count > 0)
                {
                    newGroup.styleTagName = (string)newGroup.parentTagName[newGroup.parentTagName.Count - 1];
                    newGroup.parentTagName.RemoveAt(newGroup.parentTagName.Count - 1);
                }

                cssStyleGroups.Add(newGroup);
            }

            return true;
        }

        private static bool readTagClass(string input, out string tagName, out string tagClass)
        {
            string k = Utils.getHeaderSection(input);

            tagName = Utils.ExtractBefore(ref k, '.');
            tagClass = k;

            return (tagName != "") && (tagClass != "");

        }

        private static bool readTagID(string input, out string tagName, out string tagID)
        {
            string k = Utils.getHeaderSection(input);

            tagName = Utils.ExtractBefore(ref k, '#');
            tagID = k;

            return (tagName != "") && (tagID != "");
        }

        private static bool readOneDeclare(string input, out string key, out string val)
        {
            key = "";
            val = "";

            string k;

            k = input; key = Utils.ExtractBefore(ref k, ':').Trim();
            k = input; val = Utils.ExtractAfter(ref k, ':').Trim();

            if (key == "") key = input;

            return (key != "");
        }

        private static bool readTagDeclare(string input, PropertyList styles)
        {
            string k = Utils.getDeclareSection(input);

            string nextDeclare = Utils.ExtractBefore(ref k, ';');
            if (nextDeclare == "")
            {
                nextDeclare = k;
                k = "";
            }
            while (nextDeclare != "")
            {
                string key, value;
                readOneDeclare(nextDeclare, out key, out value);
                if (key != "")
                    styles.Add(key, new PropertyItemType(key, value));

                nextDeclare = Utils.ExtractBefore(ref k, ';');
                if (nextDeclare == "")
                {
                    nextDeclare = k;
                    k = "";
                }
            }

            return true;
        }
        /// <summary>
        ///Decode a full cssStyle into useable format
        /// No checking is avaliable, crash if any error
        /// </summary>
        public static ArrayList DecodeCssStyle(string input)
        {
            ArrayList retVal = new ArrayList();
            ArrayList cssStyleGroups = new ArrayList();

            Utils.readTagName(input, ref cssStyleGroups);
            for (Int32 i = 0; i < cssStyleGroups.Count; i++)
            {
                CssStyleGroupType curItem = (CssStyleGroupType)(cssStyleGroups[i]);
                CssStyleType newCssStyleType = new CssStyleType();
                newCssStyleType.styleTagName = curItem.styleTagName;
                newCssStyleType.tagName = (new CssHeaderStyleType(curItem.styleTagName)).tagName;
                if (newCssStyleType.tagName == "")
                    newCssStyleType.tagName = CssHeaderStyleType.UnspecifiedTagName;
                newCssStyleType.parentTagName = curItem.parentTagName;
                readTagDeclare(input, newCssStyleType.styles);

                retVal.Add(newCssStyleType);
            }
            return retVal;
        }
        #endregion
        #region MatchParentTag() routines
        private static bool matchHeader(HtmlTag input, CssHeaderStyleType chs)
        {
            return (((chs.tagName == CssHeaderStyleType.UnspecifiedTagName) ||
                      (input.Name == chs.tagName)) &&
                      ((chs.tagClass == "") || (chs.tagClass == input["class"])) &&
                      ((chs.tagID == "") || (chs.tagID == input["id"])));
        }
        private static bool haveOtherClassID(HtmlTag input, CssHeaderStyleType chs)
        {
            bool retVal = false;
            if ((chs.tagClass != "") && (input["class"] != ""))
                retVal = (chs.tagClass == input["class"]);
            if ((chs.tagID != "") && (input["id"] != ""))
                retVal = (chs.tagID == input["id"]);
            return retVal;
        }
        //        /// <summary>
        //        /// Check if there is parentTag match the header
        //        /// </summary>        
        //        /// <returns></returns>
        //        internal static bool MatchParentTag(HtmlTag currentTag, string header, out HtmlTag matchTag)
        //        {        	
        //        	matchTag = null;
        //        	CssHeaderStyleType chs = new CssHeaderStyleType(header);
        //        	
        //        	if (currentTag.parentTag != null)
        //        		if (chs.familyTag)
        //        		{
        //        			HtmlTag tempTag = currentTag.parentTag;
        //        			if (tempTag != null)
        //        			{
        //        				Int32 idx = tempTag.childTags.IndexOf(chs.tagName);
        //        				if (tempTag.childTags[idx] != currentTag)
        //        				{
        //        					matchTag = tempTag;
        //        					return true;
        //        				}
        //        			}
        //        		}
        //        		else //not familyTag
        //        		{
        //        			HtmlTag tempTag = currentTag.parentTag;
        //        			while (tempTag != null)
        //        			{
        //        				if ((chs.noOtherClassID) && (Utils.haveOtherClassID(tempTag, chs)))
        //        				{
        //        					return false;
        //        				}
        //        				else
        //        					if(Utils.matchHeader(tempTag, chs))
        //        					{
        //        						matchTag = tempTag;
        //        						return true;
        //        					}
        //        					else
        //        						tempTag = tempTag.parentTag;
        //        			}
        //        		}      
        //        	return false;
        //        }                
        #endregion
        #region Other Css routines                        
        //        /// <summary>
        //        /// Loop and check if currenTag matched the list ParentTagNames
        //        /// </summary>
        //        internal static bool MatchParentTags(HtmlTag currentTag, ArrayList parentTagNames)
        //        {
        //        	if (parentTagNames == null) return false;
        //        	
        //        	HtmlTag tempTag;
        //        	for (Int32 i = parentTagNames.Count -1; i >= 0; i--)
        //        		if (!(Utils.MatchParentTag(currentTag, (string)parentTagNames[i], out tempTag)))
        //        			return false;
        //        	
        //        	return true;        	
        //        }
        /// <summary>
        /// Check if currentTag matches the header
        /// </summary>
        internal static bool MatchCurrentTag(HtmlTag currentTag, string header)
        {
            CssHeaderStyleType chs = new CssHeaderStyleType(header);
            bool retVal = true;//((chs.tagClass == "") && (chs.tagID == ""));

            retVal = retVal && ((chs.tagClass == "") ||
                                ((currentTag.Contains("class")) &&
                                 (chs.tagClass == currentTag["class"])));
            retVal = retVal && ((chs.tagID == "") ||
                                ((currentTag.Contains("id")) &&
                                                     (chs.tagID == currentTag["id"])));

            return retVal;
        }

        #endregion
        /// <summary>
        /// Show about screen of qzMiniHtml
        /// </summary>
        public static void AboutScreen()
        {
            //			string ver = "";
            //			#if mh_dotNet_20
            //			ver = "20";
            //			#elif mh_dotNet_10
            //			ver = "10";
            //			#endif			
            //			
            //			string Css = "b.title {color:Indigo;}";			
            //			string Html = "<p align=\"centre\"><b class=\"title\">qzMiniHtml.Net</b><sup>ver " +
            //				Assembly.GetExecutingAssembly().GetName().Version +
            //				" </sup></p> <p>" +
            //				"<font size=\"10\">" +
            //				"qzMiniHtml.Net is a dotNet" + ver + " component that can parse html/css syntax" +
            //				" and output them on graphics or other media. <br>"+				
            //				"<br></font>" +
            //				
            //				"<p align=\"right\">"+
            //				"<font size=\"8\">Copyright &copy; (2003-2006) Leung Yat Chun Joseph (lycj) <br>"+									
            //				
            //				"email: <a href=\"mailto://\">author2004@quickzip.org</a><br>"+
            //				"www: <a href=\"http://www.quickzip.org\">http://www.quickzip.org</a><br> " +
            //				
            //				"</font></p>";
            //			
            //			mhMessageDialog.Show("About qzMiniHtml.Net",
            //			                     Html, Css, MessageBoxButtons.OK, Color.Honeydew);
        }
        public static void DebugUnit()
        {
            CreatePen(System.Drawing.Color.Lime, 9);
            CreatePen(System.Drawing.Brushes.AliceBlue, 9);

            Bitmap b = new Bitmap(20, 20);
            Graphics g = Graphics.FromImage(b);
            Debug.Assert((FontExists("Arial", g) == true), "FontExists Failed.");

            PrivateFontCollection pfc = new PrivateFontCollection();
            Debug.Assert((LoadFont("AARDC___.TTF", ref pfc) == true), "loadFont Failed.");
            Debug.Assert((UserFontExists("Aardvark Cafe", pfc) == true), "UserFontExists Failed.");
            Console.WriteLine("TextSize of abc = " + Convert.ToString(TextSize(g, "abc", new Font(pfc.Families[0], 10F))));
            Console.WriteLine("TextSize2 of abc = " + Convert.ToString(TextSize2(g, "abc", new Font(pfc.Families[0], 10F))));
            Console.WriteLine("TextSize of abcdefgh = " + Convert.ToString(TextSize(g, "abcdefgh", new Font(pfc.Families[0], 12F))));
            Console.WriteLine("TextSize2 of abcdefgh = " + Convert.ToString(TextSize2(g, "abcdefgh", new Font(pfc.Families[0], 12F))));
            Console.WriteLine("TextSize of aToz = " + Convert.ToString(TextSize(g, "abcdefghijklmnopqrstuvwxyz", new Font(pfc.Families[0], 9F))));
            Console.WriteLine("TextSize2 of aToz = " + Convert.ToString(TextSize2(g, "abcdefghijklmnopqrstuvwxyz", new Font(pfc.Families[0], 9F))));


            g.Dispose();
            b.Dispose();


            Debug.Assert((Replace("12123", '1', '3') == "32323"), "Replace Failed.");
            String aToz = "abcdefghijklmnopqrstuvwxyz";
            Debug.Assert((ExtractBefore(ref aToz, 'g') == "abcdef"), "ExtractBefore Failed.");
            Debug.Assert((aToz == "hijklmnopqrstuvwxyz"), "ExtractBefore Failed.");

            aToz = "abcdefghijklmnopqrstuvwxyz";
            Debug.Assert((ExtractAfter(ref aToz, 'w') == "xyz"), "ExtractAfter Failed.");
            Debug.Assert((aToz == "abcdefghijklmnopqrstuv"), "ExtractAfter Failed.");

            aToz = "abcdefghijklmnopqrstuvwxyz";
            Debug.Assert((ExtractBetween(aToz, 'q', 'w') == "rstuv"), "ExtractBetween Failed.");

            aToz = "abc;def;ghi";
            Debug.Assert((ExtractNextItem(ref aToz, ';') == "abc"), "ExtractNextItem_1 Failed.");
            Debug.Assert((ExtractNextItem(ref aToz, ';') == "def"), "ExtractNextItem_2 Failed.");
            Debug.Assert((aToz == "ghi"), "ExtractNextItem Failed.");

            aToz = "abc;def;ghi";
            Debug.Assert((ExtractList(aToz, ';').Count == 3), "ExtractList Failed.");
            Debug.Assert((RemoveFrontSlash("\\beta") == "beta"), "RemoveFrontSlash Failed.");
            Debug.Assert((Capitalize("clear") == "Clear"), "Capitalize Failed.");


            SimpleHash("abcdefg");
            Debug.Assert((AppendSlash(@"c:\xyz") == @"c:\xyz\"), "AppendSlash Failed.");
            Debug.Assert((RemoveSlash(@"c:\xyz\") == @"c:\xyz"), "RemoveSlash Failed.");

            aToz = "abc;def;ghi";
            ArrayList outputList = ExtractList(aToz, ';');
            aToz = "abc,def,ghi,";
            String output = "";
            foreach (object o in outputList)
                output += ((string)o + ",");
            Debug.Assert((output == aToz), "ExtractList Failed.");


            PropertyList list = ExtravtVariables("href=\"xyz\" name=abc");
            Debug.Assert(list["href"].value == "xyz", "ExtravtVariables Failed.");
            Debug.Assert(list["name"].value == "abc", "ExtravtVariables Failed.");

            Debug.Assert((ExtractFileName(@"c:\xyz\abc.txt") == @"abc.txt"), "ExtractFileName Failed.");
            Debug.Assert((ExtractFilePath(@"c:\xyz\abc.txt") == @"c:\xyz"), "ExtractFilePath Failed.");
            Debug.Assert((ExtractFileExt(@"c:\xyz\abc.txt") == @".txt"), "ExtractFileExt Failed.");

            SymbolHtml();
            Debug.Assert((LocateSymbol("&amp;") == 0038), "LocateSymbol Failed.");
            Debug.Assert((LocateSymbol("&0062;") == 0062), "LocateSymbol Failed.");
            Debug.Assert((DecodeSymbol("&amp;gt;") == ">"), "DecodeSymbol Failed.");


            aToz = @"h1 em";
            elementType e = elementType.eSpace;
            Debug.Assert((ExtractNextElement(ref aToz, ref e) == "h1"), "ExtractNextElement Failed.");
            Debug.Assert((ExtractNextElement(ref aToz, ref e) == "em"), "ExtractNextElement Failed.");

            Debug.Assert((String2Color("Green") == System.Drawing.Color.Green), "String2Color Failed.");
            Debug.Assert((WebColor2Color("#FF00FF") == System.Drawing.Color.FromArgb(255, 0, 255)), "WebColor2Color Failed.");
            Debug.Assert((StrAlign2Align("Centre") == hAlignType.Centre), "StrAlign2Align Failed.");
            Debug.Assert((StrSize2PixelSize("75%", 100) == 75), "StrSize2PixelSize (%) Failed.");
            Debug.Assert((StrSize2PixelSize("20px", 100) == 20), "StrSize2PixelSize (px) Failed.");
            Debug.Assert((StrSize2PixelSize("30em", 100) == 30), "StrSize2PixelSize (em) Failed.");
            Debug.Assert((StrPosition2PositionType("Relative") == positionStyleType.Relative), "StrPosition2PositionType Failed.");
            Debug.Assert((StrBorder2BorderType("Double") == borderStyleType.Double), "StrBorder2BorderType Failed.");
            Debug.Assert((StrBullet2BulletType("UpperAlpha") == bulletStyleType.UpperAlpha), "StrBullet2BulletType Failed.");
            Debug.Assert((StrCursor2CursorType("Pointer") == Cursors.Hand), "StrCursor2CursorType Failed.");
            Debug.Assert((StrMethod2FormMethodType("Get") == formMethodType.Get), "StrMethod2FormMethodType Failed.");
            Debug.Assert((StrType2VariableType("Alpha") == variableType.Alpha), "StrType2VariableType Failed.");

        }

        public static void DebugRun()
        {
            Run(@"c:\windows\notepad.exe", "");
            ProcessInfo pi = new ProcessInfo();
            Run2(@"c:\windows\notepad.exe", "", ref pi);
        }
    }

    public class HtmlAttributeStringSerializer : IPropertySerializer
    {
        #region Constructor

        #endregion

        #region Methods

        public string PropertyToString(IEnumerable<Tuple<string, string>> properties)
        {
            string retVal = "";
            foreach (var prop in properties)
                retVal += String.Format(" {0}=\"{1}\"", prop.Item1, prop.Item2);
            return retVal;
        }

        static char quote = '\'';
        private static void locateNextVariable(ref string working, ref string varName, ref string varValue)
        {
            working = working.Trim();

            Int32 pos1 = working.IndexOf('=');
            if (pos1 != -1)
            {
                varName = working.Substring(0, pos1);
                Int32 j = working.IndexOf(quote);
                Int32 f1 = working.IndexOf(' ');
                Int32 f2 = working.IndexOf('=');
                if (f1 == -1) { f1 = f2 + 1; }

                if ((j == -1) || (j > f1))
                {
                    varValue = working.Substring(f2 + 1, working.Length - f2 - 1);
                    f1 = working.IndexOf(' ');
                    if (f1 == -1)
                    {
                        working = "";
                    }
                    else
                    {
                        working = working.Substring(f1 + 1, working.Length - f1 - 1);
                    }

                }
                else
                {
                    working = working.Substring(j + 1, working.Length - j - 1);
                    j = working.IndexOf(quote);
                    if (j != -1)
                    {
                        varValue = working.Substring(0, j);
                        working = working.Substring(j + 1, working.Length - j - 1);
                    }
                }
            }
            else
            {
                varName = working;
                varValue = "TRUE";
                working = "";
            }

        }

        public IEnumerable<Tuple<string, string>> StringToProperty(string propertyString)
        {
            string working = propertyString;
            string varName = "", varValue = "";
            while (working != "")
            {
                locateNextVariable(ref working, ref varName, ref varValue);
                yield return new Tuple<string, string>(varName, varValue);
            }
        }

        #endregion

        #region Data

        #endregion

        #region Public Properties

        #endregion


    }

    //[Export(typeof(ICofeService))]
    //[ServicePriority(ServicePriorityAttribute.DefaultPriority_COFE)]
    public class ParamParser : IParamParser
    {
        #region Constructor

        public ParamParser(IPropertySerializer serializer)
        {
            Serializer = serializer;
        }

        //public ParamParser()
        //    : this(new ParamStringSerializer(false))
        //{

        //}

        #endregion

        #region Methods


        public string DictionaryToString(Dictionary<string, string> paramDic)
        {
            return Serializer.PropertyToString(
                from p in paramDic.Keys
                select new Tuple<string, string>(p, paramDic[p])
                );
        }

        public Dictionary<string, string> StringToDictionary(string paramString)
        {
            Dictionary<string, string> retDic = new Dictionary<string, string>();

            foreach (var tup in Serializer.StringToProperty(paramString))
                if (!retDic.ContainsKey(tup.Item1))
                    retDic.Add(tup.Item1, tup.Item2);
                else retDic[tup.Item1] = tup.Item2;

            return retDic;
        }

        #endregion

        #region Data

        #endregion 

        #region Public Properties

        public IPropertySerializer Serializer { get; private set; }

        #endregion

    }

    /// <summary>
    /// Dummy.
    /// </summary>
    public interface ICofeService
    {
    }
    public interface IParamParser : ICofeService
    {
        /// <summary>
        /// Uses to serialize the properties to string.
        /// </summary>
        IPropertySerializer Serializer { get; }

        /// <summary>
        /// Convert a dictionary contining key value pairs to ParamString.
        /// </summary>
        /// <param name="param">An array of key and value pairs</param>
        /// <returns>Param String containing both key and value, e.g. key:"value"</returns>
        string DictionaryToString(Dictionary<string, string> paramDic);

        /// <summary>
        /// Take a Paramstring and return a dictionary containing 
        /// the paramstring's key and value.
        /// </summary>
        /// <param name="input"></param>        
        /// <param name="paramDic"></param>        
        Dictionary<string, string> StringToDictionary(string paramString);
    }

    public interface IPropertySerializer
    {
        /// <summary>
        /// Convert a dictionary contining key value pairs to property string.
        /// </summary>
        /// <param name="properties">An array of key and value pairs</param>
        /// <returns>Param String containing both key and value, e.g. key:"value"</returns>
        string PropertyToString(IEnumerable<Tuple<string, string>> properties);

        /// <summary>
        /// Take a property string and return a list of tuple containing 
        /// the its key and value.
        /// </summary>
        /// <param name="input"></param>        
        /// <param name="paramDic"></param>        
        IEnumerable<Tuple<string, string>> StringToProperty(string propertyString);
    }
}
