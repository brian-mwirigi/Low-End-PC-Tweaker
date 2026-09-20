using System.Runtime.InteropServices;

namespace Tweakwell;

public sealed class ClientAreaAnimation : IClientAreaAnimation
{
    private const uint SpiGetClientAreaAnimation = 0x1042;
    private const uint SpiSetClientAreaAnimation = 0x1043;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendChange = 0x02;

    public bool GetEnabled()
    {
        var enabled = 0;
        NativeMethods.SystemParametersInfo(SpiGetClientAreaAnimation, 0, ref enabled, 0);
        return enabled != 0;
    }

    public void SetEnabled(bool enabled)
    {
        var value = enabled ? 1 : 0;
        NativeMethods.SystemParametersInfo(SpiSetClientAreaAnimation, 0, ref value, SpifUpdateIniFile | SpifSendChange);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);
    }
}
