using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKNailForRevit
{
    public class KeypadCommand : JsonSerializable, IEquatable<KeypadCommand>
    {
        public enum KeyDef
        {
            None = -1, Up = 0, UpRight = 1, Right = 2, DownRight = 3,
            Down = 4, DownLeft = 5, Left = 6, UpLeft = 7
        }

        private KeyDef m_key;
        private int m_numClicks;

        public KeypadCommand()  { m_key = KeyDef.None; m_numClicks = 0; }
        public KeypadCommand(KeyDef c, int n)  { m_key = c; m_numClicks = n; }
        public KeyDef CursorKey  { get { return m_key; } set { m_key = value; } }
        public int NumClicks  { get { return m_numClicks; } set { m_numClicks = value; } }

        public static KeyDef KeyDefFromString(string s)
        {
            string sl = s.ToLower();
                 if (sl == @"up") { return KeyDef.Up; }
            else if (sl == @"upright") { return KeyDef.UpRight; }
            else if (sl == @"right") { return KeyDef.Right; }
            else if (sl == @"downright") { return KeyDef.DownRight; }
            else if (sl == @"down") { return KeyDef.Down; }
            else if (sl == @"downleft") { return KeyDef.DownLeft; }
            else if (sl == @"left") { return KeyDef.Left; }
            else if (sl == @"upleft") { return KeyDef.UpLeft; }
            else { return KeyDef.None; }
        }

        public bool Equals(KeypadCommand other)
        { return (m_key == other.m_key && m_numClicks == other.m_numClicks); }
    }

    public class KeypadCommandPhrase : List<KeypadCommand> { }

    public class KeypadCommandEventArgs : EventArgs
    {
        private KeypadCommandPhrase m_cmdPhrase;
        public KeypadCommandEventArgs(KeypadCommandPhrase cmdPhrase)
        { m_cmdPhrase = cmdPhrase; }
        public KeypadCommandPhrase CommandPhrase { get { return m_cmdPhrase; } }
    }

    public delegate void KeypadCommandEventHandler(object sender, KeypadCommandEventArgs e);

    public interface IKeypadCommandProvider
    {
        void PushContext(KeypadCommandEventHandler handler);
        void PopContext();
    }
}
