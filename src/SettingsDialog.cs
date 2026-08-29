using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TouchCloudPad
{
    /// <summary>
    /// Settings window: tab 1 manages cloud-game accounts / browser profiles
    /// (multi-account cookie switching), tab 2 maps each on-screen button to a
    /// virtual gamepad button.
    /// </summary>
    public class SettingsDialog : Window
    {
        private readonly Config _cfg;
        private readonly MainWindow _owner;

        // account tab controls
        private ListBox _accList;
        private TextBox _accName, _accUrl, _accChrome;
        private ComboBox _accGame;

        // pad tab controls
        private StackPanel _padPanel;
        private Dictionary<string, ComboBox> _padRows = new Dictionary<string, ComboBox>();

        public SettingsDialog(Config cfg, SkinDef skin, MainWindow owner)
        {
            _cfg = cfg;
            _owner = owner;
            Title = "设置 - TouchCloudPad";
            Width = 430; Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = owner;
            Background = new SolidColorBrush(Color.FromArgb(245, 34, 40, 48));
            ResizeMode = ResizeMode.NoResize;

            var root = new Grid();
            var tabs = new TabControl { Margin = new Thickness(8) };
            tabs.Items.Add(BuildAccountTab());
            tabs.Items.Add(BuildPadTab(skin));
            root.Children.Add(tabs);
            Content = root;
        }

        // ---------------- account tab ----------------
        private UIElement BuildAccountTab()
        {
            var tab = new TabItem { Header = "账号 / 浏览器" };
            var grid = new Grid { Margin = new Thickness(8) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _accList = new ListBox { Margin = new Thickness(0, 0, 8, 0) };
            _accList.SelectionChanged += (s, e) => LoadSelected();
            grid.Children.Add(_accList); Grid.SetColumn(_accList, 0);

            var form = new StackPanel { Margin = new Thickness(0) };
            form.Children.Add(Label("账号名称"));
            _accName = new TextBox { Margin = new Thickness(0, 2, 0, 6) };
            form.Children.Add(_accName);

            form.Children.Add(Label("对应游戏"));
            _accGame = new ComboBox { Margin = new Thickness(0, 2, 0, 6) };
            _accGame.Items.Add("鸣潮 (Chrome)");
            _accGame.Items.Add("崩铁 (Chrome)");
            _accGame.Items.Add("绝区零 (桌面)");
            _accGame.SelectedIndex = 0;
            form.Children.Add(_accGame);

            form.Children.Add(Label("云游戏网址 (留空=新标签)"));
            _accUrl = new TextBox { Margin = new Thickness(0, 2, 0, 6) };
            form.Children.Add(_accUrl);

            form.Children.Add(Label("Chrome路径 (可选, 留空自动查找)"));
            _accChrome = new TextBox { Margin = new Thickness(0, 2, 0, 6) };
            form.Children.Add(_accChrome);

            var hint = new TextBlock
            {
                Text = "每个账号对应一个独立 Chrome 配置目录，Cookie/登录态互相隔离，切换账号只需选择后点“打开浏览器”。",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0),
            };
            form.Children.Add(hint);

            grid.Children.Add(form); Grid.SetColumn(form, 1);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            AddButton(btns, "添加账号", () => { AddAccount(); });
            AddButton(btns, "保存修改", () => { SaveSelected(); });
            AddButton(btns, "删除账号", () => { DeleteSelected(); });
            AddButton(btns, "打开此账号", () => { OpenSelected(); });
            grid.Children.Add(btns); Grid.SetRow(btns, 1); Grid.SetColumnSpan(btns, 2);

            tab.Content = grid;
            RefreshAccountList();
            return tab;
        }

        private void RefreshAccountList()
        {
            _accList.Items.Clear();
            foreach (var p in _cfg.Profiles)
                _accList.Items.Add(string.Format("[{0}] {1}", GameName(p.GameId), p.Name));
            if (_cfg.Profiles.Count > 0) _accList.SelectedIndex = 0;
        }

        private void LoadSelected()
        {
            int i = _accList.SelectedIndex;
            if (i < 0 || i >= _cfg.Profiles.Count) { ClearForm(); return; }
            var p = _cfg.Profiles[i];
            _accName.Text = p.Name;
            _accUrl.Text = p.Url;
            _accChrome.Text = p.ChromePath;
            int g = p.GameId == "hsr" ? 1 : p.GameId == "zzz" ? 2 : 0;
            _accGame.SelectedIndex = g;
        }

        private void ClearForm()
        {
            _accName.Text = _accUrl.Text = _accChrome.Text = "";
            _accGame.SelectedIndex = 0;
        }

        private void AddAccount()
        {
            var p = new AccountProfile
            {
                Name = "新账号" + (_cfg.Profiles.Count + 1),
                GameId = "wuwaves",
                Url = "https://www.google.com",
            };
            _cfg.Profiles.Add(p);
            RefreshAccountList();
            _accList.SelectedIndex = _cfg.Profiles.Count - 1;
            _cfg.Save();
        }

        private void SaveSelected()
        {
            int i = _accList.SelectedIndex;
            if (i < 0 || i >= _cfg.Profiles.Count) return;
            var p = _cfg.Profiles[i];
            p.Name = _accName.Text.Trim();
            p.Url = _accUrl.Text.Trim();
            p.ChromePath = _accChrome.Text.Trim();
            p.GameId = _accGame.SelectedIndex == 1 ? "hsr" : _accGame.SelectedIndex == 2 ? "zzz" : "wuwaves";
            _cfg.Save();
            RefreshAccountList();
            _accList.SelectedIndex = i;
        }

        private void DeleteSelected()
        {
            int i = _accList.SelectedIndex;
            if (i < 0 || i >= _cfg.Profiles.Count) return;
            _cfg.Profiles.RemoveAt(i);
            _cfg.Save();
            RefreshAccountList();
        }

        private void OpenSelected()
        {
            int i = _accList.SelectedIndex;
            if (i < 0 || i >= _cfg.Profiles.Count) return;
            Browser.Launch(_cfg.Profiles[i], _cfg);
        }

        // ---------------- pad tab ----------------
        private UIElement BuildPadTab(SkinDef skin)
        {
            var tab = new TabItem { Header = "手柄按键" };
            var scroller = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _padPanel = new StackPanel { Margin = new Thickness(8) };

            var skinCombo = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8) };
            foreach (var s in Skins.All) skinCombo.Items.Add(s.Name);
            int idx = 0;
            for (int i = 0; i < Skins.All.Count; i++) if (Skins.All[i].Id == skin.Id) idx = i;
            skinCombo.SelectedIndex = idx;
            SkinDef current = skin;
            skinCombo.SelectionChanged += (s, e) => { current = Skins.All[skinCombo.SelectedIndex]; RenderPadRows(current); };
            _padPanel.Children.Add(skinCombo);

            var hint = new TextBlock
            {
                Text = "左摇杆=移动，右视角区=右摇杆(转视角)。每个动作按钮对应一个 Xbox 手柄按键：A/B/X/Y、LB/RB(肩键)、LT/RT(扳机)、Start/Back、L3/R3、十字键。",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8),
            };
            _padPanel.Children.Add(hint);

            RenderPadRows(current);

            var saveBtn = new Button { Content = "保存按键", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            saveBtn.Click += (s, e) => SavePadRows(current);
            _padPanel.Children.Add(saveBtn);

            scroller.Content = _padPanel;
            tab.Content = scroller;
            return tab;
        }

        private void RenderPadRows(SkinDef skin)
        {
            while (_padPanel.Children.Count > 2) _padPanel.Children.RemoveAt(_padPanel.Children.Count - 1);
            var set = Skins.Effective(skin, _cfg);
            _padRows = new Dictionary<string, ComboBox>();

            foreach (var b in skin.Buttons)
            {
                string cur = set.Buttons.ContainsKey(b.Id) ? set.Buttons[b.Id] : b.Pad;
                var combo = new ComboBox { Width = 110, VerticalContentAlignment = VerticalAlignment.Center };
                foreach (var n in Pad.Names) combo.Items.Add(n);
                int si = 0;
                for (int i = 0; i < Pad.Names.Length; i++) if (string.Equals(Pad.Names[i], cur, StringComparison.OrdinalIgnoreCase)) { si = i; break; }
                combo.SelectedIndex = si;
                _padRows[b.Id] = combo;

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                row.Children.Add(new TextBlock
                {
                    Text = string.Format("{0} ({1})：", b.Label, b.Id),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                });
                row.Children.Add(combo);
                _padPanel.Children.Add(row);
            }
        }

        private void SavePadRows(SkinDef skin)
        {
            if (!_cfg.Skins.ContainsKey(skin.Id)) _cfg.Skins[skin.Id] = new SkinSettings();
            var s = _cfg.Skins[skin.Id];
            if (s.Buttons == null) s.Buttons = new Dictionary<string, string>();
            foreach (var kv in _padRows)
                s.Buttons[kv.Key] = (string)kv.Value.SelectedItem;
            _cfg.Save();
            MessageBox.Show(this, "手柄按键已保存。", "TouchCloudPad");
        }

        // ---------------- helpers ----------------
        private static string GameName(string id)
        {
            return id == "hsr" ? "崩铁" : id == "zzz" ? "绝区零" : "鸣潮";
        }

        private static TextBlock Label(string t)
        {
            return new TextBlock { Text = t, Foreground = Brushes.White, FontSize = 12, Margin = new Thickness(0, 2, 0, 2) };
        }

        private void AddButton(StackPanel panel, string text, Action action)
        {
            var b = new Button { Content = text, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(8, 2, 8, 2) };
            b.Click += (s, e) => action();
            panel.Children.Add(b);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _cfg.Save();
            if (_owner != null) _owner.RefreshAfterSettings();
            base.OnClosing(e);
        }
    }
}
