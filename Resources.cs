using Binarysharp.MemoryManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
// I would list the name of the person who has credit to the cheat stuff, but he's a whiny bitch so I'm not going to mention him.
public static class StringExtensions
{
    public static string[] Split(this string source, string seperator)
    {
        return source.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
    public static Match Match(this Regex regex, string input, string pattern)
    {
        return Regex.Match(input, pattern, RegexOptions.IgnoreCase);
    }
    public static MatchCollection Matches(this Regex regex, string input, string pattern)
    {
        return Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
    }
    public static bool IsMatch(this Regex regex, string input, string pattern)
    {
        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
    public static string ReplaceAtIndex(this string targetString, int index, string insertString)
    {
        char[] replacementSequence = targetString.Skip(index).Take(insertString.Length).ToArray();
        return targetString.Replace(new string(replacementSequence), insertString);
    }
    public static byte[] StringToByteArray(this string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray()
            .Reverse()
            .ToArray();
    }
}
public class Resources
{
    // Declare global vars
    public MemorySharp M;
    public Process[] Pcsx2;
    public readonly string PCSX2PROCESS = "pcsx2";
    public readonly string SOCOM2 = "3237395F";
    public bool pcsx2running;
    public bool conditional;
    public static int PCSX2Version;
    public void SetProcess()
    {
        Pcsx2 = Process.GetProcessesByName(PCSX2PROCESS);
    }
    public bool ConnectPCSX2()
    {
        SetProcess();
        if (Pcsx2.Length > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public MemorySharp SetM(string process)
    {
        return M = new MemorySharp(Process.GetProcessesByName(process).FirstOrDefault());
    }
    public bool TestR()
    {
        try
        {
            int r = M.Read<int>((IntPtr)0x20000000, false);
            if (r != 0)
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }
    public void ResetEE(int PCSX2Version)
    {
        //Shoutout to NightFyre for this!
        int EE;
        IntPtr address;
        switch (PCSX2Version)
        {
            case 1: EE = 0x277D60; address = SetM(PCSX2PROCESS).Modules.MainModule.BaseAddress + EE; M.Assembly.ExecuteAsync(address); break; //1.5 pcsx2dis
            case 2: EE = 0x265570; address = SetM(PCSX2PROCESS).Modules.MainModule.BaseAddress + EE; M.Assembly.ExecuteAsync(address); break; //1.6 pcsx2
            case 3: EE = 0xAFD6078; address = SetM(PCSX2PROCESS).Modules.MainModule.BaseAddress + EE; break; //1.7 pcsx2
        }
    }
    public IntPtr GetProcess(Process[] handle, IntPtr module, string name)
    {
        if (handle != null && module != null && name != null)
        {
            return GetProcess(handle, module, name);
        }
        return (IntPtr)0x00000000;
    }
    public enum CheatType
    {
        _8BitWrite = 0x00,
        _16BitWrite = 0x10,
        _32BitWrite = 0x20,
        _copyBytes = 0x50,
        _pointerWrite = 0x70,
        _timer = 0xB0,
        _32BitCondition = 0xC0,
        _16BitCondition = 0xD0
    }
    public enum CheatTimerIntervalType
    {
        Ticks = 0x01,
        Seconds = 0x02,
        Minutes = 0x03
    }
    public class Cheat
    {
        public int Position { get; set; }
        public CheatType CheatType { get; set; }
        public int Address { get; set; }
        public byte[] Data { get; set; }
        public string label { get; set; }
    }
    public class CheatBlock
    {
        public string Label { get; set; }
        public List<Cheat> Cheats { get; set; } = new List<Cheat>();
        public static CheatBlock Parse(string block)
        {
            string label = Regex.Match(block, "(#.{1,})\r\n", RegexOptions.IgnoreCase).Groups[1]?.Value;
            CheatBlock cheatBlock = new CheatBlock() { Label = label };
            MatchCollection cheatMatches = Regex.Matches(block, "([a-f0-9]{8}) ([a-f0-9]{8})", RegexOptions.IgnoreCase);
            int i = 0;
            foreach (Match match in cheatMatches)
            {
                string address = match.Groups[1].Value;
                byte[] data = Enumerable.Range(0, match.Groups[2].Value.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(match.Groups[2].Value.Substring(x, 2), 16))
                    .ToArray();
                Array.Reverse(data);
                cheatBlock.Cheats.Add(new Cheat()
                {
                    Position = i,
                    Address = Convert.ToInt32(address.Substring(2, 6), 16),
                    CheatType = (CheatType)Convert.ToInt32(address.Substring(0, 2), 16),
                    Data = data
                });
                i++;
            }
            return cheatBlock;
        }
    }
    public interface IByteEditor
    {
        byte[] Read(int address, int byteLength);
        byte[] Read(string addressHex, int byteLength);
        byte[] Read(UIntPtr address, int byteLength);
        void Write(int address, byte[] data);
        void Write(string addressHex, byte[] data);
        void Write(UIntPtr address, byte[] data);
    }
    public class BaseEditor : IByteEditor
    {
        protected byte[] _bytes { get; set; } = new byte[33554432];

        public byte[] Read(int address, int byteLength)
        {
            return _bytes
                           .Skip(address)
                           .Take(byteLength)
                           .ToArray();
        }
        public byte[] Read(string addressHex, int byteLength)
        {
            UIntPtr address = (UIntPtr)Convert.ToInt32(addressHex, 16);
            return Read(address, byteLength);
        }
        public byte[] Read(UIntPtr address, int byteLength)
        {
            return Read((int)address, byteLength);
        }
        public void Write(int address, byte[] data)
        {
            data.CopyTo(_bytes, address);
        }
        public void Write(string addressHex, byte[] data)
        {
            UIntPtr address = (UIntPtr)Convert.ToInt32(addressHex, 16);
            Write(address, data);
        }
        public void Write(UIntPtr address, byte[] data)
        {
            Write((int)address, data);
        }
    }
    public class CheatEngine
    {
        private CheatList _cheatList { get; set; }
        private IByteEditor _byteEditor { get; set; }
        private int _memoryOffset { get; set; } = 0x20000000;
        private readonly MemorySharp m;
        private List<CheatTimer> _cheatTimers { get; } = new List<CheatTimer>();
        Resources t = new Resources();
        public CheatEngine(IByteEditor byteEditor)
        {
            _byteEditor = byteEditor;
        }
        public CheatEngine(IByteEditor byteEditor, MemorySharp Process, int memoryOffset = 0x00000000)
        {
            _byteEditor = byteEditor;
            _memoryOffset = memoryOffset;
            m = Process;
        }
        public CheatEngine(IByteEditor byteEditor, CheatList cheatList)
        {
            _byteEditor = byteEditor;
            _cheatList = cheatList;
        }
        public CheatEngine(IByteEditor byteEditor, CheatList cheatList, int memoryOffset = 0x00000000)
        {
            _byteEditor = byteEditor;
            _cheatList = cheatList;
            _memoryOffset = memoryOffset;
        }
        public void PatchMemory(CheatList cheatList = null)
        {
            _cheatList = _cheatList ?? cheatList;
            foreach (CheatBlock cheatBlock in _cheatList.Parse())
            {
                PatchMemory(cheatBlock);
            }
        }
        public void PatchMemory(CheatBlock cheatBlock)
        {
            for (int i = 0; i < cheatBlock.Cheats.Count;)
            {
                i = i + ApplyCheat(cheatBlock, cheatBlock.Cheats[i]);
            }
        }
        public int ApplyCheat(CheatBlock cheatBlock, Cheat cheat)
        {
            switch (cheat.CheatType)
            {
                case CheatType._8BitWrite:
                    return _8BitWrite(cheat);
                case CheatType._16BitWrite:
                    return _16BitWrite(cheat);
                case CheatType._32BitWrite:
                    return _32BitWrite(cheat);
                case CheatType._copyBytes:
                    return CopyBytes(cheatBlock, cheat);
                case CheatType._pointerWrite:
                    return PointerWrite(cheatBlock, cheat);
                case CheatType._timer:
                    return Timer(cheatBlock, cheat);
                case CheatType._32BitCondition:
                    return _32BitCondition(cheatBlock, cheat);
                case CheatType._16BitCondition:
                    return _16BitCondition(cheatBlock, cheat);
            }
            return 0;
        }
        public int CopyBytes(CheatBlock cheatBlock, Cheat cheat)
        {
            int copyLength = BitConverter.ToInt32(cheat.Data, 0);
            byte[] bytesToCopy = m.Read<byte>((IntPtr)(cheat.Address + _memoryOffset), copyLength, false);
            m.Write((IntPtr)(cheatBlock.Cheats[cheat.Position + 1].Address + _memoryOffset), bytesToCopy);
            return 2;
        }
        public int Timer(CheatBlock cheatBlock, Cheat cheat)
        {
            CheatTimer cheatTimer = _cheatTimers.SingleOrDefault(c => c.Cheat == cheat);

            if (cheatTimer == null)
            {
                byte[] bytes = cheat.Data
                .Take(2)
                .ToArray();

                CheatTimerIntervalType timerIntervalType = (CheatTimerIntervalType)BitConverter.ToInt32(bytes, 0);

                int timerInterval = BitConverter.ToInt32(cheat.Data
                .Skip(1)
                .Take(3)
                .ToArray(), 0);

                _cheatTimers.Add(new CheatTimer(cheat, timerIntervalType, timerInterval));
            }
            if (cheatTimer.IsIntervalCriteriaMet())
            {
                return 1;
            }
            int currentPosition = cheatBlock.Cheats.IndexOf(cheat);
            return cheatBlock.Cheats.Count;
        }
        public int PointerWrite(CheatBlock cheatBlock, Cheat cheat)
        {
            int pointer = Convert.ToInt32(m.Read<byte>((IntPtr)(cheat.Address + _memoryOffset), 0x08, false));
            return 0;
        }
        public int _8BitWrite(Cheat cheat)
        {
            m.Write((IntPtr)(cheat.Address + _memoryOffset), cheat.Data
                .Take(1)
                .ToArray(), false);
            return 1;
        }
        public int _16BitWrite(Cheat cheat)
        {
            m.Write((IntPtr)(cheat.Address + _memoryOffset), cheat.Data
                .Take(2)
                .ToArray(), false);
            return 1;
        }
        public int _32BitWrite(Cheat cheat)
        {
            m.Write((IntPtr)(cheat.Address + _memoryOffset), cheat.Data, false);
            return 1;
        }
        public int _16BitCondition(CheatBlock cheatBlock, Cheat cheat)
        {
            IEnumerable<byte> data = m
            .Read<byte>((IntPtr)(cheat.Address + _memoryOffset), 0x04, false)
            .Take(2);
            bool conditiontrue = cheat.Data
            .Take(2)
            .SequenceEqual(data);
            if (conditiontrue)
            {
                t.ResetEE(PCSX2Version);
                Thread.Sleep(250);
            }
            return conditiontrue ? 1 : cheatBlock.Cheats.Count;
        }
        public int _32BitCondition(CheatBlock cheatBlock, Cheat cheat)
        {
            byte[] data = m
            .Read<byte>((IntPtr)(cheat.Address + _memoryOffset), 0x04, false);
            bool conditiontrue = cheat.Data
            .SequenceEqual(data);
            if (conditiontrue)
            {
                t.ResetEE(PCSX2Version);
                Thread.Sleep(250);
            }
            return conditiontrue ? 1 : cheatBlock.Cheats.Count;
        }
    }

    public class CheatList
    {
        private string _cheatListText;
        private List<CheatBlock> _cheatList { get; set; } = new List<CheatBlock>();
        public CheatList(string cheatList)
        {
            _cheatListText = cheatList;
        }
        public List<CheatBlock> Parse(string cheatList = null)
        {
            _cheatListText = _cheatListText ?? cheatList;

            string[] blocks = _cheatListText.Split("\r\n\r\n");

            foreach (string block in blocks)
            {
                _cheatList.Add(CheatBlock.Parse(block));
            }
            return _cheatList;
        }
    }
    public class CheatTimer
    {
        public Cheat Cheat { get; set; }
        private CheatTimerIntervalType _cheatTimerIntervalType { get; set; }
        private DateTime _intervalDateTime { get; set; }
        private DateTime _currentDateTime { get; set; }
        private int _interval { get; set; }
        public CheatTimer(Cheat cheat, CheatTimerIntervalType cheatTimerIntervalType, int interval)
        {
            Cheat = cheat;
            _interval = interval;
            _cheatTimerIntervalType = cheatTimerIntervalType;
            SetCheatTimer();
        }
        public void SetCheatTimer()
        {
            _currentDateTime = DateTime.Now;
            switch (_cheatTimerIntervalType)
            {
                case CheatTimerIntervalType.Ticks:
                    _intervalDateTime = _currentDateTime.AddTicks(_interval);
                    break;
                case CheatTimerIntervalType.Seconds:
                    _intervalDateTime = _currentDateTime.AddSeconds(_interval);
                    break;
                case CheatTimerIntervalType.Minutes:
                    _intervalDateTime = _currentDateTime.AddMinutes(_interval);
                    break;
            }
        }
        public bool IsIntervalCriteriaMet()
        {
            if (_currentDateTime < _intervalDateTime)
            {
                SetCheatTimer();
                return true;
            }
            return false;
        }
    }
    public static class FlashWindow
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
        /// Stop flashing. The system restores the window to its original state.            
        public const uint FLASHW_STOP = 0;
        /// Flash the window caption.            
        public const uint FLASHW_CAPTION = 1;
        /// Flash the taskbar button.            
        public const uint FLASHW_TRAY = 2;
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.            
        public const uint FLASHW_ALL = 3;
        /// Flash continuously, until the FLASHW_STOP flag is set.            
        public const uint FLASHW_TIMER = 4;
        /// Flash continuously until the window comes to the foreground.            
        public const uint FLASHW_TIMERNOFG = 12;
        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            /// The size of the structure in bytes.
            public uint cbSize;
            /// A Handle to the Window to be Flashed. The window can be either opened or minimized.
            public IntPtr hwnd;
            /// The Flash Status.                
            public uint dwFlags;
            /// The number of times to Flash the window.
            public uint uCount;
            /// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.                
            public uint dwTimeout;
        }
        /// Flash the specified Window (Form) until it receives focus.            
        public static bool Flash(Form form)
        {
            // Make sure we're running under Windows 2000 or later
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }
        /// Flash the specified Window (form) for the specified number of times            
        public static bool Flash(Form form, uint count)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_ALL, count, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }
        private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
            fi.hwnd = handle;
            fi.dwFlags = flags;
            fi.uCount = count;
            fi.dwTimeout = timeout;
            return fi;
        }
        /// helper methods           
        public static bool Tray(Form form)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_TRAY, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }
        public static bool TrayAndWindow(Form form)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_ALL, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }
        /// Stop Flashing the specified Window (form)            
        public static bool Stop(Form form)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_STOP, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }
        /// A boolean value indicating whether the application is running on Windows 2000 or later.
        private static bool Win2000OrLater => Environment.OSVersion.Version.Major >= 5;
    }
}

