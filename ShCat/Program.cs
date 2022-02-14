using System;
using System.Drawing;

using AvaloniaEdit.TextMate.Grammars;

using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace ShCat
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: shcat <filename>");
                return;
            }

            string file = Path.GetFullPath(args[0]);

            if (!File.Exists(file))
            {
                Console.WriteLine("File {0} not found", file);
                return;
            }

            CatFile(file);
        }

        static void CatFile(string file)
        {
            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry registry = new Registry(options);

            IGrammar grammar = registry.LoadGrammar(
                options.GetScopeByExtension(
                    Path.GetExtension(file)));

            if (grammar == null)
            {
                Console.WriteLine(File.ReadAllText(file));
                return;
            }

            StackElement? ruleStack = null;

            Theme theme = registry.GetTheme();

            using (StreamReader sr = new StreamReader(file))
            {
                string? line = sr.ReadLine();

                while (line != null)
                {
                    ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack);

                    ruleStack = result.RuleStack;

                    foreach (IToken token in result.Tokens)
                    {
                        int startIndex = (token.StartIndex > line.Length) ?
                            line.Length : token.StartIndex;
                        int endIndex = (token.EndIndex > line.Length) ?
                            line.Length : token.EndIndex;

                        int foreground = -1;
                        int background = -1;
                        int fontStyle = -1;

                        foreach (var themeRule in theme.Match(token.Scopes))
                        {
                            if (foreground == -1 && themeRule.foreground > 0)
                                foreground = themeRule.foreground;

                            if (background == -1 && themeRule.background > 0)
                                background = themeRule.background;

                            if (fontStyle == -1 && themeRule.fontStyle > 0)
                                fontStyle = themeRule.fontStyle;
                        }

                        WriteToken(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle, theme);
                    }

                    Console.WriteLine();
                    line = sr.ReadLine();
                }
            }
        }

        static void WriteToken(string text, int foreground, int background, int fontStyle, Theme theme)
        {
            if (foreground == -1)
            {
                Console.Write(text);
                return;
            }

            ConsoleColor colorBck = Console.ForegroundColor;

            try
            {
                // TODO: find a multiplatform way to draw bg, fg and bold/italic text for the console
                Color foregroundColor = ColorTranslator.FromHtml(theme.GetColor(foreground));
                Colorful.Console.Write(text, foregroundColor);

            }
            finally
            {
                Console.ForegroundColor = colorBck;
            }
        }
    }

    internal static class StringExtensions
    {
        internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
        {
            return str.Substring(startIndex, endIndex - startIndex);
        }
    }
}