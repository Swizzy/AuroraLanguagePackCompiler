namespace AuroraLanguagePackCompiler {
    using System.Runtime.Serialization;

    [DataContract] public class LanguagePackData {
        [DataMember(Name = "displayname", Order = 0)] public string Displayname;
        [DataMember(Name = "index", Order = int.MaxValue)] public int Index;
        [DataMember(Name = "languagecode", Order = 1)] public string Languagecode;
        [DataMember(Name = "translator", Order = 2)] public string Translator;
        [DataMember(Name = "version", Order = 3)] public string Version;
    }
}