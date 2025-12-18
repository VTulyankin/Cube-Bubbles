using System.Media;

namespace CubeBubbles.Sounds;

/// <summary>
/// Управляет звуковыми эффектами игры
/// </summary>
public sealed class GameSound : IDisposable
{
    private readonly Dictionary<SoundType, byte[]> _soundData = new();
    private readonly List<ActiveSound> _activeSounds = [];
    private const float Volume = 0.1f;
    private bool _disposed;

    /// <summary>
    /// Загружает все звуковые файлы
    /// </summary>
    public void LoadSounds()
    {
        TryLoadSound(SoundType.GameOver, "Resources/sounds/game_over.wav");
        TryLoadSound(SoundType.Add, "Resources/sounds/add.wav");
        TryLoadSound(SoundType.Hit, "Resources/sounds/hit.wav");
    }

    /// <summary>
    /// Загружает один звук из файла
    /// </summary>
    /// <param name="type">Тип звука</param>
    /// <param name="path">Путь к файлу</param>
    private void TryLoadSound(SoundType type, string path)
    {
        try
        {
            if (File.Exists(path))
            {
                _soundData[type] = File.ReadAllBytes(path);
            }
        }
        catch
        {
            // Игнорируем ошибки загрузки звуков
        }
    }

    /// <summary>
    /// Проигрывает звук
    /// </summary>
    /// <param name="type">Тип звука</param>
    public void Play(SoundType type)
    {
        if (!_soundData.TryGetValue(type, out var data))
            return;

        try
        {
            byte[] adjustedData;
            
            if (Math.Abs(Volume - 1.0f) < 0.01f)
            {
                adjustedData = data;
            }
            else
            {
                adjustedData = AdjustVolume(data, Volume);
            }

            var ms = new MemoryStream(adjustedData);
            var player = new SoundPlayer(ms);
            
            var activeSound = new ActiveSound
            {
                Player = player,
                Stream = ms,
                StartTime = DateTime.Now
            };
            
            lock (_activeSounds)
            {
                _activeSounds.Add(activeSound);
            }

            player.Load();
            player.Play();

            CleanupOldSounds();
        }
        catch
        {
            // Игнорируем ошибки воспроизведения
        }
    }

    /// <summary>
    /// Очищает завершившиеся звуки
    /// </summary>
    private void CleanupOldSounds()
    {
        Task.Run(() =>
        {
            Thread.Sleep(100);
            
            lock (_activeSounds)
            {
                var now = DateTime.Now;
                var toRemove = _activeSounds
                    .Where(s => (now - s.StartTime).TotalSeconds > 5)
                    .ToList();

                foreach (var sound in toRemove)
                {
                    try
                    {
                        sound.Player?.Dispose();
                        sound.Stream?.Dispose();
                    }
                    catch
                    {
                        // Игнорируем ошибки
                    }
                }

                _activeSounds.RemoveAll(s => toRemove.Contains(s));
            }
        });
    }

    /// <summary>
    /// Применяет громкость к WAV-данным
    /// </summary>
    /// <param name="wavData">Исходные данные WAV</param>
    /// <param name="volumeLevel">Уровень громкости</param>
    /// <returns>Модифицированные данные</returns>
    private byte[] AdjustVolume(byte[] wavData, float volumeLevel)
    {
        var result = (byte[])wavData.Clone();

        int dataStart = FindDataChunk(wavData);
        if (dataStart == -1 || dataStart >= wavData.Length)
            return result;

        for (int i = dataStart; i < wavData.Length - 1; i += 2)
        {
            short sample = BitConverter.ToInt16(wavData, i);
            int adjusted = (int)(sample * volumeLevel);
            adjusted = Math.Clamp(adjusted, short.MinValue, short.MaxValue);
            
            var bytes = BitConverter.GetBytes((short)adjusted);
            result[i] = bytes[0];
            result[i + 1] = bytes[1];
        }

        return result;
    }

    /// <summary>
    /// Находит начало аудио-данных в WAV файле
    /// </summary>
    /// <param name="wavData">WAV данные</param>
    /// <returns>Позиция начала данных или -1</returns>
    private int FindDataChunk(byte[] wavData)
    {
        for (int i = 0; i < wavData.Length - 4; i++)
        {
            if (wavData[i] == 'd' && wavData[i + 1] == 'a' && 
                wavData[i + 2] == 't' && wavData[i + 3] == 'a')
            {
                return i + 8;
            }
        }
        return 44;
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_activeSounds)
        {
            foreach (var sound in _activeSounds)
            {
                try
                {
                    sound.Player?.Dispose();
                    sound.Stream?.Dispose();
                }
                catch
                {
                    // Игнорируем ошибки
                }
            }
            _activeSounds.Clear();
        }

        _soundData.Clear();
        _disposed = true;
    }

    private class ActiveSound
    {
        public SoundPlayer? Player { get; set; }
        public MemoryStream? Stream { get; set; }
        public DateTime StartTime { get; set; }
    }
}

/// <summary>
/// Типы звуков в игре
/// </summary>
public enum SoundType
{
    GameOver,
    Add,
    Hit
}
