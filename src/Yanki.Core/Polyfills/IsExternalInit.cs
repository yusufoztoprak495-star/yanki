#if NETSTANDARD2_1
// netstandard2.1 bu tipi içermez; record ve init-only özellikler için derleyici buna ihtiyaç duyar.
// net10.0 hedefinde gerçek tip kullanılır, bu dosya derlenmez.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif
