using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MineClick
{
    unsafe class MineClick : IDalamudPlugin
    {
        const short MouseLeft = 0x01;
        const short VK_NUMPAD0 = 0x60;
        const uint WM_KEYUP = 0x101;
        const uint WM_KEYDOWN = 0x100;
        DalamudPluginInterface pi;
        long clickTime = 0;
        long pressTime = 0;
        bool draw = false;
        bool enable = false;
        bool active = false;
        bool mouseDown = false;
        Vector2 pos = new Vector2(0, 0);
        IntPtr hwnd;
        public string Name => "MineClick";

        public void Dispose()
        {
            pi.Framework.OnUpdateEvent -= Tick;
            pi.UiBuilder.OnBuildUi -= Draw;
            pi.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            pi = pluginInterface;
            hwnd = Process.GetCurrentProcess().MainWindowHandle;
            pi.Framework.OnUpdateEvent += Tick;
            pi.UiBuilder.OnBuildUi += Draw;
        }

        [HandleProcessCorruptedStateExceptions]
        void Tick(object _)
        {
            try
            {
                var addon = pi.Framework.Gui.GetUiObjectByName("Gathering", 1);
                if (addon != IntPtr.Zero)
                {
                    draw = true;
                    var aub = (AtkUnitBase*)addon;
                    pos.X = aub->X + 100;
                    pos.Y = aub->Y + 1;
                    if(enable)
                    {
                        var ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        if (GetForegroundWindow() == hwnd)
                        {
                            if(IsBitSet(GetKeyState(MouseLeft), 15))
                            {
                                if (!mouseDown)
                                {
                                    mouseDown = true;
                                    if (ms - clickTime < 250)
                                    {
                                        active = true;
                                    }
                                    //pi.Framework.Gui.Chat.Print((ms - clickTime).ToString());
                                    clickTime = ms;
                                }
                            }
                            else
                            {
                                mouseDown = false;
                            }
                            
                        }
                        if (active && ms - pressTime > 300)
                        {
                            pressTime = ms;
                            SendMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_NUMPAD0, (IntPtr)0);
                            SendMessage(hwnd, WM_KEYUP, (IntPtr)VK_NUMPAD0, (IntPtr)0);
                        }
                    }
                }
                else
                {
                    active = false;
                    draw = false;
                }
            }
            catch(Exception e)
            {
                pi.Framework.Gui.Chat.Print("MineClick Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        void Draw()
        {
            if (draw)
            {
                ImGui.SetNextWindowPos(pos);
                ImGui.Begin("MineClick", ref draw, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);
                ImGui.Checkbox("Enable##MineClick", ref enable);
                if(active)
                {
                    ImGui.SameLine();
                    if(ImGui.Button("Stop##MineClick")) active = false;
                }
                ImGui.End();
            }
        }

        [DllImport("User32.dll")]
        static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        static bool IsBitSet(short b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
    }
}
