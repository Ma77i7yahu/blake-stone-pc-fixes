// Blake Stone Mods installer: installs a modded BStone (1.3.4) bstone.exe into a
// Blake Stone: Aliens of Gold / Planet Strike folder and writes the chosen options
// to BStone's config file. GPL-2.0 (same as BStone).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Blake Stone Mods")]
[assembly: AssemblyProduct("Blake Stone Mods")]
[assembly: AssemblyVersion("1.0.0.0")]

public static class Mod
{
    public const string Version = "1.0";
    public const string ExeName = "bstone.exe";
    public const string BackupName = "bstone.original.exe";
    public const string ConfigBackupName = "bstone_config.txt.before-mods.bak";

    static byte[] exe;
    public static byte[] Exe
    {
        get
        {
            if (exe == null)
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("bstone.exe"))
                using (var m = new MemoryStream())
                {
                    s.CopyTo(m);
                    exe = m.ToArray();
                }
            }
            return exe;
        }
    }

    public static string Sha256(byte[] data)
    {
        using (var h = SHA256.Create())
            return BitConverter.ToString(h.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
    }

    public static bool IsModExe(string path)
    {
        if (!File.Exists(path)) return false;
        var fi = new FileInfo(path);
        if (fi.Length != Exe.Length) return false;
        return Sha256(File.ReadAllBytes(path)) == Sha256(Exe);
    }

    // Blake Stone data files: VSWAP.BS1 (AOG shareware), .BS3/.BS6 (AOG), .VSI (Planet Strike).
    public static string DetectGame(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return null;
        foreach (var f in Directory.GetFiles(folder))
        {
            var n = Path.GetFileName(f).ToUpperInvariant();
            if (n == "VSWAP.VSI") return "Planet Strike";
            if (n == "VSWAP.BS6" || n == "VSWAP.BS3") return "Aliens of Gold";
            if (n == "VSWAP.BS1") return "Aliens of Gold (shareware)";
        }
        return null;
    }

    public enum State { None, Stock, Mod }

    public static State GetState(string folder)
    {
        var target = Path.Combine(folder, ExeName);
        if (!File.Exists(target)) return State.None;
        return IsModExe(target) ? State.Mod : State.Stock;
    }

    // Returns a short description of what was done.
    public static string InstallExe(string folder)
    {
        var target = Path.Combine(folder, ExeName);
        var backup = Path.Combine(folder, BackupName);
        string note;
        if (File.Exists(target))
        {
            if (IsModExe(target))
                note = "The mod was already installed; bstone.exe was rewritten.";
            else if (!File.Exists(backup))
            {
                File.Move(target, backup);
                note = "Your bstone.exe was kept as " + BackupName + ".";
            }
            else
                note = "Replaced bstone.exe (the existing " + BackupName + " was kept as your backup).";
        }
        else
            note = "Installed bstone.exe (there was no bstone.exe before).";
        File.WriteAllBytes(target, Exe);
        if (!IsModExe(target)) throw new IOException("bstone.exe did not verify after writing.");
        return note;
    }

    public static string RemoveExe(string folder)
    {
        var target = Path.Combine(folder, ExeName);
        var backup = Path.Combine(folder, BackupName);
        if (File.Exists(backup))
        {
            if (File.Exists(target)) File.Delete(target);
            File.Move(backup, target);
            return "Restored your original bstone.exe.";
        }
        if (IsModExe(target))
        {
            File.Delete(target);
            return "Removed bstone.exe (there was no backup, so the folder had no BStone before).";
        }
        return "Nothing to remove: bstone.exe is not the modded one and there is no backup.";
    }

    public static string DefaultConfigPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "bibendovsky", "bstone", "bstone_config.txt");
    }

    // Sets each cvar line (name "value") in the config, adding missing ones at the end.
    public static void ApplySettings(string configPath, IList<KeyValuePair<string, string>> settings)
    {
        var dir = Path.GetDirectoryName(configPath);
        Directory.CreateDirectory(dir);
        var lines = new List<string>();
        if (File.Exists(configPath))
        {
            var bak = Path.Combine(dir, ConfigBackupName);
            if (!File.Exists(bak)) File.Copy(configPath, bak);
            lines.AddRange(File.ReadAllLines(configPath));
        }
        foreach (var kv in settings)
        {
            var line = kv.Key + " \"" + kv.Value + "\"";
            var re = new Regex("^\\s*" + Regex.Escape(kv.Key) + "(\\s|$)");
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (re.IsMatch(lines[i]))
                {
                    lines[i] = line;
                    found = true;
                }
            }
            if (!found) lines.Add(line);
        }
        File.WriteAllText(configPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
    }

    public static Dictionary<string, string> ReadSettings(string configPath)
    {
        var d = new Dictionary<string, string>();
        if (!File.Exists(configPath)) return d;
        var re = new Regex("^\\s*([A-Za-z0-9_]+)\\s+\"([^\"]*)\"");
        foreach (var l in File.ReadAllLines(configPath))
        {
            var m = re.Match(l);
            if (m.Success) d[m.Groups[1].Value] = m.Groups[2].Value;
        }
        return d;
    }

    public static IEnumerable<string> FindGameFolders()
    {
        var found = new List<string>();
        foreach (var root in new[] { @"SOFTWARE\WOW6432Node\GOG.com\Games", @"SOFTWARE\GOG.com\Games" })
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(root))
                {
                    if (k == null) continue;
                    foreach (var sub in k.GetSubKeyNames())
                    {
                        using (var g = k.OpenSubKey(sub))
                        {
                            var name = g.GetValue("gameName") as string ?? "";
                            var path = g.GetValue("path") as string;
                            if (name.IndexOf("Blake Stone", StringComparison.OrdinalIgnoreCase) >= 0 && path != null)
                                found.Add(path);
                        }
                    }
                }
            }
            catch { }
        }
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                var steam = k == null ? null : k.GetValue("SteamPath") as string;
                if (steam != null)
                {
                    var libs = new List<string> { steam };
                    var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdf))
                        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                            libs.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
                    foreach (var lib in libs)
                    {
                        var common = Path.Combine(lib, "steamapps", "common");
                        if (!Directory.Exists(common)) continue;
                        foreach (var d in Directory.GetDirectories(common, "Blake Stone*"))
                        {
                            found.Add(d);
                            foreach (var s in Directory.GetDirectories(d)) found.Add(s);
                        }
                    }
                }
            }
        }
        catch { }
        return found.Where(f => DetectGame(f) != null).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static void CreateShortcut(string folder, string game)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var lnk = Path.Combine(desktop, "Blake Stone - " + game.Replace(" (shareware)", "") + " (BStone).lnk");
        var t = Type.GetTypeFromProgID("WScript.Shell");
        var shell = Activator.CreateInstance(t);
        var sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
        var st = sc.GetType();
        st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { Path.Combine(folder, ExeName) });
        st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { folder });
        st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "Blake Stone (BStone with Blake Stone Mods " + Version + ")" });
        st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
    }
}

