#if UNITY_2017_1_OR_NEWER
#define UNITY
#endif

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ArrayExtensions;
using System.IO;

#if UNITY
using UnityEngine;
#endif

public class Logger
{
    public static void Log(string message)
    {
#if UNITY
        UnityEngine.Debug.Log(message);
#else
        System.Console.WriteLine(message);
#endif
    }

    public static void LogWarning(string message)
    {
#if UNITY
        UnityEngine.Debug.Log(message);
#else
        System.Console.WriteLine(message);
#endif
    }

    public static void LogError(string message)
    {
#if UNITY
        UnityEngine.Debug.Log(message);
#else
        System.Console.WriteLine(message);
#endif
    }
}


#if !UNITY
public class NazekaFilesLogic
{
    public static void LoadFiles()
    {
        System.Console.WriteLine(Directory.GetCurrentDirectory());

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory() + "/dict");
        
        foreach(var file in directory.GetFiles())
        {
            if (file.Name.EndsWith(".json"))
            {
                LoadedFiles.Add(file.Name.Substring(0, file.Name.Length - ".json".Length), File.ReadAllText(file.FullName));
            }

            if (file.Name.EndsWith(".txt"))
            {
                LoadedFiles.Add(file.Name.Substring(0, file.Name.Length - ".txt".Length), File.ReadAllText(file.FullName));
            }
        }
    }

    public static Dictionary<string, string> LoadedFiles = new Dictionary<string, string>();
}
#endif

