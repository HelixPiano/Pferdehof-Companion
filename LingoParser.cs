using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PferdehofGUI;

public class LingoParser
{
    private readonly string _s;
    private int _pos;

    private LingoParser(string s) { _s = s; _pos = 0; }

    public static LingoValue Parse(string text)
    {
        var p = new LingoParser(text);
        p.SkipWhitespace();
        return p.ParseValue();
    }

    private void SkipWhitespace()
    {
        while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++;
    }

    private char Peek() => _pos < _s.Length ? _s[_pos] : '\0';

    private LingoValue ParseValue()
    {
        SkipWhitespace();
        char c = Peek();
        if (c == '[') return ParseBracketedValue();
        if (c == '"') return ParseString();
        if (c == '#') return ParseSymbol();
        if (c == '<') return ParseAngleBracketKeyword();
        if (char.IsLetter(c)) return ParseKeywordOrCall();
        if (c == '-' || char.IsDigit(c)) return ParseNumber();
        throw new FormatException($"Unexpected char '{c}' at pos {_pos}");
    }

    /// <summary>Handles Director's own serialized form of VOID (and possibly other keywords),
    /// which appears in saved files as e.g. "&lt;Void&gt;" rather than the bare "VOID" that
    /// shows up in Lingo source code.</summary>
    private LingoValue ParseAngleBracketKeyword()
    {
        int start = _pos;
        _pos++; // skip '<'
        while (_pos < _s.Length && _s[_pos] != '>') _pos++;
        string inner = _s.Substring(start + 1, _pos - start - 1);
        if (_pos < _s.Length) _pos++; // skip '>'
        return new LingoKeyword { Text = inner, IsAngleBracketed = true };
    }

    /// <summary>Handles everything that starts with '[': empty list "[]", explicit empty
    /// property list "[:]", a property list "[#a: 1, ...]", or a plain linear list "[1, 2, 3]".</summary>
    private LingoValue ParseBracketedValue()
    {
        Expect('[');
        SkipWhitespace();

        if (Peek() == ']') { _pos++; return new LingoPropertyList(); }

        if (Peek() == ':')
        {
            _pos++;
            SkipWhitespace();
            Expect(']');
            return new LingoPropertyList();
        }

        if (Peek() == '#')
            return ParsePropertyListBody();

        return ParseLinearListBody();
    }

    private LingoPropertyList ParsePropertyListBody()
    {
        var list = new LingoPropertyList();
        while (true)
        {
            SkipWhitespace();
            if (Peek() != '#') throw new FormatException($"Expected '#' key at pos {_pos}");
            _pos++;
            string key = ParseIdent();
            SkipWhitespace();
            Expect(':');
            SkipWhitespace();
            var value = ParseValue();
            list.Entries.Add(new LingoEntry { Key = key, Value = value });
            SkipWhitespace();
            if (Peek() == ',') { _pos++; continue; }
            if (Peek() == ']') { _pos++; break; }
            throw new FormatException($"Expected ',' or ']' at pos {_pos}");
        }
        return list;
    }

    private LingoLinearList ParseLinearListBody()
    {
        var list = new LingoLinearList();
        while (true)
        {
            SkipWhitespace();
            list.Items.Add(ParseValue());
            SkipWhitespace();
            if (Peek() == ',') { _pos++; continue; }
            if (Peek() == ']') { _pos++; break; }
            throw new FormatException($"Expected ',' or ']' at pos {_pos}");
        }
        return list;
    }

    private string ParseIdent()
    {
        int start = _pos;
        while (_pos < _s.Length && (char.IsLetterOrDigit(_s[_pos]) || _s[_pos] == '_')) _pos++;
        return _s.Substring(start, _pos - start);
    }

    private LingoValue ParseSymbol()
    {
        _pos++; // skip '#'
        return new LingoSymbol { Name = ParseIdent() };
    }

    private LingoValue ParseString()
    {
        _pos++; // skip opening quote
        int start = _pos;
        while (_pos < _s.Length && _s[_pos] != '"') _pos++;
        string value = _s.Substring(start, _pos - start);
        _pos++; // skip closing quote
        return new LingoString { Value = value };
    }

    private LingoValue ParseNumber()
    {
        int start = _pos;
        if (Peek() == '-') _pos++;
        while (_pos < _s.Length && char.IsDigit(_s[_pos])) _pos++;
        bool isInt = true;
        if (Peek() == '.')
        {
            isInt = false;
            _pos++;
            while (_pos < _s.Length && char.IsDigit(_s[_pos])) _pos++;
        }
        string numStr = _s.Substring(start, _pos - start);
        return new LingoNumber { Value = double.Parse(numStr, CultureInfo.InvariantCulture), IsInteger = isInt };
    }

    /// <summary>Handles bare identifiers (EMPTY, VOID, ...) and function-call forms
    /// like rgb(...), date(...), float(...).</summary>
    private LingoValue ParseKeywordOrCall()
    {
        string ident = ParseIdent();
        SkipWhitespace();

        if (Peek() != '(')
            return new LingoKeyword { Text = ident };

        _pos++; // consume '('
        var args = new List<LingoValue>();
        SkipWhitespace();
        if (Peek() != ')')
        {
            while (true)
            {
                args.Add(ParseValue());
                SkipWhitespace();
                if (Peek() == ',') { _pos++; SkipWhitespace(); continue; }
                break;
            }
        }
        SkipWhitespace();
        Expect(')');

        if (ident == "rgb" && args.Count == 3 && args.All(a => a is LingoNumber))
        {
            var nums = args.Cast<LingoNumber>().ToList();
            return new LingoColor { R = (int)nums[0].Value, G = (int)nums[1].Value, B = (int)nums[2].Value };
        }

        return new LingoCall { FunctionName = ident, Args = args };
    }

    private void Expect(char c)
    {
        SkipWhitespace();
        if (Peek() != c) throw new FormatException($"Expected '{c}' at pos {_pos}, found '{Peek()}'");
        _pos++;
    }
}
