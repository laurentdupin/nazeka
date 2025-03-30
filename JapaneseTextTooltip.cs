using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;
using System.ArrayExtensions;

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

public class JapaneseTextTooltip
{
    private class DeconjugationRulesStruct
    {
        public string type;
        public string contextrule;
        public List<string> dec_end = new List<string>();
        public bool dec_end_was_array = false;
        public List<string> con_end = new List<string>();
        public bool con_end_was_array = false;
        public List<string> dec_tag = new List<string>();
        public bool dec_tag_was_array = false;
        public List<string> con_tag = new List<string>();
        public bool con_tag_was_array = false;
        public string detail;
    }

    private class DeconjugationNovel
    {
        public string text;
        public string original_text;
        public List<string> tags = new List<string>();
        public HashSet<string> seentext = new HashSet<string>();
        public List<string> process = new List<string>();
    }

    private class DictionaryEleEntry
    {
        public string reb;
        public string keb;
        public List<string> restr = new List<string>();
        public List<string> pri = new List<string>();
        public List<string> inf = new List<string>();
    }

    private class DictionarySenseEntry
    {
        public List<string> pos = new List<string>();
        public List<string> misc = new List<string>();
        public List<string> gloss = new List<string>();
        public List<string> inf = new List<string>();
        public List<string> dial = new List<string>();
        public List<string> stagk = new List<string>();
        public List<string> stagr = new List<string>();
    }

    private class DictionaryEntry
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

    private class ResultStruct
    {
        public string text;
        public List<DictionaryEntry> result;
    }

    private class PriorityRule
    {
        public string rule0;
        public string rule1;
        public int rule2;
    }

    private class FrequencyEntry
    {
        public string freq0;
        public int freq1;
        public float freq2;

        public string reading;
    }

    private static List<DeconjugationRulesStruct> DeconjugationRules = new List<DeconjugationRulesStruct>();

    private static DeconjugationNovel stdrule_deconjugate_inner(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (!my_form.text.EndsWith(my_rule.con_end[0])) return null;
        if (my_form.tags.Count > 0 && my_form.tags.Last() != my_rule.con_tag[0]) return null;

        var newtext = my_form.text.Substring(0, my_form.text.Length - my_rule.con_end[0].Length) + my_rule.dec_end[0];

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

    private static List<DeconjugationNovel> stdrule_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.text == "") return null;
        if (my_form.text.Length > my_form.original_text.Length + 10) return null;
        if (my_form.tags.Count > my_form.original_text.Length + 6) return null;
        if (my_rule.detail == "" && my_form.tags.Count == 0) return null;

        List<string> array = null;

        if (my_rule.dec_end_was_array)
            array = my_rule.dec_end;
        else if (my_rule.con_end_was_array)
            array = my_rule.con_end;
        else if (my_rule.dec_tag_was_array)
            array = my_rule.dec_tag;
        else if (my_rule.con_tag_was_array)
            array = my_rule.con_tag;

        var collection = new List<DeconjugationNovel>();

        if (array == null)
        {
            collection.Add(stdrule_deconjugate_inner(my_form, my_rule));
        }
        else
        {
            for(int i = 0; i < array.Count; i++)
            {
                var virtual_rule = new DeconjugationRulesStruct();
                virtual_rule.type = my_rule.type;
                virtual_rule.detail = my_rule.detail;

                if (my_rule.dec_end_was_array) virtual_rule.dec_end.Add(my_rule.dec_end[i]); else virtual_rule.dec_end.Add(my_rule.dec_end[0]);
                if (my_rule.con_end_was_array) virtual_rule.con_end.Add(my_rule.con_end[i]); else virtual_rule.con_end.Add(my_rule.con_end[0]);
                if (my_rule.dec_tag_was_array) virtual_rule.dec_tag.Add(my_rule.dec_tag[i]); else virtual_rule.dec_tag.Add(my_rule.dec_tag[0]);
                if (my_rule.con_tag_was_array) virtual_rule.con_tag.Add(my_rule.con_tag[i]); else virtual_rule.con_tag.Add(my_rule.con_tag[0]);

                var ret = stdrule_deconjugate_inner(my_form, virtual_rule);

                if (ret != null)
                {
                    collection.Add(ret);
                }
            }
        }

