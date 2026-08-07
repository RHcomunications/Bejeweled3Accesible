using System.Text;

namespace Bejeweled3Accessible.Audio
{
    // Single source of truth for the PAC XOR cipher, shared by PacReader and
    // PacPacker so the key can never drift between the two sides.
    public static class PacCipher
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("Bejeweled3AccessibleProtectionKey2026");

        public static void Xor(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= Key[i % Key.Length];
            }
        }
    }
}