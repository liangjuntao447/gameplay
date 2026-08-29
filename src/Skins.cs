using System;
using System.Collections.Generic;

namespace TouchCloudPad
{
    /// <summary>A single on-screen action button definition.</summary>
    public class ButtonDef
    {
        public string Id;      // stable id used for config overrides
        public string Label;   // text shown on the button
        public string Pad;     // gamepad button name (e.g. "A", "X", "LB", "LT")
        public bool Hold;      // true = hold while touched (dodge/attack/trigger)
    }

    /// <summary>Defines the touch layout for one game skin.</summary>
    public class SkinDef
    {
        public string Id;
        public string Name;
        public string Accent;              // hex ARGB accent colour
        public List<ButtonDef> Buttons;    // action buttons (right panel)
        public bool ShowCameraPad;         // show the right-side camera drag pad
    }

    /// <summary>Per-skin, user-editable settings (saved to config).</summary>
    public class SkinSettings
    {
        /// <summary>buttonId -> gamepad button name.</summary>
        public Dictionary<string, string> Buttons = new Dictionary<string, string>();
        /// <summary>User-dragged control positions: controlId -> [normalized X, normalized Y].</summary>
        public Dictionary<string, double[]> Positions = new Dictionary<string, double[]>();
    }

    internal static class Skins
    {
        /// <summary>All built-in skins, in order shown in the switcher.</summary>
        public static readonly List<SkinDef> All = new List<SkinDef>
        {
            WuWaves(),
            StarRail(),
            Zenless(),
        };

        public static SkinDef ById(string id)
        {
            foreach (var s in All)
                if (string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase))
                    return s;
            return All[0];
        }

        /// <summary>鸣潮 (Wuthering Waves) — runs in Chrome.</summary>
        private static SkinDef WuWaves()
        {
            return new SkinDef
            {
                Id = "wuwaves",
                Name = "鸣潮 WuWa",
                Accent = "#FF3FD0C9",        // teal accent
                ShowCameraPad = true,
                Buttons = new List<ButtonDef>
                {
                    new ButtonDef { Id="jump",     Label="跳", Pad="A",          Hold=false },
                    new ButtonDef { Id="dodge",    Label="闪", Pad="LB",         Hold=true  },
                    new ButtonDef { Id="attack",   Label="攻", Pad="X",          Hold=true  },
                    new ButtonDef { Id="skill",    Label="技", Pad="Y",          Hold=false },
                    new ButtonDef { Id="ult",      Label="大", Pad="B",          Hold=false },
                    new ButtonDef { Id="echo",     Label="声", Pad="RB",         Hold=false },
                    new ButtonDef { Id="interact", Label="交", Pad="Start",      Hold=false },
                    new ButtonDef { Id="char1",    Label="1",  Pad="DPad_Left",  Hold=false },
                    new ButtonDef { Id="char2",    Label="2",  Pad="DPad_Up",    Hold=false },
                    new ButtonDef { Id="char3",    Label="3",  Pad="DPad_Right", Hold=false },
                }
            };
        }

        /// <summary>崩铁 (Honkai: Star Rail) — runs in Chrome.</summary>
        private static SkinDef StarRail()
        {
            return new SkinDef
            {
                Id = "hsr",
                Name = "崩铁 HSR",
                Accent = "#FFB98DF0",        // light purple accent
                ShowCameraPad = true,
                Buttons = new List<ButtonDef>
                {
                    new ButtonDef { Id="interact", Label="交互", Pad="A",        Hold=false },
                    new ButtonDef { Id="confirm",  Label="确定", Pad="A",        Hold=false },
                    new ButtonDef { Id="dialogue", Label="对话", Pad="B",        Hold=false },
                    new ButtonDef { Id="skill",    Label="技",   Pad="X",        Hold=false },
                    new ButtonDef { Id="map",      Label="图",   Pad="Y",        Hold=false },
                    new ButtonDef { Id="menu",     Label="菜单", Pad="Start",    Hold=false },
                    new ButtonDef { Id="camera",   Label="镜",   Pad="Back",     Hold=false },
                }
            };
        }

        /// <summary>绝区零 (Zenless Zone Zero) — desktop app.</summary>
        private static SkinDef Zenless()
        {
            return new SkinDef
            {
                Id = "zzz",
                Name = "绝区零 ZZZ",
                Accent = "#FFFFB300",        // orange accent
                ShowCameraPad = true,
                Buttons = new List<ButtonDef>
                {
                    new ButtonDef { Id="attack",  Label="普", Pad="X",    Hold=true  },
                    new ButtonDef { Id="special", Label="特", Pad="Y",    Hold=false },
                    new ButtonDef { Id="dodge",   Label="闪", Pad="LB",   Hold=true  },
                    new ButtonDef { Id="assist",  Label="联", Pad="RB",   Hold=false },
                    new ButtonDef { Id="chain",   Label="连", Pad="B",    Hold=false },
                    new ButtonDef { Id="interact",Label="交", Pad="A",    Hold=false },
                    new ButtonDef { Id="switch",  Label="切", Pad="DPad_Up", Hold=false },
                }
            };
        }

        /// <summary>Build the effective settings for a skin, merging defaults + config overrides.</summary>
        public static SkinSettings Effective(SkinDef skin, Config cfg)
        {
            var s = new SkinSettings();
            foreach (var b in skin.Buttons) s.Buttons[b.Id] = b.Pad;

            if (cfg.Skins != null && cfg.Skins.ContainsKey(skin.Id))
            {
                var o = cfg.Skins[skin.Id];
                if (o.Buttons != null)
                    foreach (var b in skin.Buttons)
                        if (o.Buttons.ContainsKey(b.Id) && Pad.Known(o.Buttons[b.Id]))
                            s.Buttons[b.Id] = o.Buttons[b.Id];
            }
            return s;
        }
    }
}