public class InstallerForm : Form
{
    TextBox folderBox;
    Label stateLabel;
    CheckBox crosshair, smooth, slowStrafe, secrets, possum, shortcut;
    ComboBox movement;
    Button installBtn, removeBtn;
    public string ConfigPath = Mod.DefaultConfigPath();

    public InstallerForm()
    {
        Text = "Blake Stone Mods " + Mod.Version;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 470);

        int y = 12;
        Controls.Add(new Label
        {
            Text = "Installs a modded BStone 1.3.4 (bstone.exe) into your Blake Stone folder: aiming crosshair, " +
                   "smooth turning and motion, new movement styles, slow strafe, secrets on the automap and " +
                   "shootable downed Star Troopers. Your own bstone.exe is backed up first.",
            Location = new Point(12, y), Size = new Size(496, 52)
        });
        y += 58;

        Controls.Add(new Label { Text = "Game folder (Aliens of Gold or Planet Strike):", Location = new Point(12, y), AutoSize = true });
        y += 20;
        folderBox = new TextBox { Location = new Point(12, y), Width = 406 };
        folderBox.TextChanged += (s, e) => RefreshState();
        Controls.Add(folderBox);
        var browse = new Button { Text = "Browse...", Location = new Point(424, y - 1), Width = 84 };
        browse.Click += (s, e) => Browse();
        Controls.Add(browse);
        y += 28;
        stateLabel = new Label { Location = new Point(12, y), Size = new Size(496, 34) };
        Controls.Add(stateLabel);
        y += 40;