namespace System
{
    public static class ObjectExtensions
    {
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        public static Object Copy(this Object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
        }
        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    Array clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }

            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }
        public static T Copy<T>(this T original)
        {
            return (T)Copy((Object)original);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        internal class ArrayTraverse
        {
            public int[] Position;
            private int[] maxLengths;

            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

public class FuriganaPlacement
{
    public string Original = "";
    public string Replacement = "";
    public int Start = -1;
}

public class JapaneseTextTooltip
{
    private class DeconjugationRulesStruct
    {
        public string type;
        public string contextrule;
        public List<string> dec_end = new List<string>();
        public bool dec_end_was_array = false;
        public List<string> con_end = new List<string>();
        public string con_end0 = null;
        public bool con_end_was_array = false;
        public List<string> dec_tag = new List<string>();
        public bool dec_tag_was_array = false;
        public List<string> con_tag = new List<string>();
        public bool con_tag_was_array = false;
        public string detail;

        public void GenerateVirtualDeconjugations()
        {
            List<string> array = null;

            if (dec_end_was_array)
                array = dec_end;
            else if (con_end_was_array)
                array = con_end;
            else if (dec_tag_was_array)
                array = dec_tag;
            else if (con_tag_was_array)
                array = con_tag;

            if (array != null)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var virtual_rule = new DeconjugationRulesStruct();
                    virtual_rule.type = type;
                    virtual_rule.detail = detail;

                    if (dec_end_was_array) virtual_rule.dec_end.Add(dec_end[i]); else if (dec_end.Count > 0) virtual_rule.dec_end.Add(dec_end[0]);
                    if (con_end_was_array) virtual_rule.con_end.Add(con_end[i]); else if (con_end.Count > 0) virtual_rule.con_end.Add(con_end[0]);

                    if (dec_tag_was_array) virtual_rule.dec_tag.Add(dec_tag[i]); else if (dec_tag.Count > 0) virtual_rule.dec_tag.Add(dec_tag[0]);
                    if (con_tag_was_array) virtual_rule.con_tag.Add(con_tag[i]); else if (con_tag.Count > 0) virtual_rule.con_tag.Add(con_tag[0]);

                    if(virtual_rule.con_end.Count > 0)
                    {
                        virtual_rule.con_end0 = virtual_rule.con_end[0];
                    }

                    virtual_deconjugations.Add(virtual_rule);
                }
            }
            else
            {
                virtual_deconjugations.Add(this);
            }
        }

        public List<DeconjugationRulesStruct> virtual_deconjugations = new List<DeconjugationRulesStruct>();
    }

    public class DeconjugationNovel
    {
        public string text;
        public string original_text;
        public List<string> tags = new List<string>();
        public HashSet<string> seentext = new HashSet<string>();
        public List<string> process = new List<string>();
    }

    public class DictionaryEleEntry
    {
        public string reb;
        public string keb;
        public List<string> restr = new List<string>();
        public List<string> pri = new List<string>();
        public List<string> inf = new List<string>();
    }

    public class DictionarySenseEntry
    {
        public List<string> pos = new List<string>();
        public List<string> misc = new List<string>();
        public List<string> gloss = new List<string>();
        public List<string> inf = new List<string>();
        public List<string> dial = new List<string>();
        public List<string> stagk = new List<string>();
        public List<string> stagr = new List<string>();
    }

    public static string ReplaceFullWidthWithHalf(string full)
    {
        var output = full;

        foreach(var c in FullWidthToHalfWidthMap)
        {
            output = output.Replace(c.Key, c.Value);
        }

        return output;
    }

    public class DictionaryEntry
    {
        public int seq;
        public List<DictionaryEleEntry> k_ele = new List<DictionaryEleEntry>();
        public List<DictionaryEleEntry> r_ele = new List<DictionaryEleEntry>();
        public List<DictionarySenseEntry> sense = new List<DictionarySenseEntry>();

        //Search related stuff
        public string from;
        public DictionaryEleEntry found;
        public DictionaryEleEntry orig_found;
        public HashSet<DeconjugationNovel> deconj;
        public HashSet<string> allpos;
        public int priority;
        public List<string> priorityTags;
        public List<string> has_audio;
        public FrequencyEntry freq;

        public void CleanFullWidth()
        {
            foreach(var k in k_ele)
            {
                if(k.keb != null)
                {
                    k.keb = ReplaceFullWidthWithHalf(k.keb);
                }
            }

            foreach (var r in r_ele)
            {
                if(r.reb != null)
                {
                    r.reb = ReplaceFullWidthWithHalf(r.reb);
                }
            }
        }
    }

    private class KanjiDataStruct
    {
        public string c;
        public string g;
        public int s;
        public List<string> k;
        public List<string> o;
        public List<string> os;
        public string z;
    }

    private static void CutStringForKanaTransformation(string input, ref string prefix, ref string middle, ref string suffix)
    {
        int iFirstKana = -1;
        int iLastKana = -1;

        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (is_hira(c))
            {
                if (iFirstKana < 0)
                {
                    iFirstKana = i;
                }

                iLastKana = i;
            }
        }

        if (iFirstKana > 0)
        {
            prefix = input.Substring(0, iFirstKana);
        }

        if (iLastKana >= 0 && iLastKana < input.Length - 1)
        {
            suffix = input.Substring(iLastKana + 1);
        }

        if(prefix.Length == 0 && suffix.Length == 0)
        {
            prefix = input;
        }
        else
        {
            middle = input.Substring(prefix.Length, input.Length - prefix.Length - suffix.Length);
        }   
    }

    public class Definitions
    {
        public string input;
        public string text;
        public List<DictionaryEntry> result;
        public string followup = null;
        public Definitions followupstruct = null;
        public string kanastring = null;
        public Dictionary<string, string> ReplacementRules = new Dictionary<string, string>();

        public string ComputeKanaStringPerKebReb(string text, string reb, string keb, string textprefix, string textmiddle, string textsuffix, int kelecount)
        {
            if (reb == null || keb == null)
            {
                return null;
            }

            if (text == keb)
            {
                ReplacementRules.TryAdd(text, reb);
                return reb;
            }

            var kebprefix = "";
            var kebmiddle = "";
            var kebsuffix = "";
            CutStringForKanaTransformation(keb, ref kebprefix, ref kebmiddle, ref kebsuffix);

            if (kelecount > 1 && kebmiddle.Length == 0)
            {
                return null;
            }

            if (textprefix == kebprefix)
            {
                var rebprefix = "";
                var rebbuffer = reb;

                while (rebbuffer.Length > 0)
                {
                    if (rebbuffer == kebmiddle)
                    {
                        break;
                    }

                    rebprefix += rebbuffer[0];
                    rebbuffer = rebbuffer.Substring(1);
                }

                ReplacementRules.TryAdd(textprefix, rebprefix);

                return rebprefix + textmiddle + textsuffix;
            }

            return null;
        }

        public void ComputeKanaString()
        {
            if(is_kana(text))
            {
                kanastring = text;
                return;
            }

            var textprefix = "";
            var textmiddle = "";
            var textsuffix = "";
            CutStringForKanaTransformation(text, ref textprefix, ref textmiddle, ref textsuffix);

            foreach (var entry in result)
            {
                foreach(var r_ele in entry.r_ele)
                {
                    foreach(var k_ele in entry.k_ele)
                    {
                        var computed = ComputeKanaStringPerKebReb(text, r_ele.reb, k_ele.keb, textprefix, textmiddle, textsuffix, entry.k_ele.Count);

                        if (computed != null)
                        {
                            kanastring = computed;
                            return;
                        }

                        break;
                    }
                }
            }

            foreach (var entry in result)
            {
                foreach (var r_ele in entry.r_ele)
                {
                    bool first = false;

                    foreach (var k_ele in entry.k_ele)
                    {
                        if(first)
                        {
                            first = false;
                            continue;
                        }

                        var computed = ComputeKanaStringPerKebReb(text, r_ele.reb, k_ele.keb, textprefix, textmiddle, textsuffix, entry.k_ele.Count);

                        if (computed != null)
                        {
                            kanastring = computed;
                            return;
                        }
                    }
                }
            }

            kanastring = text;
        }
    }

    private class PriorityRule
    {
        public string rule0;
        public string rule1;
        public int rule2;
    }

    public class FrequencyEntry
    {
        public string freq0;
        public int freq1;
        public float freq2;

        public string reading;
    }

    private static List<DeconjugationRulesStruct> DeconjugationRules = new List<DeconjugationRulesStruct>();

    private static DeconjugationNovel stdrule_deconjugate_inner(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.text.Length < my_rule.con_end0.Length) return null;

        for(int i = my_form.text.Length - 1, j = my_rule.con_end0.Length - 1; j >= 0; i--, j--)
        {
            if (my_form.text[i] != my_rule.con_end0[j])
            {
                return null;
            }
        }

        if (my_form.tags.Count > 0 && my_form.tags.Last() != my_rule.con_tag[0]) return null;

        var newtext = my_form.text.Substring(0, my_form.text.Length - my_rule.con_end0.Length) + my_rule.dec_end[0];

        var  newform = new DeconjugationNovel();
        newform.text = newtext;
        newform.original_text = my_form.original_text;
        newform.tags = new List<string>(my_form.tags);
        newform.seentext = new HashSet<string>(my_form.seentext);
        newform.process = new List<string>(my_form.process);

        newform.text = newtext;

        newform.process.Add(my_rule.detail);

        if (newform.tags.Count == 0)
        {
            newform.tags.Add(my_rule.con_tag[0]);
        }

        newform.tags.Add(my_rule.dec_tag[0]);

        if (newform.seentext.Count == 0)
        {
            newform.seentext.Add(my_form.text);
        }
            
        newform.seentext.Add(newtext);

        return newform;
    }

    private static void stdrule_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule, List<DeconjugationNovel> output)
    {
        for (var i = 0; i < my_rule.virtual_deconjugations.Count; ++i)
        {
            var localrule = my_rule.virtual_deconjugations[i];

            if (my_form.text.Length < localrule.con_end0.Length) continue;

            bool failedtest = false;

            for (int k = my_form.text.Length - 1, j = localrule.con_end0.Length - 1; j >= 0; k--, j--)
            {
                if (my_form.text[k] != localrule.con_end0[j])
                {
                    failedtest = true;
                    break;
                }
            }

            if(failedtest)
            {
                continue;
            }

            if (my_form.tags.Count > 0 && my_form.tags.Last() != localrule.con_tag[0]) continue;

            var ret = stdrule_deconjugate_inner(my_form, my_rule.virtual_deconjugations[i]);

            if (ret != null)
            {
                output.Add(ret);
            }
        }
    }

    private static bool v1inftrap_check(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.tags.Count != 1) return true;

        var my_tag = my_form.tags[0];

        if (my_tag == "stem-ren")
            return false;

        return true;
    }

    private static bool saspecial_check(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.text == "") return false;

        if (!my_form.text.EndsWith(my_rule.con_end0)) return false;

        var base_text = my_form.text.Substring(0, my_form.text.Length - my_rule.con_end0.Length);

        if (base_text.EndsWith("さ"))
            return false;

        return true;
    }

    private static Dictionary<string, Func<DeconjugationNovel, DeconjugationRulesStruct, bool>> context_functions = new Dictionary<string, Func<DeconjugationNovel, DeconjugationRulesStruct, bool>>()
    {
        { "v1inftrap", v1inftrap_check },
        { "saspecial", saspecial_check }
    };

    private static DeconjugationNovel substitution_inner(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        var newtext = Regex.Replace(my_form.text, my_rule.con_end0, my_rule.dec_end[0], RegexOptions.None);

        var newform = new DeconjugationNovel();
        newform.text = newtext;
        newform.original_text = my_form.original_text;
        newform.tags = new List<string>(my_form.tags);
        newform.seentext = new HashSet<string>(my_form.seentext);
        newform.process = new List<string>(my_form.process);

        newform.text = newtext;
        newform.process.Add(my_rule.detail);

        if (newform.seentext.Count == 0)
            newform.seentext.Add(my_form.text);

        newform.seentext.Add(newtext);

        return newform;
    }

    private static void substitution_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule, List<DeconjugationNovel> output)
    {
        foreach (var virtualrule in my_rule.virtual_deconjugations)
        {
            if (!my_form.text.Contains(virtualrule.con_end0)) continue;

            var ret = substitution_inner(my_form, virtualrule);

            if (ret != null)
            {
                output.Add(ret);
            }
        }
    }

    private static HashSet<DeconjugationNovel> deconjugate(string mytext)
    {
        var processed = new HashSet<DeconjugationNovel>();
        var novel = new HashSet<DeconjugationNovel>();
    
        var start_form = new DeconjugationNovel() { text = mytext, original_text = mytext, tags = new List<string>(), seentext = new HashSet<string>(), process = new List<string>() };
        novel.Add(start_form);
    
        while(novel.Count > 0)
        {
            var new_novel = new HashSet<DeconjugationNovel>();
            var ruleoutput = new List<DeconjugationNovel>();

            foreach(var form in novel)
            {
                if(form.text == "")
                {
                    continue;
                }

                foreach(var rule in DeconjugationRules)
                {
                    switch(rule.type)
                    {
                        case "stdrule":
                            {
                                if (form.text.Length > form.original_text.Length + 10) break;
                                if (form.tags.Count > form.original_text.Length + 6) break;
                                if (rule.detail == "" && form.tags.Count == 0) break;

                                stdrule_deconjugate(form, rule, ruleoutput); 
                                break;
                            }
                        case "rewriterule":
                            {
                                if (form.text != rule.con_end0)
                                {
                                    break;
                                }

                                stdrule_deconjugate(form, rule, ruleoutput); 
                                break;
                            }
                        case "onlyfinalrule":
                            {
                                if (form.tags.Count != 0)
                                {
                                    break;
                                }

                                stdrule_deconjugate(form, rule, ruleoutput); 
                                break;
                            }
                        case "neverfinalrule":
                            {
                                if (form.tags.Count == 0)
                                {
                                    break;
                                }

                                stdrule_deconjugate(form, rule, ruleoutput); 
                                break;
                            }
                        case "contextrule":
                            {
                                if (!context_functions[rule.contextrule](form, rule))
                                {
                                    break;
                                }

                                stdrule_deconjugate(form, rule, ruleoutput); 
                                break;
                            }
                        case "substitution":
                            {
                                if (form.process.Count != 0)
                                {
                                    break;
                                }

                                substitution_deconjugate(form, rule, ruleoutput); 
                                break;
                            }
                    }
                }
            }

            foreach (var myform in ruleoutput)
            {
                if (myform != null && !processed.Contains(myform) && !novel.Contains(myform) && !new_novel.Contains(myform))
                {
                    new_novel.Add(myform);
                }
            }

            processed.UnionWith(novel);
            novel = new_novel;
        }
    
        return processed;
    }


    private static bool is_hira(char c)
    {
        return (c >= 0x3040 && c <= 0x3096) || (c >= 0x309D && c <= 0x309E);
    }

    private static bool is_kata(char c)
    {
        return (c >= 0x30A0 && c <= 0x30F6) || (c >= 0x30FD && c <= 0x30FE);
    }

    private static string replace_hira_with_kata(string text)
    {
        var newtext = "";

        for (var i = 0; i < text.Length; i++)
        {
            var code = text[i];

            int codepoint = text[i];

            if (codepoint >= 0x3040 && codepoint <= 0x3096)
                codepoint += (0x30A0 - 0x3040);
            else if (codepoint >= 0x309D && codepoint <= 0x309E)
                codepoint += (0x30A0 - 0x3040);

            newtext += (char)codepoint;
        }

        return newtext;
    }

    private static string replace_kata_with_hira(string text)
    {
        var newtext = "";

        for (var i = 0; i < text.Length; i++)
        {
            var code = text[i];

            int codepoint = text[i];

            if (codepoint >= 0x30A0 && codepoint <= 0x30F6)
                codepoint -= (0x30A0 - 0x3040);
            else if (codepoint >= 0x30FD && codepoint <= 0x30FE)
                codepoint -= (0x30A0 - 0x3040);

            newtext += (char)codepoint;
        }

        return newtext;
    }

    private static string clip(string str)
    {
        if (str == null)
            return null;

        if (str.Length <= 2)
            return "";

        return str.Substring(1, str.Length - 2);
    }

    private static List<DictionaryEntry> getfromdict(List<int> indexes, string text)
    {
        var ret = new List<DictionaryEntry>();

        foreach(var index in indexes) 
        {
            var entry = DictionaryEntries[index];
            entry.from = text;

            var found = false;

            foreach(var spelling in entry.k_ele)
            {
                if (spelling.keb == entry.from)
                {
                    entry.found = spelling;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                foreach (var spelling in entry.r_ele)
                {
                    if (spelling.reb == entry.from)
                    {
                        entry.found = spelling;
                        break;
                    }
                }
            }

            var lastpos = new List<string>();

            for (var j = 0; j < entry.sense.Count; j++)
            {
                if (entry.sense[j].pos.Count > 0)
                {
                    for (var t = 0; t < entry.sense[j].pos.Count; t++)
                    {
                        if (entry.sense[j].pos[t][0] == '&')
                        {
                            entry.sense[j].pos[t] = clip(entry.sense[j].pos[t]);
                        }
                    }
                }
                else
                {
                    entry.sense[j].pos = lastpos;
                }

                lastpos = entry.sense[j].pos;
            }

            ret.Add(entry);
        }

        return ret;
    }

    private static List<DictionaryEntry> search_inner(string text)
    {
        if (LookupKanji.ContainsKey(text))
            return getfromdict(LookupKanji[text], text);
        else if (LookupKana.ContainsKey(text))
            return getfromdict(LookupKana[text], text);
        else
            return null;
    }

    private static List<DictionaryEntry> search(string text)
    {
        var ret = search_inner(text);
        if (ret != null) return ret;

        ret = search_inner(replace_hira_with_kata(text));
        if (ret != null) return ret;

        ret = search_inner(replace_kata_with_hira(text));
        if (ret != null) return ret;

        return null;
    }


    private static List<DictionaryEntry> build_lookup_comb(HashSet<DeconjugationNovel> forms)
    {
        var looked_up = new Dictionary<string, List<DictionaryEntry>>();

        foreach (var form in forms)
        {
            if (!looked_up.ContainsKey(form.text))
            {
                var result = search(form.text);
                var copied_result = new List<DictionaryEntry>();

                if (result != null)
                {
                    for (int r = 0; r < result.Count; r++)
                    {
                        var entry = result[r];

                        entry.deconj = null;
                        entry.allpos = new HashSet<string>();

                        for (var i = 0; i < entry.sense.Count; i++)
                        {
                            var sense = entry.sense[i];

                            for (var j = 0; j < sense.pos.Count; j++)
                            {
                                entry.allpos.Add(sense.pos[j]);
                            }   
                        }

                        copied_result.Add(entry);
                    }

                    looked_up[form.text] = copied_result;
                }
            }
        }

        foreach (var form in forms)
        {
            if (looked_up.ContainsKey(form.text))
            {
                var result = looked_up[form.text];

                foreach (var entry in result)
                {
                    if (form.tags.Count > 0)
                    {
                        if (entry.allpos.Contains(form.tags[form.tags.Count - 1]))
                        {
                            if (entry.deconj == null)
                            {
                                entry.deconj = new HashSet<DeconjugationNovel>();
                            }

                            entry.deconj.Add(form);
                        }
                    }
                    else
                    {
                        if (entry.deconj == null)
                        {
                            entry.deconj = new HashSet<DeconjugationNovel>();
                        }

                        entry.deconj.Add(form);
                    }
                }
            }
        }

        var merger = new List<DictionaryEntry>();

        foreach (var result in looked_up)
        {
            foreach (var entry in result.Value)
            {
                if (entry.deconj != null)
                {
                    //entry.deconj = Array.from(entry.deconj); Dont know why copy here
                    merger.Add(entry);
                }
            }
        }

        return merger;
    }

    private static bool is_kana(object obj)
    {
        if (obj is DictionaryEntry && ((DictionaryEntry)obj).k_ele.Count > 0)
        {
            return false;
        }
           
        if (obj is DictionaryEntry)
        {
            return true;
        }

        if(!(obj is string))
        {
            return false;
        }

        var str = (string)obj;

        for (var i = 0; i < str.Length; i++)
        {
            var codepoint = str[i];
            if (!is_hira(codepoint) && !is_kata(codepoint))
            {
                return false;
            }  
        }

        return true;
    }

    private static bool prefers_kana(object obj)
    {
        if (obj is DictionaryEntry)
        {
            var entry = (DictionaryEntry)obj;

            foreach(var sense in entry.sense)
            {
                foreach (var misc in sense.misc)
                {
                    if (clip(misc) == "uk" || clip(misc) == "ek")
                    {
                        return true;
                    }  
                }
            }
        }
        return false;
    }

    private static List<DictionaryEntry> filter_kana_ish_results(List<DictionaryEntry> results)
    {
        if (results == null)
        {
            return null;
        }

        var newresults = new List<DictionaryEntry>();

        foreach(var entry in results)
        {
            if (is_kana(entry) || prefers_kana(entry))
            {
                newresults.Add(entry);
            }
        }

        return newresults;
    }

    private static bool weird_lookup(DictionaryEleEntry found)
    {
        var settocheck = new HashSet<string>() { "ik", "iK", "io", "ok", "oK" };

        if (found != null)
        {
            foreach  (var info in found.inf)
            {
                if (settocheck.Contains(clip(info)))
                {
                    return true;
                }    
            }
        }

        return false;
    }

    private static bool has_pri(DictionaryEntry entry)
    {
        foreach (var sense in entry.sense)
        {
            foreach (var ele in entry.k_ele)
            {
                if (ele.pri.Count > 0)
                    return true;
            }
            foreach (var ele in entry.r_ele)
            {
                if (ele.pri.Count > 0)
                    return true;
            }
        }

        return false;
    }

    private static bool prefers_kana(DictionaryEntry entry)
    {
        foreach (var sense in entry.sense)
        {
            foreach (var misc in sense.misc)
            {
                if (clip(misc) == "uk" || clip(misc) == "ek")
                    return true;
            }
        }

        return false;
    }

    private static bool prefers_kanji(DictionaryEntry entry)
    {
        foreach (var sense in entry.sense)
        {
            foreach (var misc in sense.misc)
            {
                if (clip(misc) == "uK" || clip(misc) == "eK")
                    return true;
            }
        }

        return false;
    }

    private static bool all_senses_have_a_tag(DictionaryEntry entry, List<string> tags)
    {
        foreach (var sense in entry.sense)
        {
            var anymatch = false;

            foreach (var misc in sense.misc)
            {
                foreach (var actualtag in tags)
                {
                    if (clip(misc) == actualtag)
                    {
                        anymatch = true;
                        break;
                    }   
                } 
            }

            if (!anymatch)
            {
                return false;
            }  
        }

        return true;
    }

    private static List<DictionaryEntry> sort_results(string text, List<DictionaryEntry> results)
    {
        if (results == null) return null;

        var reading_kana = is_kana(text);

        foreach (var entry in results)
        {
            var result_kana = is_kana(entry);
            entry.priority = (entry.seq - 1000000) / -10000000; // divided by one more order of magnitude
            entry.priorityTags = new List<string>();

            if (weird_lookup(entry.found))
            {
                entry.priority -= 50;
                entry.priorityTags.Add("weird");
            }
            if (reading_kana == result_kana && entry.deconj.Count == 0) // !entry.deconj fixes なって
            {
                entry.priority += 100;
                entry.priorityTags.Add("exact kana");
            }
            if (has_pri(entry))
            {
                entry.priority += 30;
                entry.priorityTags.Add("pri");
            }
            if (!reading_kana && prefers_kanji(entry))
            {
                entry.priority += 12;
                entry.priorityTags.Add("kanji prefers kanji");
            }
            if (reading_kana && prefers_kana(entry))
            {
                entry.priority += 10;
                entry.priorityTags.Add("kana prefers kana");
            }
            if (reading_kana && prefers_kanji(entry))
            {
                entry.priority -= 12;
                entry.priorityTags.Add("kanji disprefers kana");
            }
            if (!reading_kana && prefers_kana(entry))
            {
                entry.priority -= 10;
                entry.priorityTags.Add("kana disprefers kanji");
            }
            if (entry.sense.Count >= 3)
            {
                entry.priority += 3;
                entry.priorityTags.Add("many senses");
            }
            // FIXME: affects words with only one obscure/rare/obsolete sense
            if (all_senses_have_a_tag(entry, new List<string>() { "obsc", "rare", "obs" }))
            {
                entry.priority -= 5;
                entry.priorityTags.Add("obscure");
            }
            if (entry.deconj.Count > 0 && entry.deconj.First().process.Count == 0)
            {
                entry.priority += 1;
                entry.priorityTags.Add("no deconj");
            }
            if (entry.deconj.Count > 0 && entry.deconj.First().process.Count > 2)
            {
                entry.priority -= 1;
                entry.priorityTags.Add("long deconj");
            }

            var boost = 0;
            // looked up and found kanji
            if (entry.found.keb != null)
            {
                foreach (var r in entry.r_ele)
                {
                    foreach (var rule in PriorityRules)
                    {
                        if (rule.rule0 == entry.found.keb && rule.rule1 == r.reb)
                        {
                            if (boost == 0)
                                boost = rule.rule2;
                            else
                                boost = Math.Max(boost, rule.rule2);
                        }
                    }
                }
            }

            // looked up kana and found a kanji word
            else if (entry.k_ele.Count > 0)
            {
                foreach (var k in entry.k_ele)
                {
                    foreach (var rule in PriorityRules)
                    {
                        // this fallback only happens when searching against text looked up in kana
                        if (is_kana(rule.rule0))
                        {
                            if (rule.rule0 == text && rule.rule1 == k.keb)
                            {
                                if (boost == 0)
                                    boost = rule.rule2;
                                else
                                    boost = Math.Max(boost, rule.rule2);
                            }
                        }
                        else if (rule.rule0 == k.keb && rule.rule1 == entry.found.reb)
                        {
                            if (boost == 0)
                                boost = rule.rule2;
                            else
                                boost = Math.Max(boost, rule.rule2);
                        }
                    }
                }
            }
            // looked up kana and found a word with no kanji
            else
            {
                foreach (var rule in PriorityRules)
                {
                    if (rule.rule0 == "" && rule.rule1 == entry.found.reb)
                    {
                        if (boost == 0)
                            boost = rule.rule2;
                        else
                            boost = Math.Max(boost, rule.rule2);
                    }
                }
            }

            if (boost != 0)
                entry.priorityTags.Add("boost");

            entry.priority += boost;
        }

        results.Sort((a, b) =>
        {
            return b.priority - a.priority;
            //return a.priority - b.priority;
        });

        return results;
    }

    private static DictionaryEntry restrict_by_text(DictionaryEntry entry, string text)
    {
        // deep clone lol (we should probably do this WAY earlier)
        var term = new DictionaryEntry();

        term.seq = entry.seq;
        term.k_ele = entry.k_ele;
        term.r_ele = entry.r_ele;
        term.sense = entry.sense;

        term.from = entry.from;
        term.found = entry.found;
        term.orig_found = entry.orig_found;
        term.deconj = entry.deconj;
        term.allpos = entry.allpos;
        term.priority = entry.priority;
        term.priorityTags = entry.priorityTags;
        term.has_audio = entry.has_audio;
        term.freq = entry.freq;

        if (term.found == null) // bogus lookup
            return term;

        // Example restrictions:
        //  ゆう: 夕 only
        //  さくや: 昨夜 only
        //  evening: 夕・夕べ only
        //  last night: ゆうべ・さくや only
        // Derived:
        //  昨夜： さくや・ゆうべ only
        //  夕： ゆう・ゆうべ only
        //  夕べ： ゆうべ only
        //  evening: ゆう・ゆうべ only
        //  last night: 夕べ・昨夜 only

        // if we didn't look up kanji, look for the first fitting kanji spellings in the entry (they're ordered sensibly) if there are kanji spellings
        term.orig_found = term.found;

        if (term.found.reb != null && term.k_ele.Count > 0)
        {
            List<string> r_restr = null;
            // kanji to which the reb is restricted
            if (term.found.restr.Count > 0)
                r_restr = term.found.restr;
            else
                r_restr = new List<string>();

            // find the first kanji-including spelling that isn't restricted to readings that aren't the one we looked up
            for (var j = 0; j < term.k_ele.Count; j++)
            {
                if(term.k_ele[j].restr.Count > 0)
                {
                    // if this spelling is restricted to particular readings
                    for (var l = 0; l < term.k_ele[j].restr.Count; l++)
                    {
                        // if the reading we looked up is one of the ones it's restricted to
                        // if the reading we looked up isn't restricted to spellings other than this one
                        if (term.k_ele[j].restr[l] == term.found.reb && (r_restr.Count == 0 || r_restr.IndexOf(term.k_ele[j].keb) > -1))
                        {
                            // pretend we looked up this spelling
                            term.found = term.k_ele[j];
                            break;
                        }
                    }
                }
                // if the spelling does not have restrictions
                // if the reading is not restricted to other spellings
                else if (r_restr.Count == 0 || r_restr.IndexOf(term.k_ele[j].keb) > -1)
                {
                    // pretend we looked up this spelling
                    term.found = term.k_ele[j];
                    break;
                }
            }
        }

        // eliminate unfitting kanji spellings if we originally looked up a reading
        if (term.orig_found.reb != null && term.k_ele.Count > 0)
        {
            List<string> r_restr = null;
            // kanji to which the reb is restricted
            if (term.orig_found.restr.Count > 0)
                r_restr = term.orig_found.restr;
            else
                r_restr = new List<string>();

            var new_k_ele = new List<DictionaryEleEntry>();

            for (var j = 0; j < term.k_ele.Count; j++)
            {
                if (r_restr.Count > 0 && r_restr.IndexOf(term.k_ele[j].keb) < 0)
                    continue;
                // if this spelling is restricted to particular readings
                if (term.k_ele[j].restr.Count > 0)
                {
                    for (var l = 0; l < term.k_ele[j].restr.Count; l++)
                    {
                        // if the reading we looked up is one of the ones it's restricted to
                        // if the reading we looked up isn't restricted to spellings other than this one
                        if (term.k_ele[j].restr[l] == term.orig_found.reb)
                            new_k_ele.Add(term.k_ele[j]);
                    }
                }
                // if the spelling does not have restrictions
                // if the reading is not restricted to other spellings
                else
                {
                    new_k_ele.Add(term.k_ele[j]);
                }   
            }

            term.k_ele = new_k_ele;
        }
        // eliminate unfitting readings if we originally looked up a spelling
        if (term.orig_found.keb != null && term.r_ele.Count > 0)
        {
            List<string> k_restr = null;
            // kanji to which the reb is restricted
            if (term.orig_found.restr.Count > 0)
                k_restr = term.orig_found.restr;
            else
                k_restr = new List<string>();

            var new_r_ele = new List<DictionaryEleEntry>();

            for (var j = 0; j < term.r_ele.Count; j++)
            {
                if (k_restr.Count > 0 && k_restr.IndexOf(term.r_ele[j].reb) < 0)
                    continue;

                if (term.r_ele[j].restr.Count > 0)
                {
                    for (var l = 0; l < term.r_ele[j].restr.Count; l++)
                        if (term.r_ele[j].restr[l] == term.orig_found.keb)
                            new_r_ele.Add(term.r_ele[j]);
                }
                else
                    new_r_ele.Add(term.r_ele[j]);
            }

            term.r_ele = new_r_ele;
        }
        // eliminate unfitting definitions for the original lookup
        if (term.sense.Count > 0)
        {
            var new_sense = new List<DictionarySenseEntry>();
            for (var j = 0; j < term.sense.Count; j++)
            {
                if (term.orig_found.keb != null && term.sense[j].stagk.Count > 0)
                {
                    for (var l = 0; l < term.sense[j].stagk.Count; l++)
                        if (term.sense[j].stagk[l] == term.orig_found.keb)
                            new_sense.Add(term.sense[j]);
                }
                else if (term.orig_found.reb != null && term.sense[j].stagr.Count > 0)
                {
                    for (var l = 0; l < term.sense[j].stagr.Count; l++)
                        if (term.sense[j].stagr[l] == term.orig_found.reb)
                            new_sense.Add(term.sense[j]);
                }

                // if the spelling does not have restrictions
                // if the reading is not restricted to other spellings
                else
                    new_sense.Add(term.sense[j]);
            }
            term.sense = new_sense;
        }

        return term;
    }

    private static List<Definitions> add_extra_info(List<Definitions> results)
    {
        foreach (var lookup in results)
        {
            foreach (var entry in lookup.result)
            {
                entry.has_audio = new List<string>();

                if (entry.k_ele.Count == 0)
                {
                    foreach (var r in entry.r_ele)
                    {
                        if (LookupAudio.Contains(r.reb))
                            entry.has_audio.Add(r.reb);
                        else if (LookupAudioBroken.ContainsKey(r.reb))
                            entry.has_audio.Add(LookupAudioBroken[r.reb]);
                    }
                }
                else 
                {
                    foreach (var k in entry.k_ele)
                    {
                        foreach (var r in entry.r_ele)
                        {
                            var test_string = r.reb + ";" + k.keb;

                            if (LookupAudio.Contains(test_string))
                                entry.has_audio.Add(test_string);
                            else if (LookupAudioBroken.ContainsKey(test_string))
                                entry.has_audio.Add(LookupAudioBroken[test_string]);
                        }
                    }
                }
            }
        }

        foreach (var lookup in results)
        {
            foreach (var entry in lookup.result)
            {
                var seen = new HashSet<string>();

                foreach (var r in entry.r_ele)
                    seen.Add(r.reb);

                foreach (var k in entry.k_ele)
                    seen.Add(k.keb);

                foreach (var r in entry.r_ele)
                {
                    var reading = replace_kata_with_hira(r.reb);
                    var freq = FrequencyNovels;

                    if (freq.ContainsKey(reading))
                    {
                        foreach (var possibility in freq[reading])
                        {
                            if (seen.Contains(possibility.freq0))
                            {
                                if (reading == possibility.freq0 && entry.k_ele.Count > 0)
                                    continue;

                                if (entry.freq == null)
                                {
                                    entry.freq = new FrequencyEntry() { freq0 = possibility.freq0, reading = reading, freq1 = possibility.freq1, freq2 = possibility.freq2 };
                                }
                                else
                                {
                                    if (entry.freq.freq1 > possibility.freq1)
                                    {
                                        entry.freq = new FrequencyEntry() { freq0 = possibility.freq0, reading = reading, freq1 = possibility.freq1, freq2 = possibility.freq2 };
                                    }   
                                }
                            }
                        }
                    }
                }
            }
        }

        return results;
    }

    private static List<Definitions> skip_rereferenced_entries(List<Definitions> results)
    {
        var newresults = new List<Definitions>();
        var seenseq = new HashSet<int>();

        foreach (var lookup in results)
        {
            var newlookup = new List<DictionaryEntry>();

            foreach (var entry in lookup.result)
            {
                if (seenseq.Contains(entry.seq))
                    continue;

                seenseq.Add(entry.seq);
                newlookup.Add(restrict_by_text(entry, lookup.text));
            }

            if (newlookup.Count > 0)
            {
                newresults.Add(new Definitions() { input = lookup.input, text = lookup.text, result = newlookup, followup = lookup.followup });
            } 
        }

        return add_extra_info(newresults); // add extra information like json results and audio data now
    }

    private static int MaxDeadEndLength = 15;
    private static HashSet<char> PunctuationCharacters = new HashSet<char>()
    {
        ' ', '.', '?', '!', ',', ';', ':', '(', ')', '[', ']', '{', '}', '⟨', '⟩', '‘', '“', '”', '‘', '’', '"', '/', '\\', // English marks
        '（', '）', '｛', '｝', '［', '］', '【', '】', '、', '，', '゠', '＝', '…', '‥', '。', '〽', '「', '」', '『', '』', '〝', '〟', '⟨', '⟩', '〜', '：',
        '！', '？', '♪',
        '\r', '\n',
    };

    private static HashSet<char> Digits = new HashSet<char>()
    {
        '０', '0', '１', '1', '２', '2', '３', '3', '４', '4', '５', '5', '６', '6','７', '7', '８', '8', '９', '9',
    };

    private static Dictionary<string, Dictionary<int, List<Definitions>>> CachedResults = new Dictionary<string, Dictionary<int, List<Definitions>>>();

    public static string ReplaceFirst(string text, string search, string replace)
    {
        int pos = text.IndexOf(search);
        if (pos < 0)
        {
            return text;
        }
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    private static List<Definitions> LookupText(string text, int depth)
    {
        var originaltext = text;

        if(CachedResults.ContainsKey(originaltext) &&  CachedResults[originaltext].ContainsKey(depth))
        {
            return CachedResults[originaltext][depth];
        }

        //Logger.Log("Cache miss " + text);

        if(!text.Contains("っ") && text.Contains("つ"))
        {
            var replaced = ReplaceFirst(text, "つ", "っ");
            var result = LookupText(replaced, depth);

            if(result.Count > 0 && result[0].text.Contains("っ"))
            {
                return result;
            }
        }

        var results = new List<Definitions>();
        var second_pass = false;

        while(text.Length > 0 && (PunctuationCharacters.Contains(text[0]) || Digits.Contains(text[0])))
        {
            text = text.Substring(1);
        }

        var maxlength = Math.Min(text.Length, MaxDeadEndLength);

        for(var j = maxlength - 1; j > 0; j--)
        {
            if(PunctuationCharacters.Contains(text[j]))
            {
                maxlength = j;
            }
        }

        var i = Math.Min(text.Length, maxlength);

        while (i > 0)
        {
            var currenttext = text.Substring(0, i);

            var forms = deconjugate(currenttext);
            var result = build_lookup_comb(forms);

            if (!second_pass && is_kana(currenttext))
            {
                result = filter_kana_ish_results(result);
            }

            if (result.Count > 0)
            {
                try
                {
                    result = sort_results(currenttext, result);
                }
                catch (Exception)
                {
                    Logger.LogWarning("Failed to sort dictionary results");
                }

                results.Add(new Definitions() { input = originaltext, text = currenttext, result = result, followup = text.Substring(i) });
            }

            i--;

            if(!second_pass)
            {
                if (i <= 0 && results.Count == 0)
                {
                    i = maxlength;
                    second_pass = true;
                }
            }

            if(results.Count >= depth)
            {
                break;
            }
        }

        if (results.Count > 0)
        {
            results = skip_rereferenced_entries(results);
        }
        else if (text.Length > 0)
        {
            results = LookupText(text.Substring(1), depth);
        }

        if(!CachedResults.ContainsKey(originaltext))
        {
            CachedResults.Add(originaltext, new Dictionary<int, List<Definitions>>());
        }

        foreach(var result in results)
        {
            result.ComputeKanaString();
        }

        CachedResults[originaltext].Add(depth, results);

        return results;
    }

    private static List<DictionaryEntry> DictionaryEntries = new List<DictionaryEntry>();
    private static Dictionary<string, List<int>> LookupKanji = new Dictionary<string, List<int>>();
    private static Dictionary<string, List<int>> LookupKana = new Dictionary<string, List<int>>();
    private static HashSet<string> LookupAudio = new HashSet<string>();
    private static Dictionary<string, string> LookupAudioBroken = new Dictionary<string, string>();
    private static Dictionary<string, KanjiDataStruct> KanjiData = new Dictionary<string, KanjiDataStruct>();
    private static List<PriorityRule> PriorityRules = new List<PriorityRule>();
    private static Dictionary<string, List<FrequencyEntry>> FrequencyNovels = new Dictionary<string, List<FrequencyEntry>>();

    private static Dictionary<char, char> FullWidthToHalfWidthMap = new Dictionary<char, char>()
    {
        { '　', ' '},{ '！','!'},{ '＂', '"'},{ '＃', '#'},{ '＄', '$'},{ '％', '%'},{ '＆', '&'}, {'＇', '\''},{ '（', '('},{ '）', ')'},{ '＊', '*'},{ '＋', '+'},{ '，', ','},{ '－', '-'},{'．', '.'},{ '／', '/'},
        {'０', '0'},{ '１', '1'},{ '２', '2'},{ '３', '3'},{ '４', '4'},{ '５', '5'},{ '６', '6'},{'７', '7'},{ '８', '8'},{ '９', '9'},
        {'：', ','},{ '；', ';'},{ '＜', '<'},{ '＝', '='},{ '＞', '>'},{ '？', '?'},{ '＠', '@'},
        {'Ａ', 'A'},{ 'Ｂ', 'B'},{ 'Ｃ', 'C'},{ 'Ｄ', 'D'},{ 'Ｅ', 'E'},{ 'Ｆ', 'F'},{ 'Ｇ', 'G'},{'Ｈ', 'H'},{ 'Ｉ', 'I'},{ 'Ｊ', 'J'},{ 'Ｋ', 'K'},{ 'Ｌ', 'L'},{ 'Ｍ', 'M'},{ 'Ｎ', 'N'},{'Ｏ', 'O'},{ 'Ｐ', 'P'},{ 'Ｑ', 'Q'},{ 'Ｒ', 'R'},{ 'Ｓ', 'S'},{ 'Ｔ', 'T'},{ 'Ｕ', 'U'},{'Ｖ', 'V'},{ 'Ｗ', 'W'},{ 'Ｘ', 'X'},{ 'Ｙ', 'Y'},{ 'Ｚ', 'Z'},
        {'［', '['},{ '＼', '\\'},{'］', ']'},{ '＾', '^'},{ '＿', '_'},{ '｀', '`'},
        {'ａ', 'a'},{ 'ｂ', 'b'},{ 'ｃ', 'c'},{ 'ｄ', 'd'},{ 'ｅ', 'e'},{ 'ｆ', 'f'},{ 'ｇ', 'g'},{'ｈ', 'h'},{ 'ｉ', 'i'},{ 'ｊ', 'j'},{ 'ｋ', 'k'},{ 'ｌ', 'l'},{ 'ｍ', 'm'},{ 'ｎ', 'n'},{'ｏ', 'o'},{ 'ｐ', 'p'},{ 'ｑ', 'q'},{ 'ｒ', 'r'},{ 'ｓ', 's'},{ 'ｔ', 't'},{ 'ｕ', 'u'},{'ｖ', 'v'},{ 'ｗ', 'w'},{ 'ｘ', 'x'},{ 'ｙ', 'y'},{ 'ｚ', 'z'},
        {'｛', '{'},{ '｜', '|'},{ '｝', '}'}
    };

    public static void LoadRelevantFiles()
    {
        var deconjugatorstring = NazekaFilesLogic.LoadedFiles["deconjugator"];
        var deconjugator = JsonConvert.DeserializeObject<JArray>(deconjugatorstring);

        foreach(var child in deconjugator)
        {
            if(child.Type != JTokenType.Object)
            {
                continue;
            }

            var toadd = new DeconjugationRulesStruct();

            var objectchild = (JObject)child;

            foreach(var property in objectchild.Properties())
            {
                switch(property.Name)
                {
                    case "type": toadd.type = property.Value.Value<string>(); break;
                    case "contextrule": toadd.contextrule = property.Value.Value<string>(); break;
                    case "dec_end": if (property.Value.Type == JTokenType.String) toadd.dec_end.Add(property.Value.Value<string>()); else { toadd.dec_end = property.Value.ToObject<List<string>>(); toadd.dec_end_was_array = true; } break;
                    case "con_end": if (property.Value.Type == JTokenType.String) toadd.con_end.Add(property.Value.Value<string>()); else { toadd.con_end = property.Value.ToObject<List<string>>(); toadd.con_end_was_array = true; } break;
                    case "dec_tag": if (property.Value.Type == JTokenType.String) toadd.dec_tag.Add(property.Value.Value<string>()); else { toadd.dec_tag = property.Value.ToObject<List<string>>(); toadd.dec_tag_was_array = true; } break;
                    case "con_tag": if (property.Value.Type == JTokenType.String) toadd.con_tag.Add(property.Value.Value<string>()); else { toadd.con_tag = property.Value.ToObject<List<string>>(); toadd.con_tag_was_array = true; } break;
                    case "detail": toadd.detail = property.Value.Value<string>(); break;
                }

                if(toadd.con_end.Count > 0)
                {
                    toadd.con_end0 = toadd.con_end[0];
                }
            }

            toadd.GenerateVirtualDeconjugations();
            DeconjugationRules.Add(toadd);
        }

        var setdicoproperties = new HashSet<string>();

        foreach(var file in NazekaFilesLogic.LoadedFiles)
        {
            if(file.Key.ToLower().StartsWith("jmdict"))
            {
                var dicojson = JsonConvert.DeserializeObject<List<DictionaryEntry>>(file.Value);
                DictionaryEntries.AddRange(dicojson);
            }
        }

        for (int i = 0; i < DictionaryEntries.Count; ++i)
        {
            var entry = DictionaryEntries[i];
            entry.CleanFullWidth();

            foreach(var k_ele in entry.k_ele)
            {
                if(!LookupKanji.ContainsKey(k_ele.keb))
                {
                    LookupKanji.Add(k_ele.keb, new List<int> { i });
                }
                else
                {
                    LookupKanji[k_ele.keb].Add(i);
                }
            }

            foreach (var r_ele in entry.r_ele)
            {
                if (!LookupKanji.ContainsKey(r_ele.reb))
                {
                    LookupKanji.Add(r_ele.reb, new List<int> { i });
                }
                else
                {
                    LookupKanji[r_ele.reb].Add(i);
                }
            }
        }

        var audiotablecontent = NazekaFilesLogic.LoadedFiles["jdic audio"];
        {
            var i = 0;
            var j = 0;

            while ((j = audiotablecontent.IndexOf("\n", i)) != -1)
            {
                var subtext = audiotablecontent.Substring(i, j - i);

                if (!subtext.Contains(","))
                    LookupAudio.Add(subtext);
                else
                    LookupAudioBroken.Add(subtext.Split(',')[1], subtext.Split(',')[0]);

                i = j + 1;
            }

            var text = audiotablecontent.Substring(i, audiotablecontent.Length - i);

            if (!text.Contains(","))
                LookupAudio.Add(text);
            else
                LookupAudioBroken.Add(text.Split(',')[1], text.Split(',')[0]);
        }

        var kanjidata = NazekaFilesLogic.LoadedFiles["kanjidata"];
        var kanjidatajson = JsonConvert.DeserializeObject<List<KanjiDataStruct>>(kanjidata);

        foreach(var entry in kanjidatajson)
        {
            KanjiData.Add(entry.c, entry);
        }

        var priorityrules = NazekaFilesLogic.LoadedFiles["priority"];
        var priorityrulesjson = JsonConvert.DeserializeObject<List<JArray>>(priorityrules);

        foreach(var rule in priorityrulesjson)
        {
            if(rule.Count != 3)
            {
                Logger.LogWarning("Priority rule with wrong number of parameters");
                continue;
            }

            var priorityrule = new PriorityRule();

            var rule0 = rule[0];
            var rule1 = rule[1];
            var rule2 = rule[2];

            if (rule0.Type == JTokenType.String)
            {
                priorityrule.rule0 = rule0.Value<string>();
            }
            else
            {
                Logger.LogWarning("Priority rule formatting issues");
                continue;
            }

            if (rule1.Type == JTokenType.String)
            {
                priorityrule.rule1 = rule1.Value<string>();
            }
            else
            {
                Logger.LogWarning("Priority rule formatting issues");
                continue;
            }

            if (rule2.Type == JTokenType.Integer)
            {
                priorityrule.rule2 = rule2.Value<int>();
            }
            else
            {
                Logger.LogWarning("Priority rule formatting issues");
                continue;
            }

            PriorityRules.Add(priorityrule);
        }

        var frequencynovel = NazekaFilesLogic.LoadedFiles["freqlist_novels"];
        var frequencynoveljson = JsonConvert.DeserializeObject<Dictionary<string, List<JArray>>>(frequencynovel);

        foreach(var entry in frequencynoveljson)
        {
            var reslist = new List<FrequencyEntry>();

            foreach(var list in entry.Value)
            {
                if (list.Count != 3)
                {
                    Logger.LogWarning("Frequency with wrong number of parameters");
                    continue;
                }

                var frequency = new FrequencyEntry();

                var freq0 = list[0];
                var freq1 = list[1];
                var freq2 = list[2];

                if (freq0.Type == JTokenType.String)
                {
                    frequency.freq0 = freq0.Value<string>();
                }
                else
                {
                    Logger.LogWarning("Frequency formatting issues");
                    continue;
                }

                if (freq1.Type == JTokenType.Integer)
                {
                    frequency.freq1 = freq1.Value<int>();
                }
                else
                {
                    Logger.LogWarning("Frequency formatting issues");
                    continue;
                }

                if (freq2.Type == JTokenType.Float)
                {
                    frequency.freq2 = freq2.Value<float>();
                }
                else
                {
                    Logger.LogWarning("Frequency formatting issues");
                    continue;
                }

                reslist.Add(frequency);
            }

            FrequencyNovels.Add(entry.Key, reslist);;
        }
    }

    private static List<string> SplitTextForLookup(string text)
    {
        var output = new List<string>();
        output.Add("");

        bool lastwaspunctuation = false;

        for (int i = 0; i < text.Length; ++i)
        { 
            if(PunctuationCharacters.Contains(text[i]))
            {
                if(!lastwaspunctuation)
                {
                    output.Add("");
                }
                
                lastwaspunctuation = true;
            }
            else
            {
                output[output.Count - 1] = output[output.Count - 1] + text[i];
                lastwaspunctuation = false;
            }
        }

        return output;
    }

    public static List<Definitions> FindDefinitionsInText(string text, ref List<string> textsplit, ref List<FuriganaPlacement> furiganas)
    {
        var outputs = new List<Definitions>();

        if (textsplit != null)
        {
            textsplit.Clear();
        }

        for (int i = 0; i < 1; ++i)
        {
            var splittext = SplitTextForLookup(text);

            foreach (var subtext in splittext)
            {
                if(subtext.Length <= 0)
                {
                    continue;
                }

                if(textsplit != null)
                {
                    textsplit.Add(subtext);
                }

                var tempoutputs = new List<Definitions>();

                var subtexts = new List<string>() { subtext };
                var alreadydone = new HashSet<string>();

                while (subtexts.Count > 0)
                {
                    var nexttext = subtexts[0];
                    subtexts.RemoveAt(0);

                    var output = LookupText(nexttext, 10);
                    tempoutputs.AddRange(output);

                    alreadydone.Add(nexttext);

                    bool firstfollowup = true;

                    foreach (var result in output)
                    {
                        if (result.followup.Length > 0 && !alreadydone.Contains(result.followup))
                        {
                            if(firstfollowup)
                            {
                                subtexts.Insert(0, result.followup);
                                firstfollowup = false;
                            }
                            else
                            {
                                subtexts.Add(result.followup);
                            }
                        }

                        foreach(var previousresult in tempoutputs)
                        {
                            if(result.input == previousresult.followup && previousresult.followupstruct == null)
                            {
                                previousresult.followupstruct = result;
                            }
                        }
                    }
                }

                outputs.AddRange(tempoutputs);
            }
        }

        if(textsplit != null)
        {
            var substringtokana = new Dictionary<string, string>();
            var substringfurigana = new Dictionary<string, List<FuriganaPlacement>>();

            foreach (var substring in textsplit)
            {
                var tempsubstring = substring;

                foreach (var result in outputs)
                {
                    if (result.input == substring)
                    {
                        var currentresult = result;

                        while (currentresult != null)
                        {
                            int indexoriginal = substring.IndexOf(currentresult.text);

                            while (indexoriginal >= 0)
                            {
                                foreach (var rule in currentresult.ReplacementRules)
                                {
                                    var indexreplacement = substring.IndexOf(rule.Key, indexoriginal);

                                    while (indexreplacement >= 0)
                                    {
                                        if (indexreplacement < indexoriginal + currentresult.text.Length)
                                        {
                                            if (!substringfurigana.ContainsKey(substring))
                                            {
                                                substringfurigana.Add(substring, new List<FuriganaPlacement>());
                                            }

                                            substringfurigana[substring].Add(new FuriganaPlacement() { Original = rule.Key, Replacement = rule.Value, Start = indexreplacement });
                                        }
                                        else
                                        {
                                            break;
                                        }

                                        indexreplacement = substring.IndexOf(currentresult.text, indexreplacement + 1);
                                    }
                                }

                                indexoriginal = substring.IndexOf(currentresult.text, indexoriginal + 1);
                            }

                            tempsubstring = tempsubstring.Replace(currentresult.text, currentresult.kanastring);
                            currentresult = currentresult.followupstruct;
                        }
                    }
                }

                substringtokana.TryAdd(substring, tempsubstring);
            }

            if (furiganas != null)
            {
                furiganas.Clear();
                var nextmin = 0;

                while (nextmin < text.Length)
                {
                    var nextsubstring = text.Substring(nextmin);
                    var found = false;

                    foreach (var entry in substringfurigana)
                    {
                        if (nextsubstring.StartsWith(entry.Key))
                        {
                            foreach(var f in entry.Value)
                            {
                                furiganas.Add(new FuriganaPlacement() { Original = f.Original, Replacement = f.Replacement, Start = f.Start + nextmin });
                            }

                            nextmin += entry.Key.Length;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        nextmin += 1;
                    }
                }
            }

            var finalkana = text;

            foreach (var entry in substringtokana)
            {
                finalkana = finalkana.Replace(entry.Key, entry.Value);
            }

            //Logger.Log(text + " => " + finalkana);
        }

        return outputs;
    }
}