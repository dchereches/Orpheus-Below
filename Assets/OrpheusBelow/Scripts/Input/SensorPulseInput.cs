using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    public enum SensorSourceMode
    {
        KeyboardAndMouseOnly,
        KeyboardPlusPCMicrophone,
        KeyboardPlusESP32Serial
    }

    /// <summary>
    /// Unified Piezo / BMP280 Breath / PC Mic / Keyboard+Mouse attack input.
    /// Emits OnAttackPulse(intensity) in [0..1]:
    ///   - intensity < hardBoomThreshold (0.60)  -> Soft Lyre Attack
    ///   - intensity >= hardBoomThreshold (0.60) -> HARD ORPHIC BOOM!
    /// </summary>
    public class SensorPulseInput : MonoBehaviour
    {
        public static SensorPulseInput Instance { get; private set; }

        [Header("Input Source ([Tab] cycles in-game)")]
        public SensorSourceMode sourceMode = SensorSourceMode.KeyboardAndMouseOnly;
        public string serialPortName = "COM5";
        public int baudRate = 115200;

        [Header("Soft vs Hard Boom Thresholds")]
        [Range(0.05f, 0.40f)] public float softMinThreshold = 0.14f;
        [Range(0.45f, 0.85f)] public float hardBoomThreshold = 0.60f;
        public float pulseCooldown = 0.13f;

        [Header("Keyboard/Mouse Charge Tuning")]
        public float fullChargeSeconds = 0.40f;
        [Range(1f, 8f)] public float micSensitivity = 3.2f;

        // Live telemetry for HUD meter
        public float CurrentLiveLevel { get; private set; }
        public float LastPulseIntensity { get; private set; }
        public bool LastPulseWasBoom { get; private set; }
        public float LastPulseTime { get; private set; } = -10f;
        public bool SerialConnected => _serialRunning;

        public event Action<float, bool> OnAttackPulse; // (intensity 0..1, isHardBoom)

        // Keyboard/Mouse charge state
        private bool _isCharging;
        private float _chargeHoldTime;
        private float _cooldownTimer;

        // PC Microphone state
        private AudioClip _micClip;
        private string _micDevice;
        private readonly float[] _micSamples = new float[256];
        private float _micEnvelope;
        private float _micPeakHold;
        private bool _micRising;

        // Win32 Serial background reader (works in .NET Standard 2.1 without System.IO.Ports!)
        private Thread _serialThread;
        private volatile bool _serialRunning;
        private IntPtr _serialHandle = new IntPtr(-1);
        private readonly ConcurrentQueue<float> _serialPulseQueue = new ConcurrentQueue<float>();
        private volatile float _serialLiveLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ApplySourceMode();
        }

        private void OnDestroy()
        {
            StopMic();
            StopSerial();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.unscaledDeltaTime;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                CycleSourceMode();
            }

            UpdateKeyboardAndMouse();

            if (sourceMode == SensorSourceMode.KeyboardPlusPCMicrophone)
            {
                UpdateMicrophone();
            }
            else if (sourceMode == SensorSourceMode.KeyboardPlusESP32Serial)
            {
                UpdateSerialQueue();
            }
        }

        public void CycleSourceMode()
        {
            sourceMode = (SensorSourceMode)(((int)sourceMode + 1) % 3);
            ApplySourceMode();
        }

        private void ApplySourceMode()
        {
            StopMic();
            StopSerial();

            if (sourceMode == SensorSourceMode.KeyboardPlusPCMicrophone)
            {
                StartMic();
            }
            else if (sourceMode == SensorSourceMode.KeyboardPlusESP32Serial)
            {
                StartSerial();
            }
        }

        private void UpdateKeyboardAndMouse()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad pad = Gamepad.current;

            // Instant MAX HARD BOOM shortcut: Right Click / K / Gamepad Right Trigger
            bool instantBoom = (mouse != null && mouse.rightButton.wasPressedThisFrame)
                            || (kb != null && kb.kKey.wasPressedThisFrame)
                            || (pad != null && pad.buttonNorth.wasPressedThisFrame);

            if (instantBoom)
            {
                _isCharging = false;
                _chargeHoldTime = 0f;
                TriggerPulse(1.0f);
                return;
            }

            // Analog Tap-vs-Charge on Left Click / J / Gamepad West (Square/X):
            //   Quick tap (<0.15s) = crisp Soft Lyre Attack (~0.35)
            //   Hold & release     = scaled 0.25..1.00 (crosses 0.60 BOOM line!)
            bool attackPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                              || (kb != null && kb.jKey.wasPressedThisFrame)
                              || (pad != null && pad.buttonWest.wasPressedThisFrame);

            bool attackHeld = (mouse != null && mouse.leftButton.isPressed)
                           || (kb != null && kb.jKey.isPressed)
                           || (pad != null && pad.buttonWest.isPressed);

            bool attackReleased = (mouse != null && mouse.leftButton.wasReleasedThisFrame)
                               || (kb != null && kb.jKey.wasReleasedThisFrame)
                               || (pad != null && pad.buttonWest.wasReleasedThisFrame);

            if (attackPressed)
            {
                _isCharging = true;
                _chargeHoldTime = 0f;
            }

            if (_isCharging && attackHeld)
            {
                _chargeHoldTime += Time.deltaTime;
                float chargeNorm = Mathf.Clamp01(_chargeHoldTime / Mathf.Max(0.1f, fullChargeSeconds));
                CurrentLiveLevel = Mathf.Max(CurrentLiveLevel * 0.85f, Mathf.Lerp(0.28f, 1.0f, chargeNorm));

                // Auto-fire if held past full charge + 0.15s so big booms feel snappy
                if (_chargeHoldTime >= fullChargeSeconds + 0.15f)
                {
                    _isCharging = false;
                    _chargeHoldTime = 0f;
                    TriggerPulse(1.0f);
                }
            }
            else if (_isCharging && attackReleased)
            {
                _isCharging = false;
                float intensity = _chargeHoldTime < 0.15f
                    ? 0.36f
                    : Mathf.Lerp(0.36f, 1.0f, Mathf.Clamp01(_chargeHoldTime / Mathf.Max(0.1f, fullChargeSeconds)));
                _chargeHoldTime = 0f;
                TriggerPulse(intensity);
            }
            else if (sourceMode == SensorSourceMode.KeyboardAndMouseOnly)
            {
                CurrentLiveLevel = Mathf.MoveTowards(CurrentLiveLevel, 0f, Time.deltaTime * 4.5f);
            }
        }

        private void TriggerPulse(float rawIntensity)
        {
            if (_cooldownTimer > 0f) return;
            float clamped = Mathf.Clamp01(rawIntensity);
            if (clamped < softMinThreshold) return;

            bool isBoom = clamped >= hardBoomThreshold;
            _cooldownTimer = isBoom ? pulseCooldown * 1.45f : pulseCooldown;

            LastPulseIntensity = clamped;
            LastPulseWasBoom = isBoom;
            LastPulseTime = Time.time;
            CurrentLiveLevel = clamped;

            OnAttackPulse?.Invoke(clamped, isBoom);
        }

        // ---------------- PC Microphone Fallback (Tap Desk or Blow Into Mic!) ----------------
        private void StartMic()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0) return;
            _micDevice = Microphone.devices[0];
            _micClip = Microphone.Start(_micDevice, true, 1, 22050);
            _micEnvelope = 0f;
            _micPeakHold = 0f;
            _micRising = false;
        }

        private void StopMic()
        {
            if (!string.IsNullOrEmpty(_micDevice) && Microphone.IsRecording(_micDevice))
            {
                Microphone.End(_micDevice);
            }
            _micClip = null;
        }

        private void UpdateMicrophone()
        {
            if (_micClip == null || string.IsNullOrEmpty(_micDevice)) return;
            int pos = Microphone.GetPosition(_micDevice) - _micSamples.Length;
            if (pos < 0) return;

            _micClip.GetData(_micSamples, pos);
            float peak = 0f;
            for (int i = 0; i < _micSamples.Length; i++)
            {
                float a = Mathf.Abs(_micSamples[i]);
                if (a > peak) peak = a;
            }

            float level = Mathf.Clamp01(peak * micSensitivity);
            _micEnvelope = Mathf.Lerp(_micEnvelope, level, level > _micEnvelope ? 0.65f : 0.22f);
            if (!_isCharging) CurrentLiveLevel = _micEnvelope;

            // Peak-hold envelope detector so a single punch/puff fires ONE clean pulse at its true peak!
            if (_micEnvelope > softMinThreshold)
            {
                if (_micEnvelope >= _micPeakHold)
                {
                    _micPeakHold = _micEnvelope;
                    _micRising = true;
                }
                else if (_micRising && _micEnvelope < _micPeakHold * 0.72f)
                {
                    _micRising = false;
                    TriggerPulse(_micPeakHold);
                    _micPeakHold = 0f;
                }
            }
            else
            {
                if (_micRising && _micPeakHold >= softMinThreshold)
                {
                    TriggerPulse(_micPeakHold);
                }
                _micRising = false;
                _micPeakHold = 0f;
            }
        }

        // ---------------- ESP32 Serial (Piezo + BMP280 Breath via Win32 COM) ----------------
        private void UpdateSerialQueue()
        {
            if (!_isCharging)
            {
                CurrentLiveLevel = Mathf.Max(CurrentLiveLevel * 0.85f, _serialLiveLevel);
            }
            while (_serialPulseQueue.TryDequeue(out float p))
            {
                TriggerPulse(p);
            }
        }

        private void StartSerial()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            StopSerial();
            string path = "\\\\.\\" + serialPortName.Trim();
            _serialHandle = CreateFileW(path, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (_serialHandle == IntPtr.Zero || _serialHandle == new IntPtr(-1))
            {
                _serialRunning = false;
                return;
            }

            DCB dcb = new DCB();
            dcb.DCBlength = Marshal.SizeOf(typeof(DCB));
            if (GetCommState(_serialHandle, ref dcb))
            {
                dcb.BaudRate = (uint)baudRate;
                dcb.ByteSize = 8;
                dcb.Parity = 0;
                dcb.StopBits = 0; // ONESTOPBIT
                SetCommState(_serialHandle, ref dcb);
            }

            COMMTIMEOUTS timeouts = new COMMTIMEOUTS
            {
                ReadIntervalTimeout = 10,
                ReadTotalTimeoutMultiplier = 0,
                ReadTotalTimeoutConstant = 25
            };
            SetCommTimeouts(_serialHandle, ref timeouts);

            _serialRunning = true;
            _serialThread = new Thread(SerialReadLoop) { IsBackground = true, Name = "OrpheusESP32Serial" };
            _serialThread.Start();
#endif
        }

        private void StopSerial()
        {
            _serialRunning = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_serialHandle != IntPtr.Zero && _serialHandle != new IntPtr(-1))
            {
                CloseHandle(_serialHandle);
                _serialHandle = new IntPtr(-1);
            }
