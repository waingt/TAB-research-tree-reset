using CommandLine;
using CommandLine.Text;
using DXVision.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TAB_researchtreereset
{
    public class BaseOption
    {
        [Option(Required = false, HelpText = @"specify the path to save folder, default: (MyDocuments)\My Games\They Are Billions\Saves\")]
        public string save_folder { get; set; }
        [Option(Required = false, HelpText = @"specify the path to TAB game folder, default: (Steam installtion path)\steamapps\common\They Are Billions\")]
        public string tab_folder { get; set; }
        internal MethodInfo pswd_gen_method, check_gen_method;
    }
    public class BaseOptionWithSaveNameOrPath : BaseOption
    {
        [Value(0, Required = true, MetaName = "save_name_or_path")]
        public string save_name_or_path { get; set; }

        [Usage(ApplicationAlias = "TABRTreset")]
        public static IEnumerable<Example> Examples
        {
            get => new List<Example>() { new Example(" ", new resetOption { save_name_or_path = "<save_name_or_path>" }) };
        }
    }
    public class BaseOptionWithDatNameOrPath : BaseOption
    {
        [Value(0, Required = true)]
        public string dat_name_or_path { get; set; }
    }
    public class BaseOptionWithSaveFolderNameOrPath : BaseOption
    {
        [Value(0, Required = true)]
        public string save_folder_name_or_path { get; set; }
    }
    public class BaseOptionWithDatFolderNameOrPath : BaseOption
    {
        [Value(0, Required = true)]
        public string dat_folder_name_or_path { get; set; }
    }

    [Verb("reset", HelpText = "read the save, delete all researched technology, add coresponding research points and write back to save.")]
    public class resetOption : BaseOptionWithSaveNameOrPath
    {
    }

    [Verb("gencheck", HelpText = "generate .zxcheck for specific file")]
    public class gencheckOption : BaseOptionWithSaveNameOrPath { }

    [Verb("genpswd", HelpText = "generate all password of saves(*.zxsave) in savepswd.json, and generate all password of game data file(*.dat) in datpswd.json")]
    public class genpswdOption : BaseOption { }

    [Verb("unpacksave", HelpText = "unzip save(.zxsav) to a same name folder")]
    public class unpacksaveOption : BaseOptionWithSaveNameOrPath { }

    [Verb("unpackdat", HelpText = "unzip data(.dat) to a same name folder")]
    public class unpackdatOption : BaseOptionWithDatNameOrPath { }

    [Verb("packsave", HelpText = "zip folder to a same name .zxsav file with proper password, and generate .zxcheck")]
    public class packsaveOption : BaseOptionWithSaveFolderNameOrPath { }

    [Verb("packdat", HelpText = "zip folder to a same name .dat file with proper password")]
    public class packdatOption : BaseOptionWithDatFolderNameOrPath { }

    public static class ObjectExtension
    {
        public static object GetProperty(this object obj, string property_name)
            => obj.GetType().GetProperty(property_name).GetValue(obj);
        public static void SetProperty(this object obj, string property_name, object value)
            => obj.GetType().GetProperty(property_name).SetValue(obj, value);
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
                Run(new resetOption { save_name_or_path = name });
                Console.WriteLine("Succeeded. Press any key to continue");
                Console.ReadKey();
                return;
            }
            else
            {
                //command line mode
                Parser parser = Parser.Default;
                parser.ParseArguments(args, Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray()).WithParsed(Run).WithNotParsed(e => Console.WriteLine());
            }
        }

        private static void Run(object obj)
        {
            init((BaseOption)obj);
            switch (obj)
            {
                case resetOption o:
                    reset_research_tree(o.save_name_or_path);
                    break;
                case genpswdOption _:
                    generate_password();
                    break;
                case gencheckOption o:
                    generate_check(o.save_name_or_path);
                    break;
                case unpacksaveOption o:
                    unpack(o.save_name_or_path, true);
                    break;
                case unpackdatOption o:
                    unpack(o.dat_name_or_path, false);
                    break;
                case packsaveOption o:
                    pack(o.save_folder_name_or_path, true);
                    break;
                case packdatOption o:
                    pack(o.dat_folder_name_or_path, false);
                    break;
            }
        }
        static BaseOption baseOption;
        private static void init(BaseOption opt)
        {
            try
            {
                if (string.IsNullOrEmpty(opt.tab_folder))
                    opt.tab_folder = Path.Combine((string)Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam").GetValue("SteamPath"), @"steamapps\common\They Are Billions\");
                if (!Directory.Exists(opt.tab_folder)) throw new DirectoryNotFoundException(opt.tab_folder);
            }
            catch (Exception e)
            {
                throw new Exception("cannot find TAB folder automatically\n consider specify TAB folder\n use \"TABRTreset help\" to get more help\n" + e.ToString(), e);
            }
            try
            {
                if (string.IsNullOrEmpty(opt.save_folder))
                    opt.save_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\They Are Billions\Saves\");
                if (!Directory.Exists(opt.save_folder)) throw new DirectoryNotFoundException(opt.save_folder);
            }
            catch (Exception e) { throw new Exception("cannot find save folder automatically\n consider specify save folder\n use \"TABRTreset help\" to get more help\n" + e.ToString(), e); }


            // TODO search for methods in TAB executive using method signatures & IL code instead of method names for backward compatibility
            var tab_asmb = Assembly.LoadFrom(Path.Combine(opt.tab_folder, "TheyAreBillions.exe"));
            var serializer_class = tab_asmb.GetType("#=zBitRis$e$O2Cn5vz3w==");
            opt.pswd_gen_method = serializer_class.GetMethod("#=zAmh0ZrvDqn3lwW$PWQ==", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("cannot find password generate method");
            opt.check_gen_method = serializer_class.GetMethod("#=zPYy9RcqnMpGU", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("cannot find check generate method");

            baseOption = opt;

            if (opt is BaseOptionWithSaveNameOrPath o)
                o.save_name_or_path = Path.IsPathRooted(o.save_name_or_path) ? o.save_name_or_path : Path.Combine(o.save_folder, Path.GetFileNameWithoutExtension(o.save_name_or_path) + ".zxsav");
            else if (opt is BaseOptionWithDatNameOrPath odat)
                odat.dat_name_or_path = Path.IsPathRooted(odat.dat_name_or_path) ? odat.dat_name_or_path : Path.Combine(odat.dat_name_or_path, Path.GetFileNameWithoutExtension(odat.dat_name_or_path) + ".dat");
            else if (opt is BaseOptionWithSaveFolderNameOrPath osavefolder)
                osavefolder.save_folder_name_or_path = Path.IsPathRooted(osavefolder.save_folder_name_or_path) ? osavefolder.save_folder_name_or_path : Path.Combine(osavefolder.save_folder, osavefolder.save_folder_name_or_path);
            else if (opt is BaseOptionWithDatFolderNameOrPath odatfolder)
                odatfolder.dat_folder_name_or_path = Path.IsPathRooted(odatfolder.dat_folder_name_or_path) ? odatfolder.dat_folder_name_or_path : Path.Combine(odatfolder.tab_folder, odatfolder.dat_folder_name_or_path);
        }
        static void generate_password()
        {
            File.WriteAllText("savepswd.json", Newtonsoft.Json.JsonConvert.SerializeObject(Directory.EnumerateFiles(baseOption.save_folder, "*.zxsav").ToDictionary(Path.GetFileName, get_save_pswd), Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText("datpswd.json", Newtonsoft.Json.JsonConvert.SerializeObject(Directory.EnumerateFiles(baseOption.tab_folder, "*.dat").ToDictionary(Path.GetFileName, get_dat_pswd), Newtonsoft.Json.Formatting.Indented));
        }
        static DXVision.DXTableManager read_dat(string dat_name)
        {
            ZipSerializer.Current.Password = get_dat_pswd(dat_name);
            return (DXVision.DXTableManager)ZipSerializer.Read(Path.Combine(baseOption.tab_folder, Path.GetFileNameWithoutExtension(dat_name) + ".dat"));
        }
        static void reset_research_tree(string save_path)
        {
            var dict = read_dat("ZXCampaign").Tables["Researchs"].Rows.Values.ToDictionary(v => v[0], v => int.Parse(v[1]));
            ZipSerializer.Current.Password = get_save_pswd(save_path);
            var zxgamestate = ZipSerializer.Read(save_path, "Data");
            var zxcampaignstate = zxgamestate.GetProperty("CampaignState");
            int research_point_to_add = 0;
            foreach (var t in (List<string>)zxcampaignstate.GetProperty("IDResearchUnlocked"))
                research_point_to_add += dict[t];
            zxcampaignstate.SetProperty("IDResearchUnlocked", new List<string>());
            zxcampaignstate.SetProperty("IDResearchsRecentUnlocked", new List<string>());
            zxcampaignstate.SetProperty("ResearchPoints", (int)zxcampaignstate.GetProperty("ResearchPoints") + research_point_to_add);
            zxgamestate.SetProperty("CampaignState", zxcampaignstate);
            ZipSerializer.Write(save_path, "Data", zxgamestate);
            generate_check(save_path);
        }

        static string get_save_pswd(string save_path)
        {
            baseOption.pswd_gen_method.Invoke(null, new object[] { save_path, 2, false });
            return ZipSerializer.Current.Password;
        }

        static string get_dat_pswd(string dat_path)
        {
            var pswd = DXVision.DXHelper_HashCode.From(Path.GetFileNameWithoutExtension(dat_path) + ".dat").ToString();
            pswd += DXVision.DXHelper_HashCode.From(pswd).ToString();
            pswd += DXVision.DXHelper_HashCode.From(pswd).ToString();
            return pswd;
        }
        static void generate_check(string save_path)
        {
            File.WriteAllText(Path.ChangeExtension(save_path, ".zxcheck"), baseOption.check_gen_method.Invoke(null, new object[] { save_path, 2 }).ToString());
        }
        static void unpack(string save_path, bool IsSave)
        {
            var target_path = Path.Combine(Path.GetDirectoryName(save_path), Path.GetFileNameWithoutExtension(save_path));
            var t = new Ionic.Zip.ZipFile(save_path);
            t.Password = IsSave ? get_save_pswd(save_path) : get_dat_pswd(save_path);
            t.ExtractAll(target_path);
        }

        static void pack(string folder_path, bool IsSave)
        {
            var target_path = Path.ChangeExtension(folder_path.TrimEnd(Path.DirectorySeparatorChar), IsSave ? ".zxsave" : ".dat");
            var t = new Ionic.Zip.ZipFile();
            t.Password = IsSave ? get_save_pswd(folder_path) : get_dat_pswd(folder_path);
            t.AddDirectory(folder_path);
            t.Save(target_path);
            if (IsSave) generate_check(target_path);
        }
    }
}
