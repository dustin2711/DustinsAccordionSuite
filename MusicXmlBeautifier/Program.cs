// See https://aka.ms/new-console-template for more information

using MusicXmlBeautifier;

string file = @"G:\My Drive\MusicXmls\BigInJapan.musicxml";

Console.WriteLine("***MusicXmlBeautifier***\n\n" +
    "This programm will add bass lyrics to the given musicxml file: " + file);

Beautifier.CreateMusicXmlForAccordion(file,
    clampBassNotesToAccordionRange: false,
    removeNotesOutOfRange: true,
    removeStaccato: true,
    voicesToExclude: new List<int>() { },
    reduceMainHandChordsToOneTone: true,
    removeLineBreaks: true,
    lyricSimplification: LyricsSimplification.All,
    lyricsReplacements: new LyricsReplacement[]
    {
        //new LyricsReplacement("Fdur", "F", "A", "F", "A"),
        //new LyricsReplacement("Bdur", "B", "F", "B", "F"),
        //new LyricsReplacement("Bdur", "B", "BF", "B", "BF"),
        //new LyricsReplacement("Bdur", "B", "FB", "B", "FB"),
        //new LyricsReplacement("Cdur", "C", "GC", "C", "GC"),
        //new LyricsReplacement("Dmol", "D", "AD", "D", "AD", "D"),
        //new LyricsReplacement("Dmol", "D", "AD", "D", "AD"),
    });
Console.Read();


/*
 * Gebundene lange Bassnoten über mehrere Takte brauchen Lyrics nur am Anfang
 * Option, automatisch Dul und Mol-Akkorde spielen. F und A wird zu Fdur zB?
 */