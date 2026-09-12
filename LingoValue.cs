using System.Collections.Generic;

namespace PferdehofGUI;

public abstract class LingoValue { }

public class LingoString : LingoValue { public string Value = ""; }
public class LingoSymbol : LingoValue { public string Name = ""; }
public class LingoNumber : LingoValue { public double Value; public bool IsInteger; }
public class LingoColor : LingoValue { public int R, G, B; }

/// <summary>Bare Lingo keywords with no leading '#', e.g. EMPTY, VOID - and also Director's own
/// angle-bracket serialization of VOID, which appears as literal "&lt;Void&gt;" in saved files
/// even though "VOID" is the keyword's spelling in Lingo source code. IsAngleBracketed tracks
/// which form was read, so re-serializing reproduces the exact original text.</summary>
public class LingoKeyword : LingoValue
{
    public string Text = "";
    public bool IsAngleBracketed;
}

/// <summary>A function-call-style value, e.g. date(19860412), float(65).
/// Anything of the form ident(args...) that isn't rgb(...) (which becomes LingoColor).</summary>
public class LingoCall : LingoValue
{
    public string FunctionName = "";
    public List<LingoValue> Args = new();
}

/// <summary>A plain comma-separated list with no #key: pairs, e.g. [0, 0, 0, 0, 0].</summary>
public class LingoLinearList : LingoValue
{
    public List<LingoValue> Items = new();
}

public class LingoEntry
{
    public string Key = "";
    public LingoValue Value = null!;
}

public class LingoPropertyList : LingoValue
{
    public List<LingoEntry> Entries = new();
}
