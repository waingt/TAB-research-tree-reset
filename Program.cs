using CommandLine;
using CommandLine.Text;
using DXVision.Serialization;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

[assembly: AssemblyVersion("2.0")]
[assembly: AssemblyTitle("TABRTreset")]
[assembly: AssemblyProduct("TABRTreset")]
[assembly: AssemblyCopyright("TABRTreset is a tool to crack encryption mechanism in game They Are Billions.\nThe initial purpose of it is to reset research tree.In fact its fullname is They Are Billions research tree reset.\nCurrently works on Steam Editon V.1.1.3.It ensures no backward compatibility.\n")]

namespace TAB_researchtreereset
{
    public class BaseOption
    {
        [Option(Required = false, HelpText = @"specify the path to save folder, default: (MyDocuments)\My Games\They Are Billions\Saves\")]
        public string save_folder { get; set; }
        [Option(Required = false, HelpText = @"specify the path to TAB game folder, default: (Steam installtion path)\steamapps\common\They Are Billions\")]
        public string tab_folder { get; set; }
        internal MethodInfo pswd_gen_method, check_gen_method;
        public virtual void Run()
        {
            try
            {
                if (string.IsNullOrEmpty(tab_folder))
                    tab_folder = Path.Combine((string)Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam").GetValue("SteamPath"), @"steamapps\common\They Are Billions\");
                if (!Directory.Exists(tab_folder)) throw new DirectoryNotFoundException(tab_folder);
            }
            catch (Exception e)
            {
                throw new Exception("cannot find TAB folder automatically\n consider specify TAB folder\n use \"TABRTreset help\" to get more help\n" + e.ToString(), e);
            }
            try
            {
                if (string.IsNullOrEmpty(save_folder))
                    save_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\They Are Billions\Saves\");
                if (!Directory.Exists(save_folder)) throw new DirectoryNotFoundException(save_folder);
            }
            catch (Exception e) { throw new Exception("cannot find save folder automatically\n consider specify save folder\n use \"TABRTreset help\" to get more help\n" + e.ToString(), e); }

            // TODO search for methods in TAB executive using method signatures & IL code instead of method names for backward compatibility
            var tab_asmb = Assembly.LoadFrom(Path.Combine(tab_folder, "TheyAreBillions.exe"));
            var serializer_class = tab_asmb.GetType("#=zBitRis$e$O2Cn5vz3w==");
            pswd_gen_method = serializer_class.GetMethod("#=zAmh0ZrvDqn3lwW$PWQ==", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("cannot find password generate method");
            check_gen_method = serializer_class.GetMethod("#=zPYy9RcqnMpGU", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("cannot find check generate method");
        }
        public DXVision.DXTableManager read_dat(string dat_name)
        {
            ZipSerializer.Current.Password = get_dat_pswd(dat_name);
            return (DXVision.DXTableManager)ZipSerializer.Read(Path.Combine(tab_folder, Path.GetFileNameWithoutExtension(dat_name) + ".dat"));
        }

        public string get_save_pswd(string save_path)
        {
            pswd_gen_method.Invoke(null, new object[] { save_path, 2, false });
            return ZipSerializer.Current.Password;
        }

        public string get_dat_pswd(string dat_path)
        {
            var pswd = DXVision.DXHelper_HashCode.From(Path.GetFileNameWithoutExtension(dat_path) + ".dat").ToString();
            pswd += DXVision.DXHelper_HashCode.From(pswd).ToString();
            pswd += DXVision.DXHelper_HashCode.From(pswd).ToString();
            return pswd;
        }
        public void generate_check(string save_path)
        {
            File.WriteAllText(Path.ChangeExtension(save_path, ".zxcheck"), check_gen_method.Invoke(null, new object[] { save_path, 2 }).ToString());
        }
        public void unpack(string save_path, bool IsSave)
        {
            var target_path = Path.Combine(Path.GetDirectoryName(save_path), Path.GetFileNameWithoutExtension(save_path));
            var t = new Ionic.Zip.ZipFile(save_path);
            t.Password = IsSave ? get_save_pswd(save_path) : get_dat_pswd(save_path);
            t.ExtractAll(target_path, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
        }

        public void pack(string folder_path, bool IsSave)
        {
            var target_path = Path.ChangeExtension(folder_path.TrimEnd(Path.DirectorySeparatorChar), IsSave ? ".zxsav" : ".dat");
            var t = new Ionic.Zip.ZipFile();
            t.Password = IsSave ? get_save_pswd(folder_path) : get_dat_pswd(folder_path);
            t.AddDirectory(folder_path);
            t.Save(target_path);
            if (IsSave) generate_check(target_path);
        }
    }
    public class BaseOptionWithSaveNameOrPath : BaseOption
    {
        [Value(0, Required = true, MetaName = "save_name_or_path")]
        public string save_name_or_path { get; set; }

        public override void Run()
        {
            base.Run();
            save_name_or_path = Path.IsPathRooted(save_name_or_path) ? save_name_or_path : Path.Combine(save_folder, Path.GetFileNameWithoutExtension(save_name_or_path) + ".zxsav");
        }
        public void ManipulateData(Action<XmlDocument> action)
        {
            var zipfile = new ZipFile(save_name_or_path) { Password = get_save_pswd(save_name_or_path) };
            var zipentry = zipfile["Data"];
            MemoryStream memoryStream = new MemoryStream();
            zipentry.Extract(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(memoryStream);
            action(xmlDocument);
            memoryStream = new MemoryStream();
            xmlDocument.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            zipfile.UpdateEntry("Data", memoryStream);
            zipfile.Save();
            generate_check(save_name_or_path);
        }

    }
    public class BaseOptionWithDatNameOrPath : BaseOption
    {
        [Value(0, Required = true, MetaName = "dat_name_or_path")]
        public string dat_name_or_path { get; set; }
        public override void Run()
        {
            base.Run();
            dat_name_or_path = Path.IsPathRooted(dat_name_or_path) ? dat_name_or_path : Path.Combine(tab_folder, Path.GetFileNameWithoutExtension(dat_name_or_path) + ".dat");
        }
    }
    public class BaseOptionWithSaveFolderNameOrPath : BaseOption
    {
        [Value(0, Required = true, MetaName = "save_folder_name_or_path")]
        public string save_folder_name_or_path { get; set; }
        public override void Run()
        {
            base.Run();
            save_folder_name_or_path = Path.IsPathRooted(save_folder_name_or_path) ? save_folder_name_or_path : Path.Combine(save_folder, save_folder_name_or_path);
        }
    }
    public class BaseOptionWithDatFolderNameOrPath : BaseOption
    {
        [Value(0, Required = true, MetaName = "dat_folder_name_or_path")]
        public string dat_folder_name_or_path { get; set; }
        public override void Run()
        {
            base.Run();
            dat_folder_name_or_path = Path.IsPathRooted(dat_folder_name_or_path) ? dat_folder_name_or_path : Path.Combine(tab_folder, dat_folder_name_or_path);
        }
    }

    [Verb("reset", HelpText = "read the save, delete all researched technology, add coresponding research points and write back to save.")]
    public class resetOption : BaseOptionWithSaveNameOrPath
    {
        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(resetOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new resetOption { save_name_or_path = "<save_name_or_path>" }) };
        }

        public override void Run()
        {
            base.Run();
            ManipulateData(xmlDocument =>
            {
                xmlDocument.SelectSingleNode("//Collection[@name=\"IDResearchsRecentUnlocked\"]").InnerXml = xmlDocument.SelectSingleNode("//Collection[@name=\"IDResearchUnlocked\"]").InnerXml;
            });
            return;
        }
    }

    [Verb("resetperk", HelpText = "reset hero perks")]
    public class resetperkOption : BaseOptionWithSaveNameOrPath
    {
        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(resetperkOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new resetperkOption { save_name_or_path = "<save_name_or_path>" }) };
        }
        public override void Run()
        {
            base.Run();
            ManipulateData(xmlDocument =>
            {
                xmlDocument.SelectSingleNode("//Collection[@name=\"HeroPerksTakenNow\"]").InnerXml = xmlDocument.SelectSingleNode("//Collection[@name=\"HeroPerksTaken\"]").InnerXml;
            });
            return;
        }
    }
    [Verb("gencheck", HelpText = "generate .zxcheck for specific file")]
    public class gencheckOption : BaseOptionWithSaveNameOrPath
    {
        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(gencheckOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new gencheckOption { save_name_or_path = "<save_name_or_path>" }) };
        }
        public override void Run()
        {
            base.Run();

            generate_check(save_name_or_path);
        }
    }

    [Verb("genpswd", HelpText = "generate all password of saves(*.zxsave) in savepswd.json, and generate all password of game data file(*.dat) in datpswd.json")]
    public class genpswdOption : BaseOption
    {
        public override void Run()
        {
            base.Run();


            File.WriteAllText("savepswd.json", Newtonsoft.Json.JsonConvert.SerializeObject(Directory.EnumerateFiles(save_folder, "*.zxsav").ToDictionary(Path.GetFileName, get_save_pswd), Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText("datpswd.json", Newtonsoft.Json.JsonConvert.SerializeObject(Directory.EnumerateFiles(tab_folder, "*.dat").ToDictionary(Path.GetFileName, get_dat_pswd), Newtonsoft.Json.Formatting.Indented));
        }
    }

    [Verb("unpacksave", HelpText = "unzip save(.zxsav) to a same name folder")]
    public class unpacksaveOption : BaseOptionWithSaveNameOrPath
    {

        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(unpacksaveOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new unpacksaveOption { save_name_or_path = "<save_name_or_path>" }) };
        }
        public override void Run()
        {
            base.Run();

            unpack(save_name_or_path, true);
        }
    }

    [Verb("unpackdat", HelpText = "unzip data(.dat) to a same name folder")]
    public class unpackdatOption : BaseOptionWithDatNameOrPath
    {
        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(unpackdatOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new unpackdatOption { dat_name_or_path = "<dat_name_or_path>" }) };
        }
        public override void Run()
        {
            base.Run();

            unpack(dat_name_or_path, false);
        }
    }