        return collection;
    }

    private static List<DeconjugationNovel> rewriterule_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.text != my_rule.con_end[0])
        {
            return null;
        }
            
        return stdrule_deconjugate(my_form, my_rule);
    }

    private static List<DeconjugationNovel> onlyfinalrule_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.tags.Count != 0)
        {
            return null;
        }

        return stdrule_deconjugate(my_form, my_rule);
    }

    private static List<DeconjugationNovel> neverfinalrule_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.tags.Count == 0)
        {
            return null;
        }

        return stdrule_deconjugate(my_form, my_rule);
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

        if (!my_form.text.EndsWith(my_rule.con_end[0])) return false;

        var base_text = my_form.text.Substring(0, my_form.text.Length - my_rule.con_end[0].Length);

        if (base_text.EndsWith("Ç≥"))
            return false;

        return true;
    }

    private static Dictionary<string, Func<DeconjugationNovel, DeconjugationRulesStruct, bool>> context_functions = new Dictionary<string, Func<DeconjugationNovel, DeconjugationRulesStruct, bool>>()
    {
        { "v1inftrap", v1inftrap_check },
        { "saspecial", saspecial_check }
    };

    private static List<DeconjugationNovel> contextrule_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (!context_functions[my_rule.contextrule](my_form, my_rule))
        {
            return null;
        }

        return stdrule_deconjugate(my_form, my_rule);
    }

    private static DeconjugationNovel substitution_inner(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (!my_form.text.Contains(my_rule.con_end[0])) return null;

        var newtext = Regex.Replace(my_form.text, my_rule.con_end[0], my_rule.dec_end[0], RegexOptions.None);

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

    private static List<DeconjugationNovel> substitution_deconjugate(DeconjugationNovel my_form, DeconjugationRulesStruct my_rule)
    {
        if (my_form.process.Count != 0) return null;

        if (my_form.text == "") return null;

        List<string> array = null;

        if (my_rule.dec_end_was_array)
            array = my_rule.dec_end;
        else if (my_rule.con_end_was_array)
            array = my_rule.con_end;

        var collection = new List<DeconjugationNovel>();

        if (array == null)
        {
            collection.Add(substitution_inner(my_form, my_rule));
        }
        else
        {
            for (int i = 0; i < array.Count; i++)
            {
                var virtual_rule = new DeconjugationRulesStruct();
                virtual_rule.type = my_rule.type;
                virtual_rule.detail = my_rule.detail;

                if (my_rule.dec_end_was_array) virtual_rule.dec_end.Add(my_rule.dec_end[i]); else virtual_rule.dec_end.Add(my_rule.dec_end[0]);
                if (my_rule.con_end_was_array) virtual_rule.con_end.Add(my_rule.con_end[i]); else virtual_rule.con_end.Add(my_rule.con_end[0]);

                var ret = substitution_inner(my_form, virtual_rule);

                if (ret != null)
                {
                    collection.Add(ret);
                }
            }
        }

        return collection;
    }

    private static Dictionary<string, Func<DeconjugationNovel, DeconjugationRulesStruct, List<DeconjugationNovel>>> rule_functions = new Dictionary<string, Func<DeconjugationNovel, DeconjugationRulesStruct, List<DeconjugationNovel>>>()
    {
        { "stdrule", stdrule_deconjugate },
        { "rewriterule", rewriterule_deconjugate },
        { "onlyfinalrule", onlyfinalrule_deconjugate },
        { "neverfinalrule", neverfinalrule_deconjugate },
        { "contextrule", contextrule_deconjugate },
        { "substitution", substitution_deconjugate }
    };

    private static HashSet<DeconjugationNovel> deconjugate(string mytext)
    {
        var processed = new HashSet<DeconjugationNovel>();
        var novel = new HashSet<DeconjugationNovel>();
    
        var start_form = new DeconjugationNovel() { text = mytext, original_text = mytext, tags = new List<string>(), seentext = new HashSet<string>(), process = new List<string>() };
        novel.Add(start_form);
    
        while(novel.Count > 0)
        {
            var new_novel = new HashSet<DeconjugationNovel>();

            foreach(var form in novel)
            {
                foreach(var rule in DeconjugationRules)
                {
                    var newform = rule_functions[rule.type](form, rule);
                
                    if(newform != null)
                    {
                        foreach(var myform in newform)
                        {
                            if(myform != null && !processed.Contains(myform) && !novel.Contains(myform) && !new_novel.Contains(myform))
                                new_novel.Add(myform);
                        }
                    }
                }
            }

            processed.UnionWith(novel);
            novel = new_novel;
        }
    
        return processed;
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
            if (!(codepoint >= 0x3040 && codepoint <= 0x30FF))
                return false;
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
            if (reading_kana == result_kana && entry.deconj.Count == 0) // !entry.deconj fixes Ç»Ç¡Çƒ
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
        var term = ObjectExtensions.Copy<DictionaryEntry>(entry);

        if (term.found == null) // bogus lookup
            return term;

        // Example restrictions:
        //  Ç‰Ç§: ó[ only
        //  Ç≥Ç≠Ç‚: çñÈ only
        //  evening: ó[ÅEó[Ç◊ only
        //  last night: Ç‰Ç§Ç◊ÅEÇ≥Ç≠Ç‚ only
        // Derived:
        //  çñÈÅF Ç≥Ç≠Ç‚ÅEÇ‰Ç§Ç◊ only
        //  ó[ÅF Ç‰Ç§ÅEÇ‰Ç§Ç◊ only
        //  ó[Ç◊ÅF Ç‰Ç§Ç◊ only
        //  evening: Ç‰Ç§ÅEÇ‰Ç§Ç◊ only
        //  last night: ó[Ç◊ÅEçñÈ only

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

    private static List<ResultStruct> add_extra_info(List<ResultStruct> results)
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

    private static List<ResultStruct> skip_rereferenced_entries(List<ResultStruct> results)
    {
        var newresults = new List<ResultStruct>();
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
                newresults.Add(new ResultStruct() { text = lookup.text, result = newlookup });
            } 
        }

        return add_extra_info(newresults); // add extra information like json results and audio data now
    }

    private static List<ResultStruct> LookupText(string text, bool strict_alternatives)
    {
        var results = new List<ResultStruct>();
        var first = true;

        while (text.Length > 0)
        {
            var forms = deconjugate(text);
            var result = build_lookup_comb(forms);

            if (!first && strict_alternatives && is_kana(text))
            {
                result = filter_kana_ish_results(result);
            }

            if (result.Count > 0)
            {
                try
                {
                    result = sort_results(text, result);
                }
                catch (Exception)
                {
                    UnityEngine.Debug.LogWarning("Failed to sort dictionary results");
                }

                results.Add(new ResultStruct() { text = text, result = result});
            }

            text = text.Substring(0, text.Length - 1);

            if (results.Count > 0)
            {
                first = false;
            } 
        }

        if (results.Count > 0)
        {
            return skip_rereferenced_entries(results);
        }

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

    private static void LoadRelevantFiles()
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
            }

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
                    LookupAudioBroken.Add(subtext.Split(",")[1], subtext.Split(",")[0]);

                i = j + 1;
            }

            var text = audiotablecontent.Substring(i, audiotablecontent.Length - i);

            if (!text.Contains(","))
                LookupAudio.Add(text);
            else
                LookupAudioBroken.Add(text.Split(",")[1], text.Split(",")[0]);
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
                UnityEngine.Debug.LogWarning("Priority rule with wrong number of parameters");
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
                UnityEngine.Debug.LogWarning("Priority rule formatting issues");
                continue;
            }

            if (rule1.Type == JTokenType.String)
            {
                priorityrule.rule1 = rule1.Value<string>();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Priority rule formatting issues");
                continue;
            }

            if (rule2.Type == JTokenType.Integer)
            {
                priorityrule.rule2 = rule2.Value<int>();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Priority rule formatting issues");
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
                    UnityEngine.Debug.LogWarning("Frequency with wrong number of parameters");
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
                    UnityEngine.Debug.LogWarning("Frequency formatting issues");
                    continue;
                }

                if (freq1.Type == JTokenType.Integer)
                {
                    frequency.freq1 = freq1.Value<int>();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Frequency formatting issues");
                    continue;
                }

                if (freq2.Type == JTokenType.Float)
                {
                    frequency.freq2 = freq2.Value<float>();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Frequency formatting issues");
                    continue;
                }
            }

            FrequencyNovels.Add(entry.Key, reslist);;
        }
    }

    public static void TestFunctionnality()
    {
        LoadRelevantFiles();
        var output = LookupText("é©ìÆé‘25Åìä÷ê≈î≠ï\Ç≈ÉJÉiÉ_éÒëäÅgïÒïúë[íuÅhã≠í≤", true);
        UnityEngine.Debug.Log(output.Count);
    }
}