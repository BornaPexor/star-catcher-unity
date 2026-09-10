using System.Globalization;
using System.Threading;
using UnityEditor;
using UnityEngine;

// Unity's PrefColor serializer formats using CurrentCulture, but reads using
// InvariantCulture. Mono's fa-IR decimal separator can be '/', which the reader
// does not accept. Normalize only numeric formatting inside this Editor process.
// This does not change Windows settings or the user's language.
[InitializeOnLoad]
public static class EditorNumberFormat
{
    static EditorNumberFormat() { Apply(); }

    public static void Apply()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        if (original.NumberFormat.NumberDecimalSeparator == ".") return;
        var culture = (CultureInfo)original.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ".";
        culture.NumberFormat.NumberGroupSeparator = ",";
        culture.NumberFormat.NegativeSign = "-";
        Thread.CurrentThread.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        Debug.Log("STAR_CATCHER_EDITOR_NUMBER_FORMAT: normalized decimal separator from " + original.NumberFormat.NumberDecimalSeparator);
    }
}
