namespace TruthWeaver.Parsing;

/// <summary>
/// Follows a JSON or YAML token stream and remembers where the reader is, so that when the text turns out to be
/// malformed the diagnostic can name the nearest valid ancestor: the innermost container that was still open when the
/// parser gave up. The front ends feed it the events their reader raises; it knows nothing about either syntax.
/// </summary>
internal sealed class TreePathTracker
{
    private readonly List<Frame> frames = [];

    /// <summary>Gets the path of the innermost container that is still open, or the root when none is.</summary>
    public string ContainerPath
    {
        get
        {
            string path = TreePath.Root;
            for (int i = 0; i < this.frames.Count - 1; i++)
            {
                path = this.frames[i].Select(path);
            }

            return path;
        }
    }

    /// <summary>Records a scalar value (JSON string, number, true, false, null).</summary>
    public void Scalar()
    {
        this.BeginValue();
    }

    /// <summary>Records a JSON property name: the next value belongs to this key.</summary>
    /// <param name="name">The key.</param>
    public void Key(string name)
    {
        if (this.frames.Count > 0)
        {
            this.frames[^1].Name = name;
        }
    }

    /// <summary>Records the start of an object or array.</summary>
    /// <param name="isArray"><see langword="true"/> for an array or sequence, <see langword="false"/> for an object or mapping.</param>
    public void Enter(bool isArray)
    {
        this.BeginValue();
        this.frames.Add(new Frame { IsArray = isArray });
    }

    /// <summary>Records the end of an object or array.</summary>
    public void Exit()
    {
        if (this.frames.Count > 0)
        {
            this.frames.RemoveAt(this.frames.Count - 1);
        }
    }

    /// <summary>
    /// Records a YAML scalar. In a mapping the scalars alternate key, value, key, value, so the tracker toggles which it is
    /// expecting; in a sequence it is the next item.
    /// </summary>
    /// <param name="text">The scalar's text, used as the key when a key is expected.</param>
    public void YamlScalar(string text)
    {
        this.BeginValue();
        if (this.frames.Count > 0 && !this.frames[^1].IsArray)
        {
            Frame mapping = this.frames[^1];
            if (mapping.ExpectKey)
            {
                mapping.Name = text;
            }

            mapping.ExpectKey = !mapping.ExpectKey;
        }
    }

    /// <summary>Records the end of a YAML mapping or sequence, which completes a key or value of the mapping around it.</summary>
    public void YamlExit()
    {
        this.Exit();
        if (this.frames.Count > 0 && !this.frames[^1].IsArray)
        {
            this.frames[^1].ExpectKey = !this.frames[^1].ExpectKey;
        }
    }

    // A value starting inside an array is the array's next item.
    private void BeginValue()
    {
        if (this.frames.Count > 0 && this.frames[^1].IsArray)
        {
            this.frames[^1].ItemIndex++;
        }
    }

    private sealed class Frame
    {
        public bool IsArray { get; init; }

        public string? Name { get; set; }

        public int ItemIndex { get; set; } = -1;

        public bool ExpectKey { get; set; } = true;

        // The path of the child this frame is currently on.
        public string Select(string parent)
        {
            if (this.IsArray)
            {
                return this.ItemIndex >= 0 ? TreePath.Index(parent, this.ItemIndex) : parent;
            }

            return this.Name is null ? parent : TreePath.Property(parent, this.Name);
        }
    }
}
