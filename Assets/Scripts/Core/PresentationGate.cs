using System.Collections.Generic;

namespace SkullKing.Core
{
    /// <summary>Owner-based local presentation barrier; repeated acquisition cannot leak a counter.</summary>
    public sealed class PresentationGate
    {
        private readonly HashSet<object> _owners = new HashSet<object>();
        public bool IsHeld => _owners.Count > 0;
        public void Set(object owner, bool held)
        {
            if (owner == null) return;
            if (held) _owners.Add(owner);
            else _owners.Remove(owner);
        }
    }
}
