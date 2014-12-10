using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ACT.TPMonitor
{
    class Util
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        public static IntPtr FFXIVProcessId { private get; set; }
        public static Language GameLanguage { get; private set; }

        private static Rectangle _screenRect;
        public static Widget _partyListUI;
        public static Dictionary<Role, int> SortOrder;

        private static string _basePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"My Games\FINAL FANTASY XIV - A Realm Reborn");
        private static string _cfgFile = Path.Combine(_basePath, @"FFXIV.cfg");
        private static readonly string _ADDON_DAT = @"ADDON.DAT";
        private static readonly string _COMMON_DAT = @"COMMON.DAT";
        private static DateTime _addonLastWrite = DateTime.MinValue;
        private static DateTime _commonLastWrite = DateTime.MinValue;

        public static void InitializedAtLogin()
        {
            _addonLastWrite = DateTime.MinValue;
            _commonLastWrite = DateTime.MinValue;

            using (StreamReader sr = new StreamReader(_cfgFile, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Equals("<Version>"))
                    {
                        // read skip
                        line = sr.ReadLine();   //GuidVersion
                        line = sr.ReadLine();   //ConfigVersion
                        line = sr.ReadLine();   //Language

                        GameLanguage = (Language)int.Parse(line.Split('\t')[1].ToString());
                    }
                    else if (line.Equals("<Network Settings>"))
                    {
                        // read skip
                        sr.ReadLine();  //UPnP
                        sr.ReadLine();  //Port
                        sr.ReadLine();  //LastLogin0
                        sr.ReadLine();  //LastLogin1
                    }
                }
            }
        }

        private static Rectangle GetWindowSize(string path)
        {
            Rectangle screenRect = new Rectangle(new Point(0, 0), new Size(0, 0));

            using (StreamReader sr = new StreamReader(_cfgFile, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Equals("<Display Settings>"))
                    {
                        // read skip
                        sr.ReadLine();  //MainAdapter

                        // Location, ScreenSize
                        int left = (int)uint.Parse(sr.ReadLine().Split('\t')[1].ToString());    //ScreenLeft
                        int top = (int)uint.Parse(sr.ReadLine().Split('\t')[1].ToString());     //ScreenTop
                        int width = int.Parse(sr.ReadLine().Split('\t')[1].ToString());         //ScreenWidth
                        int height = int.Parse(sr.ReadLine().Split('\t')[1].ToString());        //ScreenHeight

                        //ScreenMode (0:Window, 1:FullScreen, 2:Virtual)
                        int mode = int.Parse(sr.ReadLine().Split('\t')[1].ToString());

                        RECT windowRect = new RECT();
                        GetWindowRect(FFXIVProcessId, ref windowRect);

                        switch (mode)
                        {
                            case 0:
                                // Window
                                if (FFXIVProcessId != IntPtr.Zero)
                                {
                                    RECT clientRect = new RECT();
                                    GetClientRect(FFXIVProcessId, ref clientRect);

                                    left = windowRect.left + SystemInformation.FrameBorderSize.Width;
                                    top = windowRect.bottom - clientRect.bottom - SystemInformation.FrameBorderSize.Height;
                                    width = clientRect.right;
                                    height = clientRect.bottom;
                                }
                                break;
                            case 1:
                                // FullScreen
                                left = 0;
                                top = 0;
                                width = int.Parse(sr.ReadLine().Split('\t')[1].ToString());     //FullScreenWidth
                                height = int.Parse(sr.ReadLine().Split('\t')[1].ToString());    //FullScreenHeight
                                break;
                            case 2:
                                // Virtual
                                Screen s = Screen.FromPoint(new Point(left + width / 2, top + height / 2));
                                left = s.Bounds.Left;
                                top = s.Bounds.Top;
                                width = s.Bounds.Width;
                                height = s.Bounds.Height;
                                break;
                            default:
                                break;
                        }

                        screenRect = new Rectangle(new Point(left, top), new Size(width, height));
                        break;
                    }
                }
            }
            return screenRect;
        }

        public static Widget GetPartyListLocation(string path)
        {
            return GetPartyListLocation(path, 0f);
        }

        public static Widget GetPartyListLocation(string path, float scale)
        {
            string addonFile = Path.Combine(path, _ADDON_DAT);
            _screenRect = GetWindowSize(path);
            Widget widget = new Widget();
            widget.Rect = new Rectangle(new Point(0, 0), new Size(0, 0));
            widget.Scale = 1.0f;

            using (System.IO.StreamReader sr = new System.IO.StreamReader(addonFile, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                string s = sr.ReadToEnd();
                string[] textLine = s.Split('\n');
                for (int i = 0; i < textLine.Length; i++)
                {
                    if (textLine[i].Equals("n:_PartyList_a"))
                    {
                        float widthPercent = GetFloat(textLine[i + 2].Substring(2));
                        float heightPercent = GetFloat(textLine[i + 3].Substring(2));
                        int width = int.Parse(textLine[i + 4].Substring(2));
                        int height = int.Parse(textLine[i + 5].Substring(2));
                        float widgetScale = scale == 0f ? GetFloat(textLine[i + 7].Substring(2)) : scale;

                        width = (int)(width * widgetScale);
                        height = (int)(height * widgetScale);

                        int x;
                        if (widthPercent < 30)
                        {
                            x = (int)((_screenRect.Width * widthPercent / 100));
                        }
                        else if (widthPercent < 70)
                        {
                            x = (int)((_screenRect.Width * (widthPercent / 100)) - (width / 2));
                        }
                        else
                        {
                            x = (int)((_screenRect.Width * (widthPercent / 100)) - width);
                        }
                        x += _screenRect.Left;

                        int y;
                        if (heightPercent < 30)
                        {
                            y = (int)((_screenRect.Height * heightPercent / 100));
                        }
                        else if (heightPercent < 80)
                        {
                            y = (int)((_screenRect.Height * (heightPercent / 100)) - (height / 2));
                        }
                        else
                        {
                            y = (int)((_screenRect.Height * (heightPercent / 100)) - height);
                        }
                        y += _screenRect.Top;
                        widget.Rect = new Rectangle(new Point(x, y), new Size(width, height));
                        widget.Scale = widgetScale;
                        _partyListUI = widget;
                        break;
                    }
                }
            }
            return _partyListUI;
        }

        public static void SetPartySort(string path)
        {
            string commonFile = Path.Combine(path, _COMMON_DAT);
            if (_commonLastWrite.CompareTo(File.GetLastWriteTime(commonFile)) != -1)
            {
                return;
            }

            SortOrder = new Dictionary<Role, int>();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(commonFile, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                string s = sr.ReadToEnd();
                string[] textLine = s.Split(new string[] { "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < textLine.Length; i++)
                {
                    if (textLine[i].StartsWith("PartyListSortTypeTank"))
                    {
                        SortOrder.Add(Role.TANK, int.Parse(textLine[i].Split('\t')[1]));
                        continue;
                    }
                    else if (textLine[i].StartsWith("PartyListSortTypeHealer"))
                    {
                        SortOrder.Add(Role.HEALER, int.Parse(textLine[i].Split('\t')[1]));
                        continue;
                    }
                    else if (textLine[i].StartsWith("PartyListSortTypeDps"))
                    {
                        SortOrder.Add(Role.DPS, int.Parse(textLine[i].Split('\t')[1]));
                        continue;
                    }
                    else if (textLine[i].StartsWith("PartyListSortTypeOther"))
                    {
                        SortOrder.Add(Role.OTHER, int.Parse(textLine[i].Split('\t')[1]));
                        break;
                    }
                }
            }
            _commonLastWrite = File.GetLastWriteTime(commonFile);
        }

        private static float GetFloat(string v)
        {
            if (v.IndexOf(".") > 0)
                return float.Parse(v);
            else
            {
                System.Globalization.CultureInfo cultureFr = new System.Globalization.CultureInfo("fr-fr");
                return float.Parse(v, cultureFr);
            }
        }

        public static bool IsActive(IntPtr hWnd)
        {
            return hWnd == GetForegroundWindow();
        }
    }
}
