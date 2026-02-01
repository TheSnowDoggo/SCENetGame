using System.Collections.Frozen;
using System.ComponentModel;
using System.Text;

namespace SCENetGame;

internal sealed class TextEntry
{
    private static readonly FrozenDictionary<ConsoleKey, EntryKey> DefaultKeyMappings = new Dictionary<ConsoleKey, EntryKey>()
    {
        { ConsoleKey.Enter     , EntryKey.Enter   },
        { ConsoleKey.Escape    , EntryKey.Exit    },
        { ConsoleKey.Backspace , EntryKey.Delete  },
        { ConsoleKey.LeftArrow , EntryKey.Back    },
        { ConsoleKey.RightArrow, EntryKey.Forward },
    }.ToFrozenDictionary();

    private StringBuilder _sb = new();
    private int _index;

    public event Action TextChanged;
    public event Action IndexChanged;

    public event Action Enter;
    public event Action Exit;

    public int Length => _sb.Length;

    public int Index
    {
        get
        {
            return _index;
        }
        set
        {
            if (value == _index)
            {
                return;
            }

            if (value < 0 || value > _sb.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value was out of range of current input.");
            }

            _index = value;

            IndexChanged?.Invoke();
        }
    }

    public string Text
    {
        get => _sb.ToString();
        set => _sb = new StringBuilder(value);
    }

    public int MaxCharacters { get; set; }

    public IReadOnlyDictionary<ConsoleKey, EntryKey> KeyMappings { get; set; } = DefaultKeyMappings;

    public void Next(ConsoleKeyInfo cki)
    {
        if (!KeyMappings.TryGetValue(cki.Key, out EntryKey key))
        {
            NextCharacter(cki.KeyChar);
            return;
        }

        switch (key)
        {
        case EntryKey.Enter:
            Enter.Invoke();
            break;
        case EntryKey.Exit:
            Exit.Invoke();
            break;
        case EntryKey.Delete:
            Delete();
            break;
        case EntryKey.Back:
            Back();
            break;
        case EntryKey.Forward:
            Forward();
            break;
        default:
            throw new InvalidEnumArgumentException(nameof(key), (int)key, typeof(EntryKey));
        }
    }

    public void Clear()
    {
        Index = 0;
        
        if (_sb.Length == 0)
        {
            return;
        }

        _sb.Clear();
        TextChanged?.Invoke();
    }

    private void NextCharacter(char chr)
    {
        if (chr < ' ')
        {
            return;
        }

        if (_sb.Length >= MaxCharacters)
        {
            return;
        }

        if (Index >= _sb.Length)
        {
            _sb.Append(chr);
        }
        else
        {
            _sb.Insert(Index, chr);
        }

        Index++;

        TextChanged?.Invoke();
    }

    private void Delete()
    {
        if (Index <= 0)
        {
            return;
        }

        _sb.Remove(Index - 1, 1);
        Index--;

        TextChanged?.Invoke();
    }

    private void Back()
    {
        if (Index <= 0)
        {
            return;
        }

        Index--;
    }

    private void Forward()
    {
        if (Index >= _sb.Length)
        {
            return;
        }

        Index++;
    }
}