// See https://aka.ms/new-console-template for more information

using MusicXmlBeautifier;

string file = @"C:\Users\Dustin\Downloads\Mononoke.musicxml";

Console.WriteLine("***MusicXmlBeautifier***\n\n" +
    "This programm will accordion add bass lyrics to the given musicxml file: " + file);

Beautifier.CreateMusicXmlForAccordion(file,
    clampBassNotesToAccordionRange: true,
    removeNotesOutOfRange: true,
    removeStaccato: true,
    voicesToExclude: new List<int>() { },
    reduceMainHandChordsToOneTone: true,
    removeLineBreaks: true,
    lyricSimplification: LyricsSimplification.All,
    lyricsReplacements: new LyricsReplacement[]
    {
        new("CDur", new string[] { "C", "E", "G" }, true),
        new("Ddur", new string[] { "D", "F#", "A" }, true),
        new("Ddur", new string[] { "D", "F#", "G", "A" }, true),
        new("Emol", new string[] { "E", "G", "H" }, true),
        new("Gdur", new string[] { "G", "H", "D" }, true),
        new("Amol", new string[] { "A", "C", "E" }, true),
        new("ACD", new string[] { "A", "C", "D" }, true),
        new("HCD", new string[] { "H", "C", "D" }, true),
    });
Console.Read();


/*
 * Gebundene lange Bassnoten über mehrere Takte brauchen Lyrics nur am Anfang
 * Option, automatisch Dul und Mol-Akkorde spielen. F und A wird zu Fdur zB?
 */