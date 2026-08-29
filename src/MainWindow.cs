using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TouchCloudPad
{
    /// <summary>
    /// The always-on-top frosted-glass overlay. It floats above the cloud game
    /// (Chrome or the ZZZ desktop app) and turns touchscreen touches into
    /// **virtual gamepad (XInput / Xbox 360)** input via the ViGEmBus driver.
    /// There is no keyboard/mouse output anymore: the left stick = movement,
    /// the camera pad = right stick, and the action buttons = gamepad buttons.
    ///
    /// Layout lives on a Canvas so controls can be repositioned freely
    /// ("布局" edit mode). The whole panel is inside a Viewbox, so resizing or
    /// the scale slider grows/shrinks everything proportionally.
    /// </summary>
    public class MainWindow : Window
    {
        // logical (pre-scale) design size; Viewbox scales it to the window
        private const double BASE_W = 520;
        private const double BASE_H = 400;
        private const double BODY_W = 496;   // BASE_W - 2*12 margin
        private const double BODY_H = 300;

        private Config _cfg;
        private SkinDef _skin;
        private SkinSettings _skinSet;
        private XInputGamepad _gamepad;

        // ---- UI ----
        private ComboBox _skinCombo;
        private ComboBox _accountCombo;
        private CheckBox _topmostBox;
        private Slider _scaleSlider;
        private ToggleButton _layoutBtn;
        private Button _openBrowserButton;
        private TextBlock _statusText;

        private Canvas _bodyCanvas;
        private Grid _joyBase;
        private Ellipse _joyKnob;
        private Border _cameraPad;
        private Dictionary<string, Border> _buttons = new Dictionary<string, Border>();
        private Dictionary<string, Point> _ctrlPos = new Dictionary<string, Point>();

        // ---- runtime state ----
        private Dictionary<long, ActivePointer> _pointers = new Dictionary<long, ActivePointer>();
        private bool _layoutEdit;

        private class ActivePointer
        {
            public long Id;
            public string Kind;   // "joy", "camera", buttonId, or "drag:<id>"
            public Point Last;
        }

        public MainWindow()
        {
            _cfg = Config.Load();
            _skin = Skins.ById(_cfg.SkinId);
            _skinSet = Skins.Effective(_skin, _cfg);
            _gamepad = new XInputGamepad();

            BuildWindow();
            ApplySkin();
            RebuildAccountList();
            _cfg.Save();
        }

        // =====================================================================
        //  Construction
        // =====================================================================
        private void BuildWindow()
        {
            Title = "TouchCloudPad";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = _cfg.AlwaysOnTop;
            Width = BASE_W;
            Height = BASE_H;
            Left = _cfg.WindowLeft;
            Top = _cfg.WindowTop;
            ResizeMode = ResizeMode.CanResize;
            MinWidth = 300;
            MinHeight = 230;
            Opacity = _cfg.Opacity;

            var root = new Border
            {
                Width = BASE_W,
                Height = BASE_H,
                Background = new SolidColorBrush(Color.FromArgb(205, 30, 38, 48)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
            };

            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // title
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // account
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(BODY_H) }); // body
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // status

            var title = BuildTitleBar();
            panel.Children.Add(title);
            Grid.SetRow(title, 0);

            var account = BuildAccountBar();
            panel.Children.Add(account);
            Grid.SetRow(account, 1);

            _bodyCanvas = new Canvas();
            panel.Children.Add(_bodyCanvas);
            Grid.SetRow(_bodyCanvas, 2);

            _statusText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 11,
                Opacity = 0.85,
                Margin = new Thickness(12, 4, 12, 8),
            };
            panel.Children.Add(_statusText);
            Grid.SetRow(_statusText, 3);

            root.Child = panel;

            // proportional scaling
            _viewbox = new Viewbox { Stretch = Stretch.Uniform, Child = root };
            Content = _viewbox;
        }

        private Viewbox _viewbox;

        private UIElement BuildTitleBar()
        {
            var bar = new Grid { Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "触控云手柄",
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(12, 8, 8, 8),
                VerticalAlignment = VerticalAlignment.Center,
            };
            bar.Children.Add(title);
            Grid.SetColumn(title, 0);

            _skinCombo = new ComboBox { Width = 120, Margin = new Thickness(4, 6, 4, 6), FontSize = 12 };
            foreach (var s in Skins.All) _skinCombo.Items.Add(s.Name);
            _skinCombo.SelectedIndex = 0;
            _skinCombo.SelectionChanged += (s, e) =>
            {
                _cfg.SkinId = Skins.All[_skinCombo.SelectedIndex].Id;
                ApplySkin();
                _cfg.Save();
            };
            bar.Children.Add(_skinCombo); Grid.SetColumn(_skinCombo, 1);

            _topmostBox = new CheckBox
            {
                Content = "顶置",
                Foreground = Brushes.White,
                FontSize = 12,
                IsChecked = _cfg.AlwaysOnTop,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 6, 2, 6),
            };
            _topmostBox.Checked += (s, e) => { Topmost = true; _cfg.AlwaysOnTop = true; _cfg.Save(); };
            _topmostBox.Unchecked += (s, e) => { Topmost = false; _cfg.AlwaysOnTop = false; _cfg.Save(); };
            bar.Children.Add(_topmostBox); Grid.SetColumn(_topmostBox, 2);

            var settingsBtn = MakeIconButton("⚙", 26);
            settingsBtn.Click += (s, e) => new SettingsDialog(_cfg, _skin, this).ShowDialog();
            bar.Children.Add(settingsBtn); Grid.SetColumn(settingsBtn, 3);

            var minBtn = MakeIconButton("–", 26);
            minBtn.Click += (s, e) => WindowState = WindowState.Minimized;
            bar.Children.Add(minBtn); Grid.SetColumn(minBtn, 4);

            var closeBtn = MakeIconButton("✕", 26);
            closeBtn.Click += (s, e) => Close();
            bar.Children.Add(closeBtn); Grid.SetColumn(closeBtn, 5);

            bar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 1) DragMove();
            };
            return bar;
        }

        private UIElement BuildAccountBar()
        {
            var bar = new Grid { Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)) };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = "账号",
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(12, 5, 6, 5),
                VerticalAlignment = VerticalAlignment.Center,
            };
            bar.Children.Add(lbl); Grid.SetColumn(lbl, 0);

            _accountCombo = new ComboBox
            {
                Width = 150,
                Margin = new Thickness(4, 4, 4, 4),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            _accountCombo.SelectionChanged += (s, e) => { _cfg.ActiveProfile = _accountCombo.SelectedIndex; _cfg.Save(); };
            bar.Children.Add(_accountCombo); Grid.SetColumn(_accountCombo, 1);

            _openBrowserButton = new Button
            {
                Content = "打开浏览器",
                FontSize = 12,
                Margin = new Thickness(4, 4, 4, 4),
                Padding = new Thickness(8, 2, 8, 2),
            };
            _openBrowserButton.Click += (s, e) => OpenBrowser();
            bar.Children.Add(_openBrowserButton); Grid.SetColumn(_openBrowserButton, 2);

            var manage = new Button
            {
                Content = "管理",
                FontSize = 12,
                Margin = new Thickness(4, 4, 4, 4),
                Padding = new Thickness(8, 2, 8, 2),
            };
            manage.Click += (s, e) => new SettingsDialog(_cfg, _skin, this).ShowDialog();
            bar.Children.Add(manage); Grid.SetColumn(manage, 3);

            _layoutBtn = new ToggleButton
            {
                Content = "布局",
                FontSize = 12,
                Margin = new Thickness(4, 4, 4, 4),
                Padding = new Thickness(8, 2, 8, 2),
            };
            _layoutBtn.Checked += (s, e) => { _layoutEdit = true; UpdateStatus(); };
            _layoutBtn.Unchecked += (s, e) => { _layoutEdit = false; UpdateStatus(); };
            bar.Children.Add(_layoutBtn); Grid.SetColumn(_layoutBtn, 4);

            var scalePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(6, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center,
            };
            scalePanel.Children.Add(new TextBlock
            {
                Text = "缩放",
                Foreground = Brushes.White,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
            });
            _scaleSlider = new Slider { Width = 90, Minimum = 0.6, Maximum = 2.0, Value = 1.0, VerticalAlignment = VerticalAlignment.Center };
            _scaleSlider.ValueChanged += (s, e) =>
            {
                if (_cfg != null)
                {
                    double v = _scaleSlider.Value;
                    Width = BASE_W * v;
                    Height = BASE_H * v;
                }
            };
            scalePanel.Children.Add(_scaleSlider);
            bar.Children.Add(scalePanel); Grid.SetColumn(scalePanel, 5);

            return bar;
        }

        private Button MakeIconButton(string text, int size)
        {
            return new Button
            {
                Content = text,
                Width = size,
                Height = size,
                FontSize = 12,
                Margin = new Thickness(3, 6, 3, 6),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        // =====================================================================
        //  Skin layout (Canvas positions)
        // =====================================================================
        private void ApplySkin()
        {
            _skin = Skins.ById(_cfg.SkinId);
            _skinSet = Skins.Effective(_skin, _cfg);
            int idx = 0;
            for (int i = 0; i < Skins.All.Count; i++)
                if (Skins.All[i].Id == _skin.Id) idx = i;
            _skinCombo.SelectedIndex = idx;

            _bodyCanvas.Children.Clear();
            _buttons.Clear();
            _ctrlPos.Clear();
            LoadPositions();

            Color accent = ParseColor(_skin.Accent);

            _joyBase = new Grid { Width = 104, Height = 104, Background = GlassBrush(accent, 0.16) };
            _joyBase.Children.Add(new Ellipse
            {
                Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                StrokeThickness = 1,
            });
            _joyKnob = new Ellipse
            {
                Width = 44, Height = 44,
                Fill = GlassBrush(accent, 0.55),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _joyBase.Children.Add(_joyKnob);
            _bodyCanvas.Children.Add(_joyBase);

            _cameraPad = new Border
            {
                Width = 150, Height = 150,
                Background = GlassBrush(Color.FromArgb(160, 255, 255, 255), 0.10),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
            };
            _cameraPad.Child = new TextBlock
            {
                Text = "视角",
                Foreground = Brushes.White,
                Opacity = 0.6,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _bodyCanvas.Children.Add(_cameraPad);

            foreach (var b in _skin.Buttons)
            {
                var border = new Border
                {
                    Width = 46, Height = 46,
                    Background = GlassBrush(accent, 0.30),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(23),
                    Tag = b,
                };
                border.Child = new TextBlock
                {
                    Text = b.Label,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                _bodyCanvas.Children.Add(border);
                _buttons[b.Id] = border;
            }

            PlaceAllControls();
            UpdateStatus();
        }

        private void LoadPositions()
        {
            _ctrlPos["joy"] = new Point(0.14, 0.62);
            _ctrlPos["camera"] = new Point(0.50, 0.50);
            int i = 0;
            foreach (var b in _skin.Buttons)
            {
                int col = i % 2;
                int row = i / 2;
                _ctrlPos[b.Id] = new Point(0.72 + col * 0.17, 0.84 - row * 0.17);
                i++;
            }
            if (_cfg.Skins.ContainsKey(_skin.Id))
            {
                var pos = _cfg.Skins[_skin.Id].Positions;
                if (pos != null)
                    foreach (var kv in pos)
                        if (kv.Value != null && kv.Value.Length >= 2 && _ctrlPos.ContainsKey(kv.Key))
                            _ctrlPos[kv.Key] = new Point(kv.Value[0], kv.Value[1]);
            }
        }

        private void PlaceAllControls()
        {
            if (_joyBase != null) PlaceControl("joy", _joyBase, _joyBase.Width, _joyBase.Height);
            if (_cameraPad != null) PlaceControl("camera", _cameraPad, _cameraPad.Width, _cameraPad.Height);
            foreach (var kv in _buttons)
                PlaceControl(kv.Key, kv.Value, kv.Value.Width, kv.Value.Height);
        }

        private void PlaceControl(string id, FrameworkElement el, double w, double h)
        {
            if (!_ctrlPos.ContainsKey(id)) return;
            Point n = _ctrlPos[id];
            Canvas.SetLeft(el, n.X * BODY_W - w / 2);
            Canvas.SetTop(el, n.Y * BODY_H - h / 2);
        }

        private Brush GlassBrush(Color accent, double alpha)
        {
            var g = new LinearGradientBrush();
            g.StartPoint = new Point(0, 0);
            g.EndPoint = new Point(0, 1);
            Color top = Color.FromArgb((byte)(255 * (alpha + 0.10)), accent.R, accent.G, accent.B);
            Color bot = Color.FromArgb((byte)(255 * alpha), accent.R, accent.G, accent.B);
            g.GradientStops.Add(new GradientStop(top, 0));
            g.GradientStops.Add(new GradientStop(bot, 1));
            return g;
        }

        private static Color ParseColor(string hex)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return Color.FromArgb(255, 63, 208, 201); }
        }

        // =====================================================================
        //  Account / browser
        // =====================================================================
        private void RebuildAccountList()
        {
            _accountCombo.Items.Clear();
            var matches = new List<AccountProfile>();
            foreach (var p in _cfg.Profiles)
                if (p.GameId == _skin.Id) matches.Add(p);
            if (matches.Count == 0)
            {
                _accountCombo.Items.Add("（未配置账号）");
                _accountCombo.SelectedIndex = 0;
                _openBrowserButton.IsEnabled = false;
                return;
            }
            foreach (var p in matches) _accountCombo.Items.Add(p.Name);
            int active = _cfg.ActiveProfile;
            if (active >= 0 && active < matches.Count) _accountCombo.SelectedIndex = active;
            else _accountCombo.SelectedIndex = 0;
            _openBrowserButton.IsEnabled = true;
        }

        private void OpenBrowser()
        {
            int idx = _accountCombo.SelectedIndex;
            var matches = new List<AccountProfile>();
            foreach (var p in _cfg.Profiles)
                if (p.GameId == _skin.Id) matches.Add(p);
            if (idx < 0 || idx >= matches.Count) { UpdateStatus("请先在“管理”中添加账号"); return; }
            bool ok = Browser.Launch(matches[idx], _cfg);
            UpdateStatus(ok ? "已打开浏览器（使用所选账号）" : "未找到 Chrome");
        }

        public void RefreshAfterSettings() { RebuildAccountList(); ApplySkin(); }

        private void UpdateStatus(string msg = null)
        {
            if (_statusText == null) return;
            if (!string.IsNullOrEmpty(msg)) { _statusText.Text = msg; return; }
            string gp = _gamepad != null && _gamepad.Available
                ? "手柄:已连接"
                : "手柄:未连接 · " + (_gamepad != null && !string.IsNullOrEmpty(_gamepad.Error) ? _gamepad.Error : "请安装 ViGEmBus 驱动");
            string mode = _layoutEdit ? "· 拖动控件调整位置，松开即保存" : "";
            _statusText.Text = string.Format("皮肤:{0}  {1}  {2}", _skin.Name, gp, mode);
        }

        // =====================================================================
        //  Pointer / touch handling (crash-safe)
        // =====================================================================
        protected override void OnPreviewTouchDown(TouchEventArgs e)
        {
            try
            {
                Point p = e.GetTouchPoint(this).Position;
                bool claimed = StartPointer(e.TouchDevice.Id, p);
                if (claimed) { e.TouchDevice.Capture(this); e.Handled = true; }
            }
            catch (Exception ex) { CrashLog.Write(ex); }
            base.OnPreviewTouchDown(e);
        }
        protected override void OnPreviewTouchMove(TouchEventArgs e)
        {
            try { MovePointer(e.TouchDevice.Id, e.GetTouchPoint(this).Position); }
            catch (Exception ex) { CrashLog.Write(ex); }
            base.OnPreviewTouchMove(e);
        }
        protected override void OnPreviewTouchUp(TouchEventArgs e)
        {
            try { EndPointer(e.TouchDevice.Id); e.TouchDevice.Capture(null); }
            catch (Exception ex) { CrashLog.Write(ex); }
            base.OnPreviewTouchUp(e);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            try { if (StartPointer(-1, e.GetPosition(this))) e.Handled = true; }
            catch (Exception ex) { CrashLog.Write(ex); }
            base.OnPreviewMouseLeftButtonDown(e);
        }
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            try { if (e.LeftButton == MouseButtonState.Pressed) MovePointer(-1, e.GetPosition(this)); }
            catch (Exception ex) { CrashLog.Write(ex); }
            base.OnPreviewMouseMove(e);
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            try { EndPointer(-1); }
            catch (Exception ex) { CrashLog.Write(ex); }
            base.OnPreviewMouseLeftButtonUp(e);
        }

        private string HitControl(Point p)
        {
            if (_joyBase != null && InEllipse(p, CenterOf(_joyBase), _joyBase.ActualWidth / 2 + 6)) return "joy";
            foreach (var kv in _buttons)
            {
                var border = kv.Value;
                if (InEllipse(p, CenterOf(border), border.ActualWidth / 2 + 2)) return kv.Key;
            }
            if (_cameraPad != null && InRect(p, _cameraPad)) return "camera";
            return null;
        }

        private bool InEllipse(Point p, Point c, double r)
        {
            return (p.X - c.X) * (p.X - c.X) + (p.Y - c.Y) * (p.Y - c.Y) <= r * r;
        }
        private bool InRect(Point p, FrameworkElement el)
        {
            Point tl = el.TranslatePoint(new Point(0, 0), this);
            return new Rect(tl, new Size(el.ActualWidth, el.ActualHeight)).Contains(p);
        }
        private Point CenterOf(FrameworkElement el)
        {
            return el.TranslatePoint(new Point(el.ActualWidth / 2, el.ActualHeight / 2), this);
        }

        private bool StartPointer(long id, Point p)
        {
            string cid = HitControl(p);
            if (cid == null) return false;

            if (_layoutEdit)
            {
                _pointers[id] = new ActivePointer { Id = id, Kind = "drag:" + cid, Last = p };
                return true;
            }

            if (cid == "joy")
            {
                _pointers[id] = new ActivePointer { Id = id, Kind = "joy", Last = p };
                MoveJoystick(p);
                return true;
            }
            if (cid == "camera")
            {
                _pointers[id] = new ActivePointer { Id = id, Kind = "camera", Last = p };
                return true;
            }
            _pointers[id] = new ActivePointer { Id = id, Kind = cid, Last = p };
            PressButton(cid, true);
            return true;
        }

        private void MovePointer(long id, Point p)
        {
            ActivePointer ptr;
            if (!_pointers.TryGetValue(id, out ptr)) return;
            if (ptr.Kind.StartsWith("drag:")) { MoveControlDrag(ptr, p); return; }
            switch (ptr.Kind)
            {
                case "joy": MoveJoystick(p); break;
                case "camera": MoveCamera(ptr, p); break;
                default: break;
            }
        }

        private void MoveJoystick(Point p)
        {
            Point c = CenterOf(_joyBase);
            double dx = p.X - c.X, dy = p.Y - c.Y;
            double mag = Math.Sqrt(dx * dx + dy * dy);
            double maxR = Math.Max(1, _joyBase.ActualWidth / 2 - 12);
            if (mag > maxR) { dx = dx / mag * maxR; dy = dy / mag * maxR; }

            _joyKnob.Margin = new Thickness(dx, dy, -dx, -dy);

            if (!GamepadActive()) return;
            double nx = Math.Max(-1, Math.Min(1, dx / maxR));
            double ny = Math.Max(-1, Math.Min(1, dy / maxR));
            _gamepad.SetStick(false, (short)(nx * 32767), (short)(-ny * 32767)); // Y up = positive
            _gamepad.Update();
        }

        private void MoveCamera(ActivePointer ptr, Point p)
        {
            double dx = p.X - ptr.Last.X;
            double dy = p.Y - ptr.Last.Y;
            ptr.Last = p;
            if (!GamepadActive()) return;

            double sens = _cfg.CameraSensitivity;
            if (sens <= 0) sens = 120;
            double rx = Math.Max(-1, Math.Min(1, dx / sens));
            double ry = Math.Max(-1, Math.Min(1, dy / sens));
            _gamepad.SetStick(true, (short)(rx * 32767), (short)(-ry * 32767));
            _gamepad.Update();
        }

        private void MoveControlDrag(ActivePointer ptr, Point p)
        {
            string cid = ptr.Kind.Substring(5);
            Point origin = _bodyCanvas.TranslatePoint(new Point(0, 0), this);
            double nx = Math.Max(0.02, Math.Min(0.98, (p.X - origin.X) / BODY_W));
            double ny = Math.Max(0.02, Math.Min(0.98, (p.Y - origin.Y) / BODY_H));
            _ctrlPos[cid] = new Point(nx, ny);
            FrameworkElement el = ElementFor(cid);
            if (el != null) PlaceControl(cid, el, el.Width, el.Height);
        }

        private FrameworkElement ElementFor(string cid)
        {
            if (cid == "joy") return _joyBase;
            if (cid == "camera") return _cameraPad;
            if (_buttons.ContainsKey(cid)) return _buttons[cid];
            return null;
        }

        private void EndPointer(long id)
        {
            ActivePointer ptr;
            if (!_pointers.TryGetValue(id, out ptr)) return;
            _pointers.Remove(id);

            if (ptr.Kind.StartsWith("drag:"))
            {
                string cid = ptr.Kind.Substring(5);
                if (!_cfg.Skins.ContainsKey(_skin.Id)) _cfg.Skins[_skin.Id] = new SkinSettings();
                _cfg.Skins[_skin.Id].Positions = new Dictionary<string, double[]>();
                foreach (var kv in _ctrlPos)
                    _cfg.Skins[_skin.Id].Positions[kv.Key] = new[] { kv.Value.X, kv.Value.Y };
                _cfg.Save();
                UpdateStatus("布局已保存");
                return;
            }

            if (ptr.Kind == "joy")
            {
                if (GamepadActive()) { _gamepad.SetStick(false, 0, 0); _gamepad.Update(); }
                _joyKnob.Margin = new Thickness(0);
            }
            else if (_buttons.ContainsKey(ptr.Kind))
            {
                PressButton(ptr.Kind, false);
            }
        }

        private void PressButton(string id, bool press)
        {
            var bdef = _skin.Buttons.Find(x => x.Id == id);
            if (bdef == null) return;
            string pad = _skinSet.Buttons[id];
            if (!GamepadActive()) return;
            if (Pad.IsTrigger(pad))
                _gamepad.SetTrigger(Pad.IsRightTrigger(pad), press);
            else
                _gamepad.SetButton(Pad.Bit(pad), press);
            _gamepad.Update();
        }

        private bool GamepadActive()
        {
            return _gamepad != null && _gamepad.Available;
        }

        // =====================================================================
        //  Focus preservation + close
        // =====================================================================
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                ex |= (int)(Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW);
                Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex);
                Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            }
            catch (Exception ex) { CrashLog.Write(ex); }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _cfg.WindowLeft = Left;
            _cfg.WindowTop = Top;
            _cfg.Save();
            if (_gamepad != null) _gamepad.Dispose();
            base.OnClosing(e);
        }
    }
}