    [Verb("packsave", HelpText = "zip folder to a same name .zxsav file with proper password, and generate .zxcheck")]
    public class packsaveOption : BaseOptionWithSaveFolderNameOrPath
    {
        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(packsaveOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new packsaveOption { save_folder_name_or_path = "<save_folder_name_or_path>" }) };
        }
        public override void Run()
        {
            base.Run();

            pack(save_folder_name_or_path, true);
        }
    }

    [Verb("packdat", HelpText = "zip folder to a same name .dat file with proper password")]
    public class packdatOption : BaseOptionWithDatFolderNameOrPath
    {
        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(typeof(packdatOption).GetCustomAttribute<VerbAttribute>().HelpText + '\n', new packdatOption { dat_folder_name_or_path = "<dat_folder_name_or_path>" }) };
        }
        public override void Run()
        {
            base.Run();

            pack(dat_folder_name_or_path, false);
        }
    }

    public static class ObjectExtension
    {
        public static object GetProperty(this object obj, string property_name)
            => obj.GetType().GetProperty(property_name).GetValue(obj);
        public static void SetProperty(this object obj, string property_name, object value)
            => obj.GetType().GetProperty(property_name).SetValue(obj, value);
        public static void ModifyProperty(this object obj, string property_name, Func<object, object> func)
        {
            var p = obj.GetType().GetProperty(property_name);
            p.SetValue(obj, func(p.GetValue(obj)));
        }
        public static void ModifyProperties(this object obj, Func<Dictionary<string, object>, Dictionary<string, object>> func)
        {
            var dict = obj.GetType().GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name, p => p.GetValue(obj));
            var modify = func(dict);
            foreach (var pairs in modify) obj.SetProperty(pairs.Key, pairs.Value);
        }
    }
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                //interactive mode
                Console.WriteLine("Input the name of save for research tree reseting");
                var name = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(name)) throw new ArgumentNullException();
                new resetOption { save_name_or_path = name }.Run();
                Console.WriteLine("Succeeded. Press any key to continue");
                Console.ReadKey();
                return;
            }
            else
            {
                //command line mode
                Parser parser = Parser.Default;
                parser.ParseArguments(args, Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray()).WithParsed((BaseOption o) => o.Run()).WithNotParsed(e => Console.WriteLine());
            }
        }
    }
}
