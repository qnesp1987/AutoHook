using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

namespace AutoHook.Utils;

internal unsafe class MovementLock : IDisposable
{
    private static readonly int[] BlockedKeys = { 321, 322, 323, 324, 325, 326 };

    [Signature("F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7", ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Infallible)]
    private nint _forceDisableMovementPtr;
    private ref int ForceDisableMovement => ref *(int*)(_forceDisableMovementPtr + 4);

    private delegate byte InputKeyDelegate(nint a1, int key);

    [Signature("E8 ?? ?? ?? ?? 4C 8D 76 06", DetourName = nameof(IsKeyPressedDetour), Fallibility = Fallibility.Infallible)]
    private Hook<InputKeyDelegate>? _isPressedHook;

    [Signature("48 89 5C 24 ?? 56 41 56 41 57 48 83 EC 20 48 63 C2", DetourName = nameof(IsKeyClickedDetour), Fallibility = Fallibility.Infallible)]
    private Hook<InputKeyDelegate>? _isClickedHook;

    [Signature("E8 ?? ?? ?? ?? 84 C0 74 35 EB 05", DetourName = nameof(IsKeyHeldDetour), Fallibility = Fallibility.Infallible)]
    private Hook<InputKeyDelegate>? _isHeldHook;

    [Signature("E8 ?? ?? ?? ?? 88 43 0F", DetourName = nameof(IsKeyReleasedDetour), Fallibility = Fallibility.Infallible)]
    private Hook<InputKeyDelegate>? _isReleasedHook;

    public bool Locked { get; private set; }

    public MovementLock()
    {
        Svc.Hook.InitializeFromAttributes(this);
    }

    public void Lock()
    {
        if (Locked) return;
        _isPressedHook?.Enable();
        _isClickedHook?.Enable();
        _isHeldHook?.Enable();
        _isReleasedHook?.Enable();
        ForceDisableMovement++;
        Locked = true;
    }

    public void Unlock()
    {
        if (!Locked) return;
        _isPressedHook?.Disable();
        _isClickedHook?.Disable();
        _isHeldHook?.Disable();
        _isReleasedHook?.Disable();
        if (ForceDisableMovement > 0) ForceDisableMovement--;
        Locked = false;
    }

    private byte IsKeyPressedDetour(nint a1, int key)
        => BlockedKeys.Contains(key) ? (byte)0 : _isPressedHook!.Original(a1, key);

    private byte IsKeyClickedDetour(nint a1, int key)
        => BlockedKeys.Contains(key) ? (byte)0 : _isClickedHook!.Original(a1, key);

    private byte IsKeyHeldDetour(nint a1, int key)
        => BlockedKeys.Contains(key) ? (byte)0 : _isHeldHook!.Original(a1, key);

    private byte IsKeyReleasedDetour(nint a1, int key)
        => BlockedKeys.Contains(key) ? (byte)0 : _isReleasedHook!.Original(a1, key);

    public void Dispose()
    {
        Unlock();
        _isPressedHook?.Dispose();
        _isClickedHook?.Dispose();
        _isHeldHook?.Dispose();
        _isReleasedHook?.Dispose();
    }
}