#endif
            _serialThread = null;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void SerialReadLoop()
        {
            byte[] buf = new byte[128];
            StringBuilder lineBuf = new StringBuilder(128);
            float breathPeak = 0f;
            bool breathActive = false;

            while (_serialRunning)
            {
                if (!ReadFile(_serialHandle, buf, (uint)buf.Length, out uint bytesRead, IntPtr.Zero) || bytesRead == 0)
                {
                    Thread.Sleep(4);
                    continue;
                }

                for (uint i = 0; i < bytesRead; i++)
                {
                    char c = (char)buf[i];
                    if (c == '\n' || c == '\r')
                    {
                        if (lineBuf.Length > 0)
                        {
                            ParseSerialLine(lineBuf.ToString().Trim(), ref breathPeak, ref breathActive);
                            lineBuf.Clear();
                        }
                    }
                    else if (lineBuf.Length < 120)
                    {
                        lineBuf.Append(c);
                    }
                }
            }
        }

        private void ParseSerialLine(string line, ref float breathPeak, ref bool breathActive)
        {
            if (string.IsNullOrEmpty(line) || line[0] == '#') return;

            // Format 1: Explicit pulse from Piezo/Breath sketch -> "P,0.84" or "S,0.35"
            if (line.StartsWith("P,", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(line.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                {
                    val = Mathf.Clamp01(val);
                    _serialLiveLevel = val;
                    _serialPulseQueue.Enqueue(val);
                }
                return;
            }
            if (line.StartsWith("S,", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(line.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                {
                    _serialLiveLevel = Mathf.Clamp01(val);
                }
                return;
            }

            // Format 2: Existing breath_controller_spi.ino format -> "dpPa,norm" (e.g. "42.10,0.785")
            int comma = line.IndexOf(',');
            if (comma > 0 && float.TryParse(line.Substring(comma + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float norm))
            {
                float exhale = Mathf.Clamp01(norm);
                _serialLiveLevel = exhale;
                if (exhale > softMinThreshold)
                {
                    breathActive = true;
                    if (exhale > breathPeak) breathPeak = exhale;
                }
                else if (breathActive)
                {
                    breathActive = false;
                    _serialPulseQueue.Enqueue(breathPeak);
                    breathPeak = 0f;
                }
            }
        }

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct DCB
        {
            public int DCBlength;
            public uint BaudRate;
            public uint Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public sbyte XonChar;
            public sbyte XoffChar;
            public sbyte ErrorChar;
            public sbyte EofChar;
            public sbyte EvtChar;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMMTIMEOUTS
        {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
#endif
    }
}
