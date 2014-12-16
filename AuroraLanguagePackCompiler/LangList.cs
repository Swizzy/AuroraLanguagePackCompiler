namespace AuroraLanguagePackCompiler {
    using System;
    using System.Globalization;

    internal class LangList {
        public readonly CultureInfo CultureInfo;

        public LangList(CultureInfo cultureInfo) { CultureInfo = cultureInfo; }

        internal static int LangListCompare(LangList list1, LangList list2) { return String.Compare(list1.ToString(), list2.ToString(), StringComparison.OrdinalIgnoreCase); }

        public override string ToString() { return string.Format("{0} [{1}] [{2}]", CultureInfo.EnglishName, CultureInfo.NativeName, CultureInfo.Name); }
    }
}