        var box = new GroupBox { Text = "Options (saved in BStone's config; most can also be changed in game)", Location = new Point(12, y), Size = new Size(496, 222) };
        Controls.Add(box);
        int by = 22;
        crosshair = AddCheck(box, "Aiming crosshair", ref by, true);
        smooth = AddCheck(box, "Smooth motion (draws at your display's refresh rate)", ref by, true);
        box.Controls.Add(new Label { Text = "Movement:", Location = new Point(12, by + 3), AutoSize = true });
        movement = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(90, by), Width = 390 };
        movement.Items.AddRange(new object[] {
            "Duke Nukem 3D style: full-speed strafe and backpedal, no slide",
            "Simple momentum: short speed-up and slow-down",
            "Original Blake Stone movement" });
        movement.SelectedIndex = 0;
        movement.SelectedIndexChanged += (s, e) => slowStrafe.Enabled = movement.SelectedIndex == 1;
        box.Controls.Add(movement);
        by += 30;
        slowStrafe = AddCheck(box, "Slow strafe: sideways at 40% speed (simple momentum only)", ref by, true);
        slowStrafe.Enabled = false;
        secrets = AddCheck(box, "Show all secret areas and push walls on the automap", ref by, true);
        possum = AddCheck(box, "Downed Star Troopers can be shot (one hit kills them)", ref by, true);
        shortcut = AddCheck(box, "Create a desktop shortcut", ref by, true);
        y += 230;

        installBtn = new Button { Text = "Install", Location = new Point(252, y), Width = 84, Height = 28 };
        installBtn.Click += (s, e) => DoInstall();
        removeBtn = new Button { Text = "Remove", Location = new Point(338, y), Width = 84, Height = 28 };
        removeBtn.Click += (s, e) => DoRemove();
        var close = new Button { Text = "Close", Location = new Point(424, y), Width = 84, Height = 28 };
        close.Click += (s, e) => Close();
        Controls.AddRange(new Control[] { installBtn, removeBtn, close });
        y += 36;
        Controls.Add(new Label { Text = "Based on BStone by Boris I. Bendovsky and contributors. GPL-2.0.", Location = new Point(12, y), AutoSize = true, ForeColor = SystemColors.GrayText });

        LoadConfigDefaults();
        var f = Mod.FindGameFolders().FirstOrDefault();
        if (f != null) folderBox.Text = f;
        RefreshState();
    }

    CheckBox AddCheck(Control parent, string text, ref int y, bool on)
    {
        var c = new CheckBox { Text = text, Location = new Point(12, y), AutoSize = true, Checked = on };
        parent.Controls.Add(c);
        y += 26;
        return c;
    }

    // Shows what the config already holds, so a reinstall keeps the player's choices.
    void LoadConfigDefaults()
    {
        var d = Mod.ReadSettings(ConfigPath);
        Func<string, bool, bool> b = (k, def) => d.ContainsKey(k) ? d[k] != "0" : def;
        crosshair.Checked = b("gp_show_crosshair", true);
        smooth.Checked = b("gp_smooth_motion", true);
        secrets.Checked = b("gp_automap_secrets", true);
        possum.Checked = b("gp_possum_shootable", true);
        slowStrafe.Checked = b("gp_slow_strafe", true);
        if (d.ContainsKey("gp_momentum"))
            movement.SelectedIndex = d["gp_momentum"] == "0" ? 2 : (d.ContainsKey("gp_momentum_style") && d["gp_momentum_style"] == "0" ? 1 : 0);
    }

    void Browse()
    {
        using (var dlg = new FolderBrowserDialog { Description = "Choose your Blake Stone folder (the one with VSWAP.BS6 or VSWAP.VSI)" })
        {
            if (Directory.Exists(folderBox.Text)) dlg.SelectedPath = folderBox.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK) folderBox.Text = dlg.SelectedPath;
        }
    }

    void RefreshState()
    {
        var folder = folderBox.Text.Trim();
        var game = Mod.DetectGame(folder);
        if (game == null)
        {
            stateLabel.Text = Directory.Exists(folder) ? "No Blake Stone data files (VSWAP.BS6 / VSWAP.VSI) in this folder." : "Choose your game folder.";
            installBtn.Enabled = removeBtn.Enabled = false;
            return;
        }
        var st = Mod.GetState(folder);
        bool backup = File.Exists(Path.Combine(folder, Mod.BackupName));
        stateLabel.Text = "Found Blake Stone: " + game + ". " +
            (st == Mod.State.Mod ? "The mod is installed." :
             st == Mod.State.Stock ? "A bstone.exe is there now; it will be kept as " + Mod.BackupName + "." :
             "No bstone.exe yet; the mod brings its own BStone.") +
            (backup && st == Mod.State.Mod ? " Backup: " + Mod.BackupName + "." : "");
        installBtn.Enabled = true;
        installBtn.Text = st == Mod.State.Mod ? "Reinstall" : "Install";
        removeBtn.Enabled = st == Mod.State.Mod || backup;
    }

    public List<KeyValuePair<string, string>> ChosenSettings()
    {
        Func<bool, string> s = v => v ? "1" : "0";
        var l = new List<KeyValuePair<string, string>>();
        Action<string, string> add = (k, v) => l.Add(new KeyValuePair<string, string>(k, v));
        add("gp_show_crosshair", s(crosshair.Checked));
        add("gp_smooth_motion", s(smooth.Checked));
        add("gp_momentum", s(movement.SelectedIndex != 2));
        add("gp_momentum_style", movement.SelectedIndex == 1 ? "0" : "1");
        add("gp_slow_strafe", s(slowStrafe.Checked));
        add("gp_automap_secrets", s(secrets.Checked));
        add("gp_possum_shootable", s(possum.Checked));
        return l;
    }

    void DoInstall()
    {
        var folder = folderBox.Text.Trim();
        var game = Mod.DetectGame(folder);
        try
        {
            var note = Mod.InstallExe(folder);
            Mod.ApplySettings(ConfigPath, ChosenSettings());
            if (shortcut.Checked)
            {
                try { Mod.CreateShortcut(folder, game); note += "\nA desktop shortcut was created."; }
                catch (Exception ex) { note += "\nThe desktop shortcut could not be created: " + ex.Message; }
            }
            MessageBox.Show(this, "Blake Stone Mods " + Mod.Version + " installed.\n\n" + note +
                "\n\nRun bstone.exe from the game folder" + (shortcut.Checked ? " or the desktop shortcut" : "") +
                ". The options are under Options > Game Switches 2 (secrets and Star Troopers: config only).",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Install failed: " + ex.Message + "\n\nIf the game is running, close it and try again.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RefreshState();
    }

    void DoRemove()
    {
        var folder = folderBox.Text.Trim();
        var question = File.Exists(Path.Combine(folder, Mod.BackupName))
            ? "Remove the mod from this folder and restore your original bstone.exe?"
            : "There is no backup (" + Mod.BackupName + ") in this folder, so the mod's bstone.exe will be deleted. Continue?";
        if (MessageBox.Show(this, question, Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            MessageBox.Show(this, Mod.RemoveExe(folder) +
                "\n\nThe mod's settings in bstone_config.txt are harmless to stock BStone, which drops them the next time it saves.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Remove failed: " + ex.Message + "\n\nIf the game is running, close it and try again.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RefreshState();
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}
