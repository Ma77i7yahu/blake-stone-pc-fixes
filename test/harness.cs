// Tests the installer's logic by reflection on scratch folders.
// Usage: harness.exe <BlakeStoneMods.exe> <stock bstone.exe> <tested modded bstone.exe> <config to copy> <scratch dir>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

class Harness
{
    static int fails;
    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + what);
        if (!ok) fails++;
    }
    static bool Same(string a, string b) { return File.Exists(a) && File.Exists(b) && File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b)); }

    [STAThread]
    static int Main(string[] a)
    {
        var asm = Assembly.LoadFrom(a[0]);
        var mod = asm.GetType("Mod");
        Func<string, object[], object> call = (n, args) => mod.GetMethod(n).Invoke(null, args);
        string stock = a[1], tested = a[2], cfg = a[3], scratch = a[4];
        if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
        Directory.CreateDirectory(scratch);

        var embedded = (byte[])mod.GetProperty("Exe").GetValue(null, null);
        Check(embedded.SequenceEqual(File.ReadAllBytes(tested)), "embedded bstone.exe == tested exe");

        // Stock BStone already in the folder.
        var g1 = Path.Combine(scratch, "aog");
        Directory.CreateDirectory(g1);
        File.WriteAllText(Path.Combine(g1, "vswap.bs6"), "x");
        File.Copy(stock, Path.Combine(g1, "bstone.exe"));
        Check((string)call("DetectGame", new object[] { g1 }) == "Aliens of Gold", "detect AOG (lower-case name)");
        Check(call("GetState", new object[] { g1 }).ToString() == "Stock", "state Stock");
        call("InstallExe", new object[] { g1 });
        Check(Same(Path.Combine(g1, "bstone.original.exe"), stock), "install: stock kept as bstone.original.exe");
        Check(Same(Path.Combine(g1, "bstone.exe"), tested), "install: bstone.exe == tested");
        Check(call("GetState", new object[] { g1 }).ToString() == "Mod", "state Mod");
        call("InstallExe", new object[] { g1 });
        Check(Same(Path.Combine(g1, "bstone.original.exe"), stock) && Same(Path.Combine(g1, "bstone.exe"), tested), "reinstall keeps backup");
        call("RemoveExe", new object[] { g1 });
        Check(Same(Path.Combine(g1, "bstone.exe"), stock) && !File.Exists(Path.Combine(g1, "bstone.original.exe")), "remove restores stock");

        // Backup exists and a foreign exe sits in place: backup must survive.
        File.Copy(stock, Path.Combine(g1, "bstone.original.exe"));
        File.WriteAllText(Path.Combine(g1, "bstone.exe"), "other build");
        call("InstallExe", new object[] { g1 });
        Check(Same(Path.Combine(g1, "bstone.original.exe"), stock) && Same(Path.Combine(g1, "bstone.exe"), tested), "foreign exe + backup: backup kept");

        // No BStone at all (GOG DOS folder), Planet Strike data.
        var g2 = Path.Combine(scratch, "ps");
        Directory.CreateDirectory(g2);
        File.WriteAllText(Path.Combine(g2, "VSWAP.VSI"), "x");
        Check((string)call("DetectGame", new object[] { g2 }) == "Planet Strike", "detect PS");
        Check(call("GetState", new object[] { g2 }).ToString() == "None", "state None");
        call("InstallExe", new object[] { g2 });
        Check(Same(Path.Combine(g2, "bstone.exe"), tested) && !File.Exists(Path.Combine(g2, "bstone.original.exe")), "fresh install, no backup");
        call("RemoveExe", new object[] { g2 });
        Check(!File.Exists(Path.Combine(g2, "bstone.exe")), "fresh remove deletes bstone.exe");
        File.WriteAllText(Path.Combine(g2, "bstone.exe"), "someone else's");
        call("RemoveExe", new object[] { g2 });
        Check(File.ReadAllText(Path.Combine(g2, "bstone.exe")) == "someone else's", "remove leaves a foreign exe alone");

        Check(call("DetectGame", new object[] { scratch }) == null, "no data = not a game folder");

        // Config editing on a copy of a real config.
        var cdir = Path.Combine(scratch, "profile");
        Directory.CreateDirectory(cdir);
        var c = Path.Combine(cdir, "bstone_config.txt");
        File.Copy(cfg, c);
        var settings = new List<KeyValuePair<string, string>> {
            new KeyValuePair<string, string>("gp_show_crosshair", "0"),
            new KeyValuePair<string, string>("gp_momentum_style", "0"),
            new KeyValuePair<string, string>("gp_brand_new", "1") };
        call("ApplySettings", new object[] { c, settings });
        var before = File.ReadAllLines(cfg);
        var after = File.ReadAllLines(c);
        Check(Same(Path.Combine(cdir, "bstone_config.txt.before-mods.bak"), cfg), "config backup made");
        var changed = before.Where((l, i) => i >= after.Length || after[i] != l).ToList();
        Check(after.Length == before.Length + 1 && after.Last() == "gp_brand_new \"1\"", "missing cvar appended");
        Check(changed.Count == 2 && changed.All(l => l.StartsWith("gp_show_crosshair ") || l.StartsWith("gp_momentum_style ")), "only the chosen lines changed");
        var rd = (Dictionary<string, string>)call("ReadSettings", new object[] { c });
        Check(rd["gp_show_crosshair"] == "0" && rd["gp_momentum_style"] == "0" && rd["gp_momentum"] == "1", "read back");
        call("ApplySettings", new object[] { c, settings });
        Check(File.ReadAllLines(c).Length == after.Length, "second apply adds nothing");

        var c2 = Path.Combine(scratch, "new", "sub", "bstone_config.txt");
        call("ApplySettings", new object[] { c2, settings });
        Check(File.ReadAllLines(c2).Length == 3, "config created when missing");

        // Form defaults follow the given config.
        var form = (System.Windows.Forms.Form)Activator.CreateInstance(asm.GetType("InstallerForm"));
        var chosen = (List<KeyValuePair<string, string>>)asm.GetType("InstallerForm").GetMethod("ChosenSettings").Invoke(form, null);
        var real = (Dictionary<string, string>)call("ReadSettings", new object[] { call("DefaultConfigPath", null) });
        Check(chosen.All(kv => !real.ContainsKey(kv.Key) || real[kv.Key] == kv.Value), "form preselects the current config (" + string.Join(" ", chosen.Select(kv => kv.Key + "=" + kv.Value)) + ")");

        var folders = ((IEnumerable<string>)call("FindGameFolders", null)).ToList();
        Console.WriteLine("found game folders: " + string.Join(" | ", folders));
        Check(folders.Count > 0, "auto-detects an installed game");

        Console.WriteLine(fails == 0 ? "ALL PASS" : fails + " FAILED");
        return fails;
    }
}
